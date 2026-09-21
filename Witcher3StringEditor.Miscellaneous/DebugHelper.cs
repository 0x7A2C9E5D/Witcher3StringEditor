namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     A helper class for debugging.
/// </summary>
public static class DebugHelper
{
    /// <summary>
    ///     Indicates whether the application is running in debug mode.
    /// </summary>
#if DEBUG
    public static bool IsDebug => true; // Debug
#else
    public static bool IsDebug => false; // Release
#endif
}