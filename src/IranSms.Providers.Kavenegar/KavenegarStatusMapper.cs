namespace IranSms.Providers.Kavenegar
{
    /// <summary>
    /// Maps Kavenegar numeric status codes (see research doc §6) to the
    /// normalized <see cref="MessageDeliveryState"/>.
    /// </summary>
    internal static class KavenegarStatusMapper
    {
        /// <summary>
        /// Converts a raw Kavenegar status string to a delivery state.
        /// Unknown or malformed values map to <see cref="MessageDeliveryState.Unknown"/>.
        /// </summary>
        /// <param name="status">The raw status string.</param>
        /// <returns>The normalized delivery state.</returns>
        public static MessageDeliveryState ToDeliveryState(string status)
        {
            if (!int.TryParse(status, out var code))
                return MessageDeliveryState.Unknown;

            switch (code)
            {
                case 1:
                    return MessageDeliveryState.Queued;
                case 2:
                    return MessageDeliveryState.Scheduled;
                case 4:
                case 5:
                    return MessageDeliveryState.SentToOperator;
                case 6:
                    return MessageDeliveryState.Failed;
                case 10:
                    return MessageDeliveryState.Delivered;
                case 11:
                    return MessageDeliveryState.Undelivered;
                case 13:
                    return MessageDeliveryState.Cancelled;
                case 14:
                    return MessageDeliveryState.Blocked;
                case 100:
                    return MessageDeliveryState.Unknown;
                default:
                    return MessageDeliveryState.Unknown;
            }
        }
    }
}