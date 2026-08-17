using System.Globalization;

namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Parses Melipayamak plain-text responses (a numeric recId or an error code)
    /// and maps delivery status codes to the normalized state.
    /// </summary>
    internal static class MelipayamakResponse
    {
        /// <summary>
        /// Parses a send response body into a recId string.
        /// Positive values are success; known negative/zero values raise
        /// <see cref="IranSmsException"/> with the documented message.
        /// </summary>
        /// <param name="body">The raw response body.</param>
        /// <returns>The recId.</returns>
        /// <exception cref="IranSmsException">The body is an error code.</exception>
        public static string ParseRecId(string body)
        {
            var trimmed = body.Trim();
            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
            {
                if (code > 0)
                    return code.ToString(CultureInfo.InvariantCulture);

                throw new IranSmsException($"Melipayamak API error ({code}): {DescribeError(code)}")
                {
                    ProviderName = "Melipayamak",
                    ProviderStatusCode = (int)code,
                    RawResponseBody = body,
                };
            }

            throw new IranSmsException($"Melipayamak returned an unrecognized response: {Truncate(trimmed)}")
            {
                ProviderName = "Melipayamak",
                RawResponseBody = body,
            };
        }

        /// <summary>Describes a documented Melipayamak error code.</summary>
        public static string DescribeError(long code)
        {
            switch (code)
            {
                case -111:
                    return "Invalid requester.";
                case -110:
                    return "An API key must be used instead of the password.";
                case -109:
                    return "Allowed-IP list must be configured.";
                case -108:
                    return "IP is blocked.";
                case 0:
                    return "Wrong username or password.";
                case 2:
                    return "Insufficient credit.";
                case 3:
                    return "Daily send limit reached.";
                case 4:
                    return "Volume send limit reached.";
                case 5:
                    return "Invalid sender number.";
                case 6:
                    return "System is updating.";
                case 7:
                    return "Message contains a filtered word.";
                case 9:
                    return "Sending from public lines is forbidden.";
                case 10:
                    return "User is disabled.";
                case 11:
                    return "Not sent.";
                case 12:
                    return "Documents incomplete.";
                case 14:
                    return "Message contains a link.";
                case 15:
                    return "Cannot send to more than one number without cancel-11.";
                case 16:
                    return "Receiver not found.";
                case 17:
                    return "Empty message.";
                case 18:
                    return "Invalid receiver number.";
                case 35:
                    return "Number is in the telecom blacklist.";
                default:
                    return "Unknown error.";
            }
        }

        /// <summary>
        /// Maps a Melipayamak delivery status number to a <see cref="MessageDeliveryState"/>
        /// (see the official "GetDeliveries/GetDelivery" return-value table).
        /// </summary>
        /// <param name="status">The raw delivery status.</param>
        /// <returns>The normalized delivery state.</returns>
        public static MessageDeliveryState MapDeliveryState(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return MessageDeliveryState.Unknown;

            if (!long.TryParse(status!.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                return MessageDeliveryState.Unknown;

            switch (code)
            {
                case -1:            // not sent
                case 3:             // telecom error
                case 5:             // unknown error
                case 300:           // filtered
                case 500:           // not accepted
                    return MessageDeliveryState.Failed;
                case 0:             // sent to telecom
                case 8:             // reached telecom
                case 200:           // sent
                    return MessageDeliveryState.SentToOperator;
                case 1:             // reached the phone
                    return MessageDeliveryState.Delivered;
                case 2:             // not reached the phone
                case 16:            // not reached telecom
                    return MessageDeliveryState.Undelivered;
                case 35:            // blacklist
                    return MessageDeliveryState.Blocked;
                case 400:           // in the send queue
                    return MessageDeliveryState.Queued;
                default:            // null, -2/-3/-10, -108/-109/-110, 100
                    return MessageDeliveryState.Unknown;
            }
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}