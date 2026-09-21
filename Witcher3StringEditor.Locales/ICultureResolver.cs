using System.Globalization;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Defines a contract for culture resolution functionality
///     Provides methods to determine supported cultures and resolve the most appropriate culture for the application
/// </summary>
public interface ICultureResolver
{
    /// <summary>
    ///     Gets the cultures supported by the application as an immutable snapshot
    ///     The snapshot is de-duplicated by culture name and does not change afterward
    /// </summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    ///     Resolves the most appropriate supported culture based on the system's UI culture
    /// </summary>
    /// <returns>
    ///     The best matching supported culture. When the system culture is not supported at all,
    ///     the fallback culture (<c>"en"</c>) is returned;
    /// </returns>
    CultureInfo ResolveSupportedCulture();
}