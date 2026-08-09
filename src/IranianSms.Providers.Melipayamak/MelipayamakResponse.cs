using System.Globalization;

namespace IranianSms.Providers.Melipayamak
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
        /// <see cref="IranianSmsException"/> with the documented message.
        /// </summary>
        /// <param name="body">The raw response body.</param>
        /// <returns>The recId.</returns>
        /// <exception cref="IranianSmsException">The body is an error code.</exception>
        public static string ParseRecId(string body)
        {
            var trimmed = body.Trim();
            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
            {
                if (code > 0)
                    return code.ToString(CultureInfo.InvariantCulture);

                throw new IranianSmsException($"Melipayamak API error ({code}): {DescribeError(code)}")
                {
                    ProviderName = "Melipayamak",
                    ProviderStatusCode = (int)code,
                    RawResponseBody = body,
                };
            }

            throw new IranianSmsException($"Melipayamak returned an unrecognized response: {Truncate(trimmed)}")
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
        /// Maps a Melipayamak delivery status string/number to a
        /// <see cref="MessageDeliveryState"/>. Values: null/-1 = not-sent-available,
        /// 1 = delivered to receiver, 2/7 ?= sent to operator, 3 = failed etc.
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
                case -1:
                    return MessageDeliveryState.Failed;
                case 0:
                case 1:
                    return MessageDeliveryState.Delivered;
                case 2:
                case 4:
                    return MessageDeliveryState.SentToOperator;
                case 5:
                case 8:
                    return MessageDeliveryState.Queued;
                case 16:
                case 17:
                case 18:
                    return MessageDeliveryState.Failed;
                default:
                    return MessageDeliveryState.Unknown;
            }
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}