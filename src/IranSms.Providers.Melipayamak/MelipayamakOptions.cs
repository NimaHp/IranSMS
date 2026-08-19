namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Options for the Melipayamak provider registration.
    /// </summary>
    public sealed class MelipayamakOptions : SmsClientOptions
    {
        /// <summary>Melipayamak panel username.</summary>
        public string? Username { get; set; }

        /// <summary>Melipayamak panel password (or ApiKey).</summary>
        public string? Password { get; set; }
    }
}