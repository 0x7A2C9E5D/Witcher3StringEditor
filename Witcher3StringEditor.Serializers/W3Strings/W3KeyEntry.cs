namespace Witcher3StringEditor.Serializers.W3Strings;

internal sealed class W3KeyEntry
{
    /// <summary>
    ///     The hash of the localization key, which is what the key block is searched by
    /// </summary>
    public uint KeyHash { get; init; }

    /// <summary>
    ///     The id of the string the key resolves to, which an entry of the string block is stored under
    /// </summary>
    public uint Id { get; init; }
}