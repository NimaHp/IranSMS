namespace IranianSms.Providers.SmsIr
{
    /// <summary>
    /// Maps SMS.ir delivery-state strings to the normalized
    /// <see cref="MessageDeliveryState"/>.
    /// </summary>
    internal static class SmsIrStatusMapper
    {
        /// <summary>
        /// Converts a raw SMS.ir delivery status string to a delivery state.
        /// Unknown or malformed values map to <see cref="MessageDeliveryState.Unknown"/>.
        /// </summary>
        /// <param name="status">The raw status string (e.g. <c>1</c>, <c>Delivered</c>).</param>
        /// <returns>The normalized delivery state.</returns>
        public static MessageDeliveryState ToDeliveryState(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return MessageDeliveryState.Unknown;

            switch (status!.Trim().ToLowerInvariant())
            {
                case "1":
                case "sent":
                    return MessageDeliveryState.SentToOperator;
                case "2":
                case "delivered":
                    return MessageDeliveryState.Delivered;
                case "3":
                case "failed":
                case "unsent":
                    return MessageDeliveryState.Failed;
                case "4":
                case "canceled":
                case "cancelled":
                    return MessageDeliveryState.Cancelled;
                case "0":
                case "pending":
                case "queued":
                    return MessageDeliveryState.Queued;
                default:
                    return MessageDeliveryState.Unknown;
            }
        }
    }
}