
namespace IranSms
{
    /// <summary>
    /// Aggregate result of a bulk or heterogeneous send: every recipient outcome plus
    /// batch-level counters. <see cref="Items"/> always holds one entry per requested
    /// recipient, so partial failures stay observable.
    /// </summary>
    public sealed class SmsBulkSendResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsBulkSendResult"/> class.
        /// </summary>
        /// <param name="items">Per-recipient outcomes, in request order.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
        public SmsBulkSendResult(IEnumerable<SmsSendItemResult> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            var list = new List<SmsSendItemResult>();
            foreach (var item in items)
            {
                if (item is null)
                    throw new ArgumentException("Items cannot contain null entries.", nameof(items));
                list.Add(item);
            }

            if (list.Count == 0)
                throw new ArgumentException("At least one send result is required.", nameof(items));

            Items = list;
        }

        /// <summary>Per-recipient outcomes, in request order.</summary>
        public IReadOnlyList<SmsSendItemResult> Items { get; }

        /// <summary>Number of recipients the provider accepted.</summary>
        public int SucceededCount
        {
            get
            {
                var count = 0;
                foreach (var item in Items)
                {
                    if (item.Succeeded)
                        count++;
                }

                return count;
            }
        }

        /// <summary>Number of recipients the provider rejected.</summary>
        public int FailedCount => Items.Count - SucceededCount;

        /// <summary>Indicates every recipient was accepted.</summary>
        public bool AllSucceeded => FailedCount == 0;

        /// <summary>
        /// Indicates some recipients were accepted and some rejected — the partial-failure case.
        /// </summary>
        public bool IsPartialFailure => FailedCount > 0 && SucceededCount > 0;

        /// <summary>Indicates every recipient was rejected.</summary>
        public bool AllFailed => SucceededCount == 0;

        /// <summary>Total reported cost of the accepted sends, when the provider reports cost.</summary>
        public decimal? TotalCost { get; set; }
    }
}
