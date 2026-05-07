using System.Runtime.InteropServices;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Services;

internal class AppDiagnostics(
    IAppSettings appSettings,
    ICultureResolver cultureResolver)
    : IAppDiagnostics
{
    public void LogStartupInfo()
    {
        LogApplicationInfo();
        LogEnvironmentInfo();
        LogLocalizationInfo();
    }

    private static void LogApplicationInfo()
    {
        Log.Information("Application started.");
        Log.Information("Application Version: {Version}", ThisAssembly.AssemblyFileVersion);
        Log.Information("Is Debug: {IsDebug}", !BuildInformation.IsReleaseBuild);
    }

    private static void LogEnvironmentInfo()
    {
        Log.Information("OS Version: {Version}",
            $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Log.Information(".Net Runtime: {Runtime}", RuntimeInformation.FrameworkDescription);
        Log.Information("Current Directory: {Directory}", Environment.CurrentDirectory);
        Log.Information("AppData Folder: {Folder}", AppPaths.AppDataDirectory);
    }

    private void LogLocalizationInfo()
    {
        var supportedCultures = cultureResolver.SupportedCultures;
        Log.Information("Installed Language Packs: {Languages}",
            string.Join(", ", supportedCultures.Select(x => x.Name)));
        Log.Information("Current Language: {Language}", appSettings.Language);
    }
}