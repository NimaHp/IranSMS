namespace IranSms.Providers.Mock
{
    /// <summary>
    /// Options for the Mock provider registration. No credentials required.
    /// </summary>
    public sealed class MockSmsOptions : SmsClientOptions
    {
        /// <summary>Optional display name (default "Mock").</summary>
        public string? ProviderName { get; set; }
    }
}