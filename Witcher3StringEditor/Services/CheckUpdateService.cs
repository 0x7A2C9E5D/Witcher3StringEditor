using System.Net.Http;
using CommunityToolkit.Diagnostics;
using Serilog;
using ZeroQL.Client;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides update checking functionality
///     Implements the ICheckUpdateService interface to handle checking for application updates
/// </summary>
internal class CheckUpdateService : ICheckUpdateService
{
    /// <summary>
    ///     The URL to check for updates
    /// </summary>
    private readonly Uri updateUrl = new("https://api.nexusmods.com/v2/graphql");

    /// <summary>
    ///     Checks if an update is available for the application
    /// </summary>
    /// <returns>True if an update is available, false otherwise or if an error occurred</returns>
    public async Task<bool> CheckUpdate()
    {
        try
        {
            Log.Information("Checking for updates..."); // Log start of update check
            var lastestVersion = await FetchLatestVersion(); // Fetch latest version
            var currentVersion = GetCurrentVersion(); // Get current version
            var isUpdateAvailable = lastestVersion > currentVersion; // Compare versions
            Log.Information(
                "Update check completed. Current version: {CurrentVersion}, Latest version: {LatestVersion}, Update available: {IsUpdateAvailable}",
                currentVersion, lastestVersion, isUpdateAvailable); // Log update check results
            return isUpdateAvailable; // Return update availability
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check for updates"); // Log any errors
            return false; // Return false on failure
        }
    }

    /// <summary>
    ///     Gets the current application version from assembly file version
    /// </summary>
    /// <returns>The current application version</returns>
    /// <exception cref="InvalidOperationException">Thrown when version parsing fails</exception>
    private static Version GetCurrentVersion()
    {
        Guard.IsTrue(Version.TryParse(ThisAssembly.AssemblyFileVersion,
            out var currentVersion)); // Parse current version
        Guard.IsNotNull(currentVersion); // Ensure current version is not null
        return currentVersion; // Return current version
    }

    /// <summary>
    ///     Fetches the latest version information from NexusMods API
    /// </summary>
    /// <returns>The latest available version</returns>
    private async Task<Version> FetchLatestVersion()
    {
        using var httpClient = new HttpClient(); // Create HTTP client
        httpClient.BaseAddress = updateUrl; // Set base address
        using var zeroQlClient = new ZeroQLClient(httpClient); // Create ZeroQL client
        var filter = new ModsFilter
        {
            Name =
            [
                new BaseFilterValueEqualsWildcard
                {
                    Op = new FilterComparisonOperatorEqualsWildcard(),
                    Value = "The Witcher3 String Editor NextGen"
                }
            ]
        }; // Create mods filter
        var result =
            (await zeroQlClient.Query(q => q.Mods<string[]>(filter: filter, selector: p => p.Nodes(m => m.Version))))
            .Data; // Query mods
        Guard.IsNotNull(result); // Ensure result is not null
        Guard.IsNotEmpty(result); // Ensure result is not empty
        Guard.IsTrue(Version.TryParse(result[0], out var latestVersion)); // Parse latest version
        Guard.IsNotNull(latestVersion); // Ensure latest version is not null
        return latestVersion; // Return latest version
    }
}