using System.Globalization;
using IranSms.Providers.SmsIr.Json;

namespace IranSms.Providers.SmsIr
{
    /// <summary>
    /// SMS.ir SMS provider client (REST API v1).
    /// Supports single/bulk send (max 100 mobiles), OTP (verify) and delivery status lookup.
    /// Implements <see cref="IDisposable"/> to release the internal HttpClient when caller did not supply one.
    /// </summary>
    public sealed class SmsIrClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter, ISmsAccountInfo, IDisposable
    {
        private const int MaxBulkRecipients = 100;

        // Official SMS.ir REST endpoint paths (relative to the /v1 base URL).
        private const string SendBulkPath = "send/bulk";
        private const string SendVerifyPath = "send/verify";
        private const string SendStatusPrefix = "send/";
        private const string CreditPath = "credit";
        private const string LinePath = "line";

        private readonly ISmsIrTransport _transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmsIrClient"/> class.
        /// </summary>
        /// <param name="apiKey">The SMS.ir X-API-KEY (private panel key).</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="apiKey"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="apiKey"/> is empty.</exception>
        public SmsIrClient(string apiKey, HttpClient? httpClient = null)
            : this(new SmsIrHttpTransport(apiKey, httpClient), apiKey)
        {
        }

        internal SmsIrClient(ISmsIrTransport transport, string apiKey)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (apiKey is null)
                throw new ArgumentNullException(nameof(apiKey));
            if (apiKey.Length == 0)
                throw new ArgumentException("API key cannot be empty.", nameof(apiKey));
        }

        /// <inheritdoc />
        public string ProviderName => "SmsIr";

        /// <inheritdoc />
        public SmsCapabilities Capabilities =>
            SmsCapabilities.Send
            | SmsCapabilities.BulkSend
            | SmsCapabilities.OtpSend
            | SmsCapabilities.DeliveryStatus
            | SmsCapabilities.AccountInfo
            | SmsCapabilities.LineManagement;

        /// <inheritdoc />
        public async Task<SmsSendResult> SendAsync(
            string recipient,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            if (senderLine is null)
                throw new ArgumentException("SMS.ir requires a sender line (lineNumber) for send.", nameof(senderLine));

            var request = new SmsIrBulkSendRequest
            {
                LineNumber = ParseLineNumber(senderLine),
                MessageText = message,
                Mobiles = new[] { recipient },
            };

            var data = await PostCoreAsync(SendBulkPath, request, cancellationToken).ConfigureAwait(false);
            return BuildBulkResult(data);
        }

        /// <inheritdoc />
        public async Task<SmsSendResult> SendBulkAsync(
            IEnumerable<string> recipients,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            if (recipients is null)
                throw new ArgumentNullException(nameof(recipients));
            if (senderLine is null)
                throw new ArgumentException("SMS.ir requires a sender line (lineNumber) for send.", nameof(senderLine));

            var list = recipients as IReadOnlyList<string> ?? recipients.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            if (list.Count > MaxBulkRecipients)
                throw new ArgumentException($"SMS.ir accepts at most {MaxBulkRecipients} recipients per call.", nameof(recipients));

            var request = new SmsIrBulkSendRequest
            {
                LineNumber = ParseLineNumber(senderLine),
                MessageText = message,
                Mobiles = list.ToArray(),
            };

            var data = await PostCoreAsync(SendBulkPath, request, cancellationToken).ConfigureAwait(false);
            return BuildBulkResult(data);
        }

        /// <inheritdoc />
        public async Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (request.SendDate.HasValue)
                throw new NotSupportedException($"{ProviderName} does not honour OtpRequest.SendDate — schedule delivery in your application instead.");

            var templateIdText = request.TemplateId;
            if (string.IsNullOrWhiteSpace(templateIdText))
                throw new ArgumentException("SMS.ir OTP requires a template id (TemplateId).", nameof(request));

            if (!long.TryParse(templateIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var templateId))
                throw new ArgumentException("SMS.ir template id must be an integer.", nameof(request));

            var parameters = new List<SmsIrVerifyParameter>();
            if (request.Parameters is { Count: > 0 })
            {
                foreach (var pair in request.Parameters)
                {
                    parameters.Add(new SmsIrVerifyParameter
                    {
                        Name = pair.Key,
                        Value = pair.Value,
                    });
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                    throw new ArgumentException("SMS.ir OTP requires a Code or Parameters.", nameof(request));

                parameters.Add(new SmsIrVerifyParameter
                {
                    Name = "Code",
                    Value = request.Code!,
                });
            }

            var payload = new SmsIrVerifyRequest
            {
                Mobile = recipient,
                TemplateId = templateId,
                Parameters = parameters.ToArray(),
            };

            var data = await PostCoreAsync(SendVerifyPath, payload, cancellationToken).ConfigureAwait(false);
            return new OtpSendResult(data.MessageId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
            {
                Cost = data.Cost,
            };
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Type != MessageIdentifierType.ProviderMessageId)
                throw new ArgumentException("SMS.ir delivery status supports provider message ids only.", nameof(message));

            if (!long.TryParse(message.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var messageId))
                throw new ArgumentException("SMS.ir message id must be an integer.", nameof(message));

            var data = await GetCoreAsync($"{SendStatusPrefix}{messageId}", cancellationToken).ConfigureAwait(false);
            var status = SmsIrStatusMapper.ToDeliveryState(data.DeliveryState);

            return new MessageStatusResult(status, message)
            {
                RawStatus = data.DeliveryState?.ToString(CultureInfo.InvariantCulture),
                Recipient = data.Mobile?.ToString(CultureInfo.InvariantCulture),
                Price = data.Cost,
                SendDate = data.SendDateTime is long sendAt ? DateTimeOffset.FromUnixTimeSeconds(sendAt) : null,
                MessageText = data.MessageText,
            };
        }

        /// <inheritdoc />
        public async Task<AccountBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default)
        {
            // GET /v1/credit — data is a bare decimal (e.g. { status:1, data: 165.3 })
            var body = await _transport.GetAsync(CreditPath, cancellationToken).ConfigureAwait(false);
            var raw = SmsIrJson.DeserializeRaw(body);
            if (raw is null || raw.Status != 1)
                throw new IranSmsException(raw is null ? "SMS.ir returned an unparseable envelope." : $"SMS.ir API error ({raw.Status}): {raw.Message}")
                {
                    ProviderName = ProviderName,
                    ProviderStatusCode = raw?.Status,
                    RawResponseBody = body,
                };
            var creditVal = raw.DataElement.HasValue ? SmsIrJson.ExtractDecimal(raw.DataElement.Value) : 0m;
            return new AccountBalanceResult(creditVal);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetSenderLinesAsync(CancellationToken cancellationToken = default)
        {
            // GET /v1/line — data is Array<Long>
            var body = await _transport.GetAsync(LinePath, cancellationToken).ConfigureAwait(false);
            var envelope = SmsIrJson.Deserialize<long[]>(body);
            if (envelope is null || envelope.Status != 1)
            {
                var raw = SmsIrJson.DeserializeRaw(body);
                if (raw is null || raw.Status != 1)
                    throw new IranSmsException(raw is null ? "SMS.ir returned an unparseable envelope." : $"SMS.ir API error ({raw.Status}): {raw.Message}")
                    {
                        ProviderName = ProviderName,
                        ProviderStatusCode = raw?.Status,
                        RawResponseBody = body,
                    };
                if (raw.DataElement is null || raw.DataElement.Value.ValueKind != System.Text.Json.JsonValueKind.Array)
                    return Array.Empty<string>();
                var list = new List<string>();
                foreach (var el in raw.DataElement.Value.EnumerateArray())
                {
                    if (el.ValueKind == System.Text.Json.JsonValueKind.Number && el.TryGetInt64(out var n))
                        list.Add(n.ToString(CultureInfo.InvariantCulture));
                    else if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                        list.Add(el.GetString() ?? string.Empty);
                }

                return list;
            }

            var data = envelope.Data;
            if (data is null || data.Length == 0)
                return Array.Empty<string>();
            var result = new string[data.Length];
            for (var i = 0; i < data.Length; i++)
                result[i] = data[i].ToString(CultureInfo.InvariantCulture);
            return result;
        }

        private static SmsSendResult BuildBulkResult(SmsIrBulkSendResult data)
        {
            var messageId = data.MessageIds is { Length: > 0 }
                ? data.MessageIds[0].ToString(CultureInfo.InvariantCulture)
                : data.PackId?.ToString() ?? string.Empty;

            var recipientIds = data.MessageIds is null ? null : new string[data.MessageIds.Length];
            if (data.MessageIds != null)
            {
                for (var i = 0; i < data.MessageIds.Length; i++)
                    recipientIds![i] = data.MessageIds[i].ToString(CultureInfo.InvariantCulture);
            }

            return new SmsSendResult(messageId)
            {
                Cost = data.Cost,
                RecipientIds = recipientIds,
            };
        }

        private static long ParseLineNumber(string lineNumber)
        {
            if (!long.TryParse(lineNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                throw new ArgumentException("SMS.ir lineNumber must be a number.", nameof(lineNumber));
            return parsed;
        }

        private async Task<SmsIrBulkSendResult> PostCoreAsync(
            string path,
            SmsIrBulkSendRequest payload,
            CancellationToken cancellationToken)
        {
            var json = SmsIrJson.Serialize(payload);
            var body = await _transport.PostJsonAsync(path, json, cancellationToken).ConfigureAwait(false);
            return ParseBody<SmsIrBulkSendResult>(body);
        }

        private async Task<SmsIrVerifyResult> PostCoreAsync(
            string path,
            SmsIrVerifyRequest payload,
            CancellationToken cancellationToken)
        {
            var json = SmsIrJson.Serialize(payload);
            var body = await _transport.PostJsonAsync(path, json, cancellationToken).ConfigureAwait(false);
            return ParseBody<SmsIrVerifyResult>(body);
        }

        private async Task<SmsIrSendStatusResult> GetCoreAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var body = await _transport.GetAsync(path, cancellationToken).ConfigureAwait(false);
            return ParseBody<SmsIrSendStatusResult>(body);
        }

        private static TData ParseBody<TData>(string body)
            where TData : class
        {
            var envelope = SmsIrJson.Deserialize<TData>(body);
            if (envelope is null || envelope.Status != 1)
            {
                if (envelope is null)
                {
                    throw new IranSmsException("SMS.ir returned an unparseable envelope.")
                    {
                        ProviderName = "SmsIr",
                        RawResponseBody = body,
                    };
                }

                throw new IranSmsException(
                    $"SMS.ir API error ({envelope.Status}): {envelope.Message}")
                {
                    ProviderName = "SmsIr",
                    ProviderStatusCode = envelope.Status,
                    RawResponseBody = body,
                };
            }

            return envelope.Data ?? throw new IranSmsException("SMS.ir API error: empty data payload.")
            {
                ProviderName = "SmsIr",
                RawResponseBody = body,
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (_transport as IDisposable)?.Dispose();
        }
    }
}
