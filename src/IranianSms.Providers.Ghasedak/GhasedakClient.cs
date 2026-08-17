using System.Globalization;
using System.Text.Json;
using IranianSms.Providers.Ghasedak.Json;

namespace IranianSms.Providers.Ghasedak
{
    /// <summary>
    /// Ghasedak SMS provider client (REST gateway, see research doc 04-ghasedak.md).
    /// Authenticates with an ApiKey header on every request.
    /// </summary>
    public sealed class GhasedakClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter
    {
        private const int MaxBulkRecipients = 100;
        private const int MaxMessageLength = 1000;

        // Official Ghasedak WebService method names (relative to the gateway base URL).
        private const string SendSinglePath = "SendSingleSMS";
        private const string SendBulkPath = "SendBulkSMS";
        private const string SendOtpPath = "SendOtpSMS";
        private const string CheckSmsStatusPath = "CheckSmsStatus";

        private readonly IGhasedakTransport _transport;
        private readonly string _apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="GhasedakClient"/> class.
        /// </summary>
        /// <param name="apiKey">Ghasedak API key.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="apiKey"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="apiKey"/> is empty.</exception>
        public GhasedakClient(string apiKey, HttpClient? httpClient = null)
            : this(new GhasedakHttpTransport(httpClient, apiKey), apiKey)
        {
        }

        internal GhasedakClient(IGhasedakTransport transport, string apiKey)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (apiKey is null)
                throw new ArgumentNullException(nameof(apiKey));
            if (apiKey.Length == 0)
                throw new ArgumentException("API key cannot be empty.", nameof(apiKey));
            _apiKey = apiKey;
        }

        /// <inheritdoc />
        public string ProviderName => "Ghasedak";

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
            if (recipient is null)
                throw new ArgumentNullException(nameof(recipient));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            var body = new Dictionary<string, object>
            {
                ["message"] = message,
                ["receptor"] = recipient,
            };
            if (senderLine != null)
                body["lineNumber"] = senderLine;

            var json = JsonSerializer.Serialize(body);
            var raw = await _transport.PostJsonAsync(SendSinglePath, json, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw);

            var msgId = envelope != null ? GhasedakResponse.GetDataString(envelope, "MessageId") : null;
            return new SmsSendResult(msgId ?? Guid.NewGuid().ToString());
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
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            var list = recipients as IReadOnlyList<string> ?? recipients.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            if (list.Count > MaxBulkRecipients)
                throw new ArgumentException($"Ghasedak bulk send supports at most {MaxBulkRecipients} recipients.", nameof(recipients));
            if (message.Length > MaxMessageLength)
                throw new ArgumentException($"Ghasedak messages are limited to {MaxMessageLength} characters.", nameof(message));

            var body = new Dictionary<string, object>
            {
                ["message"] = message,
                ["receptors"] = list,
            };
            if (senderLine != null)
                body["lineNumber"] = senderLine;

            var json = JsonSerializer.Serialize(body);
            var raw = await _transport.PostJsonAsync(SendBulkPath, json, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw);

            var msgId = envelope != null ? GhasedakResponse.GetDataString(envelope, "MessageId") : null;
            return new SmsSendResult(msgId ?? Guid.NewGuid().ToString());
        }

        /// <inheritdoc />
        public async Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (recipient is null)
                throw new ArgumentNullException(nameof(recipient));
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.TemplateId))
                throw new ArgumentException("Ghasedak OTP requires a TemplateId (template name).", nameof(request));

            var body = new Dictionary<string, object>
            {
                ["templateName"] = request.TemplateId!,
                ["receptors"] = new[]
                {
                    new { mobile = recipient },
                },
                ["inputs"] = request.Parameters == null
                    ? Array.Empty<object>()
                    : request.Parameters.Select(kv => (object)new { param = kv.Key, value = kv.Value }).ToArray(),
            };

            var json = JsonSerializer.Serialize(body);
            var raw = await _transport.PostJsonAsync(SendOtpPath, json, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw);

            var msgId = envelope != null ? GhasedakResponse.GetDataString(envelope, "MessageId") : null;
            return new OtpSendResult(msgId ?? Guid.NewGuid().ToString());
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Value is null)
                throw new ArgumentNullException(nameof(message));

            var type = message.Type == MessageIdentifierType.ProviderMessageId ? "MessageId" : "ClientReferenceId";
            var query = new Dictionary<string, string>
            {
                ["Ids"] = message.Value,
                ["Type"] = type,
            };

            var raw = await _transport.GetAsync(CheckSmsStatusPath, query, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw);
            if (envelope == null)
            {
                throw new IranianSmsException("Ghasedak returned an empty response for delivery status.")
                {
                    ProviderName = "Ghasedak",
                    RawResponseBody = raw,
                };
            }

            var data = envelope.Data;
            if (data == null || data.Value.ValueKind != JsonValueKind.Array || data.Value.GetArrayLength() == 0)
            {
                return new MessageStatusResult(MessageDeliveryState.Unknown, message)
                {
                    RawStatus = "no-data",
                };
            }

            var item = data.Value[0];
            var state = MessageDeliveryState.Unknown;
            var rawStatus = "unknown";
            if (item.TryGetProperty("Status", out var st))
            {
                var code = st.ValueKind == JsonValueKind.Number ? st.GetInt32() : 0;
                state = GhasedakResponse.MapDeliveryState(code);
                rawStatus = code.ToString(CultureInfo.InvariantCulture);
            }

            var result = new MessageStatusResult(state, message)
            {
                RawStatus = rawStatus,
            };

            if (item.TryGetProperty("Receptor", out var rec) && rec.ValueKind == JsonValueKind.String)
                result.Recipient = rec.GetString();
            if (item.TryGetProperty("Message", out var msg) && msg.ValueKind == JsonValueKind.String)
                result.MessageText = msg.GetString();

            return result;
        }
    }
}