namespace Witcher3StringEditor.W3Strings;

public static class W3Language
{
    private static readonly Dictionary<string, uint> Keys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pl"] = 0x83496237,
            ["en"] = 0x43975139,
            ["de"] = 0x75886138,
            ["it"] = 0x45931894,
            ["fr"] = 0x23863176,
            ["cz"] = 0x24987354,
            ["es"] = 0x18796651,
            ["zh"] = 0x18632176,
            ["ru"] = 0x63481486,
            ["hu"] = 0x42378932,
            ["jp"] = 0x54834893,
            ["ar"] = 0x00000000,
            ["br"] = 0x00000000,
            ["esMX"] = 0x00000000,
            ["kr"] = 0x00000000,
            ["tr"] = 0x00000000,
            ["ua"] = 0x00000000,
            ["cn"] = 0x00000000
        };

    private static readonly Dictionary<string, uint> Magics =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pl"] = 0x73946816,
            ["en"] = 0x79321793,
            ["de"] = 0x42791159,
            ["it"] = 0x12375973,
            ["fr"] = 0x75921975,
            ["cz"] = 0x21793217,
            ["es"] = 0x42387566,
            ["zh"] = 0x16875467,
            ["ru"] = 0x42386347,
            ["hu"] = 0x67823218,
            ["jp"] = 0x59825646,
            ["ar"] = 0x00000000,
            ["br"] = 0x00000000,
            ["esMX"] = 0x00000000,
            ["kr"] = 0x00000000,
            ["tr"] = 0x00000000,
            ["ua"] = 0x00000000,
            ["cn"] = 0x00000000
        };

    private static readonly Dictionary<uint, string> KeyToLanguage = BuildKeyLookup();
    private static readonly Dictionary<ushort, string> Key1ToLanguage = BuildKey1Lookup();

    private static Dictionary<uint, string> BuildKeyLookup()
    {
        // Only a key owned by exactly one language identifies that language: every
        // language added after the game's release shares key 0.
        return Keys.GroupBy(pair => pair.Value).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().Key);
    }

    private static Dictionary<ushort, string> BuildKey1Lookup()
    {
        // Same rule as the full key, applied to the high half of the key.
        return Keys.GroupBy(pair => (ushort)(pair.Value >> 16)).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().Key);
    }

    public static string? FromKey(uint key)
    {
        return KeyToLanguage.GetValueOrDefault(key);
    }

    public static string? FromKey1(ushort key1)
    {
        return Key1ToLanguage.GetValueOrDefault(key1);
    }

    public static uint KeyForLanguage(string language)
    {
        return Keys.TryGetValue(language, out var key)
            ? key
            : throw new ArgumentException($"unknown language: {language}", nameof(language));
    }

    public static uint MagicForLanguage(string language)
    {
        return Magics.TryGetValue(language, out var magic)
            ? magic
            : throw new ArgumentException($"unknown language: {language}", nameof(language));
    }
}