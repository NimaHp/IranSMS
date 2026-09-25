
namespace IranSms
{
    /// <summary>
    /// Classification helpers for <see cref="MessageDeliveryState"/>. Every provider
    /// maps its own status codes onto the normalized enum; these helpers give callers a
    /// single vocabulary for polling, alerting and retrying.
    /// </summary>
    public static class MessageDeliveryStateExtensions
    {
        /// <summary>
        /// Determines whether the state is terminal — the provider will not report a
        /// different outcome for this message anymore.
        /// </summary>
        /// <param name="state">The normalized delivery state.</param>
        /// <returns><see langword="true"/> for terminal states; otherwise <see langword="false"/>.</returns>
        public static bool IsFinal(this MessageDeliveryState state)
        {
            switch (state)
            {
                case MessageDeliveryState.Delivered:
                case MessageDeliveryState.Failed:
                case MessageDeliveryState.Cancelled:
                case MessageDeliveryState.Blocked:
                case MessageDeliveryState.Undelivered:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the message reached the recipient device.
        /// </summary>
        /// <param name="state">The normalized delivery state.</param>
        /// <returns><see langword="true"/> only for <see cref="MessageDeliveryState.Delivered"/>.</returns>
        public static bool IsSuccessful(this MessageDeliveryState state)
            => state == MessageDeliveryState.Delivered;

        /// <summary>
        /// Determines whether the message ended in a non-successful terminal state.
        /// </summary>
        /// <param name="state">The normalized delivery state.</param>
        /// <returns><see langword="true"/> for failed, cancelled, blocked and undelivered states.</returns>
        public static bool IsFailure(this MessageDeliveryState state)
        {
            switch (state)
            {
                case MessageDeliveryState.Failed:
                case MessageDeliveryState.Cancelled:
                case MessageDeliveryState.Blocked:
                case MessageDeliveryState.Undelivered:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the message is still in flight and worth polling again.
        /// </summary>
        /// <param name="state">The normalized delivery state.</param>
        /// <returns><see langword="true"/> for queued, scheduled and sent-to-operator states.</returns>
        public static bool IsPending(this MessageDeliveryState state)
        {
            switch (state)
            {
                case MessageDeliveryState.Queued:
                case MessageDeliveryState.Scheduled:
                case MessageDeliveryState.SentToOperator:
                    return true;
                default:
                    return false;
            }
        }
    }
}
