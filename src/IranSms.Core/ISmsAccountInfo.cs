
namespace IranSms
{
    /// <summary>
    /// Optional capability: account credit/balance and sender-line queries.
    /// Advertised by providers with <see cref="SmsCapabilities.AccountInfo"/>.
    /// Providers may return a single default line, a full list, or an empty list
    /// when their API does not expose sender-line enumeration.
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
