namespace IranSms.Providers.Kavenegar
{
    /// <summary>
    /// Options for the Kavenegar provider registration.
    /// </summary>
    public sealed class KavenegarOptions : SmsClientOptions
    {
        /// <summary>Kavenegar API key.</summary>
        public string? ApiKey { get; set; }
    }
}