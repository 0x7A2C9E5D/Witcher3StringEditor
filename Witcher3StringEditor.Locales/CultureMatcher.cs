using System.Globalization;

namespace Witcher3StringEditor.Locales;

public static class CultureMatcher
{
    public static IEnumerable<CultureInfo> Matches(CultureInfo targetCulture, IList<CultureInfo> availableCultures)
    {
        var targetName = targetCulture.Name;
        var targetParentName = targetCulture.Parent.Name;
        
        return availableCultures.Where(x =>
            x.Name == targetName || x.Name == targetParentName || x.Parent.Name == targetParentName);
    }
}