
namespace IranianSms
{
    /// <summary>
    /// Kind of identifier used to reference a previously sent message.
    /// </summary>
    public enum MessageIdentifierType
    {
        /// <summary>Provider-assigned message id (returned by the send call).</summary>
        ProviderMessageId = 0,

        /// <summary>Client-supplied reference id (localid / clientReferenceId).</summary>
        ClientReferenceId = 1,
    }

    /// <summary>
    /// Value object identifying a message for status lookups (netstandard2.0-safe,
    /// no record structs — value semantics via <see cref="IEquatable{T}"/>).
    /// </summary>
    public readonly struct MessageIdentifier : IEquatable<MessageIdentifier>
    {
        /// <summary>Gets the identifier value.</summary>
        public string Value { get; }

        /// <summary>Gets the kind of identifier (<see cref="MessageIdentifierType"/>).</summary>
        public MessageIdentifierType Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageIdentifier"/> struct.
        /// </summary>
        /// <param name="value">The identifier value.</param>
        /// <param name="type">The kind of identifier.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is null or whitespace.</exception>
        public MessageIdentifier(string value, MessageIdentifierType type)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Message identifier value cannot be null or whitespace.", nameof(value));

            Value = value;
            Type = type;
        }

        /// <summary>Indicates whether the current <see cref="MessageIdentifier"/> is equal to another.</summary>
        /// <param name="other">A <see cref="MessageIdentifier"/> to compare with this one.</param>
        /// <returns><see langword="true"/> if equal by value and type; otherwise <see langword="false"/>.</returns>
        public bool Equals(MessageIdentifier other)
            => Type == other.Type && string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <summary>Indicates whether this instance equals a specified object.</summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="MessageIdentifier"/> with the same value and type.</returns>
        public override bool Equals(object? obj) => obj is MessageIdentifier other && Equals(other);

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code equal for identifiers with the same value and type.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Type * 397) ^ (Value?.GetHashCode() ?? 0);
            }
        }

        /// <summary>Determines whether two <see cref="MessageIdentifier"/> instances are equal.</summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> if the identifiers are equal.</returns>
        public static bool operator ==(MessageIdentifier left, MessageIdentifier right) => left.Equals(right);

        /// <summary>Determines whether two <see cref="MessageIdentifier"/> instances are not equal.</summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><see langword="true"/> if the identifiers are not equal.</returns>
        public static bool operator !=(MessageIdentifier left, MessageIdentifier right) => !left.Equals(right);

        /// <summary>Returns a string representation of this identifier (e.g. <c>ProviderMessageId:42</c>).</summary>
        /// <returns>The identifier value prefixed by its type.</returns>
        public override string ToString() => $"{Type}:{Value}";
    }
}