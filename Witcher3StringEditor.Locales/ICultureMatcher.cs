using System.Globalization;

namespace Witcher3StringEditor.Locales;

public interface ICultureMatcher
{
    /// <summary>
    ///     Matches a target culture with available cultures
    /// </summary>
    /// <param name="targetCulture"></param>
    /// <param name="availableCultures"></param>
    /// <returns></returns>
    IEnumerable<CultureInfo> Matches(CultureInfo targetCulture, IList<CultureInfo> availableCultures);
}