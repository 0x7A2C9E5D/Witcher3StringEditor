using System.Text;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The hash of one localization key, which the game resolves key by.
/// </summary>
internal sealed class LocalizationKeyHash
{
    /// <summary>
    ///     Holds the hash a localization key name produced.
    /// </summary>
    /// <param name="hash">The hash the key resolves to.</param>
    private LocalizationKeyHash(uint hash)
    {
        Hash = hash;
    }

    /// <summary>
    ///     The hash the key resolves to, which keys the entries of a container.
    /// </summary>
    public uint Hash { get; }

    /// <summary>
    ///     Computes the hash of a localization key: the name in lowercase, over its UTF-16 code units,
    ///     multiplying by 31 and adding one code unit at a time.
    /// </summary>
    /// <param name="key">The localization key to hash.</param>
    /// <returns>The hash of the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the key is null.</exception>
    public static LocalizationKeyHash Of(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var lower = key.ToLowerInvariant();
        var utf16 = Encoding.Unicode.GetBytes(lower);
        uint hash = 0;
        for (var i = 0; i < utf16.Length; i += 2)
        {
            var unit = (uint)(utf16[i] | (utf16[i + 1] << 8));
            hash = unchecked(hash * 31 + unit);
        }

        return new LocalizationKeyHash(hash);
    }
}