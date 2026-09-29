using JetBrains.Annotations;

namespace Witcher3StringEditor.W3Strings;

[PublicAPI]
public sealed class W3Language
{
    public static readonly W3Language Ar = new("ar", "ar", 0x00000000, 0x00000000);

    public static readonly W3Language Br = new("br", "pt", 0x00000000, 0x00000000);

    public static readonly W3Language Cn = new("cn", "zh-Hans", 0x00000000, 0x00000000);

    public static readonly W3Language Cz = new("cz", "cs", 0x24987354, 0x21793217);

    public static readonly W3Language De = new("de", "de", 0x75886138, 0x42791159);

    public static readonly W3Language En = new("en", "en", 0x43975139, 0x79321793);

    public static readonly W3Language Es = new("es", "es", 0x18796651, 0x42387566);

    public static readonly W3Language Esmx = new("esMX", "es-MX", 0x00000000, 0x00000000);

    public static readonly W3Language Fr = new("fr", "fr", 0x23863176, 0x75921975);

    public static readonly W3Language Hu = new("hu", "hu", 0x42378932, 0x67823218);

    public static readonly W3Language It = new("it", "it", 0x45931894, 0x12375973);

    public static readonly W3Language Jp = new("jp", "ja", 0x54834893, 0x59825646);

    public static readonly W3Language Kr = new("kr", "ko", 0x00000000, 0x00000000);

    public static readonly W3Language Pl = new("pl", "pl", 0x83496237, 0x73946816);

    public static readonly W3Language Ru = new("ru", "ru", 0x63481486, 0x42386347);

    public static readonly W3Language Zh = new("zh", "zh-Hant", 0x18632176, 0x16875467);

    public static readonly W3Language Tr = new("tr", "tr", 0x00000000, 0x00000000);

    public static readonly W3Language Ua = new("ua", "uk", 0x00000000, 0x00000000);

    // Built on first use: the field initializers run in declaration order, and All is one of
    // them, so the language instances may not all exist while they are still running.
    private static readonly Lazy<Dictionary<uint, W3Language>> KeyToLanguage = new(BuildKeyLookup);
    private static readonly Lazy<Dictionary<ushort, W3Language>> Key1ToLanguage = new(BuildKey1Lookup);
    private static readonly Lazy<Dictionary<string, W3Language>> CodeToLanguage = new(BuildCodeLookup);

    private W3Language(string code, string cultureCode, uint key, uint magic)
    {
        Code = code;
        CultureCode = cultureCode;
        Key = key;
        Magic = magic;
    }

    public string Code { get; }

    public string CultureCode { get; }

    public uint Key { get; }

    public uint Magic { get; }

    public static IReadOnlyList<W3Language> All { get; } =
        [Ar, Br, Cn, Cz, De, En, Es, Esmx, Fr, Hu, It, Jp, Kr, Pl, Ru, Zh, Tr, Ua];

    private static Dictionary<uint, W3Language> BuildKeyLookup()
    {
        // Only a key owned by exactly one language identifies that language: every
        // language added after the game's release shares key 0.
        return All.GroupBy(language => language.Key).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static Dictionary<ushort, W3Language> BuildKey1Lookup()
    {
        // Same rule as the full key, applied to the high half of the key.
        return All.GroupBy(language => (ushort)(language.Key >> 16)).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static Dictionary<string, W3Language> BuildCodeLookup()
    {
        var map = new Dictionary<string, W3Language>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in All)
        {
            map[language.Code] = language;
            map[language.CultureCode] = language;
        }

        return map;
    }

    public static W3Language? FromKey(uint key)
    {
        return KeyToLanguage.Value.GetValueOrDefault(key);
    }

    public static W3Language? FromKey1(ushort key1)
    {
        return Key1ToLanguage.Value.GetValueOrDefault(key1);
    }

    public static W3Language? FromCode(string? code)
    {
        return code is not null && CodeToLanguage.Value.TryGetValue(code, out var language) ? language : null;
    }

    public override string ToString()
    {
        return Code;
    }
}