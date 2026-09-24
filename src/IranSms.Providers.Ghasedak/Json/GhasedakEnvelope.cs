using System.Globalization;
using System.Text.Json;

namespace IranSms.Providers.Ghasedak.Json
{
    /// <summary>
    /// The Ghasedak API response envelope:
    /// <c>{ "Data": ..., "IsSuccess": bool, "StatusCode": int, "Message": string }</c>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1507:Use nameof to express symbol names", Justification = "JSON wire names are protocol constants, not symbols.")]
    internal sealed class GhasedakEnvelope
    {
        /// <summary>Raw payload element (object or array).</summary>
        public JsonElement? Data { get; set; }

        /// <summary>Whether the request succeeded.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>HTTP-ish status code (200 = ok).</summary>
        public int StatusCode { get; set; }

        /// <summary>Human message (usually Persian).</summary>
        public string? Message { get; set; }

        /// <summary>Deserializes the envelope from a JSON body. Returns null on malformed JSON (caller wraps as IranSmsException).</summary>
        public static GhasedakEnvelope? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return null;
                if (!root.TryGetProperty("IsSuccess", out var success) ||
                    (success.ValueKind != JsonValueKind.True && success.ValueKind != JsonValueKind.False))
                    return null;
                if (!TryReadStatusCode(root, out var statusCode))
                    return null;
                if (root.TryGetProperty("Message", out var message) &&
                    message.ValueKind != JsonValueKind.String && message.ValueKind != JsonValueKind.Null)
                    return null;

                var env = new GhasedakEnvelope
                {
                    IsSuccess = success.GetBoolean(),
                    StatusCode = statusCode,
                    Message = message.ValueKind == JsonValueKind.String ? message.GetString() : null,
                };
                if (root.TryGetProperty("Data", out var data) && data.ValueKind != JsonValueKind.Null)
                    env.Data = data.Clone();

                return env;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool TryReadStatusCode(JsonElement root, out int statusCode)
        {
            if (root.TryGetProperty("StatusCode", out var value))
            {
                if (value.ValueKind == JsonValueKind.Number)
                    return value.TryGetInt32(out statusCode);
                if (value.ValueKind == JsonValueKind.String)
                    return int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out statusCode);
            }

            statusCode = 0;
            return false;
        }
    }
}
