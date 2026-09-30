using JetBrains.Annotations;

namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     A language a container can be written for
///     Every language carries the key a container stores in two halves and the magic its strings are
///     obfuscated with, both of which come from the game's own content files
/// </summary>
[PublicAPI]
public sealed class W3Language
{
    private W3Language(string code, string cultureCode, uint key, uint magic)
    {
        Code = code;
        CultureCode = cultureCode;
        Key = key;
        Magic = magic;
    }

    /// <summary>
    ///     The code the game's content files are named after, for example "cn"
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     The culture code that code stands for, for example "zh-Hans"
    /// </summary>
    public string CultureCode { get; }

    /// <summary>
    ///     The full 32-bit language key a container stores for the language
    /// </summary>
    public uint Key { get; }

    /// <summary>
    ///     The magic the strings and ids of the language are obfuscated with
    /// </summary>
    public uint Magic { get; }

    /// <summary>
    ///     Every language this build knows
    /// </summary>
    /// <remarks>
    ///     Every language the game added after its release shares key 0 and magic 0, which is why a zero
    ///     key identifies no language at all and the containers of those languages are stored without
    ///     obfuscation. They are listed as the languages they are
    /// </remarks>
    public static IReadOnlyList<W3Language> All { get; } =
    [
        new(code: "ar", cultureCode: "ar", key: 0x00000000, magic: 0x00000000),
        new(code: "br", cultureCode: "pt", key: 0x00000000, magic: 0x00000000),
        new(code: "cn", cultureCode: "zh-Hans", key: 0x00000000, magic: 0x00000000),
        new(code: "cz", cultureCode: "cs", key: 0x24987354, magic: 0x21793217),
        new(code: "de", cultureCode: "de", key: 0x75886138, magic: 0x42791159),
        new(code: "en", cultureCode: "en", key: 0x43975139, magic: 0x79321793),
        new(code: "es", cultureCode: "es", key: 0x18796651, magic: 0x42387566),
        new(code: "esMX", cultureCode: "es-MX", key: 0x00000000, magic: 0x00000000),
        new(code: "fr", cultureCode: "fr", key: 0x23863176, magic: 0x75921975),
        new(code: "hu", cultureCode: "hu", key: 0x42378932, magic: 0x67823218),
        new(code: "it", cultureCode: "it", key: 0x45931894, magic: 0x12375973),
        new(code: "jp", cultureCode: "ja", key: 0x54834893, magic: 0x59825646),
        new(code: "kr", cultureCode: "ko", key: 0x00000000, magic: 0x00000000),
        new(code: "pl", cultureCode: "pl", key: 0x83496237, magic: 0x73946816),
        new(code: "ru", cultureCode: "ru", key: 0x63481486, magic: 0x42386347),
        new(code: "zh", cultureCode: "zh-Hant", key: 0x18632176, magic: 0x16875467),
        new(code: "tr", cultureCode: "tr", key: 0x00000000, magic: 0x00000000),
        new(code: "ua", cultureCode: "uk", key: 0x00000000, magic: 0x00000000)
    ];

    /// <summary>
    ///     The language everything that cannot name one falls back to
    /// </summary>
    public static W3Language Default => FromCode("en")!;

    /// <summary>
    ///     Gets the language that owns the given full key
    /// </summary>
    /// <param name="key">The full 32-bit language key, both halves together</param>
    /// <returns>The language, or null when no language owns the key</returns>
    /// <remarks>
    ///     Only a key owned by exactly one language identifies that language: every language the game
    ///     added after its release shares key 0, so a zero key names none of them
    /// </remarks>
    public static W3Language? FromKey(uint key)
    {
        // Two are taken so that a key with a rival is recognized as one that names neither.
        var owned = All.Where(language => language.Key == key).Take(2).ToArray();
        return owned.Length == 1 ? owned[0] : null;
    }

    /// <summary>
    ///     Gets the language the given code or culture code names
    /// </summary>
    /// <param name="code">The code of the language, or its culture code</param>
    /// <returns>The language, or null when no language answers to the code</returns>
    public static W3Language? FromCode(string? code)
    {
        return All.FirstOrDefault(language =>
            string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.CultureCode, code, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString()
    {
        return Code;
    }
}
