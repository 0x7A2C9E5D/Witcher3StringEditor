using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.Implementation;

/// <summary>
///     Represents The Witcher 3 string item with string-based properties
///     Implements the IW3StringItem interface to provide a concrete implementation for The Witcher 3 string data
///     This type is used internally for serialization and deserialization operations
/// </summary>
/// <remarks>
///     Deliberately a class instead of a record: the members are mutable, so synthesized value equality over all
///     five members would silently violate the hash/equality contract as soon as a value changes (for example when
///     a translation updates <see cref="Text" />)
/// </remarks>
internal class W3StringItem : IW3StringItem
{
    /// <summary>
    ///     Gets or sets the string ID of The Witcher 3 string item
    ///     This represents the unique identifier for the string in The Witcher 3 system
    /// </summary>
    public string StrId { get;set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the hexadecimal key of The Witcher 3 string item
    ///     This represents the hexadecimal representation of the string's key
    /// </summary>
    public string KeyHex { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the key name of The Witcher 3 string item
    ///     This represents the string key in a readable format
    /// </summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the old text of The Witcher 3 string item
    ///     This represents the original text before any modifications or translations
    /// </summary>
    public string OldText { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current text of The Witcher 3 string item
    ///     This represents the current text, which may be the original text or a translated/modified version
    /// </summary>
    public string Text { get; set; } = string.Empty;
}