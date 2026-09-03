using GTranslate.Translators;
using Microsoft.Extensions.DependencyInjection;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Resolves translator instances from the dependency injection container on demand
///     Translators are registered as transient: every call to <see cref="GetTranslator" /> returns
///     a fresh instance that the caller must dispose after use
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve translators</param>
internal sealed class TranslatorProvider(IServiceProvider serviceProvider) : ITranslatorProvider
{
    /// <inheritdoc />
    public IReadOnlyList<string> GetTranslatorNames()
    {
        var translators = serviceProvider.GetServices<ITranslator>().ToArray(); // Resolve all translators
        try
        {
            // Return the display names with the version suffix stripped (e.g. "GoogleTranslator2" -> "GoogleTranslator")
            return [.. translators.Select(x => x.Name.Replace("2", string.Empty))];
        }
        finally
        {
            // Dispose the transient instances after extracting their names
            foreach (var translator in translators.OfType<IDisposable>())
                translator.Dispose();
        }
    }

    /// <inheritdoc />
    public ITranslator GetTranslator(string name)
    {
        return serviceProvider.GetServices<ITranslator>() // Resolve all translators
            .First(x => x.Name.Contains(name)); // Return the one matching the settings name
    }
}