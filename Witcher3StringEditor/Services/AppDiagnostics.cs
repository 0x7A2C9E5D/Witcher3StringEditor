using System.Runtime.InteropServices;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides application diagnostics functionality
/// </summary>
/// <param name="appSettings"></param>
/// <param name="cultureResolver"></param>
internal class AppDiagnostics(
    IAppSettings appSettings,
    ICultureResolver cultureResolver)
    : IAppDiagnostics
{
    /// <summary>
    ///     Logs information about the application startup
    /// </summary>
    public void LogStartupInfo()
    {
        LogApplicationInfo();
        LogEnvironmentInfo();
        LogLocalizationInfo();
    }

    /// <summary>
    ///     Logs information about the application
    /// </summary>
    private static void LogApplicationInfo()
    {
        Log.Information("Application started.");
        Log.Information("Application Version: {Version}", ThisAssembly.AssemblyFileVersion);
        Log.Information("Is Debug: {IsDebug}", !BuildInformation.IsReleaseBuild);
    }

    /// <summary>
    ///     Logs information about the environment
    /// </summary>
    private static void LogEnvironmentInfo()
    {
        Log.Information("OS Version: {Version}",
            $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Log.Information(".Net Runtime: {Runtime}", RuntimeInformation.FrameworkDescription);
        Log.Information("Current Directory: {Directory}", Environment.CurrentDirectory);
        Log.Information("AppData Folder: {Folder}", AppPaths.AppDataDirectory);
    }

    /// <summary>
    ///     Logs information about the localization
    /// </summary>
    private void LogLocalizationInfo()
    {
        var supportedCultures = cultureResolver.SupportedCultures;
        Log.Information("Installed Language Packs: {Languages}",
            string.Join(", ", supportedCultures.Select(x => x.Name)));
        Log.Information("Current Language: {Language}", appSettings.Language);
    }
}