using System.Globalization;
using System.IO;
using System.Security;
using Serilog;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Provides culture resolution functionality for the application
///     Detects and resolves supported cultures based on the culture-specific resource directories that are
///     deployed next to the application
/// </summary>
public class CultureResolver : ICultureResolver
{
    /// <summary>
    ///     The culture used when the system culture is not supported
    /// </summary>
    private static readonly CultureInfo FallbackCulture = CultureInfo.GetCultureInfo("en");

    /// <summary>
    ///     The injected matcher used to select the most suitable supported culture
    /// </summary>
    private readonly ICultureMatcher matcher;

    /// <summary>
    ///     Initializes a new instance of the CultureResolver class
    ///     Scans the application directory for culture-specific resource folders
    ///     and builds a list of supported cultures
    /// </summary>
    /// <param name="cultureMatcher">The matcher used to pick the best supported culture</param>
    public CultureResolver(ICultureMatcher cultureMatcher)
    {
        matcher = cultureMatcher; // Inject the ICultureMatcher instance
        // Initialize the list of supported cultures with English as the default culture
        var supportedCultures = new List<CultureInfo> { FallbackCulture };
        // Scan directories in the application's base directory to find additional supported cultures
        // Each directory name is treated as a culture name (e.g., "zh-CN", "ru-RU", etc.)
        var baseDirectory = AppContext.BaseDirectory;
        if (Directory.Exists(baseDirectory))
            foreach (var directory in GetDirectories(baseDirectory))
                try
                {
                    // Create a DirectoryInfo object to get the directory name
                    var directoryInfo = new DirectoryInfo(directory);

                    // Try to create a CultureInfo object from the directory name and add it to the list
                    // If the directory name is not a valid culture name, an exception will be thrown
                    supportedCultures.Add(CultureInfo.GetCultureInfo(directoryInfo.Name));
                }
                catch (CultureNotFoundException ex)
                {
                    // A directory that is not a culture name is expected; log it at debug level for diagnostics
                    // This prevents crashes when encountering non-culture directories
                    Log.Debug(ex, "Ignored a non-culture directory: {Directory}", directory);
                }

        // Assign the final list of supported cultures to the property. The list is de-duplicated by name because
        // a shipped "en" folder (or a case variant of it) would otherwise duplicate the seeded entry
        SupportedCultures =
        [
            .. supportedCultures
                .DistinctBy(culture => culture.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    /// <summary>
    ///     Gets the collection of cultures supported by the application
    ///     This is determined by the presence of culture-specific resource directories
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    ///     Resolves the most appropriate supported culture for the current system
    /// </summary>
    /// <returns>The best matching supported culture, or English as fallback</returns>
    public CultureInfo ResolveSupportedCulture()
    {
        var matches = matcher
            .Matches(CultureInfo.InstalledUICulture, SupportedCultures).ToList();
        return matches.Count != 0 ? matches[0] : FallbackCulture;    
    }

    /// <summary>
    ///     Enumerates the subdirectories of the specified directory, returning an empty sequence on failure
    /// </summary>
    /// <param name="baseDirectory">The directory to enumerate</param>
    /// <returns>The subdirectories, or an empty sequence when the directory cannot be read</returns>
    private static string[] GetDirectories(string baseDirectory)
    {
        try
        {
            return Directory.GetDirectories(baseDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            // The culture list degrades to the fallback culture instead of breaking the application startup
            Log.Warning(ex, "Failed to enumerate the culture directories in {Directory}", baseDirectory);
            return [];
        }
    }
}