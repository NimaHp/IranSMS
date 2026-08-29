using IranSms.Providers.Kavenegar.Json;

namespace IranSms.Providers.Kavenegar
{
    /// <summary>
    /// Kavenegar SMS provider client.
    /// </summary>
    public sealed class KavenegarClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter
    {
        private const int MaxRecipients = 200;

        // Official Kavenegar REST method paths (relative to the /v1/{api-key} base URL).
        private const string SendPath = "sms/send";
        private const string VerifyPath = "verify/lookup";
        private const string StatusPath = "sms/status";
        private const string StatusLocalMessageIdPath = "sms/statuslocalmessageid";

        private readonly IKavenegarTransport _transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="KavenegarClient"/> class.
        /// </summary>
        /// <param name="apiKey">Kavenegar API key.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/> (base address may be overridden for tests).</param>
        /// <exception cref="ArgumentNullException"><paramref name="apiKey"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="apiKey"/> is empty.</exception>
        public KavenegarClient(string apiKey, HttpClient? httpClient = null)
            : this(new KavenegarHttpTransport(apiKey, httpClient), apiKey)
        {
        }

        internal KavenegarClient(IKavenegarTransport transport, string apiKey)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (apiKey is null)
                throw new ArgumentNullException(nameof(apiKey));
            if (apiKey.Length == 0)
                throw new ArgumentException("API key cannot be empty.", nameof(apiKey));
        }

        /// <inheritdoc />
        public string ProviderName => "Kavenegar";

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
            var parameters = new Dictionary<string, string>
            {
                ["receptor"] = recipient,
                ["message"] = message,
            };
            AddOptional(parameters, "sender", senderLine);

            var entry = await SendCoreAsync(SendPath, parameters, cancellationToken).ConfigureAwait(false);
            return new SmsSendResult(entry.GetString("messageid"))
            {
                Cost = entry.GetNullableDecimal("cost"),
            };
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

            var list = recipients as IReadOnlyList<string> ?? recipients.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            if (list.Count > MaxRecipients)
                throw new ArgumentException($"Kavenegar accepts at most {MaxRecipients} recipients per call.", nameof(recipients));

            var parameters = new Dictionary<string, string>
            {
                ["receptor"] = string.Join(",", list),
                ["message"] = message,
            };
            AddOptional(parameters, "sender", senderLine);

            var entries = await SendCoreMultiAsync(SendPath, parameters, cancellationToken).ConfigureAwait(false);

            var ids = entries.Select(e => e.GetString("messageid")).ToArray();
            return new SmsSendResult(ids[0])
            {
                RecipientIds = ids,
            };
        }

        /// <inheritdoc />
        public async Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var templateName = request.TemplateId;
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("Kavenegar OTP requires a template name (TemplateId).", nameof(request));

            var parameters = new Dictionary<string, string>
            {
                ["receptor"] = recipient,
                ["template"] = templateName!,
            };

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                parameters["token"] = request.Code!;
            }
            else if (request.Parameters is { Count: > 0 })
            {
                foreach (var pair in request.Parameters)
                {
                    // Kavenegar token params: token, token2, token3, token10, token20.
                    if (string.Equals(pair.Key, "token", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pair.Key, "token2", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pair.Key, "token3", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pair.Key, "token10", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pair.Key, "token20", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters[pair.Key.ToLowerInvariant()] = pair.Value;
                    }
                }
            }
            else
            {
                throw new ArgumentException("Kavenegar OTP requires a Code or token parameters.", nameof(request));
            }

            if (request.SenderLine is not null)
                AddOptional(parameters, "sender", request.SenderLine);

            var entry = await SendCoreAsync(VerifyPath, parameters, cancellationToken).ConfigureAwait(false);
            return new OtpSendResult(entry.GetString("messageid"))
            {
                Cost = entry.GetNullableDecimal("cost"),
            };
        }

        /// <inheritdoc />
        /// <remarks>
        /// Provider message ids can be queried for the last 48 hours via
        /// <c>sms/status</c>; client reference ids are resolved through
        /// <c>sms/statuslocalmessageid</c> and only cover the last 12 hours.
        /// </remarks>
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            var isLocal = message.Type == MessageIdentifierType.ClientReferenceId;
            var method = isLocal ? StatusLocalMessageIdPath : StatusPath;
            var paramName = isLocal ? "localid" : "messageid";

            var parameters = new Dictionary<string, string> { [paramName] = message.Value };
            var entries = await SendCoreMultiAsync(method, parameters, cancellationToken).ConfigureAwait(false);
            if (entries.Count == 0)
            {
                return new MessageStatusResult(MessageDeliveryState.Unknown, message)
                {
                    RawStatus = "no-entry",
                };
            }

            var entry = entries[0];
            return new MessageStatusResult(
                KavenegarStatusMapper.ToDeliveryState(entry.GetString("status")),
                message)
            {
                RawStatus = entry.GetString("status"),
                Recipient = entry.GetNullableString("receptor"),
                Price = entry.GetNullableDecimal("cost"),
                SendDate = entry.GetNullableDateTimeOffset("date", isUnix: true),
            };
        }

        private async Task<KavenegarEntry> SendCoreAsync(
            string method,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            var entries = await SendCoreMultiAsync(method, parameters, cancellationToken).ConfigureAwait(false);
            return entries[0];
        }

        private async Task<IReadOnlyList<KavenegarEntry>> SendCoreMultiAsync(
            string method,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            var body = await _transport.PostAsync(method, parameters, cancellationToken).ConfigureAwait(false);

            KavenegarEnvelope? envelope;
            try
            {
                envelope = KavenegarJson.Deserialize(body);
            }
            catch (Exception ex)
            {
                throw new IranSmsException("Kavenegar returned a malformed response.", ex)
                {
                    ProviderName = ProviderName,
                    RawResponseBody = body,
                };
            }

            if (envelope is null || envelope.Return is null)
            {
                throw new IranSmsException("Kavenegar returned an empty envelope.")
                {
                    ProviderName = ProviderName,
                    RawResponseBody = body,
                };
            }

            if (envelope.Return.Status != 200)
            {
                throw new IranSmsException(
                    $"Kavenegar API error ({envelope.Return.Status}): {envelope.Return.Message}")
                {
                    ProviderName = ProviderName,
                    ProviderStatusCode = envelope.Return.Status,
                    RawResponseBody = body,
                };
            }

            return envelope.Entries ?? new List<KavenegarEntry>();
        }

        private static void AddOptional(Dictionary<string, string> parameters, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                parameters[key] = value!;
        }
    }
}
