using System.Globalization;

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