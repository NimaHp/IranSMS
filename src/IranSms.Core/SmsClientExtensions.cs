
namespace IranSms
{
    /// <summary>
    /// Utility extensions for capability checks on <see cref="ISmsClient"/>.
    /// </summary>
    public static class SmsClientExtensions
    {
        /// <summary>
        /// Non-boxing capability check (netstandard2.0-safe; Do NOT use <c>HasFlag</c>).
        /// </summary>
        public static bool Supports(this ISmsClient client, SmsCapabilities capability)
        {
            return (client.Capabilities & capability) == capability;
        }
    }
}