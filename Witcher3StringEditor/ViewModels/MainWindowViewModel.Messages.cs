using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Messenger wiring: every message this view model subscribes to, and what each one updates.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Registers all message handlers for the view model
    /// </summary>
    private void RegisterMessengerHandlers()
    {
        RegisterFileMessageHandlers(); // Register file message handlers
        RegisterSettingsMessageHandlers(); // Register settings message handlers
        RegisterSearchMessageHandlers(); // Register search message handlers
        RegisterDictionarySupportMessageHandler(); // Register dictionary support message handler
    }

    /// <summary>
    ///     Registers message handlers for file-related messages
    /// </summary>
    private void RegisterFileMessageHandlers()
    {
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, AsyncRequestMessage<string, bool>, string>(
            // ReSharper disable once AsyncVoidMethod
            this,
            MessageTokens.RecentFileOpened,
            async void (_, m) => { await LoadFileAsync(m.Request); }); // Open the requested file
    }

    /// <summary>
    ///     Registers message handlers for settings-related messages
    /// </summary>
    private void RegisterSettingsMessageHandlers()
    {
        // Register handler for GameExe path change notifications
        WeakReferenceMessenger.Default
            .Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(this, MessageTokens.GameExePathChanged,
                (_, _) => PlayGameCommand
                    .NotifyCanExecuteChanged()); // Update PlayGame command state when GameExe path changes
    }

    /// <summary>
    ///     Registers message handlers for search-related messages
    /// </summary>
    private void RegisterSearchMessageHandlers()
    {
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, List<W3StringItem>, string>(
            this,
            MessageTokens.DataGridPagedSourceChanged,
            (_, m) => { PagedSource = m; });
    }

    /// <summary>
    ///     Registers a handler to monitor changes to the translator setting and updates dictionary support accordingly.
    /// </summary>
    private void RegisterDictionarySupportMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<string>, string>(
            this,
            MessageTokens.TranslatorChanged,
            (_, m) => { IsSupportDictionary = m.Value == "MicrosoftTranslator"; });
    }
}