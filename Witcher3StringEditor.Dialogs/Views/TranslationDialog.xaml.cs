using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for TranslationDialog.xaml
///     This dialog provides translation functionality for The Witcher 3 string items
///     Supports both single item and batch translation modes
/// </summary>
public partial class TranslationDialog
{
    /// <summary>
    ///     Initializes a new instance of the TranslationDialog class
    ///     Sets up the UI components and registers message handlers for various operations
    /// </summary>
    public TranslationDialog()
    {
        InitializeComponent(); // Initialize UI components
        RegisterMessageHandlers(); // Register message handlers
    }

    /// <summary>
    ///     Registers all message handlers for the translate dialog
    ///     Includes both notification message handlers and async request message handlers
    /// </summary>
    private void RegisterMessageHandlers()
    {
        RegisterNotificationMessageHandlers(); // Register notification message handlers
        RegisterAsyncRequestMessageHandlers(); // Register async request message handlers
    }

    /// <summary>
    ///     Registers async request message handlers
    ///     These handlers show message boxes and wait for user responses
    /// </summary>
    private void RegisterAsyncRequestMessageHandlers()
    {
        var messageHandlers = CreateAsyncRequestHandlers(); // Create async request handlers
        foreach (var (token, message, caption) in messageHandlers)
            RegisterAsyncRequestHandler(token, message, caption); // Register the handler
    }

    /// <summary>
    ///     Registers a single async request message handler
    ///     Shows a message box with Yes/No buttons and replies with the user's choice
    /// </summary>
    /// <param name="token">The message token to listen for</param>
    /// <param name="message">Function that returns the message text</param>
    /// <param name="caption">Function that returns the caption text</param>
    private void RegisterAsyncRequestHandler(string token, Func<string> message, Func<string> caption)
    {
        WeakReferenceMessenger.Default.Register<TranslationDialog, AsyncRequestMessage<bool>, string>(
            this,
            token,
            (_, m) =>
            {
                m.Reply(MessageBox.Show(
                    message(),
                    caption(),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes);
            });
    }

    /// <summary>
    ///     Creates an array of async request message handler configurations
    ///     Each configuration defines a message box that can be shown for a specific operation
    /// </summary>
    /// <returns>An array of async request handler configurations</returns>
    private static (string, Func<string>, Func<string>)[] CreateAsyncRequestHandlers()
    {
        return
        [
            (MessageTokens.TranslatedTextNoSaved, () => Strings.TranslatedTextNoSavedMessage,
                () => Strings.TranslatedTextNoSavedCaption),
            (MessageTokens.TranslationDialogClosing, () => Strings.TranslatorTranslatingMessage,
                () => Strings.TranslatorTranslatingCaption),
            (MessageTokens.TranslationModeSwitch, () => Strings.TranslationModeSwitchMessage,
                () => Strings.TranslationModeSwitchCaption),
            (MessageTokens.TranslationNotEmpty, () => Strings.TranslationNotEmptyMessage,
                () => Strings.TranslationNotEmptyCaption)
        ];
    }

    /// <summary>
    ///     Registers notification message handlers
    ///     These handlers show message boxes to inform the user of events or errors
    /// </summary>
    private void RegisterNotificationMessageHandlers()
    {
        var notificationHandlers = CreateNotificationHandlers(); // Create notification handlers
        foreach (var (token, message, caption) in notificationHandlers)
            RegisterNotificationHandler(token, message, caption); // Register the handler
    }

    /// <summary>
    ///     Registers a single notification message handler
    ///     Shows a message box with an OK button to inform the user
    /// </summary>
    /// <param name="token">The message token to listen for</param>
    /// <param name="message">Function that returns the message text based on the message value</param>
    /// <param name="caption">Function that returns the caption text</param>
    private void RegisterNotificationHandler(string token, Func<ValueChangedMessage<string>, string> message,
        Func<string> caption)
    {
        WeakReferenceMessenger.Default.Register<TranslationDialog, ValueChangedMessage<string>, string>(
            this,
            token,
            (r, m) =>
            {
                _ = MessageBox.Show(message.Invoke(m),
                    caption(),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
    }

    /// <summary>
    ///     Creates an array of notification message handler configurations
    ///     Each configuration defines a message box that can be shown for a specific notification
    /// </summary>
    /// <returns>An array of notification handler configurations</returns>
    private static (string, Func<ValueChangedMessage<string>, string>, Func<string>)[] CreateNotificationHandlers()
    {
        return
        [
            (MessageTokens.TranslatedTextInvalid, _ => Strings.TranslatedTextInvalidMessage,
                () => Strings.TranslatedTextInvalidCaption),
            (MessageTokens.TranslateError, m => m.Value, () => Strings.TranslateErrorCaption)
        ];
    }

    /// <summary>
    ///     Handles the closed event of the translate dialog
    ///     Unregisters all message handlers to prevent memory leaks when the dialog is closed
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="e">The event arguments</param>
    private void TranslationDialog_OnClosed(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}