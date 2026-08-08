using IranianSms.Providers.SmsIr.Json;

namespace IranianSms.Providers.SmsIr
{
    /// <summary>
    /// SMS.ir SMS provider client (REST API v1, see research doc 02-smsir.md).
    /// Supports single send, bulk send (max 100 mobiles), OTP (verify) and
    /// delivery status lookup.
    /// </summary>
    public sealed class SmsIrClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter
    {
        private const int MaxBulkRecipients = 100;
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
            | SmsCapabilities.DeliveryStatus;

        /// <inheritdoc />
        public async Task<SmsSendResult> SendAsync(
            string recipient,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            if (senderLine is null)
                throw new ArgumentException("SMS.ir requires a sender line (lineNumber) for send.", nameof(senderLine));

            var payload = new Dictionary<string, object>
            {
                ["lineNumber"] = senderLine,
                ["messageText"] = message,
                ["mobile"] = recipient,
            };

            var data = await PostCoreAsync("send", payload, cancellationToken).ConfigureAwait(false);
            return new SmsSendResult(ReadMessageId(data));
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

            var payload = new Dictionary<string, object>
            {
                ["lineNumber"] = senderLine,
                ["messageText"] = message,
                ["mobiles"] = list,
            };

            var data = await PostCoreAsync("send/bulk", payload, cancellationToken).ConfigureAwait(false);
            return new SmsSendResult(ReadMessageId(data));
        }

        /// <inheritdoc />
        public async Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var templateId = request.TemplateId;
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("SMS.ir OTP requires a template id (TemplateId).", nameof(request));

            if (!long.TryParse(templateId, out var parsedTemplateId))
                throw new ArgumentException("SMS.ir template id must be an integer.", nameof(request));

            var parameters = new List<Dictionary<string, object>>();
            if (request.Parameters is { Count: > 0 })
            {
                foreach (var pair in request.Parameters)
                {
                    parameters.Add(new Dictionary<string, object>
                    {
                        ["name"] = pair.Key,
                        ["value"] = pair.Value,
                    });
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                    throw new ArgumentException("SMS.ir OTP requires a Code or Parameters.", nameof(request));

                parameters.Add(new Dictionary<string, object>
                {
                    ["name"] = "Code",
                    ["value"] = request.Code!,
                });
            }

            var payload = new Dictionary<string, object>
            {
                ["mobile"] = recipient,
                ["templateId"] = parsedTemplateId,
                ["parameters"] = parameters,
            };

            var data = await PostCoreAsync("send/verify", payload, cancellationToken).ConfigureAwait(false);
            return new OtpSendResult(ReadMessageId(data));
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Type != MessageIdentifierType.ProviderMessageId)
                throw new ArgumentException("SMS.ir delivery status supports provider message ids only.", nameof(message));

            if (!long.TryParse(message.Value, out var messageId))
                throw new ArgumentException("SMS.ir message id must be an integer.", nameof(message));

            var data = await GetCoreAsync($"send/{messageId}", cancellationToken).ConfigureAwait(false);
            var status = SmsIrStatusMapper.ToDeliveryState(data.GetNullableString("deliveryState"));

            return new MessageStatusResult(status, message)
            {
                RawStatus = data.GetNullableString("deliveryState"),
                Recipient = data.GetNullableString("mobile"),
                Price = data.GetNullableDecimal("cost"),
                SendDate = data.GetNullableDateTimeOffset("sendDateTime", isUnix: true),
            };
        }

        private async Task<SmsIrData> PostCoreAsync(
            string path,
            object payload,
            CancellationToken cancellationToken)
        {
            var json = SmsIrJson.Serialize(payload);
            var body = await _transport.PostJsonAsync(path, json, cancellationToken).ConfigureAwait(false);
            return ParseBody(body);
        }

        private async Task<SmsIrData> GetCoreAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var body = await _transport.PostJsonAsync(path, null, cancellationToken).ConfigureAwait(false);
            return ParseBody(body);
        }

        private static SmsIrData ParseBody(string body)
        {
            var envelope = SmsIrJson.Deserialize(body);
            if (envelope is null || envelope.Status != 1)
            {
                if (envelope is null)
                {
                    throw new IranianSmsException("SMS.ir returned an empty envelope.")
                    {
                        ProviderName = "SmsIr",
                        RawResponseBody = body,
                    };
                }

                throw new IranianSmsException(
                    $"SMS.ir API error ({envelope.Status}): {envelope.Message}")
                {
                    ProviderName = "SmsIr",
                    ProviderStatusCode = envelope.Status,
                    RawResponseBody = body,
                };
            }

            return envelope.Data!;
        }

        private static string ReadMessageId(SmsIrData data)
            => data.GetNullableString("messageId") ?? string.Empty;
    }
}