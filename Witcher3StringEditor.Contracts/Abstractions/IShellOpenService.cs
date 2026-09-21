namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for opening file system paths and URIs with the operating system's default handler
///     This covers both opening a folder in the system file explorer and opening a URL in the default browser
/// </summary>
public interface IShellOpenService
{
    /// <summary>
    ///     Opens the specified file system path or URI using the operating system's default handler
    /// </summary>
    /// <param name="path">The absolute file system path or the URI to open</param>
    /// <exception cref="ArgumentException"><paramref name="path" /> is null, empty or consists only of white-space</exception>
    /// <remarks>
    ///     The operation is best-effort: implementations throw only for invalid arguments and report any
    ///     failure to hand the target over to the operating system by logging. A successful call does not
    ///     guarantee that a handler was found or that the opened window was brought to the foreground.
    /// </remarks>
    void Open(string path);
}