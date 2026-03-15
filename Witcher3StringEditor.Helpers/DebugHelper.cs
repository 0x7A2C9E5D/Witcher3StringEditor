namespace Witcher3StringEditor.Helpers;

public static class DebugHelper
{
#if DEBUG
    public static bool IsDebug => true; // Debug
#else
    public static bool IsDebug => false; // Release
#endif
}