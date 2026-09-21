namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     A helper class for debugging.
/// </summary>
public static class DebugHelper
{
    /// <summary>
    ///     Indicates whether the application was built with the debug configuration
    /// </summary>
    /// <remarks>
    ///     This reflects the build configuration only — it is a compile time constant, not a runtime probe of an
    ///     attached debugger (see <see cref="System.Diagnostics.Debugger.IsAttached" />). It must not be used as a
    ///     runtime feature or permission gate, because it is always <c>false</c> in shipped builds
    /// </remarks>
#if DEBUG
    public static bool IsDebug => true; // Debug build
#else
    public static bool IsDebug => false; // Release build
#endif
}