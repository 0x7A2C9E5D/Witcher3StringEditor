using System.Text;

namespace Witcher3StringEditor.W3Strings.Primitives;

public static class LocalizationKeyHash
{
    public static uint Compute(string key)
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

        return hash;
    }
}