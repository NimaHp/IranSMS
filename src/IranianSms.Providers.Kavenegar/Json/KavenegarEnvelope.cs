using System.Globalization;
using System.Text.Json;

namespace IranianSms.Providers.Kavenegar.Json
{
    /// <summary>
    /// The Kavenegar API response envelope:
    /// <c>{ "return": { "status": 200, "message": "..." }, "entries": [ ... ] }</c>.
    /// </summary>
    internal sealed class KavenegarEnvelope
    {
        /// <summary>Gets or sets the return/status section.</summary>
        public KavenegarReturn? Return { get; set; }

        /// <summary>Gets or sets the result entries.</summary>
        public List<KavenegarEntry>? Entries { get; set; }
    }

    /// <summary>The <c>return</c> object of a Kavenegar response.</summary>
    internal sealed class KavenegarReturn
    {
        /// <summary>Gets or sets the API status code (200 = success).</summary>
        public int Status { get; set; }

        /// <summary>Gets or sets the human-readable status message.</summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// A single Kavenegar result entry (send/status/... output object).
    /// Wraps the raw JSON element so arbitrary fields can be read lazily.
    /// </summary>
    internal sealed class KavenegarEntry
    {
        private readonly JsonElement _element;

        /// <summary>Initializes a new instance wrapping a JSON element.</summary>
        public KavenegarEntry(JsonElement element)
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

        /// <summary>Gets a string property value, or an empty string when absent.</summary>
        public string GetString(string name)
            => GetNullableString(name) ?? string.Empty;

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
                {
                    return DateTimeOffset.FromUnixTimeSeconds(seconds);
                }

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

    /// <summary>
    /// Serialization helpers for Kavenegar JSON payloads.
    /// </summary>
    internal static class KavenegarJson
    {
        /// <summary>
        /// Parses a Kavenegar response body into an envelope.
        /// The <c>entries</c> array may be absent or contain a single object,
        /// in which case it is normalized to a one-element list.
        /// </summary>
        /// <param name="json">The raw response body.</param>
        /// <returns>The parsed envelope, or null when the body is not valid JSON.</returns>
        public static KavenegarEnvelope? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            using (var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                var envelope = new KavenegarEnvelope();

                if (root.TryGetProperty("return", out var returnElement) &&
                    returnElement.ValueKind == JsonValueKind.Object)
                {
                    var ret = new KavenegarReturn();
                    if (returnElement.TryGetProperty("status", out var statusProp))
                    {
                        if (statusProp.ValueKind == JsonValueKind.Number && statusProp.TryGetInt32(out var status))
                            ret.Status = status;
                        else if (statusProp.ValueKind == JsonValueKind.String &&
                                 int.TryParse(statusProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStatus))
                            ret.Status = parsedStatus;
                    }

                    if (returnElement.TryGetProperty("message", out var messageProp) &&
                        messageProp.ValueKind == JsonValueKind.String)
                    {
                        ret.Message = messageProp.GetString();
                    }

                    envelope.Return = ret;
                }

                if (root.TryGetProperty("entries", out var entriesElement))
                {
                    var entries = new List<KavenegarEntry>();
                    if (entriesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in entriesElement.EnumerateArray())
                            entries.Add(new KavenegarEntry(item.Clone()));
                    }
                    else if (entriesElement.ValueKind == JsonValueKind.Object)
                    {
                        entries.Add(new KavenegarEntry(entriesElement.Clone()));
                    }

                    envelope.Entries = entries;
                }

                return envelope;
            }
        }
    }
}