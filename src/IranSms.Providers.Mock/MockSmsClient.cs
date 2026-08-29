using System.Globalization;

namespace IranSms.Providers.Mock
{
    /// <summary>
    /// In-memory Mock provider. No network calls: every send records the
    /// payload and returns a deterministic, monotonically increasing message id
    /// ("mock-1", "mock-2", ...). Delivery lookups return the recorded payload
    /// (state: Delivered for single/OTP sends, Queued for bulk). Useful for
    /// tests, demos and local development without a real provider account.
    /// </summary>
    public sealed class MockSmsClient : ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter
    {
        private readonly object _lock = new object();
        private readonly List<MockMessage> _messages = new List<MockMessage>();
        private long _nextId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockSmsClient"/> class.
        /// </summary>
        /// <param name="providerName">Optional display name (default "Mock").</param>
        public MockSmsClient(string? providerName = null)
        {
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? "Mock" : providerName!;
        }

        /// <inheritdoc />
        public string ProviderName { get; }

        /// <inheritdoc />
        public SmsCapabilities Capabilities =>
            SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus;

        /// <summary>
        /// Gets a snapshot of all messages recorded so far (newest last).
        /// </summary>
        public IReadOnlyList<MockMessage> Messages
        {
            get
            {
                lock (_lock)
                {
                    return _messages.ToArray();
                }
            }
        }

        /// <summary>
        /// Removes all recorded messages and resets the id counter.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
                _nextId = 0;
            }
        }

        /// <inheritdoc />
        public Task<SmsSendResult> SendAsync(
            string recipient,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException("Recipient is required.", nameof(recipient));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            var id = NextId();
            var entry = new MockMessage(
                id,
                recipient,
                message,
                senderLine,
                MessageDeliveryState.Delivered,
                null,
                DateTimeOffset.UtcNow);

            lock (_lock)
            {
                _messages.Add(entry);
            }

            return Task.FromResult(new SmsSendResult(id));
        }

        /// <inheritdoc />
        public Task<SmsSendResult> SendBulkAsync(
            IEnumerable<string> recipients,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default)
        {
            if (recipients is null)
                throw new ArgumentNullException(nameof(recipients));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            var list = recipients.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(recipients));

            var ids = new string[list.Count];
            lock (_lock)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    ids[i] = NextIdLocked();
                    _messages.Add(new MockMessage(
                        ids[i],
                        list[i],
                        message,
                        senderLine,
                        MessageDeliveryState.Queued,
                        null,
                        DateTimeOffset.UtcNow));
                }
            }

            return Task.FromResult(new SmsSendResult(ids[0]) { RecipientIds = ids });
        }

        /// <inheritdoc />
        public Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException("Recipient is required.", nameof(recipient));
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            cancellationToken.ThrowIfCancellationRequested();

            var code = request.Code
                ?? (request.Parameters != null && request.Parameters.TryGetValue("token", out var t) ? t : null)
                ?? "000000";

            var id = NextId();
            var entry = new MockMessage(
                id,
                recipient,
                code,
                request.SenderLine,
                MessageDeliveryState.Delivered,
                request.TemplateId,
                DateTimeOffset.UtcNow);

            lock (_lock)
            {
                _messages.Add(entry);
            }

            return Task.FromResult(new OtpSendResult(id));
        }

        /// <inheritdoc />
        public Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default)
        {
            if (message.Value is null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            MockMessage? found = null;
            lock (_lock)
            {
                // Mock only tracks provider-assigned ids; client reference ids are never matched.
                if (message.Type == MessageIdentifierType.ProviderMessageId)
                {
                    found = _messages.FirstOrDefault(m =>
                        string.Equals(m.Id, message.Value, StringComparison.Ordinal));
                }
            }

            if (found is null)
            {
                return Task.FromResult(new MessageStatusResult(MessageDeliveryState.Unknown, message)
                {
                    RawStatus = "not-found",
                });
            }

            return Task.FromResult(new MessageStatusResult(found.State, message)
            {
                RawStatus = ((int)found.State).ToString(CultureInfo.InvariantCulture),
                Recipient = found.Recipient,
                MessageText = found.MessageText,
                SendDate = found.SendDate,
            });
        }

        private string NextId()
        {
            lock (_lock)
            {
                return NextIdLocked();
            }
        }

        private string NextIdLocked()
        {
            _nextId++;
            return "mock-" + _nextId.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A single recorded message in the Mock provider's in-memory store.
    /// </summary>
    public sealed class MockMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockMessage"/> class.
        /// </summary>
        /// <param name="id">Provider message id.</param>
        /// <param name="recipient">Destination number.</param>
        /// <param name="messageText">Message text (or OTP code).</param>
        /// <param name="senderLine">Sender line used (may be null).</param>
        /// <param name="state">Delivery state.</param>
        /// <param name="templateId">Template id for OTP sends (may be null).</param>
        /// <param name="sendDate">UTC send timestamp.</param>
        public MockMessage(
            string id,
            string recipient,
            string messageText,
            string? senderLine,
            MessageDeliveryState state,
            string? templateId,
            DateTimeOffset sendDate)
        {
            Id = id;
            Recipient = recipient;
            MessageText = messageText;
            SenderLine = senderLine;
            State = state;
            TemplateId = templateId;
            SendDate = sendDate;
        }

        /// <summary>Provider message id.</summary>
        public string Id { get; }

        /// <summary>Destination number.</summary>
        public string Recipient { get; }

        /// <summary>Message text (or OTP code).</summary>
        public string MessageText { get; }

        /// <summary>Sender line used (may be null).</summary>
        public string? SenderLine { get; }

        /// <summary>Delivery state.</summary>
        public MessageDeliveryState State { get; }

        /// <summary>Template id for OTP sends (may be null).</summary>
        public string? TemplateId { get; }

        /// <summary>UTC send timestamp.</summary>
        public DateTimeOffset SendDate { get; }
    }
}
