using System.Globalization;

namespace Witcher3StringEditor.Locales;

public static class CultureMatcher
{
    public static IEnumerable<CultureInfo> Matches(CultureInfo targetCulture, IList<CultureInfo> availableCultures)
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