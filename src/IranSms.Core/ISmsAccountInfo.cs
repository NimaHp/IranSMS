
namespace IranSms
{
    /// <summary>
    /// Optional capability: account credit/balance and sender-line queries.
    /// Advertised by providers with <see cref="SmsCapabilities.AccountInfo"/>.
    /// All providers expose credit; sender lines are available on every provider
    /// but may be a single default line (Kavenegar/Ghasedak) or a full list (SmsIr).
    /// </summary>
    public interface ISmsAccountInfo
    {
        /// <summary>
        /// Gets the remaining credit/balance of the account.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<AccountBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the sender lines (numbers) available to the account.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlyList<string>> GetSenderLinesAsync(CancellationToken cancellationToken = default);
    }
}
