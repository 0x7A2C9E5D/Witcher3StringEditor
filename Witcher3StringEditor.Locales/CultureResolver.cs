using System.Globalization;
using System.IO;
using System.Reflection;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Defines a contract for culture resolution functionality
///     Provides methods to determine supported cultures and resolve the most appropriate culture for the application
/// </summary>
public interface ICultureResolver
{
    /// <summary>
    ///     Gets the collection of cultures supported by the application
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    ///     Resolves the most appropriate supported culture based on the system's UI culture
    /// </summary>
    /// <returns>The best matching supported culture, or a fallback culture if no match is found</returns>
    CultureInfo ResolveSupportedCulture();
}

/// <summary>
///     Provides culture resolution functionality for the application
///     Detects and resolves supported cultures based on available resource directories
/// </summary>
public class CultureResolver : ICultureResolver
{
    /// <summary>
    ///     Defines a contract for culture resolution functionality
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
    ///     Resolves the most appropriate supported culture for the current user
    ///     Tries to match the user's display culture first, then the culture the operating system was
    ///     installed in, and falls back through parent cultures
    ///     If no match is found, defaults to English ("en")
    /// </summary>
    /// <returns>The best matching supported culture, or English as fallback</returns>
    public CultureInfo ResolveSupportedCulture()
    {
        // The display culture is what the user reads, and it can differ from the installation culture
        // (an English install with a Chinese display language), so it decides first; the installation
        // culture is the fallback for a machine where the user set nothing of their own
        return Resolve(CultureInfo.CurrentUICulture)
               ?? Resolve(CultureInfo.InstalledUICulture)
               ?? CultureInfo.GetCultureInfo("en");
    }

    /// <summary>
    ///     Finds the supported culture matching a culture, if any
    /// </summary>
    /// <param name="cultureInfo">The culture to match</param>
    /// <returns>The best matching supported culture, or null when none matches</returns>
    private CultureInfo? Resolve(CultureInfo cultureInfo)
    {
        var matches = matcher.Matches(cultureInfo, SupportedCultures).ToList();
        return matches.Count != 0 ? matches[0] : null;
    }
}