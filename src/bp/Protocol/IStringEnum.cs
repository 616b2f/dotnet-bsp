namespace BaseProtocol.Protocol
{
    /// <summary>
    /// Interface that describes a string-based enumeration.
    /// String-based enumerations are serialized simply as their <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    /// When implementing this interface, a constructor that takes a single string as parameters is required by
    /// <see cref="StringEnumConverter{TStringEnumType}"/>.
    /// </remarks>
    public interface IStringEnum
{
    /// <summary>
    /// Gets the value of the enumeration.
    /// </summary>
    string Value { get; }
}
}


