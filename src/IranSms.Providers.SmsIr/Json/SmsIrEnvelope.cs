using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IranSms.Providers.SmsIr.Json
{
    /// <summary>
    /// The SMS.ir response envelope: <c>{ "status": 1, "message": "...", "data": ... }</c>.
    /// <c>Status</c> is 1 on success; <c>Data</c> is a typed model per endpoint.
    /// </summary>
    /// <typeparam name="TData">Type of the <c>data</c> payload for the endpoint.</typeparam>
    internal sealed class SmsIrResponse<TData>
        where TData : class
    {
        /// <summary>Gets or sets the API status code (1 = success).</summary>
        public int Status { get; set; }

        /// <summary>Gets or sets the human-readable status message.</summary>
        public string? Message { get; set; }

        /// <summary>Gets or sets the typed payload.</summary>
        public TData? Data { get; set; }
    }

    /// <summary>Bulk-send request body (POST <c>/v1/send/bulk</c>).</summary>
    internal sealed class SmsIrBulkSendRequest
    {
        /// <summary>The sender line (lineNumber).</summary>
        [JsonPropertyName("lineNumber")]
        public long LineNumber { get; set; }

        /// <summary>The message text (messageText).</summary>
        [JsonPropertyName("messageText")]
        public string? MessageText { get; set; }

        /// <summary>Recipient mobile numbers (mobiles).</summary>
        [JsonPropertyName("mobiles")]
        public string[]? Mobiles { get; set; }

        /// <summary>Optional scheduled send time in Unix seconds (sendDateTime).</summary>
        [JsonPropertyName("sendDateTime")]
        public long? SendDateTime { get; set; }
    }

    /// <summary>Bulk-send return data (packId, messageIds, cost).</summary>
    internal sealed class SmsIrBulkSendResult
    {
        /// <summary>Unique id of the send set (packId).</summary>
        [JsonPropertyName("packId")]
        public Guid? PackId { get; set; }

        /// <summary>Per-recipient message ids; null/0 values mark blacklisted or invalid numbers (messageIds).</summary>
        [JsonPropertyName("messageIds")]
        public long[]? MessageIds { get; set; }

        /// <summary>Credit consumed by the send set (cost).</summary>
        [JsonPropertyName("cost")]
        public decimal? Cost { get; set; }
    }

    /// <summary>Verify/OTP request body (POST <c>/v1/send/verify</c>).</summary>
    internal sealed class SmsIrVerifyRequest
    {
        /// <summary>Recipient mobile number (mobile).</summary>
        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        /// <summary>Template identifier registered in the panel (templateId).</summary>
        [JsonPropertyName("templateId")]
        public long TemplateId { get; set; }

        /// <summary>Template parameter replacements (parameters).</summary>
        [JsonPropertyName("parameters")]
        public SmsIrVerifyParameter[]? Parameters { get; set; }
    }

    /// <summary>A single template parameter (name, value).</summary>
    internal sealed class SmsIrVerifyParameter
    {
        /// <summary>The key defined in the template without surrounding <c>#</c> (name).</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>The replacement value (value).</summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    /// <summary>Verify/OTP return data (messageId, cost).</summary>
    internal sealed class SmsIrVerifyResult
    {
        /// <summary>Unique message id (messageId).</summary>
        [JsonPropertyName("messageId")]
        public long? MessageId { get; set; }

        /// <summary>Credit consumed by the send (cost).</summary>
        [JsonPropertyName("cost")]
        public decimal? Cost { get; set; }
    }

    /// <summary>
    /// Delivery-status return data (GET <c>/v1/send/{messageId}</c>).
    /// Times are Unix seconds.
    /// </summary>
    internal sealed class SmsIrSendStatusResult
    {
        /// <summary>Unique message id (messageId).</summary>
        [JsonPropertyName("messageId")]
        public long? MessageId { get; set; }

        /// <summary>Recipient mobile number as a Long (mobile).</summary>
        [JsonPropertyName("mobile")]
        public long? Mobile { get; set; }

        /// <summary>The message text (messageText).</summary>
        [JsonPropertyName("messageText")]
        public string? MessageText { get; set; }

        /// <summary>Send time in Unix seconds (sendDateTime).</summary>
        [JsonPropertyName("sendDateTime")]
        public long? SendDateTime { get; set; }

        /// <summary>The sender line (lineNumber).</summary>
        [JsonPropertyName("lineNumber")]
        public long? LineNumber { get; set; }

        /// <summary>Credit charged (cost).</summary>
        [JsonPropertyName("cost")]
        public decimal? Cost { get; set; }

        /// <summary>Delivery state code as a nullable byte (deliveryState).</summary>
        [JsonPropertyName("deliveryState")]
        public byte? DeliveryState { get; set; }

        /// <summary>Delivery time in Unix seconds (deliveryDateTime).</summary>
        [JsonPropertyName("deliveryDateTime")]
        public long? DeliveryDateTime { get; set; }
    }

    /// <summary>JSON serialization helpers for SMS.ir payloads (camelCase).</summary>
    internal static class SmsIrJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>Raw envelope for endpoints where data is a primitive/array (credit, line).</summary>
        internal sealed class RawEnvelope
        {
            public int Status { get; set; }
            public string? Message { get; set; }
            public JsonElement? DataElement { get; set; }
        }

        internal static RawEnvelope? DeserializeRaw(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var raw = new RawEnvelope();
                if (root.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.Number && st.TryGetInt32(out var s))
                    raw.Status = s;
                if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    raw.Message = msg.GetString();
                if (root.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
                    raw.DataElement = data.Clone();
                return raw;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal static decimal ExtractDecimal(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d))
                return d;
            if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                return p;
            return 0m;
        }

        /// <summary>Serializes a request payload to JSON (camelCase keys).</summary>
        public static string Serialize<T>(T payload)
            => JsonSerializer.Serialize(payload, Options);

        /// <summary>Deserializes an SMS.ir response envelope with a typed data payload.</summary>
        public static SmsIrResponse<T>? Deserialize<T>(string json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<SmsIrResponse<T>>(json, Options);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
