using System.Globalization;
using System.Text.Json;
using IranSms.Providers.Ghasedak.Json;

namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Ghasedak SMS provider client (REST gateway).
    /// Authenticates with an ApiKey header on every request.
    /// Implements <see cref="IDisposable"/> to release the internal <see cref="HttpClient"/> when caller did not supply one.
    /// </summary>
    public sealed class GhasedakClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter, ISmsAccountInfo, IDisposable
    {
        private const int MaxBulkRecipients = 100;
        private const int MaxMessageLength = 1000;

        // Official Ghasedak WebService method names (relative to the gateway base URL).
        private const string SendSinglePath = "SendSingleSMS";
        private const string SendBulkPath = "SendBulkSMS";
        private const string SendOtpPath = "SendOtpWithParams";
        private const string CheckSmsStatusPath = "CheckSmsStatus";
        private const string AccountInfoPath = "GetAccountInformation";

        private readonly IGhasedakTransport _transport;

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
        }

        /// <inheritdoc />
        public string ProviderName => "Ghasedak";

        /// <inheritdoc />
        public SmsCapabilities Capabilities =>
            SmsCapabilities.Send
            | SmsCapabilities.BulkSend
            | SmsCapabilities.OtpSend
            | SmsCapabilities.DeliveryStatus
            | SmsCapabilities.AccountInfo;

        /// <inheritdoc />
        public async Task<SmsSendResult> SendAsync(
            string recipient,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            var text = SmsValidation.EnsureMessage(message);
            if (text.Length > MaxMessageLength)
                throw new ArgumentException($"Ghasedak messages are limited to {MaxMessageLength} characters.", nameof(message));

            var body = new Dictionary<string, object>
            {
                ["message"] = text,
                ["receptor"] = SmsValidation.EnsureRecipient(recipient),
            };
            var line = SmsValidation.EnsureSenderLine(senderLine);
            if (line != null)
                body["lineNumber"] = line;

            var json = JsonSerializer.Serialize(body);
            var raw = await _transport.PostJsonAsync(SendSinglePath, json, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw, SendSinglePath);

            var msgId = envelope != null ? GhasedakResponse.GetDataString(envelope, "MessageId") : null;
            if (string.IsNullOrWhiteSpace(msgId))
                throw MissingMessageId("SendSingleSMS", raw);
            return new SmsSendResult(msgId!);
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
            var line = SmsValidation.EnsureSenderLine(senderLine);

            var source = recipients as IReadOnlyList<string> ?? MaterializeRecipients(recipients, MaxBulkRecipients);
            if (source.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            if (source.Count > MaxBulkRecipients)
                throw new ArgumentException($"Ghasedak bulk send supports at most {MaxBulkRecipients} recipients.", nameof(recipients));

            var list = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
                list[i] = SmsValidation.EnsureRecipient(source[i], nameof(recipients));
            if (text.Length > MaxMessageLength)
                throw new ArgumentException($"Ghasedak messages are limited to {MaxMessageLength} characters.", nameof(message));

            var body = new Dictionary<string, object>
            {
                ["message"] = text,
                ["receptors"] = list,
            };
            if (line != null)
                body["lineNumber"] = line;

            var json = JsonSerializer.Serialize(body);
            var raw = await _transport.PostJsonAsync(SendBulkPath, json, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw, SendBulkPath);

            var msgIds = envelope != null ? GhasedakResponse.GetDataItemStrings(envelope, "Receptors", "MessageId") : null;
            if (msgIds == null || msgIds.Length == 0)
                throw MissingMessageId("SendBulkSMS", raw);
            return new SmsSendResult(msgIds[0])
            {
                RecipientIds = msgIds,
            };
        }

        /// <inheritdoc />
        public async Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default)
        {
            var receptor = SmsValidation.EnsureRecipient(recipient);
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (request.SendDate.HasValue)
                throw new NotSupportedException($"{ProviderName} does not honour OtpRequest.SendDate — schedule delivery in your application instead.");
            SmsValidation.EnsureClientReferenceId(request.ClientReferenceId, nameof(request));
            SmsValidation.EnsureSenderLine(request.SenderLine, nameof(request));

            if (string.IsNullOrWhiteSpace(request.TemplateId))
                throw new ArgumentException("Ghasedak OTP requires a TemplateId (template name).", nameof(request));

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request.Parameters is { Count: > 0 })
            {
                foreach (var parameter in request.Parameters)
                    parameters[parameter.Key] = parameter.Value;
            }
            else if (!string.IsNullOrWhiteSpace(request.Code))
            {
                parameters["param1"] = request.Code!;
            }

            if (!parameters.TryGetValue("param1", out var param1) || string.IsNullOrWhiteSpace(param1))
                throw new ArgumentException("Ghasedak OTP requires Code or Parameters with a non-empty param1.", nameof(request));
            foreach (var parameter in parameters)
            {
                if (!IsOtpParameterName(parameter.Key) || string.IsNullOrWhiteSpace(parameter.Value))
                    throw new ArgumentException("Ghasedak OTP parameters must be named param1 through param10 and have values.", nameof(request));
            }

            var body = new Dictionary<string, object>
            {
                ["templateName"] = request.TemplateId!,
                ["receptors"] = new[]
                {
                    new { mobile = receptor },
                },
            };
            for (var i = 1; i <= 10; i++)
            {
                if (parameters.TryGetValue("param" + i.ToString(CultureInfo.InvariantCulture), out var value))
                    body["param" + i.ToString(CultureInfo.InvariantCulture)] = value;
            }

            var json = JsonSerializer.Serialize(body);
            var raw = await _transport.PostJsonAsync(SendOtpPath, json, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw, SendOtpPath);

            var msgIds = envelope != null ? GhasedakResponse.GetDataItemStrings(envelope, "Items", "MessageId") : null;
            if (msgIds == null || msgIds.Length == 0)
                throw MissingMessageId("SendOtpWithParams", raw);
            return new OtpSendResult(msgIds[0]);
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Value is null)
                throw new ArgumentNullException(nameof(message));

            string type;
            switch (message.Type)
            {
                case MessageIdentifierType.ProviderMessageId:
                    type = "1";
                    break;
                case MessageIdentifierType.ClientReferenceId:
                    type = "2";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message.Type, "Unsupported message identifier type.");
            }

            var query = new Dictionary<string, string>
            {
                ["Ids"] = message.Value,
                ["Type"] = type,
            };

            var raw = await _transport.GetAsync(CheckSmsStatusPath, query, cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw, CheckSmsStatusPath);
            if (envelope == null)
            {
                throw new IranSmsException("Ghasedak returned an empty response for delivery status.")
                {
                    ProviderName = "Ghasedak",
                    Kind = SmsErrorKind.MalformedResponse,
                    Operation = CheckSmsStatusPath,
                    RawResponseBody = raw,
                };
            }

            var data = envelope.Data;
            if (data == null || (data.Value.ValueKind != JsonValueKind.Array && data.Value.ValueKind != JsonValueKind.Null))
                throw MalformedResponse(raw);
            if (data.Value.ValueKind == JsonValueKind.Null || data.Value.GetArrayLength() == 0)
            {
                return new MessageStatusResult(MessageDeliveryState.Unknown, message)
                {
                    RawStatus = "no-data",
                };
            }

            var item = data.Value[0];
            if (item.ValueKind != JsonValueKind.Object)
                throw MalformedResponse(raw);

            var state = MessageDeliveryState.Unknown;
            var rawStatus = "unknown";
            if (item.TryGetProperty("Status", out var st) && st.ValueKind != JsonValueKind.Null)
            {
                long code;
                if (st.ValueKind == JsonValueKind.Number)
                {
                    if (!st.TryGetInt64(out code))
                        throw MalformedResponse(raw);
                }
                else if (st.ValueKind != JsonValueKind.String || !long.TryParse(st.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                    throw MalformedResponse(raw);
                if (code < int.MinValue || code > int.MaxValue)
                    throw MalformedResponse(raw);
                state = GhasedakResponse.MapDeliveryState((int)code);
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

        /// <inheritdoc />
        public async Task<AccountBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default)
        {
            // GET GetAccountInformation — https://ghasedak.me/docs (ApiKey header)
            var raw = await _transport.GetAsync(AccountInfoPath, new Dictionary<string, string>(), cancellationToken).ConfigureAwait(false);
            var envelope = GhasedakResponse.EnsureSuccess(GhasedakEnvelope.Deserialize(raw), raw, AccountInfoPath);
            if (envelope?.Data is null || envelope.Data.Value.ValueKind != JsonValueKind.Object)
                throw new IranSmsException("Ghasedak did not return account information.")
                {
                    ProviderName = ProviderName,
                    Kind = SmsErrorKind.MalformedResponse,
                    Operation = AccountInfoPath,
                    RawResponseBody = raw,
                };

            var data = envelope.Data.Value;
            if (!data.TryGetProperty("Credit", out var creditValue))
                throw MissingCredit(raw);
            decimal credit;
            if (creditValue.ValueKind == JsonValueKind.Number)
            {
                if (!creditValue.TryGetDecimal(out credit))
                    throw MissingCredit(raw);
            }
            else if (creditValue.ValueKind != JsonValueKind.String ||
                !decimal.TryParse(creditValue.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out credit))
                throw MissingCredit(raw);

            DateTimeOffset? expireDate = null;
            if (data.TryGetProperty("ExpireDate", out var ed) && ed.ValueKind == JsonValueKind.String)
            {
                var s = ed.GetString();
                if (!string.IsNullOrWhiteSpace(s) && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    expireDate = dt;
            }

            string? plan = null;
            if (data.TryGetProperty("Plan", out var pl) && pl.ValueKind == JsonValueKind.String)
                plan = pl.GetString();

            return new AccountBalanceResult(credit)
            {
                ExpireDate = expireDate,
                AccountType = plan,
            };
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GetSenderLinesAsync(CancellationToken cancellationToken = default)
        {
            // Ghasedak has no dedicated sender-line list endpoint; the default line is selected
            // by the gateway when lineNumber is omitted. Return an empty list to signal that.
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (_transport as IDisposable)?.Dispose();
        }

        private static List<string> MaterializeRecipients(IEnumerable<string> recipients, int max)
        {
            var list = new List<string>();
            foreach (var recipient in recipients)
            {
                list.Add(recipient);
                if (list.Count > max)
                    throw new ArgumentException($"Ghasedak bulk send supports at most {max} recipients.", nameof(recipients));
            }
            return list;
        }

        private static bool IsOtpParameterName(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith("param", StringComparison.Ordinal))
                return false;
            if (!int.TryParse(name.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                return false;
            return number >= 1 && number <= 10;
        }

        private static IranSmsException MissingMessageId(string operation, string raw)
            => new IranSmsException($"Ghasedak did not return a MessageId for {operation}.")
            {
                ProviderName = "Ghasedak",
                Kind = SmsErrorKind.MalformedResponse,
                Operation = operation,
                RawResponseBody = raw,
            };

        private static IranSmsException MissingCredit(string raw)
            => new IranSmsException("Ghasedak did not return a valid Credit for GetAccountInformation.")
            {
                ProviderName = "Ghasedak",
                Kind = SmsErrorKind.MalformedResponse,
                Operation = "GetAccountInformation",
                RawResponseBody = raw,
            };

        private static IranSmsException MalformedResponse(string raw)
            => new IranSmsException("Ghasedak returned a malformed response.")
            {
                ProviderName = "Ghasedak",
                Kind = SmsErrorKind.MalformedResponse,
                RawResponseBody = raw,
            };
    }
}
