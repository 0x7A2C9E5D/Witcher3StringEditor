using System.Runtime.InteropServices;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides application diagnostics functionality
/// </summary>
/// <param name="appSettings">The application settings</param>
/// <param name="cultureResolver">The culture resolver used to report the installed language packs</param>
internal class AppDiagnostics(
    IAppSettings appSettings,
    ICultureResolver cultureResolver)
    : IAppDiagnostics
{
    /// <summary>
    ///     Logs information about the application startup
    ///     Diagnostics must never abort startup, so any failure degrades to a warning
    /// </summary>
    public void LogStartupInfo()
    {
        try
        {
            LogApplicationInfo();
            LogEnvironmentInfo();
            LogLocalizationInfo();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to collect startup diagnostics");
        }
    }

    /// <summary>
    ///     Logs information about the application
    /// </summary>
    private static void LogApplicationInfo()
    {
        Log.Information("Application started");
        // Report the same version string the UI shows so support logs and the about-dialog agree
        Log.Information("Application Version: {Version}", ThisAssembly.AssemblyInformationalVersion);
        Log.Information("Is Debug: {IsDebug}", DebugHelper.IsDebug);
    }

    /// <summary>
    ///     Logs information about the environment
    ///     Paths are masked because log files are commonly attached to public bug reports
    /// </summary>
    private static void LogEnvironmentInfo()
    {
        Log.Information("OS Version: {Version}",
            $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Log.Information(".Net Runtime: {Runtime}", RuntimeInformation.FrameworkDescription);
        Log.Information("Current Directory: {Directory}", MaskUserProfile(Environment.CurrentDirectory));
        Log.Information("AppData Folder: {Folder}", MaskUserProfile(AppPaths.AppDataDirectory));
    }

    /// <summary>
    ///     Logs information about the localization
    /// </summary>
    private void LogLocalizationInfo()
    {
        var supportedCultures = cultureResolver.SupportedCultures;
        Log.Information("Installed Language Packs: {Languages}",
            supportedCultures is { Count: > 0 }
                ? string.Join(", ", supportedCultures.Select(x => x.Name))
                : "(none)");
        Log.Information("Current Language: {Language}", appSettings.Language);
    }

    /// <summary>
    ///     Replaces the user profile root inside a path with a placeholder so the Windows username is not logged
    /// </summary>
    /// <param name="path">The path to mask</param>
    /// <returns>The path with the user profile root replaced by <c>%USERPROFILE%</c></returns>
    private static string MaskUserProfile(string path)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrEmpty(userProfile)
            ? path
            : path.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
    }
}