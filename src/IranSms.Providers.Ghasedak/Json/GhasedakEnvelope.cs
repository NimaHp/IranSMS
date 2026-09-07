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
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var env = new GhasedakEnvelope
                {
                    IsSuccess = root.TryGetProperty("IsSuccess", out var s) && s.ValueKind == JsonValueKind.True,
                    StatusCode = ReadStatusCode(root),
                    Message = root.TryGetProperty("Message", out var m) && m.ValueKind == JsonValueKind.String
                        ? m.GetString()
                        : null,
                };
                if (root.TryGetProperty("Data", out var data) && data.ValueKind != JsonValueKind.Null)
                {
                    env.Data = data.Clone();
                }

                return env;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static int ReadStatusCode(JsonElement root)
        {
            if (root.TryGetProperty("StatusCode", out var sc))
            {
                if (sc.ValueKind == JsonValueKind.Number && sc.TryGetInt32(out var i))
                    return i;
                if (sc.ValueKind == JsonValueKind.String && int.TryParse(sc.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }

            return 0;
        }
    }
}
