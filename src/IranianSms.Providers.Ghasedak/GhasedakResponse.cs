using System.Globalization;
using System.Text.Json;
using IranianSms.Providers.Ghasedak.Json;

namespace IranianSms.Providers.Ghasedak
{
    /// <summary>
    /// Parses Ghasedak envelope responses and maps status codes.
    /// </summary>
    internal static class GhasedakResponse
    {
        /// <summary>
        /// Ensures the envelope reports success; otherwise throws
        /// <see cref="IranianSmsException"/> with the provider message.
        /// </summary>
        public static GhasedakEnvelope? EnsureSuccess(GhasedakEnvelope? envelope, string rawBody)
        {
            if (envelope == null)
            {
                throw new IranianSmsException($"Ghasedak returned an unrecognized response: {Truncate(rawBody)}")
                {
                    ProviderName = "Ghasedak",
                    RawResponseBody = rawBody,
                };
            }

            if (!envelope.IsSuccess || envelope.StatusCode != 200)
            {
                throw new IranianSmsException($"Ghasedak API error ({(envelope.StatusCode)}): {envelope.Message ?? "unknown"}")
                {
                    ProviderName = "Ghasedak",
                    ProviderStatusCode = envelope.StatusCode,
                    RawResponseBody = rawBody,
                };
            }

            return envelope;
        }

        /// <summary>Extracts the first string property (or number as string) called <paramref name="prop"/> from the Data object.</summary>
        public static string? GetDataString(GhasedakEnvelope envelope, string prop)
        {
            var data = envelope.Data;
            if (data == null || data.Value.ValueKind != JsonValueKind.Object)
                return null;

            if (!data.Value.TryGetProperty(prop, out var el))
                return null;

            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.GetRawText(),
                _ => null,
            };
        }

        /// <summary>
        /// Maps a Ghasedak delivery status code (0-8) to a
        /// <see cref="MessageDeliveryState"/>.
        /// </summary>
        public static MessageDeliveryState MapDeliveryState(int status)
        {
            switch (status)
            {
                case 0:
                    return MessageDeliveryState.Unknown;
                case 1:
                    return MessageDeliveryState.Cancelled;
                case 2:
                    return MessageDeliveryState.Blocked;
                case 3:
                    return MessageDeliveryState.SentToOperator;
                case 4:
                    return MessageDeliveryState.Undelivered;
                case 5:
                    return MessageDeliveryState.Delivered;
                case 6:
                    return MessageDeliveryState.Failed;
                case 7:
                    return MessageDeliveryState.Unknown; // error-checking state — treat as unknown
                case 8:
                    return MessageDeliveryState.Unknown;
                default:
                    return MessageDeliveryState.Unknown;
            }
        }

        /// <summary>Parses a JSON number (int/long) from a JsonElement.</summary>
        public static long GetInt64(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Number)
            {
                if (el.TryGetInt64(out var l))
                    return l;
                if (el.TryGetInt32(out var i))
                    return i;
            }

            if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            return 0;
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}