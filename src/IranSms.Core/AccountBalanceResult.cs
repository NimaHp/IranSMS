
namespace IranSms
{
    /// <summary>
    /// Normalized account balance/credit payload.
    /// </summary>
    public sealed class AccountBalanceResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountBalanceResult"/> class.
        /// </summary>
        /// <param name="credit">Remaining credit as reported by the provider (provider-specific unit/currency).</param>
        public AccountBalanceResult(decimal credit)
        {
            Credit = credit;
        }

        /// <summary>Remaining credit (provider-specific unit; e.g. rial for Kavenegar/Ghasedak, decimal for SmsIr, double for Melipayamak).</summary>
        public decimal Credit { get; }

        /// <summary>Optional expiry / audit date returned by the provider (Kavenegar expiredate, Ghasedak ExpireDate); null when not supplied.</summary>
        public DateTimeOffset? ExpireDate { get; set; }

        /// <summary>Optional account type/plan label (Kavenegar type, Ghasedak Plan); null when not supplied.</summary>
        public string? AccountType { get; set; }
    }
}
