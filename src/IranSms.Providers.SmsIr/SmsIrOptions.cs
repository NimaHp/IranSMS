namespace IranSms.Providers.SmsIr
{
    /// <summary>
    /// Options for the SMS.ir provider registration.
    /// </summary>
    public sealed class SmsIrOptions : SmsClientOptions
    {
        /// <summary>The SMS.ir X-API-KEY (private panel key).</summary>
        public string? ApiKey { get; set; }
    }
}