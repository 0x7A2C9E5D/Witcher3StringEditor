using System.Text.RegularExpressions;
using GTranslate.Translators;
using Microsoft.Extensions.DependencyInjection;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Resolves translator instances from the dependency injection container on demand
///     Translators are registered as transient: every call to <see cref="GetTranslator" /> returns
///     a fresh instance that the caller must dispose after use
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve translators</param>
internal sealed partial class TranslatorProvider(IServiceProvider serviceProvider) : ITranslatorProvider
{
    /// <summary>
    ///     Gets the display names of all registered translators
    /// </summary>
    /// <returns>The distinct display names of the registered translators</returns>
    public IReadOnlyList<string> GetTranslatorNames()
    {
        var translators = serviceProvider.GetServices<ITranslator>().ToArray(); // Resolve all translators
        try
        {
            // Return the display names with the version suffix stripped (e.g. "GoogleTranslator2" -> "GoogleTranslator")
            return [.. translators.Select(x => NormalizeName(x.Name)).Distinct(StringComparer.OrdinalIgnoreCase)];
        }
        finally
        {
            // Dispose the transient instances after extracting their names
            foreach (var translator in translators.OfType<IDisposable>())
                translator.Dispose();
        }
    }

    /// <summary>
    ///     Resolves the translator instance matching the specified settings name
    /// </summary>
    /// <param name="name">The translator name stored in settings (e.g. "MicrosoftTranslator")</param>
    /// <returns>A transient translator instance matching the name, owned by the caller</returns>
    /// <exception cref="InvalidOperationException">No translator matches <paramref name="name" /></exception>
    public ITranslator GetTranslator(string name)
    {
        return TryGetTranslator(name, out var translator) ? translator! : throw new InvalidOperationException($"No translator matching the name '{name}' is registered.");
    }

    /// <summary>
    ///     Attempts to resolve the translator instance matching the specified settings name
    /// </summary>
    /// <param name="name">The translator name stored in settings</param>
    /// <param name="translator">The resolved transient translator, or null when no match was found</param>
    /// <returns>True when a matching translator was resolved; otherwise false</returns>
    public bool TryGetTranslator(string name, out ITranslator? translator)
    {
        translator = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        var translators = serviceProvider.GetServices<ITranslator>().ToList(); // Resolve all translators
        var match = translators.FirstOrDefault(x =>
            string.Equals(NormalizeName(x.Name), NormalizeName(name), StringComparison.OrdinalIgnoreCase));
        translator = match;

        // Dispose every transient instance that is not handed over to the caller
        foreach (var disposable in translators.OfType<IDisposable>().Where(x => !ReferenceEquals(x, match)))
            disposable.Dispose();

        return match is not null;
    }

    /// <summary>
    ///     Strips a trailing numeric version suffix from a translator name
    /// </summary>
    /// <param name="name">The translator name as reported by the implementation</param>
    /// <returns>The name without a trailing version number, used as the stable selection key</returns>
    private static string NormalizeName(string name)
    {
        return TrailingVersionSuffix().Replace(name, string.Empty);
    }

    /// <summary>
    ///     Matches a trailing run of digits, for example the version suffix of "GoogleTranslator2"
    /// </summary>
    /// <returns>The compiled regular expression</returns>
    [GeneratedRegex(@"\d+$")]
    private static partial Regex TrailingVersionSuffix();
}