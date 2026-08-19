namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Options for the Ghasedak provider registration.
    /// </summary>
    public sealed class GhasedakOptions : SmsClientOptions
    {
        /// <summary>Ghasedak API key.</summary>
        public string? ApiKey { get; set; }
    }
}