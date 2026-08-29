namespace IranSms.Providers.SmsIr
{
    /// <summary>
    /// Maps SMS.ir delivery-state strings to the normalized
    /// <see cref="MessageDeliveryState"/>.
    /// </summary>
    internal static class SmsIrStatusMapper
    {
        /// <summary>
        /// Converts a raw SMS.ir delivery status byte to a delivery state.
        /// Unknown or out-of-range values map to <see cref="MessageDeliveryState.Unknown"/>.
        /// </summary>
        /// <param name="status">The raw delivery-state byte (1-7 per the official docs).</param>
        /// <returns>The normalized delivery state.</returns>
        public static MessageDeliveryState ToDeliveryState(byte? status)
        {
            switch (status)
            {
                // Official SMS.ir delivery codes:
                //   1 = delivered to device, 2 = not delivered,
                //   3 = processing in telecom, 4 = not reached telecom,
                //   5 = reached telecom, 6 = error, 7 = blacklist.
                case 1:
                    return MessageDeliveryState.Delivered;
                case 2:
                case 4:
                    return MessageDeliveryState.Undelivered;
                case 3:
                case 5:
                    return MessageDeliveryState.SentToOperator;
                case 6:
                    return MessageDeliveryState.Failed;
                case 7:
                    return MessageDeliveryState.Blocked;
                case 0:
                    return MessageDeliveryState.Queued;
                default:
                    return MessageDeliveryState.Unknown;
            }
        }
    }
}
