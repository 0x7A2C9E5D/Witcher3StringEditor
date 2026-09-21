using System.Globalization;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Defines a contract for matching a requested culture against the cultures the application supports
/// </summary>
public interface ICultureMatcher
{
    /// <summary>
    ///     Matches a target culture with the available cultures
    /// </summary>
    /// <param name="targetCulture">The culture to match; must not be null</param>
    /// <param name="availableCultures">
    ///     The cultures the application supports; must not be null, may be empty
    /// </param>
    /// <returns>
    ///     A never-null sequence of matching cultures ordered by descending quality (the best match first), or an
    ///     empty sequence when nothing matches. The result is never null, so callers can safely index or iterate it
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="targetCulture" /> or <paramref name="availableCultures" /> is null
    /// </exception>
    IEnumerable<CultureInfo> Matches(CultureInfo targetCulture, IReadOnlyList<CultureInfo> availableCultures);
}