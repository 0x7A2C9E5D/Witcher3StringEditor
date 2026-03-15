using System.Globalization;
using System.IO;
using System.Reflection;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Provides culture resolution functionality for the application
///     Detects and resolves supported cultures based on available resource directories
/// </summary>
public class CultureResolver : ICultureResolver
{
    /// <summary>
    ///      Defines a contract for culture resolution functionality
    /// </summary>
    private readonly ICultureMatcher matcher;
    
    /// <summary>
    ///     Initializes a new instance of the CultureResolver class
    ///     Scans the application directory for culture-specific resource folders
    ///     and builds a list of supported cultures
    /// </summary>
    public CultureResolver(ICultureMatcher cultureMatcher)
    {
        matcher = cultureMatcher; // Inject the ICultureMatcher instance 
        // Initialize the list of supported cultures with English as the default culture
        var supportedCultures = new List<CultureInfo> { new("en") };
        // Scan directories in the application's location to find additional supported cultures
        // Each directory name is treated as a culture name (e.g., "zh-CN", "ru-RU", etc.)
        foreach (var directory in Directory.GetDirectories(
                     Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!))
            try
            {
                // Create a DirectoryInfo object to get the directory name
                var directoryInfo = new DirectoryInfo(directory);

                // Try to create a CultureInfo object from the directory name and add it to the list
                // If the directory name is not a valid culture name, an exception will be thrown
                supportedCultures.Add(new CultureInfo(directoryInfo.Name));
            }
            catch (Exception)
            {
                // Ignore directories that don't correspond to valid culture names
                // This prevents crashes when encountering non-culture directories
            }

        // Assign the final list of supported cultures to the property
        SupportedCultures = supportedCultures;
    }

    /// <summary>
    ///     Gets the collection of cultures supported by the application
    ///     This is determined by the presence of culture-specific resource directories
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    ///     Resolves the most appropriate supported culture for the current system
    ///     Tries to match the installed UI culture, and falls back through parent cultures
    ///     If no match is found, defaults to English ("en")
    /// </summary>
    /// <returns>The best matching supported culture, or English as fallback</returns>
    public CultureInfo ResolveSupportedCulture()
    {
        // Get the installed UI culture of the system as the starting point
        var cultureInfo = CultureInfo.InstalledUICulture;
        // Check if the exact culture is supported, if so return it immediately
        var matches = matcher.Matches(cultureInfo, SupportedCultures).ToList();
        return matches.Count != 0 ? matches[0] : CultureInfo.GetCultureInfo("en");
    }
}