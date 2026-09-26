
namespace IranSms
{
    /// <summary>
    /// Template-based OTP request: the provider holds a pre-approved text pattern and the
    /// library fills its placeholders. Used by Kavenegar (<c>verify/lookup</c> with
    /// <c>template</c> + <c>token</c>), Ghasedak (<c>SendOtpWithParams</c> with
    /// <c>templateName</c> + <c>param1..param10</c>) and SMS.ir (<c>send/verify</c> with
    /// <c>templateId</c> + <c>parameters</c>).
    /// </summary>
    /// <remarks>
    /// Parameter names are provider-specific and are not normalized by the core:
    /// Kavenegar expects <c>token</c>/<c>token2</c>/<c>token3</c>/<c>token10</c>/<c>token20</c>,
    /// Ghasedak expects <c>param1</c>..<c>param10</c>, and SMS.ir accepts arbitrary names
    /// defined by the registered template.
    /// </remarks>
    public sealed class OtpTemplateRequest : OtpRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OtpTemplateRequest"/> class.
        /// </summary>
        /// <param name="templateId">
        /// Provider template identifier: template name (Kavenegar, Ghasedak) or the numeric
        /// template id (SMS.ir).
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="templateId"/> is null or whitespace.</exception>
        public OtpTemplateRequest(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("Template id is required.", nameof(templateId));

            TemplateId = templateId;
        }

        /// <summary>Provider template identifier (template name for Kavenegar/Ghasedak, numeric id for SMS.ir).</summary>
        public string TemplateId { get; }

        /// <summary>Template parameters; null when the template has no placeholders.</summary>
        public IReadOnlyDictionary<string, string>? Parameters { get; set; }

        /// <summary>
        /// Adds or replaces a single template parameter and returns the same instance.
        /// </summary>
        /// <param name="name">Provider-specific parameter name.</param>
        /// <param name="value">Parameter value; must not be null or whitespace.</param>
        /// <returns>The same request instance for chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="value"/> is null or whitespace.</exception>
        public OtpTemplateRequest SetParameter(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Parameter name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Parameter value is required.", nameof(value));

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Parameters is not null)
            {
                foreach (var pair in Parameters)
                    parameters[pair.Key] = pair.Value;
            }

            parameters[name] = value;
            Parameters = parameters;
            return this;
        }
    }
}
