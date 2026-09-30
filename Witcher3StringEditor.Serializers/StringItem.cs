using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Represents The Witcher 3 string item with string-based properties
///     Implements the IStringItem interface to provide a concrete implementation for The Witcher 3 string data
///     This record is used internally for serialization and deserialization operations
/// </summary>
internal record StringItem : IStringItem
{
    /// <summary>
    ///     Initializes a new instance of the StringItem record
    ///     Creates an empty StringItem with default values
    /// </summary>
    public StringItem()
    {
    }

    /// <summary>
    ///     Gets or sets the string ID of The Witcher 3 string item
    ///     This represents the unique identifier for the string in The Witcher 3 system
    /// </summary>
    public string StrId { get; set; } = string.Empty;

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