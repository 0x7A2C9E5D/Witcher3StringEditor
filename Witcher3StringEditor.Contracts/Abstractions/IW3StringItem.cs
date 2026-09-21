namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for The Witcher 3 string items
///     Represents a single string entry from The Witcher 3 game files with its metadata and text content
/// </summary>
/// <remarks>
///     <para>
///         Equality is deliberately not part of this contract. <c>StrId</c> alone is not a unique identity across
///         implementations (it can repeat for different keys), and each implementation keeps its own default equality
///         semantics — one implementation may be a record with value equality while another is reference-equal.
///     </para>
///     <para>
///         Consumers that need deterministic lookups across implementations must therefore use an explicit key
///         (for example <c>StrId</c> combined with <c>KeyHex</c>) or a dedicated comparer instead of relying on
///         <see cref="object.Equals(object)" /> / <see cref="object.GetHashCode" />.
///     </para>
///     <para>
///         Instances are mutable and are not thread-safe. The same cached instance may be read by the UI while a
///         background translation writes to it, so instances must be confined to a single thread at a time; a
///         read-only item contract is required for safe cross-layer sharing.
///     </para>
/// </remarks>
public interface IW3StringItem
{
    /// <summary>
    ///     Gets or sets the string identifier
    ///     A unique identifier for the string within The Witcher 3 system. Must never be null
    /// </summary>
    string StrId { get; set; }

    /// <summary>
    ///     Gets or sets the hexadecimal key
    ///     The hexadecimal representation of the string's key. Must never be null
    /// </summary>
    string KeyHex { get; set; }

    /// <summary>
    ///     Gets or sets the key name
    ///     A readable name for the string key. Must never be null
    /// </summary>
    string KeyName { get; set; }

    /// <summary>
    ///     Gets or sets the original text
    ///     The original text of the string before any modifications or translations. Must never be null
    /// </summary>
    string OldText { get; set; }

    /// <summary>
    ///     Gets or sets the current text
    ///     The current text of the string, which may be the original text or a translated/modified version.
    ///     Must never be null
    /// </summary>
    string Text { get; set; }
}