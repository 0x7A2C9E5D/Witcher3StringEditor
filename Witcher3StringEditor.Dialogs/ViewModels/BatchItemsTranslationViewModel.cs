using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentResults;
using GTranslate;
using GTranslate.Translators;
using Serilog;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for batch translation operations
///     Handles translation of multiple items with start and end index controls
///     Inherits from TranslationViewModelBase to share common translation functionality
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public sealed partial class BatchItemsTranslationViewModel : TranslationViewModelBase
{
    /// <summary>
    ///     Gets or sets the end index for batch translation
    /// </summary>
    [ObservableProperty] private int endIndex;

    /// <summary>
    ///     Gets or sets the minimum value for the end index (based on start index)
    /// </summary>
    [ObservableProperty] private int endIndexMin;

    /// <summary>
    ///     Gets or sets the count of failed translations
    /// </summary>
    [ObservableProperty] private int failureCount;

    /// <summary>
    ///     Gets or sets a value indicating whether a translation operation is in progress
    ///     Notifies CanExecute changes for the Cancel command when this value changes
    /// </summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isBusy;

    /// <summary>
    ///     Gets or sets the maximum value for indices (typically the total item count)
    /// </summary>
    [ObservableProperty] private int maxValue;

    /// <summary>
    ///     Gets or sets the count of pending translations
    /// </summary>
    [ObservableProperty] private int pendingCount;

    /// <summary>
    ///     Gets or sets the start index for batch translation
    /// </summary>
    [ObservableProperty] private int startIndex;

    /// <summary>
    ///     Gets or sets the count of successful translations
    /// </summary>
    [ObservableProperty] private int successCount;

    /// <summary>
    ///     Initializes a new instance of the BatchItemsTranslationViewModel class
    /// </summary>
    /// <param name="appSettings">Application settings service</param>
    /// <param name="translator">Translation service</param>
    /// <param name="w3StringItems">Collection of items to translate</param>
    /// <param name="startIndex">Initial start index for translation</param>
    /// <param name="dictionaryService">Dictionary service</param>
    public BatchItemsTranslationViewModel(IAppSettings appSettings, ITranslator translator,
        IReadOnlyList<ITrackableW3StringItem> w3StringItems, int startIndex, IDictionaryMangerService? dictionaryService = null) : base(appSettings, translator,
        w3StringItems, dictionaryService)
    {
        StartIndex = startIndex; // Set start index
        EndIndex = MaxValue = W3StringItems.Count; // Set end index and maximum value
        Log.Information("Initializing BatchItemsTranslationViewModel."); // Log initialization
    }

    /// <summary>
    ///     Gets a value indicating whether the Cancel command can be executed
    ///     Cancel is available when a translation operation is in progress
    /// </summary>
    private bool CanCancel => IsBusy;

    /// <summary>
    ///     Gets a value indicating whether the Start command can be executed
    ///     Start is available when no translation operation is in progress
    /// </summary>
    private bool CanStart => !IsBusy;

    /// <summary>
    ///     Disposes of the view model resources
    ///     Cancels any ongoing translation operations and disposes the cancellation token source
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        // Cancel any ongoing translation operations
        if (CancellationTokenSource is not null)
        {
            // Check if cancellation is not already requested
            if (!CancellationTokenSource.IsCancellationRequested)
                await CancellationTokenSource.CancelAsync(); // Cancel the cancellation token
            CancellationTokenSource.Dispose(); // Dispose the cancellation token source
        }

        Log.Information("BatchItemsTranslationViewModel is being disposed.");
    }

    /// <summary>
    ///     Gets a value indicating whether a translation operation is currently in progress
    /// </summary>
    /// <returns>True if busy, false otherwise</returns>
    public override bool GetIsBusy()
    {
        return IsBusy;
    }

    /// <summary>
    ///     Called when the StartIndex property changes
    ///     Updates the minimum end index and resets translation counts if not busy
    /// </summary>
    /// <param name="value">The new start index value</param>
    partial void OnStartIndexChanged(int value)
    {
        EndIndexMin = value > MaxValue ? MaxValue : value; // Update the minimum end index
        if (!IsBusy) ResetTranslationCounts(); // Reset translation counts if not busy
    }

    /// <summary>
    ///     Called when the EndIndex property changes
    ///     Resets translation counts if not busy
    /// </summary>
    /// <param name="value">The new end index value</param>
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnEndIndexChanged(int value)
    {
        if (!IsBusy) ResetTranslationCounts(); // Reset translation counts if not busy
    }

    /// <summary>
    ///     Resets the translation counters (success, failure, pending)
    /// </summary>
    private void ResetTranslationCounts()
    {
        SuccessCount = 0; // Reset success count
        FailureCount = 0; // Reset failure count
        PendingCount = EndIndex - StartIndex + 1; // Reset pending count
    }

    /// <summary>
    ///     Called when the IsBusy property changes
    ///     Logs the busy state change
    /// </summary>
    /// <param name="value">The new busy state value</param>
    partial void OnIsBusyChanged(bool value)
    {
        Log.Information("The batch translation is in progress: {0}.", value);
    }

    /// <summary>
    ///     Starts the batch translation process
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        try
        {
            await ExecuteBatchTranslation(); // Execute the batch translation process
        }
        finally
        {
            IsBusy = false; // Clear the busy flag
        }
    }

    /// <summary>
    ///     Executes the batch translation process
    ///     Sets up the cancellation token and processes the selected range of items
    /// </summary>
    private async Task ExecuteBatchTranslation()
    {
        IsBusy = true; // Set the busy flag to prevent concurrent operations
        ResetTranslationCounts(); // Reset counters for success, failure, and pending items
        CancellationTokenSource?.Dispose(); // Dispose of any existing cancellation token source
        CancellationTokenSource = new CancellationTokenSource(); // Create a new cancellation token source
        await ProcessTranslationItems(W3StringItems.Skip(StartIndex - 1).Take(PendingCount), // Process selected items
            ToLanguage, FormLanguage, CancellationTokenSource.Token);
    }

    /// <summary>
    ///     Processes a collection of items for translation
    /// </summary>
    /// <param name="items">The items to translate</param>
    /// <param name="toLanguage">The target language</param>
    /// <param name="fromLanguage">The source language</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    private async Task ProcessTranslationItems(IEnumerable<ITrackableW3StringItem> items, ILanguage toLanguage,
        ILanguage fromLanguage,
        CancellationToken cancellationToken)
    {
        foreach (var item in items) // Process each item in the collection
            if (!cancellationToken.IsCancellationRequested) // Check if operation has been canceled
            {
                await ProcessSingleItem(item, toLanguage, fromLanguage); // Translate the current item
                PendingCount--; // Decrement the pending items counter
            }
            else
            {
                Log.Information("The batch translation has been cancelled."); // Log cancellation
                break; // Exit the loop if canceled
            }
    }

    /// <summary>
    ///     Processes a single item for translation
    ///     Sends the translated result via messaging if successful
    /// </summary>
    /// <param name="item">The item to translate</param>
    /// <param name="toLanguage">The target language</param>
    /// <param name="fromLanguage">The source language</param>
    private async Task ProcessSingleItem(ITrackableW3StringItem item, ILanguage toLanguage, ILanguage fromLanguage)
    {
        try
        {
            var translation =
                await TranslateItem(Translator, item.Text, toLanguage, fromLanguage); // Perform translation
            if (translation.IsSuccess) // Check if translation succeeded
            {
                item.Text = translation.Value; // Update with translated text
                SuccessCount++; // Increment success counter
            }
            else
            {
                FailureCount++; // Increment failure counter
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "The translator: {Name} returned an error. Exception: {ExceptionMessage}", // Log any errors
                Translator.Name, ex.Message);
            FailureCount++; // Increment failure counter
        }
    }

    /// <summary>
    ///     Translates a single text item
    /// </summary>
    /// <param name="translator">The translation service to use</param>
    /// <param name="text">The text to translate</param>
    /// <param name="tLanguage">The target language</param>
    /// <param name="fLanguage">The source language</param>
    /// <returns>A Result containing the translated text if successful</returns>
    private static async Task<Result<string>> TranslateItem(ITranslator translator, string text, ILanguage tLanguage,
        ILanguage fLanguage)
    {
        var translation =
            (await translator.TranslateAsync(text, tLanguage, fLanguage)).Translation; // Perform translation
        if (IsTranslationValid(translation)) return Result.Ok(translation); // Return success if valid
        LogEmptyTranslationResult(translator.Name); // Log error if invalid
        return Result.Fail(string.Empty); // Return failure
    }

    /// <summary>
    ///     Checks if a translation result is valid (not null or whitespace)
    /// </summary>
    /// <param name="translation">The translation result to check</param>
    /// <returns>True if the translation is valid, false otherwise</returns>
    private static bool IsTranslationValid(string translation)
    {
        return !string.IsNullOrWhiteSpace(translation);
    }

    /// <summary>
    ///     Logs an error when a translation returns empty data
    /// </summary>
    /// <param name="translatorName">The name of the translator that returned empty data</param>
    private static void LogEmptyTranslationResult(string translatorName)
    {
        Log.Error("The translator: {Name} returned empty data.", translatorName);
    }

    /// <summary>
    ///     Cancels the ongoing translation operation
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task Cancel()
    {
        if (CancellationTokenSource is not null) // Check if cancellation token source exists
            await CancellationTokenSource.CancelAsync(); // Cancel the operation
    }
}