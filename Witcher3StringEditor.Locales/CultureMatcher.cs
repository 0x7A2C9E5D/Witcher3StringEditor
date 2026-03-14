using System.Globalization;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Locales;

/// <summary>
///     Provides culture matching functionality for the application
/// </summary>
[PublicAPI]
public class CultureMatcher : ICultureMatcher
{
    /// <summary>
    ///     Matches a target culture with available cultures
    /// </summary>
    /// <param name="targetCulture"></param>
    /// <param name="availableCultures"></param>
    /// <returns>
    ///     An array of matching cultures
    /// </returns>
    public IEnumerable<CultureInfo> Matches(CultureInfo targetCulture, IList<CultureInfo> availableCultures)
    {
        var bestMatches = availableCultures
            .Where(x => x.Name == targetCulture.Name).ToArray();
        if (bestMatches.Length != 0) return bestMatches;
        var targetParentName = targetCulture.Parent.Name;
        bestMatches = availableCultures
            .Where(x => x.Name == targetParentName).ToArray();
        if (bestMatches.Length != 0) return bestMatches;
        bestMatches = availableCultures
            .Where(x => x.Parent.Name == targetParentName).ToArray();
        return bestMatches.Length != 0 ? bestMatches : [];
    }
}