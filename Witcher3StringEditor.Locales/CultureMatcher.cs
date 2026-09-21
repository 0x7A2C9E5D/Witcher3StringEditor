using System.Globalization;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Provides culture matching functionality for the application
/// </summary>
public class CultureMatcher : ICultureMatcher
{
    /// <summary>
    ///     Matches a target culture with available cultures
    /// </summary>
    /// <param name="targetCulture">The culture to match</param>
    /// <param name="availableCultures">The cultures the application supports</param>
    /// <returns>
    ///     The matching cultures ordered by descending quality (the best match first), or an empty sequence when
    ///     nothing matches
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="targetCulture" /> or <paramref name="availableCultures" /> is null
    /// </exception>
    public IEnumerable<CultureInfo> Matches(CultureInfo targetCulture, IReadOnlyList<CultureInfo> availableCultures)
    {
        if (availableCultures.Count == 0) return [];

        // Walk the whole ancestor chain so deeper hierarchies (e.g. zh-Hans-CN -> zh-Hans) are reached instead of
        // only the direct parent
        for (var culture = targetCulture;; culture = culture.Parent)
        {
            var name = culture.Name;
            // A root or invariant culture has an empty name; matching on it would return an arbitrary culture
            if (string.IsNullOrEmpty(name)) break;
            var exactMatches = MatchesName(availableCultures, name);
            if (exactMatches.Length != 0) return exactMatches;
            if (ReferenceEquals(culture, culture.Parent)) break; // The invariant culture is its own parent
        }

        // Siblings share the direct parent of the target culture
        var parentName = targetCulture.Parent.Name;
        if (!string.IsNullOrEmpty(parentName))
        {
            var siblings = availableCultures
                .Where(x => string.Equals(x.Parent.Name, parentName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (siblings.Length != 0) return siblings;
        }

        // Finally fall back to any supported culture of the same language, which covers neutral cultures such as
        // "zh" that are not part of the ancestor chain of "zh-Hans-CN"
        var languageName = targetCulture.TwoLetterISOLanguageName;
        if (languageName.Length == 0 ||
            string.Equals(languageName, "iv", StringComparison.OrdinalIgnoreCase)) return [];
        var sameLanguage = availableCultures
            .Where(x => string.Equals(x.TwoLetterISOLanguageName, languageName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return sameLanguage.Length != 0 ? sameLanguage : [];
    }

    /// <summary>
    ///     Finds the available cultures with the specified name
    /// </summary>
    /// <param name="availableCultures">The cultures the application supports</param>
    /// <param name="name">The culture name to match</param>
    /// <returns>The matching cultures</returns>
    private static CultureInfo[] MatchesName(IReadOnlyList<CultureInfo> availableCultures, string name)
    {
        return [.. availableCultures.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))];
    }
}