using System.Globalization;
using System.Text.Json;
using IranSms.Providers.Ghasedak.Json;

namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Parses Ghasedak envelope responses and maps status codes.
    /// </summary>
    internal static class GhasedakResponse
    {
        /// <summary>
        /// Ensures the envelope reports success; otherwise throws
        /// <see cref="IranSmsException"/> with the provider message.
        /// </summary>
        public static GhasedakEnvelope? EnsureSuccess(GhasedakEnvelope? envelope, string rawBody, string operation)
        {
            if (envelope == null)
            {
                throw IranSmsException.MalformedResponse(
                    "Ghasedak returned an unrecognized response.",
                    providerName: "Ghasedak",
                    rawResponseBody: rawBody,
                    operation: operation);
            }

            if (!envelope.IsSuccess || envelope.StatusCode != 200)
            {
                var error = IranSmsException.ProviderRejected(
                    $"Ghasedak API error ({envelope.StatusCode}).",
                    envelope.StatusCode,
                    "Ghasedak",
                    operation);
                error.RawResponseBody = rawBody;
                error.Kind = SmsErrorKindExtensions.FromHttpStatus(envelope.StatusCode);
                throw error;
            }

            return envelope;
        }

        /// <summary>Extracts the first string property (or number as string) called <paramref name="prop"/> from the Data object.</summary>
        public static string? GetDataString(GhasedakEnvelope envelope, string prop)
        {
            var data = envelope.Data;
            if (data == null || data.Value.ValueKind != JsonValueKind.Object)
                return null;

            return GetString(data.Value, prop);
        }

        public static string[]? GetDataItemStrings(GhasedakEnvelope envelope, string collectionProp, string itemProp)
        {
            var data = envelope.Data;
            if (data == null || data.Value.ValueKind != JsonValueKind.Object ||
                !data.Value.TryGetProperty(collectionProp, out var items) || items.ValueKind != JsonValueKind.Array)
                return null;

            var result = new string[items.GetArrayLength()];
            for (var i = 0; i < result.Length; i++)
            {
                var item = items[i];
                if (item.ValueKind != JsonValueKind.Object)
                    return null;

                var value = GetString(item, itemProp);
                if (string.IsNullOrWhiteSpace(value))
                    return null;
                result[i] = value!;
            }

            return result;
        }

        private static string? GetString(JsonElement element, string prop)
        {
            if (!element.TryGetProperty(prop, out var value))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null,
            };
        }

        /// <summary>
        /// Maps a Ghasedak delivery status code (0-8) to a
        /// <see cref="MessageDeliveryState"/>. Covers the complete official
        /// "status of sent messages" table; <c>7</c> is the provider's
        /// error-diagnosis mode and <c>8</c> is "unspecified", so both stay
        /// <see cref="MessageDeliveryState.Unknown"/> — they are not delivery outcomes.
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

    }
}
