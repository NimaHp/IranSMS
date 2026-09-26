using System.Globalization;

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

        private const int MaxBulkRecipients = 100;

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

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["from"] = line,
                ["to"] = RequireRecipient(recipient, nameof(recipient)),
                ["text"] = text,
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

            var text = SmsValidation.EnsureMessage(message);
            var line = RequireSenderLine(senderLine, nameof(senderLine));

            var list = recipients as IReadOnlyList<string> ?? MaterializeRecipients(recipients, MaxBulkRecipients);
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));
            if (list.Count > MaxBulkRecipients)
                throw new ArgumentException($"Melipayamak accepts at most {MaxBulkRecipients} recipients per call.", nameof(recipients));
            var normalizedRecipients = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
                normalizedRecipients[i] = RequireRecipient(list[i], nameof(recipients));

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["from"] = line,
                ["to"] = string.Join(",", normalizedRecipients),
                ["text"] = text,
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
            var to = RequireRecipient(recipient, nameof(recipient));
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.SendDate.HasValue)
                throw new NotSupportedException("Melipayamak (and all current providers) do not honour OtpRequest.SendDate — schedule delivery in your application instead.");

            if (request is not OtpCodeRequest codeRequest)
                throw new ArgumentException(
                    "Melipayamak OTP is code-based — pass an OtpCodeRequest with a positive integer code.",
                    nameof(request));

            if (!int.TryParse(codeRequest.Code, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) || code <= 0)
                throw new ArgumentException("Melipayamak OTP requires a positive integer Code.", nameof(request));

            SmsValidation.EnsureClientReferenceId(request.ClientReferenceId, nameof(request));
            var from = RequireSenderLine(request.SenderLine, nameof(request));

            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
                ["from"] = from,
                ["to"] = to,
                ["code"] = codeRequest.Code,
            };

            var response = await _transport.PostFormAsync(SendOtpPath, form, cancellationToken).ConfigureAwait(false);
            return new OtpSendResult(MelipayamakResponse.ParseRecId(response));
        }

        /// <inheritdoc />
        public async Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Value is null)
                throw new ArgumentNullException(nameof(message));
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
            var envelope = MelipayamakResponse.ParseRestEnvelope(body);
            string? status;
            if (envelope is not null)
            {
                if (!envelope.HasRetStatus || envelope.RetStatus != 1)
                {
                    var code = MelipayamakResponse.TryParseInteger(envelope.Value, out var parsedCode) && MelipayamakResponse.IsErrorCode(parsedCode)
                        ? parsedCode
                        : (long?)null;
                    MelipayamakResponse.ThrowApiError(body, envelope, code);
                }

                status = envelope.Value;
            }
            else
            {
                status = body;
            }

            return new MessageStatusResult(MelipayamakResponse.MapDeliveryState(status), message)
            {
                RawStatus = status?.Trim(),
            };
        }

        /// <inheritdoc />
        public async Task<AccountBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default)
        {
            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
            };
            var body = await _transport.PostFormAsync(GetCreditPath, form, cancellationToken).ConfigureAwait(false);
            var parsed = MelipayamakResponse.ParseRestEnvelope(body);
            if (parsed is not null)
            {
                if (MelipayamakResponse.TryParseInteger(parsed.Value, out var valueCode) && MelipayamakResponse.IsErrorCode(valueCode))
                    MelipayamakResponse.ThrowApiError(body, parsed, valueCode);
                if (!parsed.HasRetStatus || parsed.RetStatus != 1)
                {
                    var code = MelipayamakResponse.TryParseInteger(parsed.Value, out var parsedCode) && MelipayamakResponse.IsErrorCode(parsedCode)
                        ? parsedCode
                        : (long?)null;
                    MelipayamakResponse.ThrowApiError(body, parsed, code);
                }

                if (decimal.TryParse(parsed.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var credit))
                    return new AccountBalanceResult(credit);
                if (!string.IsNullOrWhiteSpace(parsed.Value))
                    throw new IranSmsException("Melipayamak returned an unrecognized credit value.")
                    {
                        ProviderName = ProviderName,
                        Kind = SmsErrorKind.MalformedResponse,
                        Operation = GetCreditPath,
                        RawResponseBody = body,
                    };
            }

            var trimmed = body.Trim().Trim('"');
            if (MelipayamakResponse.TryParseInteger(trimmed, out var plainCode) && MelipayamakResponse.IsErrorCode(plainCode))
                MelipayamakResponse.ThrowPlainError(body, plainCode);
            if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var plainCredit) && plainCredit >= 0)
                return new AccountBalanceResult(plainCredit);

            throw new IranSmsException("Melipayamak returned an unrecognized credit response.")
            {
                ProviderName = ProviderName,
                Kind = SmsErrorKind.MalformedResponse,
                Operation = GetCreditPath,
                RawResponseBody = body,
            };
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetSenderLinesAsync(CancellationToken cancellationToken = default)
        {
            var form = new Dictionary<string, string>
            {
                ["username"] = _username,
                ["password"] = _password,
            };
            var body = await _transport.PostFormAsync(GetNumbersPath, form, cancellationToken).ConfigureAwait(false);
            var parsed = MelipayamakResponse.ParseRestEnvelope(body);
            if (parsed is not null)
            {
                if (MelipayamakResponse.TryParseInteger(parsed.Value, out var valueCode) && MelipayamakResponse.IsErrorCode(valueCode))
                    MelipayamakResponse.ThrowApiError(body, parsed, valueCode);
                if (!parsed.HasRetStatus || parsed.RetStatus != 1)
                {
                    var code = MelipayamakResponse.TryParseInteger(parsed.Value, out var parsedCode) && MelipayamakResponse.IsErrorCode(parsedCode)
                        ? parsedCode
                        : (long?)null;
                    MelipayamakResponse.ThrowApiError(body, parsed, code);
                }

                var value = parsed.Value ?? string.Empty;
                var t = value.Trim();
                if (string.IsNullOrEmpty(t))
                    return Array.Empty<string>();
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

        private static List<string> MaterializeRecipients(IEnumerable<string> recipients, int max)
        {
            var list = new List<string>();
            foreach (var recipient in recipients)
            {
                list.Add(recipient);
                if (list.Count > max)
                    throw new ArgumentException($"Melipayamak accepts at most {max} recipients per call.", nameof(recipients));
            }
            return list;
        }

        private static string RequireRecipient(string recipient, string parameterName)
            => SmsValidation.EnsureRecipient(recipient, parameterName);

        private static string RequireSenderLine(string? senderLine, string parameterName)
        {
            var line = SmsValidation.EnsureSenderLine(senderLine, parameterName);
            if (line is null)
                throw new ArgumentException("Melipayamak requires a sender line ('from').", parameterName);

            return line;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (_transport as IDisposable)?.Dispose();
        }
    }
}
