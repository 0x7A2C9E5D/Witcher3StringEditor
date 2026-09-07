using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Shared.Extensions;

/// <summary>
///     Provides culture-related helpers for W3Language enum values
/// </summary>
public static class W3LanguageExtensions
{
    /// <summary>
    ///     Gets the culture code associated with a W3Language value via its Description attribute
    /// </summary>
    /// <param name="language">The W3Language value</param>
    /// <returns>The culture code, for example "en" or "zh-Hans"</returns>
    public static string GetCultureCode(this W3Language language)
    {
        return typeof(W3Language).GetField(language.ToString())!
            .GetCustomAttribute<DescriptionAttribute>()!
            .Description;
    }

    /// <summary>
    ///     Creates the culture info for a W3Language value
    /// </summary>
    /// <param name="language">The W3Language value</param>
    /// <returns>The culture info for the language</returns>
    public static CultureInfo GetCultureInfo(this W3Language language)
    {
        return CultureInfo.GetCultureInfo(language.GetCultureCode());
    }
}