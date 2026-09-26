namespace IranSms.Providers.SmsIr
{
    /// <summary>
    /// Maps SMS.ir delivery-state values to the normalized
    /// <see cref="MessageDeliveryState"/>.
    /// </summary>
    internal static class SmsIrStatusMapper
    {
        /// <summary>
        /// Converts a raw SMS.ir delivery state to a delivery state, following the official
        /// table of <c>deliveryState</c> codes (1-7). Undocumented values — including a
        /// non-null <c>0</c>, which SMS.ir never documents — and <c>null</c> (the
        /// provider's own "not known yet") map to
        /// <see cref="MessageDeliveryState.Unknown"/>.
        /// </summary>
        /// <param name="status">The raw delivery-state value.</param>
        /// <returns>The normalized delivery state.</returns>
        /// <remarks>
        /// The provider distinguishes two failure points: <c>2</c> is "did not reach the
        /// handset" while <c>4</c> is "did not reach the operator" — the message never left
        /// SMS.ir. Codes 3 and 5 both mean the operator has the message (3 while it is
        /// being processed, 5 once it is handed over).
        /// </remarks>
        public static MessageDeliveryState ToDeliveryState(byte? status)
        {
            switch (status)
            {
                case 1:                                  // رسیده به گوشی — reached the handset
                    return MessageDeliveryState.Delivered;
                case 2:                                  // نرسیده به گوشی — did not reach the handset
                    return MessageDeliveryState.Undelivered;
                case 3:                                  // پردازش در مخابرات — being processed at the operator
                case 5:                                  // رسیده به مخابرات — handed to the operator
                    return MessageDeliveryState.SentToOperator;
                case 4:                                  // نرسیده به مخابرات — never reached the operator
                case 6:                                  // خطا — error
                    return MessageDeliveryState.Failed;
                case 7:                                  // لیست سیاه — blacklist
                    return MessageDeliveryState.Blocked;
                default:
                    return MessageDeliveryState.Unknown;
            }
        }
    }
}
