namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for The Witcher 3 string items
///     Represents a single string entry from The Witcher 3 game files with its metadata and text content
/// </summary>
public interface IW3StringItem
{
    /// <summary>
    ///     Gets or sets the string identifier
    ///     A unique identifier for the string within The Witcher 3 system
    /// </summary>
    public string StrId { get; set; }

    /// <summary>
    ///     Gets or sets the hexadecimal key
    ///     The hexadecimal representation of the string's key
    /// </summary>
    public string KeyHex { get; set; }

    /// <summary>
    ///     Gets or sets the key name
    ///     A readable name for the string key
    /// </summary>
    public string KeyName { get; set; }

    /// <summary>
    ///     Gets or sets the original text
    ///     The original text of the string before any modifications or translations
    /// </summary>
    public string OldText { get; set; }

    /// <summary>
    ///     Gets or sets the current text
    ///     The current text of the string, which may be the original text or a translated/modified version
    /// </summary>
    public string Text { get; set; }
}