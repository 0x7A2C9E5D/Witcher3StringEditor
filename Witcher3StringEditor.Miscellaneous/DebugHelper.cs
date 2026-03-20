namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     A helper class for debugging.
/// </summary>
public static class DebugHelper
{
#if DEBUG
    /// <summary>
    ///     Indicates whether the application is running in debug mode.
    /// </summary>
    public static bool IsDebug => true; // Debug
#else
    public static bool IsDebug => false; // Release
#endif
}