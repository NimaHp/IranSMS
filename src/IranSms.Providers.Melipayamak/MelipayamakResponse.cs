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
            var envelope = ParseRestEnvelope(body);
            if (envelope is not null)
            {
                if (TryParseInteger(envelope.Value, out var code) && IsErrorCode(code))
                    ThrowApiError(body, envelope, code);
                if (!envelope.HasRetStatus || envelope.RetStatus != 1)
                    ThrowApiError(body, envelope);

                if (string.IsNullOrWhiteSpace(envelope.Value))
                    throw UnrecognizedResponse(body);

                if (long.TryParse(envelope.Value!.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                {
                    if (code <= 0)
                        ThrowApiError(body, envelope, code);
                    return code.ToString(CultureInfo.InvariantCulture);
                }

                throw UnrecognizedResponse(body);
            }

            if (long.TryParse(body.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var plainCode))
            {
                if (plainCode <= 0 || IsErrorCode(plainCode))
                    ThrowPlainError(body, plainCode);
                return plainCode.ToString(CultureInfo.InvariantCulture);
            }

            throw UnrecognizedResponse(body);
        }

        internal static bool TryParseInteger(string? value, out long code)
            => long.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out code);

        internal static bool IsErrorCode(long code)
        {
            switch (code)
            {
                case -111:
                case -110:
                case -109:
                case -108:
                case 0:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 9:
                case 10:
                case 11:
                case 12:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 35:
                    return true;
                default:
                    return false;
            }
        }

        internal static void ThrowApiError(string body, RestEnvelope envelope, long? valueCode = null)
        {
            var code = valueCode ?? envelope.RetStatus;
            throw new IranSmsException($"Melipayamak API error ({code}).")
            {
                ProviderName = "Melipayamak",
                ProviderStatusCode = (int)code,
                Kind = SmsErrorKind.ProviderRejected,
                RawResponseBody = body,
            };
        }

        internal static void ThrowPlainError(string body, long code)
            => throw new IranSmsException($"Melipayamak API error ({code}).")
            {
                ProviderName = "Melipayamak",
                ProviderStatusCode = (int)code,
                Kind = SmsErrorKind.ProviderRejected,
                RawResponseBody = body,
            };

        private static IranSmsException UnrecognizedResponse(string body)
            => new IranSmsException("Melipayamak returned an unrecognized response.")
            {
                ProviderName = "Melipayamak",
                Kind = SmsErrorKind.MalformedResponse,
                RawResponseBody = body,
            };

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

            status = NormalizeStatus(status!);
            if (!long.TryParse(status, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
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

        private static string NormalizeStatus(string status)
        {
            var value = status.Trim();
            if (!value.StartsWith("[", StringComparison.Ordinal))
                return value;

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(value);
                if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array ||
                    document.RootElement.GetArrayLength() == 0)
                    return value;

                var first = document.RootElement[0];
                return first.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => first.GetString() ?? value,
                    System.Text.Json.JsonValueKind.Number => first.GetRawText(),
                    _ => value,
                };
            }
            catch (System.Text.Json.JsonException)
            {
                return value;
            }
        }

        internal sealed class RestEnvelope
        {
            public string? Value { get; set; }
            public bool HasValue { get; set; }
            public int RetStatus { get; set; }
            public bool HasRetStatus { get; set; }
            public string? StrRetStatus { get; set; }
        }

        internal static RestEnvelope? ParseRestEnvelope(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;
            var trimmed = body.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
                return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
                var root = doc.RootElement;
                var env = new RestEnvelope();
                if (root.TryGetProperty("Value", out var v))
                {
                    env.HasValue = true;
                    if (v.ValueKind == System.Text.Json.JsonValueKind.String)
                        env.Value = v.GetString();
                    else if (v.ValueKind != System.Text.Json.JsonValueKind.Null)
                        env.Value = v.GetRawText();
                }

                if (root.TryGetProperty("RetStatus", out var rs) && rs.ValueKind == System.Text.Json.JsonValueKind.Number && rs.TryGetInt32(out var rsi))
                {
                    env.RetStatus = rsi;
                    env.HasRetStatus = true;
                }
                else if (root.TryGetProperty("RetStatus", out var rs2) && rs2.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(rs2.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rsi2))
                {
                    env.RetStatus = rsi2;
                    env.HasRetStatus = true;
                }
                if (root.TryGetProperty("StrRetStatus", out var srs) && srs.ValueKind == System.Text.Json.JsonValueKind.String)
                    env.StrRetStatus = srs.GetString();
                return env;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
