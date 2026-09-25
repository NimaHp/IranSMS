
namespace IranSms
{
    /// <summary>
    /// Shared input validation used by every provider client so that identical
    /// inputs fail identically regardless of the provider.
    /// </summary>
    /// <remarks>
    /// Behaviour: invalid caller input throws <see cref="ArgumentNullException"/> or
    /// <see cref="ArgumentException"/> — never <see cref="IranSmsException"/>, which is
    /// reserved for provider, transport and protocol failures. Validation happens before
    /// any network call, so a rejected input never consumes credit.
    /// </remarks>
    public static class SmsValidation
    {
        /// <summary>
        /// Validates a destination number and returns its normalized form.
        /// </summary>
        /// <param name="recipient">Destination number as supplied by the caller.</param>
        /// <param name="parameterName">Name of the caller parameter, used in the exception.</param>
        /// <returns>The normalized recipient (see <see cref="NormalizeRecipient(string, string)"/>).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="recipient"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="recipient"/> is empty or contains no digits.</exception>
        public static string EnsureRecipient(string recipient, string parameterName = "recipient")
        {
            if (recipient is null)
                throw new ArgumentNullException(parameterName);

            var normalized = NormalizeRecipient(recipient, parameterName);
            if (normalized.Length == 0)
                throw new ArgumentException("Recipient is required.", parameterName);

            return normalized;
        }

        /// <summary>
        /// Normalizes a destination number: trims, transliterates Persian/Arabic digits to
        /// ASCII and removes formatting characters (space, dash, underscore, parentheses).
        /// The country prefix is never rewritten, so <c>+98…</c>, <c>0098…</c> and
        /// <c>09…</c> are all preserved as the caller wrote them.
        /// </summary>
        /// <param name="recipient">Destination number to normalize.</param>
        /// <param name="parameterName">Name of the caller parameter, used in the exception.</param>
        /// <returns>The normalized recipient.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="recipient"/> is null.</exception>
        public static string NormalizeRecipient(string recipient, string parameterName = "recipient")
        {
            if (recipient is null)
                throw new ArgumentNullException(parameterName);

            var trimmed = recipient.Trim();
            if (trimmed.Length == 0)
                return string.Empty;

            var buffer = new char[trimmed.Length];
            var length = 0;
            foreach (var character in trimmed)
            {
                var digit = TransliterateDigit(character);
                if (digit.Length == 1)
                {
                    buffer[length++] = digit[0];
                    continue;
                }

                if (!IsFormattingCharacter(character))
                    buffer[length++] = character;
            }

            return new string(buffer, 0, length);
        }

        /// <summary>
        /// Validates a message body.
        /// </summary>
        /// <param name="message">Message text; must contain at least one non-whitespace character.</param>
        /// <param name="parameterName">Name of the caller parameter, used in the exception.</param>
        /// <returns>The message text unchanged.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="message"/> is empty or whitespace.</exception>
        public static string EnsureMessage(string message, string parameterName = "message")
        {
            if (message is null)
                throw new ArgumentNullException(parameterName);
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message is required.", parameterName);

            return message;
        }

        /// <summary>
        /// Validates an optional sender line.
        /// </summary>
        /// <param name="senderLine">Sender line; null means "use the provider default".</param>
        /// <param name="parameterName">Name of the caller parameter, used in the exception.</param>
        /// <returns>The trimmed sender line, or null when none was supplied.</returns>
        /// <exception cref="ArgumentException"><paramref name="senderLine"/> is empty or whitespace.</exception>
        public static string? EnsureSenderLine(string? senderLine, string parameterName = "senderLine")
        {
            if (senderLine is null)
                return null;
            if (string.IsNullOrWhiteSpace(senderLine))
                throw new ArgumentException("Sender line cannot be empty or whitespace.", parameterName);

            return senderLine.Trim();
        }

        /// <summary>
        /// Validates an optional client reference id (idempotency / correlation key).
        /// </summary>
        /// <param name="clientReferenceId">Client-supplied reference; null means "not supplied".</param>
        /// <param name="parameterName">Name of the caller parameter, used in the exception.</param>
        /// <returns>The trimmed reference, or null when none was supplied.</returns>
        /// <exception cref="ArgumentException"><paramref name="clientReferenceId"/> is empty or whitespace.</exception>
        public static string? EnsureClientReferenceId(string? clientReferenceId, string parameterName = "clientReferenceId")
        {
            if (clientReferenceId is null)
                return null;
            if (string.IsNullOrWhiteSpace(clientReferenceId))
                throw new ArgumentException("Client reference id cannot be empty or whitespace.", parameterName);

            return clientReferenceId.Trim();
        }

        private static bool IsFormattingCharacter(char character)
        {
            return char.IsWhiteSpace(character)
                || character == '-'
                || character == '_'
                || character == '('
                || character == ')';
        }

        private static string TransliterateDigit(char character)
        {
            switch (character)
            {
                case '\u06F0':
                case '\u0660':
                    return "0";
                case '\u06F1':
                case '\u0661':
                    return "1";
                case '\u06F2':
                case '\u0662':
                    return "2";
                case '\u06F3':
                case '\u0663':
                    return "3";
                case '\u06F4':
                case '\u0664':
                    return "4";
                case '\u06F5':
                case '\u0665':
                    return "5";
                case '\u06F6':
                case '\u0666':
                    return "6";
                case '\u06F7':
                case '\u0667':
                    return "7";
                case '\u06F8':
                case '\u0668':
                    return "8";
                case '\u06F9':
                case '\u0669':
                    return "9";
                default:
                    return string.Empty;
            }
        }
    }
}
