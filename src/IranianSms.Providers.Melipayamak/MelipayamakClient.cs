namespace IranianSms.Providers.Melipayamak
{
    /// <summary>
    /// Melipayamak SMS provider client (REST API, see research doc 03-melipayamak.md).
    /// Authenticates with username/password (or ApiKey) in the form body.
    /// </summary>
    public sealed class MelipayamakClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter
    {
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
            | SmsCapabilities.DeliveryStatus;

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

            var body = await _transport.PostFormAsync("SendSMS", form, cancellationToken).ConfigureAwait(false);
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

            var response = await _transport.PostFormAsync("SendBulkSMS", form, cancellationToken).ConfigureAwait(false);
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

            var response = await _transport.PostFormAsync("SendOtp", form, cancellationToken).ConfigureAwait(false);
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

            var body = await _transport.PostFormAsync("GetDelivery", form, cancellationToken).ConfigureAwait(false);
            return new MessageStatusResult(MelipayamakResponse.MapDeliveryState(body), message)
            {
                RawStatus = body.Trim(),
            };
        }
    }
}