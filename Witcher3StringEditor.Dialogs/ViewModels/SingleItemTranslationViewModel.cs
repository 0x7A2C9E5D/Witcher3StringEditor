using System.Globalization;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FluentResults;
using GTranslate;
using GTranslate.Translators;
using Serilog;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Dialogs.Models;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for single item translation operations
///     Handles translation of individual items with navigation between items
///     Inherits from TranslationViewModelBase to share common translation functionality
/// </summary>
public sealed partial class SingleItemTranslationViewModel : TranslationViewModelBase
{
    /// <summary>
    ///     Gets or sets the index of the currently selected item for translation
    ///     Notifies CanExecute changes for Previous and Next commands when this value changes
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int currentItemIndex = -1;

    /// <summary>
    ///     Gets or sets the current translate item model containing the text to translate
    /// </summary>
    [ObservableProperty] private TranslateItemModel? currentTranslateItemModel;

    /// <summary>
    ///     Gets or sets a value indicating whether a translation operation is in progress
    ///     Notifies CanExecute changes for Previous, Next, and Save commands when this value changes
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isBusy;

    /// <summary>
    ///     Initializes a new instance of the SingleItemTranslationViewModel class
    /// </summary>
    /// <param name="appSettings">Application settings service</param>
    /// <param name="translator">Translation service</param>
    /// <param name="w3StringItems">Collection of items to translate</param>
    /// <param name="index">Initial index of the item to translate</param>
    public SingleItemTranslationViewModel(IAppSettings appSettings, ITranslator translator,
        IReadOnlyList<ITrackableW3StringItem> w3StringItems,
        int index) : base(appSettings, translator, w3StringItems)
    {
        CurrentItemIndex = index;
        Log.Information("Initializing SingleItemTranslationViewModel."); // Log initialization
    }

    /// <summary>
    ///     Gets a value indicating whether the Save command can be executed
    ///     Save is available when no translation operation is in progress
    /// </summary>
    private bool CanSave => !IsBusy;

    /// <summary>
    ///     Gets a value indicating whether the Previous command can be executed
    ///     Previous is available when not at the first item and no translation operation is in progress
    /// </summary>
    private bool CanPrevious => CurrentItemIndex > 0 && !IsBusy;

    /// <summary>
    ///     Gets a value indicating whether the Next command can be executed
    ///     Next is available when not at the last item and no translation operation is in progress
    /// </summary>
    private bool CanNext => CurrentItemIndex < W3StringItems.Count - 1 && !IsBusy;

    /// <summary>
    ///     Gets a value indicating whether a translation operation is currently in progress
    /// </summary>
    /// <returns>True if busy, false otherwise</returns>
    public override bool GetIsBusy()
    {
        return IsBusy;
    }

    /// <summary>
    ///     Disposes of the view model resources
    ///     Cancels any ongoing translation operations and disposes the cancellation token source
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        // Cancel any ongoing translation operations
        if (CancellationTokenSource is not null)
        {
            if (!CancellationTokenSource.IsCancellationRequested) // Check if cancellation is not already requested
                await CancellationTokenSource.CancelAsync(); // Cancel the cancellation token
            CancellationTokenSource.Dispose(); // Dispose the cancellation token source
        }

        Log.Information("SingleItemTranslationViewModel is being disposed."); // Log disposal
    }

    /// <summary>
    ///     Called when the CurrentItemIndex property changes
    ///     Updates the current translate item model with the selected item's data
    /// </summary>
    /// <param name="value">The new current item index value</param>
    partial void OnCurrentItemIndexChanged(int value)
    {
        var selectedItem = W3StringItems[value];
        CurrentTranslateItemModel = new TranslateItemModel { Id = selectedItem.TrackingId, Text = selectedItem.Text };
    }

    /// <summary>
    ///     Called when the IsBusy property changes
    ///     Sends a message to notify other components of the busy state change
    /// </summary>
    /// <param name="value">The new busy state value</param>
    partial void OnIsBusyChanged(bool value)
    {
        _ = WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(value), MessageTokens.TranslatorIsBusy);
    }

    /// <summary>
    ///     Translates the current item's text
    ///     Uses the configured translator to perform the translation operation
    /// </summary>
    [RelayCommand]
    private async Task Translate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CurrentTranslateItemModel
                    ?.TranslatedText)) // Check if translation text is empty
            {
                IsBusy = true; // Set busy state
                Guard.IsNotNullOrWhiteSpace(CurrentTranslateItemModel?.Text);
                CancellationTokenSource?.Dispose(); // Dispose of the cancellation token source
                CancellationTokenSource = new CancellationTokenSource(); // Create a new cancellation token source
                CurrentTranslateItemModel.TranslatedText = string.Empty; // Clear the translation text
                Log.Information("Starting translation."); // Log start of translation
                var translationResult = await ExecuteTranslationTask(CurrentTranslateItemModel.Text,
                    ToLanguage, FormLanguage, CancellationTokenSource); // Execute the translation task
                if (translationResult.IsSuccess) // Check if translation was successful
                {
                    Guard.IsNotNullOrWhiteSpace(translationResult.Value); // Check if translation result is not empty
                    CurrentTranslateItemModel.TranslatedText = translationResult.Value; // Set the translation text
                    Log.Information("Translation completed."); // Log completion of translation
                }
                else
                {
                    Log.Error("Translation operation was cancelled."); // Log cancellation of translation
                }
            }
            else
            {
                // Send a message to notify other components that the translation text is not empty
                _ = await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                    MessageTokens.TranslationNotEmpty);
            }
        }
        catch (Exception ex)
        {
            const string errorMessage = "The translator: {0} returned an error. Exception: {1}";
            _ = WeakReferenceMessenger.Default.Send(
                new ValueChangedMessage<string>(string.Format(CultureInfo.InvariantCulture, errorMessage,
                    Translator.Name, ex.Message)), // Send error message
                "TranslateError");
            Log.Error(ex, errorMessage, Translator.Name, ex.Message); // Log error
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <param name="text">The text to translate</param>
    /// <param name="toLanguage">The target language</param>
    /// <param name="formLanguage">The source language</param>
    /// <param name="cancellationTokenSource">The cancellation token source</param>
    /// <returns>A Result containing the translated text if successful</returns>
    private async Task<Result<string>> ExecuteTranslationTask(string text,
        ILanguage toLanguage, ILanguage formLanguage, CancellationTokenSource cancellationTokenSource)
    {
        var translateTask = Translator.TranslateAsync(text, toLanguage, formLanguage); // Start translation
        var completedTask = await Task.WhenAny( // Wait for first task to complete
            translateTask, // Translation task
            Task.Delay(Timeout.Infinite, cancellationTokenSource.Token) // Cancellation task
        );
        return !completedTask.IsCanceled
            ? Result.Ok((await translateTask).Translation)
            : Result.Fail(string.Empty); // Return result or failure
    }

    /// <summary>
    ///     Saves the translated text to the underlying data model
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(CurrentTranslateItemModel
                    ?.TranslatedText)) // Check if translation text is not empty
                SaveTranslation(); // Save translation
            else
                _ = WeakReferenceMessenger.Default.Send(new ValueChangedMessage<string>(string.Empty),
                    MessageTokens
                        .TranslatedTextInvalid); // Send message to notify other components that the translation text is invalid
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save translation."); // Log error
        }
    }

    /// <summary>
    ///     Navigates to another item (previous or next) in the collection
    ///     Automatically saves the current translation if it hasn't been saved yet
    /// </summary>
    /// <param name="direction">The direction to navigate (-1 for previous, 1 for next)</param>
    private async Task Navigate(int direction)
    {
        try
        {
            if (CurrentTranslateItemModel is { IsSaved: false } // Check if current translation is unsaved
                && !string.IsNullOrWhiteSpace(CurrentTranslateItemModel.TranslatedText) // And has text content
                && await WeakReferenceMessenger.Default.Send(
                    new AsyncRequestMessage<bool>(), // Ask user to confirm save
                    MessageTokens.TranslatedTextNoSaved))
                SaveTranslation(); // Save the translation if confirmed
            CurrentItemIndex += direction; // Move to the next/previous item
            Log.Information("Translator {TranslatorName} moved to {Direction} item (new index: {NewIndex})",
                Translator.Name, direction > 0 ? "next" : "previous", CurrentItemIndex); // Log navigation
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to move to {Direction} item",
                direction > 0 ? "next" : "previous"); // Log error
        }
    }

    /// <summary>
    ///     Saves the current translation to the underlying data model
    ///     Finds the original item by ID, clones it, updates the text, and sends a message with the updated item
    /// </summary>
    private void SaveTranslation()
    {
        Guard.IsNotNull(CurrentTranslateItemModel); // Ensure we have a current item
        var found = W3StringItems // Find the original item by ID
            .First(x => x.TrackingId == CurrentTranslateItemModel?.Id);
        found.Text = CurrentTranslateItemModel.TranslatedText; // Update with translated text
        CurrentTranslateItemModel.IsSaved = true; // Mark as saved
    }

    /// <summary>
    ///     Navigates to the previous item in the collection
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPrevious))]
    private async Task Previous()
    {
        await Navigate(-1);
    }

    /// <summary>
    ///     Navigates to the next item in the collection
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNext))]
    private async Task Next()
    {
        await Navigate(1);
    }
}