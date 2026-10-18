namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     One key of a container: the hash of a localization key, and the id it resolves to.
/// </summary>
/// <remarks>
///     The hash is stored as it is; the id is held obfuscated until the container has been decoded.
/// </remarks>
internal sealed class W3KeyEntry
{
    /// <summary>
    ///     The hash of the localization key, which the key block is searched by.
    /// </summary>
    public uint KeyHash { get; init; }

    /// <summary>
    ///     The id of the string the key resolves to, as the block holds it until the container is decoded.
    /// </summary>
    public uint Id { get; init; }
}