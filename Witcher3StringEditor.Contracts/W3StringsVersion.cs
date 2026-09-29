namespace Witcher3StringEditor.Contracts;

/// <summary>
///     The container version written for <c>.w3strings</c> files.
///     The version field selects the payload encoding: values below 164 store UTF-16LE text whose offsets
///     and lengths are counted in two-byte units, 164 and above store UTF-8 text counted in bytes.
/// </summary>
public enum W3StringsVersion
{
    /// <summary>
    ///     Version 162
    ///     The classic UTF-16LE container, the value used by the containers shipped with the game.
    /// </summary>
    Classic = 162,

    /// <summary>
    ///     Version 163
    ///     The UTF-16LE container written by the legacy external encoder this application used to shell out to.
    ///     It is the same layout as <see cref="Classic"/> and differs only in the version field.
    ///     Kept so that settings written by older builds still deserialize; it is never offered as a choice
    ///     in the save dialog, where <see cref="Classic"/> takes its place.
    /// </summary>
    Legacy = 163,

    /// <summary>
    ///     Version 164
    ///     The UTF-8 container used by the remastered generation.
    /// </summary>
    Utf8 = 164
}
