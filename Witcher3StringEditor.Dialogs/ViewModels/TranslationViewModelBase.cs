using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using GTranslate;
using GTranslate.Translators;
using Serilog;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Locales;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     Base class for translation view models
///     Provides common functionality for both single item and batch translation operations
///     Implements ObservableObject for property change notifications and IAsyncDisposable for async cleanup
/// </summary>
public abstract partial class TranslationViewModelBase : ObservableObject, IAsyncDisposable
{
    /// <summary>
    ///     The dictionary service used for managing dictionaries
    /// </summary>
    private protected readonly IDictionaryService? DictionaryService;

    /// <summary>
    ///     The translation service used for translating text
    /// </summary>
    private protected readonly ITranslator Translator;

    /// <summary>
    ///     The collection of items to translate
    /// </summary>
    private protected readonly IReadOnlyList<ITrackableW3StringItem> W3StringItems;

    /// <summary>
    ///     The cancellation token source for managing translation operation cancellation
    /// </summary>
    private protected CancellationTokenSource? CancellationTokenSource;

    /// <summary>
    ///     Gets or sets the source language for translation
    /// </summary>
    [ObservableProperty] private ILanguage formLanguage;

    /// <summary>
    ///     Gets or sets a value indicating whether the dictionary service is supported
    /// </summary>
    [ObservableProperty] private bool isDictionarySupported;

    /// <summary>
    ///     Gets or sets the collection of supported languages for the current translator
    /// </summary>
    [ObservableProperty] private IEnumerable<ILanguage> languages;

    /// <summary>
    ///     Gets or sets the selected dictionary for translation
    /// </summary>
    [ObservableProperty] private DictionaryInfo? selectedDictionary;

    /// <summary>
    ///     Gets or sets the target language for translation
    /// </summary>
    [ObservableProperty] private ILanguage toLanguage;

    /// <summary>
    ///     Initializes a new instance of the TranslationViewModelBase class
    /// </summary>
    /// <param name="appSettings">Application settings service</param>
    /// <param name="translator">Translation service</param>
    /// <param name="w3StringItems">Collection of items to translate</param>
    /// <param name="dictionaryService">Dictionary service</param>
    protected TranslationViewModelBase(IAppSettings appSettings, ITranslator translator,
        IReadOnlyList<ITrackableW3StringItem> w3StringItems, IDictionaryService? dictionaryService = null)
    {
        Translator = translator; // Initialize the translator
        DictionaryService = dictionaryService; // Initialize the dictionary service (optional)
        W3StringItems = w3StringItems; // Initialize the collection of items to translate
        Languages = GetSupportedLanguages(
            translator); // Get the supported languages for the translator and set the Languages property
        FormLanguage = Language.GetLanguage("en"); // Set the default source language to English
        ToLanguage =
            GetPreferredLanguage(
                appSettings); // Get the preferred target language from application settings and set the ToLanguage property
    }

    private protected DictionaryInfo NoneDictionary { get; } = new(
        string.Empty,
        Strings.NoDictionary,
        string.Empty,
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("en"),
        0);

    /// <summary>
    ///     Updates the availability of the dictionary service
    /// </summary>
    public ObservableCollection<DictionaryInfo> Dictionaries { get; } = [];

    /// <summary>
    ///     Disposes of the view model resources asynchronously
    ///     Must be implemented by derived classes
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation</returns>
    public abstract ValueTask DisposeAsync();

    /// <summary>
    ///     Checks if the current language is supported by the dictionary service
    /// </summary>
    /// <returns>
    ///     <code>true</code> if the language is supported, <code>false</code> otherwise.
    /// </returns>
    private bool CanUseDictionary()
    {
        return Translator.Name == "MicrosoftTranslator" &&
               IsEnglishCulture(
                   FormLanguage); // Dictionary is only supported for Microsoft Translator when the source language is English
    }

    /// <summary>
    ///     Checks if a given language is English
    /// </summary>
    /// <param name="language"></param>
    /// <returns>
    ///     <code>true</code> if the language is English, <code>false</code> otherwise.
    /// </returns>
    private static bool IsEnglishCulture(ILanguage language)
    {
        var culture = CultureInfo.GetCultureInfo(language.ISO6391); // Get the culture info for the given language
        return IsEnglishCulture(culture); // Check if the culture is English using the helper method
    }

    /// <summary>
    ///     Checks if a given culture is English
    /// </summary>
    /// <param name="culture"></param>
    /// <returns>
    ///     <code>true</code> if the culture is English, <code>false</code> otherwise.
    /// </returns>
    private static bool IsEnglishCulture(CultureInfo culture)
    {
        var enCultureName = CultureInfo.GetCultureInfo("en").Name; // Get the culture name for English
        return culture.Name == enCultureName ||
               culture.Parent.Name ==
               enCultureName; // Check if the culture is English or has English as its parent culture
    }

    /// <summary>
    ///     Gets a value indicating whether a translation operation is currently in progress
    ///     Must be implemented by derived classes
    /// </summary>
    /// <returns>True if busy, false otherwise</returns>
    public abstract bool GetIsBusy();

    /// <summary>
    ///     Gets the collection of supported languages for a specific translator
    /// </summary>
    /// <param name="translator">The translator to get supported languages for</param>
    /// <returns>A collection of supported languages</returns>
    private static IEnumerable<ILanguage> GetSupportedLanguages(ITranslator translator)
    {
        return translator.Name switch // Return languages based on translator type
        {
            "MicrosoftTranslator" => Language.LanguageDictionary.Values.Where(x => // Microsoft supported languages
                x.SupportedServices.HasFlag(TranslationServices.Microsoft)),
            "GoogleTranslator" => Language.LanguageDictionary.Values.Where(x => // Google supported languages
                x.SupportedServices.HasFlag(TranslationServices.Google)),
            "YandexTranslator" => Language.LanguageDictionary.Values.Where(x => // Yandex supported languages
                x.SupportedServices.HasFlag(TranslationServices.Google)),
            _ => Language.LanguageDictionary.Values // Default to all languages
        };
    }

    /// <summary>
    ///     Gets the preferred language from application settings
    /// </summary>
    /// <param name="appSettings">Application settings service</param>
    /// <returns>The preferred language</returns>
    private static Language GetPreferredLanguage(IAppSettings appSettings)
    {
        var description = typeof(W3Language).GetField(appSettings.PreferredLanguage.ToString())!
            .GetCustomAttribute<DescriptionAttribute>()!.Description; // Get language description
        return description == "es-MX" ? new Language("es") : new Language(description); // Return preferred language
    }

    /// <summary>
    ///     Called when the FormLanguage property changes
    ///     Logs the change in source language
    /// </summary>
    /// <param name="value">The new source language value</param>
    partial void OnFormLanguageChanged(ILanguage value)
    {
        UpdateDictionaryAvailability(); // Update dictionary availability based on the new source language
        Log.Information("The source language has been changed to: {Name}.",
            value.Name); // Log the change in source language
    }

    /// <summary>
    ///     Updates the availability of the dictionary service
    /// </summary>
    private void UpdateDictionaryAvailability()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ToLanguage == null || DictionaryService == null) return; // If target language is null, exit early
        IsDictionarySupported =
            CanUseDictionary(); // Check if dictionary can be used based on current translator and source language
        if (!IsDictionarySupported) return; // If dictionary is not supported, exit early
        LoadDictionariesForCurrentLanguage(); // Load dictionaries for the current language
    }

    /// <summary>
    ///     Loads dictionaries for the current target language.
    /// </summary>
    private void LoadDictionariesForCurrentLanguage()
    {
        InitializeDictionaryCollection(); // Initialize the dictionary collection
        var targetLanguage =
            CultureInfo.GetCultureInfo(ToLanguage.ISO6391); // Get the culture info for the target language
        var matchingDictionaries =
            DictionaryService!.Find(targetLanguage).ToArray(); // Find matching dictionaries for the target language
        AddMatchingDictionaries(matchingDictionaries); // Add matching dictionaries to the collection

        Log.Information(
            "Dictionary availability has been updated. Is supported: {IsSupported}. Found {Count} matching dictionaries for target language: {Language}.",
            IsDictionarySupported, matchingDictionaries.Length,
            ToLanguage.Name); // Log the updated dictionary availability
    }

    /// <summary>
    ///     Initializes the dictionary collection with default values.
    /// </summary>
    private void InitializeDictionaryCollection()
    {
        if (Dictionaries.Count >= 1)
            Dictionaries.Clear(); // Clear the dictionary collection if it already contains items
        Dictionaries.Add(NoneDictionary); // Add the "None" dictionary to the collection
        SelectedDictionary = NoneDictionary; // Set the selected dictionary to "None"
    }

    /// <summary>
    ///     Adds matching dictionaries to the collection.
    /// </summary>
    /// <param name="matchingDictionaries">The dictionaries to add.</param>
    private void AddMatchingDictionaries(DictionaryInfo[] matchingDictionaries)
    {
        if (matchingDictionaries.Length > 0)
            matchingDictionaries.ForEach(x => Dictionaries.Add(x));
    }

    /// <summary>
    ///     Called when the ToLanguage property changes
    ///     Logs the change in target language
    /// </summary>
    /// <param name="value">The new target language value</param>
    partial void OnToLanguageChanged(ILanguage value)
    {
        UpdateDictionaryAvailability(); // Update dictionary availability based on the new target language
        Log.Information("The target language has been changed to: {Name}.",
            value.Name); // Log the change in target language
    }
}