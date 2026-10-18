using System.Text;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The hash of one localization key: the key it was computed from, and the hash the game resolves it by
/// </summary>
/// <remarks>
///     <para>
///         A container does not store localization keys, it stores their hashes: the name of a key is only
///         there while the item carrying it is being written, so the hash is what the key block is searched
///         by and what a container can give back.
///     </para>
///     <para>
///         The hash is computed from the name in lowercase, over its UTF-16 code units, by multiplying the
///         running value by 31 and adding one code unit at a time, wrapping as it goes: two names that differ
///         only in case are one key to the game, and a name no key was ever written for still has a hash.
///         The name is kept next to the hash it produced, because a hash on its own no longer says which key
///         it belongs to, and the two are what the entry carries once it is written.
///     </para>
/// </remarks>
internal sealed class LocalizationKeyHash
{
    /// <summary>
    ///     Holds the key a hash was computed from, and the hash it produced
    /// </summary>
    /// <param name="hash">The hash the key resolves to</param>
    private LocalizationKeyHash(uint hash)
    {
        Hash = hash;
    }

    /// <summary>
    ///     The hash the key resolves to, which keys the entries of a container
    /// </summary>
    public uint Hash { get; }

    /// <summary>
    ///     Computes the hash of a localization key
    /// </summary>
    /// <param name="key">The localization key to hash</param>
    /// <returns>The hash of the key, with the key it was computed from</returns>
    /// <exception cref="ArgumentNullException">Thrown when the key is null</exception>
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