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
            | SmsCapabilities.SenderLines;

        /// <inheritdoc />
        public async Task<SmsSendResult> SendAsync(
            string recipient,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            var text = SmsValidation.EnsureMessage(message);
            var line = RequireSenderLine(senderLine, nameof(senderLine));

            var request = new SmsIrBulkSendRequest
            {
                LineNumber = ParseLineNumber(line),
                MessageText = text,
                Mobiles = new[] { RequireRecipient(recipient, nameof(recipient)) },
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
            var text = SmsValidation.EnsureMessage(message);
            var line = RequireSenderLine(senderLine, nameof(senderLine));

            var list = recipients as IReadOnlyList<string> ?? MaterializeRecipients(recipients, MaxBulkRecipients);
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            if (list.Count > MaxBulkRecipients)
                throw new ArgumentException($"SMS.ir accepts at most {MaxBulkRecipients} recipients per call.", nameof(recipients));
            var normalizedRecipients = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
                normalizedRecipients[i] = RequireRecipient(list[i], nameof(recipients));

            var request = new SmsIrBulkSendRequest
            {
                LineNumber = ParseLineNumber(line),
                MessageText = text,
                Mobiles = normalizedRecipients,
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
            var mobile = RequireRecipient(recipient, nameof(recipient));
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (request.SendDate.HasValue)
                throw new NotSupportedException($"{ProviderName} does not honour OtpRequest.SendDate — schedule delivery in your application instead.");
            SmsValidation.EnsureClientReferenceId(request.ClientReferenceId, nameof(request));

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
                    if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                        throw new ArgumentException("SMS.ir OTP parameters must have non-empty names and values.", nameof(request));
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
                Mobile = mobile,
                TemplateId = templateId,
                Parameters = parameters.ToArray(),
            };

            var data = await PostCoreAsync(SendVerifyPath, payload, cancellationToken).ConfigureAwait(false);
            if (data.MessageId is null || data.MessageId <= 0)
                throw new IranSmsException("SMS.ir did not return a valid message id for verify.")
                {
                    ProviderName = ProviderName,
                    Kind = SmsErrorKind.MalformedResponse,
                    Operation = SendVerifyPath,
                };

            return new OtpSendResult(data.MessageId.Value.ToString(CultureInfo.InvariantCulture))
            {
                Cost = data.Cost,
            };
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Value is null)
                throw new ArgumentNullException(nameof(message));
            if (message.Type != MessageIdentifierType.ProviderMessageId)
                throw new ArgumentException("SMS.ir delivery status supports provider message ids only.", nameof(message));

            if (!long.TryParse(message.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var messageId))
                throw new ArgumentException("SMS.ir message id must be an integer.", nameof(message));

            var data = await GetCoreAsync($"{SendStatusPrefix}{messageId}", cancellationToken).ConfigureAwait(false);
            var status = SmsIrStatusMapper.ToDeliveryState(data.DeliveryState);

            DateTimeOffset? sendDate = null;
            if (data.SendDateTime is long sendAt)
            {
                try
                {
                    sendDate = DateTimeOffset.FromUnixTimeSeconds(sendAt);
                }
                catch (ArgumentOutOfRangeException)
                {
                    sendDate = null;
                }
            }

            return new MessageStatusResult(status, message)
            {
                RawStatus = data.DeliveryState?.ToString(CultureInfo.InvariantCulture),
                Recipient = data.Mobile?.ToString(CultureInfo.InvariantCulture),
                Price = data.Cost,
                SendDate = sendDate,
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
                throw new IranSmsException(raw is null ? "SMS.ir returned an unparseable envelope." : $"SMS.ir API error ({raw.Status}).")
                {
                    ProviderName = ProviderName,
                    ProviderStatusCode = raw?.Status,
                    Kind = raw is null ? SmsErrorKind.MalformedResponse : SmsErrorKind.ProviderRejected,
                    Operation = CreditPath,
                    RawResponseBody = body,
                };
            if (!raw.DataElement.HasValue)
                throw new IranSmsException("SMS.ir returned an empty credit response.")
                {
                    ProviderName = ProviderName,
                    Kind = SmsErrorKind.MalformedResponse,
                    Operation = CreditPath,
                    RawResponseBody = body,
                };

            var creditVal = SmsIrJson.ExtractDecimal(raw.DataElement.Value);
            if (creditVal is null)
                throw new IranSmsException("SMS.ir returned an invalid credit response.")
                {
                    ProviderName = ProviderName,
                    Kind = SmsErrorKind.MalformedResponse,
                    Operation = CreditPath,
                    RawResponseBody = body,
                };
            return new AccountBalanceResult(creditVal.Value);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetSenderLinesAsync(CancellationToken cancellationToken = default)
        {
            // GET /v1/line — data is Array<Long>
            var body = await _transport.GetAsync(LinePath, cancellationToken).ConfigureAwait(false);
            var envelope = SmsIrJson.Deserialize<long?[]>(body);
            if (envelope is null || envelope.Status != 1)
            {
                var raw = SmsIrJson.DeserializeRaw(body);
                if (raw is null || raw.Status != 1)
                    throw new IranSmsException(raw is null ? "SMS.ir returned an unparseable envelope." : $"SMS.ir API error ({raw.Status}).")
                    {
                        ProviderName = ProviderName,
                        ProviderStatusCode = raw?.Status,
                        Kind = raw is null ? SmsErrorKind.MalformedResponse : SmsErrorKind.ProviderRejected,
                        Operation = LinePath,
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
            var result = new List<string>();
            foreach (var line in data)
            {
                if (line.HasValue)
                    result.Add(line.Value.ToString(CultureInfo.InvariantCulture));
            }
            return result;
        }

        private static SmsSendResult BuildBulkResult(SmsIrBulkSendResult data)
        {
            var ids = data.MessageIds ?? Array.Empty<long?>();
            var recipientIds = ids.Length == 0 ? null : new string[ids.Length];
            string? firstMessageId = null;
            for (var i = 0; i < ids.Length; i++)
            {
                var value = ids[i];
                recipientIds![i] = value is > 0 ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                firstMessageId ??= value is > 0 ? value.Value.ToString(CultureInfo.InvariantCulture) : null;
            }

            if (firstMessageId is null)
                throw new IranSmsException("SMS.ir did not return a valid message id for the send.")
                {
                    ProviderName = "SmsIr",
                    Kind = SmsErrorKind.MalformedResponse,
                    Operation = SendBulkPath,
                };

            return new SmsSendResult(firstMessageId)
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
            return ParseBody<SmsIrBulkSendResult>(body, path);
        }

        private async Task<SmsIrVerifyResult> PostCoreAsync(
            string path,
            SmsIrVerifyRequest payload,
            CancellationToken cancellationToken)
        {
            var json = SmsIrJson.Serialize(payload);
            var body = await _transport.PostJsonAsync(path, json, cancellationToken).ConfigureAwait(false);
            return ParseBody<SmsIrVerifyResult>(body, path);
        }

        private async Task<SmsIrSendStatusResult> GetCoreAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var body = await _transport.GetAsync(path, cancellationToken).ConfigureAwait(false);
            return ParseBody<SmsIrSendStatusResult>(body, path);
        }

        private static TData ParseBody<TData>(string body, string operation)
            where TData : class
        {
            var envelope = SmsIrJson.Deserialize<TData>(body);
            if (envelope is null || envelope.Status != 1)
            {
                if (envelope is null)
                {
                    throw IranSmsException.MalformedResponse(
                        "SMS.ir returned an unparseable envelope.",
                        providerName: "SmsIr",
                        rawResponseBody: body,
                        operation: operation);
                }

                var error = IranSmsException.ProviderRejected(
                    $"SMS.ir API error ({envelope.Status}).",
                    envelope.Status,
                    "SmsIr",
                    operation);
                error.RawResponseBody = body;
                throw error;
            }

            return envelope.Data ?? throw IranSmsException.MalformedResponse(
                "SMS.ir API error: empty data payload.",
                providerName: "SmsIr",
                rawResponseBody: body,
                operation: operation);
        }

        private static List<string> MaterializeRecipients(IEnumerable<string> recipients, int max)
        {
            var list = new List<string>();
            foreach (var recipient in recipients)
            {
                list.Add(recipient);
                if (list.Count > max)
                    throw new ArgumentException($"SMS.ir accepts at most {max} recipients per call.", nameof(recipients));
            }
            return list;
        }

        private static string RequireRecipient(string recipient, string parameterName)
            => SmsValidation.EnsureRecipient(recipient, parameterName);

        private static string RequireSenderLine(string? senderLine, string parameterName)
        {
            var line = SmsValidation.EnsureSenderLine(senderLine, parameterName);
            if (line is null)
                throw new ArgumentException("SMS.ir requires a sender line (lineNumber) for send.", parameterName);

            return line;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (_transport as IDisposable)?.Dispose();
        }
    }
}
