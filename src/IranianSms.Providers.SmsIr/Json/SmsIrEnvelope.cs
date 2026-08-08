using System.Globalization;
using System.Text.Json;

namespace IranianSms.Providers.SmsIr.Json
{
    /// <summary>
    /// The SMS.ir response envelope:
    /// <c>{ "status": 1, "message": "...", "data": ... }</c>.
    /// </summary>
    internal sealed class SmsIrEnvelope
    {
        /// <summary>Gets or sets the API status code (1 = success).</summary>
        public int Status { get; set; }

        /// <summary>Gets or sets the human-readable status message.</summary>
        public string? Message { get; set; }

        /// <summary>Gets or sets the payload (object or array).</summary>
        public SmsIrData? Data { get; set; }
    }

    /// <summary>
    /// A single SMS.ir data object (send result or delivery status).
    /// Wraps the raw JSON element.
    /// </summary>
    internal sealed class SmsIrData
    {
        private readonly JsonElement _element;

        /// <summary>Initializes a new instance wrapping a JSON element.</summary>
        public SmsIrData(JsonElement element)
        {
            _element = element;
        }

        /// <summary>Gets a string property value, or null when absent/null.</summary>
        public string? GetNullableString(string name)
        {
            if (!_element.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return null;
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return prop.ToString();
        }

        /// <summary>Gets a decimal property, or null when absent/invalid.</summary>
        public decimal? GetNullableDecimal(string name)
        {
            if (!_element.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return null;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var d))
                return d;
            if (prop.ValueKind == JsonValueKind.String &&
                decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        /// <summary>Gets a Unix-seconds DateTimeOffset, or null when absent/invalid.</summary>
        public DateTimeOffset? GetNullableDateTimeOffset(string name, bool isUnix)
        {
            if (!_element.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return null;

            if (isUnix)
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var seconds))
                    return DateTimeOffset.FromUnixTimeSeconds(seconds);

                if (prop.ValueKind == JsonValueKind.String &&
                    long.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(parsed);
                }
            }
            else if (prop.ValueKind == JsonValueKind.String &&
                     DateTimeOffset.TryParse(prop.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }

            return null;
        }
    }

    /// <summary>Serialization helpers for SMS.ir JSON payloads.</summary>
    internal static class SmsIrJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>Serializes a payload object to JSON (camelCase keys).</summary>
        public static string Serialize(object payload)
            => JsonSerializer.Serialize(payload, Options);

        /// <summary>Deserializes the SMS.ir envelope from a JSON body.</summary>
        public static SmsIrEnvelope? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            using (var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                var envelope = new SmsIrEnvelope();

                if (root.TryGetProperty("status", out var statusProp))
                {
                    if (statusProp.ValueKind == JsonValueKind.Number && statusProp.TryGetInt32(out var status))
                        envelope.Status = status;
                    else if (statusProp.ValueKind == JsonValueKind.String &&
                             int.TryParse(statusProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        envelope.Status = parsed;
                }

                if (root.TryGetProperty("message", out var messageProp) &&
                    messageProp.ValueKind == JsonValueKind.String)
                {
                    envelope.Message = messageProp.GetString();
                }

                if (root.TryGetProperty("data", out var dataProp) &&
                    dataProp.ValueKind == JsonValueKind.Object)
                {
                    envelope.Data = new SmsIrData(dataProp.Clone());
                }
                else if (root.TryGetProperty("data", out dataProp) &&
                         dataProp.ValueKind == JsonValueKind.Array &&
                         dataProp.GetArrayLength() > 0)
                {
                    envelope.Data = new SmsIrData(dataProp[0].Clone());
                }

                return envelope;
            }
        }
    }
}