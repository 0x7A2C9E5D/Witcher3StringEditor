namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     One key of a container: the hash of a localization key, and the id it resolves to
/// </summary>
/// <remarks>
///     This is the only definition of a key there is: a container holds these entries, and the stored form
///     (<see cref="W3StringsStorage" />) holds the very same ones rather than a second set. The hash is
///     stored as it is, but the id is obfuscated with the magic of the language, so it is held as the block
///     stores it until the container has been decoded: a reader decodes it into a new entry, and a writer
///     obfuscates the clear id as it lays the block out
/// </remarks>
internal sealed class W3KeyEntry
{
    /// <summary>
    ///     The hash of the localization key, which is what the key block is searched by
    /// </summary>
    public uint KeyHash { get; init; }

    /// <summary>
    ///     The id of the string the key resolves to, as the block holds it until the container is decoded
    /// </summary>
    public uint Id { get; init; }
}
