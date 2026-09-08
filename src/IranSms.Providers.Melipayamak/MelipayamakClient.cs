namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Melipayamak SMS provider client (REST API).
    /// Authenticates with username/password (or ApiKey) in the form body.
    /// Implements <see cref="IDisposable"/> to release the internal HttpClient when caller did not supply one.
    /// </summary>
    public sealed class MelipayamakClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter, ISmsAccountInfo, IDisposable
    {
        // Official Melipayamak REST action names (relative to the /api/SendSMS base URL).
        private const string SendPath = "SendSMS";
        private const string SendOtpPath = "SendOtp";
        private const string GetDeliveriesPath = "GetDeliveries2";
        private const string GetCreditPath = "GetCredit";
        private const string GetNumbersPath = "GetUserNumbers";

        private static readonly char[] NumbersSeparators = new char[] { ',', '\n', '\r' };

        private readonly IMelipayamakTransport _transport;
        private readonly string _username;
        private readonly string _password;

        /// <summary>
        /// Initializes a new instance of the <see cref="MelipayamakClient"/> class.
        /// </summary>
        /// <param name="username">Melipayamak panel username.</param>
        /// <param name="password">Melipayamak panel password (or ApiKey).</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>.</param>
        /// <exception cref="ArgumentNullException">A parameter is null.</exception>
        /// <exception cref="ArgumentException">A parameter is empty.</exception>
        public MelipayamakClient(string username, string password, HttpClient? httpClient = null)
            : this(new MelipayamakHttpTransport(httpClient), username, password)
        {
        }

        internal MelipayamakClient(IMelipayamakTransport transport, string username, string password)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (username is null)
                throw new ArgumentNullException(nameof(username));
            if (username.Length == 0)
                throw new ArgumentException("Username cannot be empty.", nameof(username));
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (password.Length == 0)
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            _username = username;
            _password = password;
        }

        /// <inheritdoc />
        public string ProviderName => "Melipayamak";

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
                throw new ArgumentException("Melipayamak requires a sender line ('from') for send.", nameof(senderLine));

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["from"] = senderLine,
                ["to"] = recipient,
                ["text"] = message,
            };

            var body = await _transport.PostFormAsync(SendPath, form, cancellationToken).ConfigureAwait(false);
            return new SmsSendResult(MelipayamakResponse.ParseRecId(body));
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
                throw new ArgumentException("Melipayamak requires a sender line ('from') for send.", nameof(senderLine));

            var list = recipients as IReadOnlyList<string> ?? recipients.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["from"] = senderLine,
                ["to"] = string.Join(",", list),
                ["text"] = message,
            };

            var response = await _transport.PostFormAsync(SendPath, form, cancellationToken).ConfigureAwait(false);
            return new SmsSendResult(MelipayamakResponse.ParseRecId(response));
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
                throw new NotSupportedException("Melipayamak (and all current providers) do not honour OtpRequest.SendDate — schedule delivery in your application instead.");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("Melipayamak OTP requires a Code.", nameof(request));

            if (request.SenderLine is null)
                throw new ArgumentException("Melipayamak OTP requires a sender line ('from').", nameof(request));

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["from"] = request.SenderLine,
                ["to"] = recipient,
                ["code"] = request.Code!,
            };

            var response = await _transport.PostFormAsync(SendOtpPath, form, cancellationToken).ConfigureAwait(false);
            return new OtpSendResult(MelipayamakResponse.ParseRecId(response));
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Type != MessageIdentifierType.ProviderMessageId)
                throw new ArgumentException("Melipayamak delivery status supports provider message ids (recId) only.", nameof(message));

            if (!long.TryParse(message.Value, out var recId))
                throw new ArgumentException("Melipayamak recId must be numeric.", nameof(message));

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["recId"] = recId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

            var body = await _transport.PostFormAsync(GetDeliveriesPath, form, cancellationToken).ConfigureAwait(false);
            return new MessageStatusResult(MelipayamakResponse.MapDeliveryState(body), message)
            {
                RawStatus = body.Trim(),
            };
        }

        /// <inheritdoc />
        public async Task<AccountBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default)
        {
            // POST GetCredit — RestClient.cs endpoint api/SendSMS/GetCredit returns JSON { Value, RetStatus, StrRetStatus }.
            // See https://github.com/Melipayamak/melipayamak-Csharp/blob/master/RestClient.cs and https://www.melipayamak.com/api/getcredit/
            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
            };
            var body = await _transport.PostFormAsync(GetCreditPath, form, cancellationToken).ConfigureAwait(false);
            var parsed = MelipayamakResponse.ParseRestEnvelope(body);
            if (parsed is not null && parsed.RetStatus == 1)
            {
                if (decimal.TryParse(parsed.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var credit))
                    return new AccountBalanceResult(credit);
                // Value may be numeric JSON — try raw
                if (!string.IsNullOrWhiteSpace(parsed.Value))
                    throw new IranSmsException($"Melipayamak returned an unrecognized credit value: {MelipayamakResponse.TruncateForLog(parsed.Value ?? string.Empty)}")
                    {
                        ProviderName = ProviderName,
                        RawResponseBody = body,
                    };
            }

            if (parsed is not null && parsed.RetStatus != 1)
                throw new IranSmsException($"Melipayamak API error ({parsed.RetStatus}): {parsed.StrRetStatus ?? MelipayamakResponse.DescribeError(parsed.RetStatus)}")
                {
                    ProviderName = ProviderName,
                    ProviderStatusCode = parsed.RetStatus,
                    RawResponseBody = body,
                };

            // Legacy plain-number fallback: some deployments return bare 42.5
            var trimmed = body.Trim().Trim('"');
            if (decimal.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var plainCredit) && plainCredit >= 0)
                return new AccountBalanceResult(plainCredit);

            throw new IranSmsException($"Melipayamak returned an unrecognized credit response: {MelipayamakResponse.TruncateForLog(body.Trim())}")
            {
                ProviderName = ProviderName,
                RawResponseBody = body,
            };
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetSenderLinesAsync(CancellationToken cancellationToken = default)
        {
            // POST GetUserNumbers — RestClient.cs: GetUserNumbersOp = "GetUserNumbers" on api/SendSMS/
            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
            };
            var body = await _transport.PostFormAsync(GetNumbersPath, form, cancellationToken).ConfigureAwait(false);
            var parsed = MelipayamakResponse.ParseRestEnvelope(body);
            if (parsed is not null)
            {
                if (parsed.RetStatus != 1)
                    throw new IranSmsException($"Melipayamak API error ({parsed.RetStatus}): {parsed.StrRetStatus ?? MelipayamakResponse.DescribeError(parsed.RetStatus)}")
                    {
                        ProviderName = ProviderName,
                        ProviderStatusCode = parsed.RetStatus,
                        RawResponseBody = body,
                    };

                var value = parsed.Value ?? string.Empty;
                var t = value.Trim();
                if (string.IsNullOrEmpty(t))
                    return Array.Empty<string>();
                // Value is often JSON-encoded array string: "[\"5000...\"]" — peel it.
                if (t.StartsWith("[", StringComparison.Ordinal))
                {
                    try
                    {
                        var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(t);
                        if (arr != null)
                            return arr.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    }
                    catch (System.Text.Json.JsonException) { }
                }

                var parts = t.Split(NumbersSeparators, StringSplitOptions.RemoveEmptyEntries);
                var lines = parts.Select(p => p.Trim().Trim('"', '\'')).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                if (lines.Length > 0)
                    return lines;
                return new[] { t.Trim('"', '\'') };
            }

            // Legacy plain fallback (bare CSV/JSON without envelope)
            var trimmed = body.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return Array.Empty<string>();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                try
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(trimmed);
                    if (arr != null)
                        return arr.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                }
                catch (System.Text.Json.JsonException) { }
            }

            var legacyParts = trimmed.Split(NumbersSeparators, StringSplitOptions.RemoveEmptyEntries);
            var legacyLines = legacyParts.Select(p => p.Trim().Trim('"', '\'')).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (legacyLines.Length > 0)
                return legacyLines;
            return new[] { trimmed.Trim('"', '\'') };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (_transport as IDisposable)?.Dispose();
        }
    }
}
