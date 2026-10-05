using System.Text;
using Witcher3StringEditor.Serializers.W3Strings;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     The payload a w3strings container is written to store: the texts of a container are either UTF-8,
///     which the Remastered game reads, or UTF-16LE, which the classic and the next-gen game read
/// </summary>
/// <remarks>
///     The two are told apart by the code page of the encoding the caller chose and by nothing else.
///     Several UTF-8 encodings differ only in whether they write a byte order mark, which a container
///     does not store: asking whether two encoding objects are equal would make the file that is written
///     depend on which of them a caller happened to build, and a save for one generation would quietly
///     write a container for the other
/// </remarks>
public static class W3StringsPayload
{
    /// <summary>
    ///     Answers whether the given encoding is the UTF-8 payload, the one the Remastered game reads
    /// </summary>
    /// <param name="encoding">The encoding the caller chose</param>
    /// <returns>True when the encoding stores its characters as UTF-8</returns>
    public static bool IsUtf8(Encoding encoding)
    {
        return encoding.CodePage == Encoding.UTF8.CodePage;
    }

    /// <summary>
    ///     Gets the version of the container that stores the given encoding
    /// </summary>
    /// <param name="encoding">The encoding the caller chose</param>
    /// <returns>
    ///     The version of the UTF-8 generation for UTF-8, and the version of the classic UTF-16LE
    ///     generation for every other encoding
    /// </returns>
    public static uint ContainerVersion(Encoding encoding)
    {
        return IsUtf8(encoding) ? W3StringsFormat.FirstUtf8Version : W3StringsFormat.Utf16LeVersion;
    }
}