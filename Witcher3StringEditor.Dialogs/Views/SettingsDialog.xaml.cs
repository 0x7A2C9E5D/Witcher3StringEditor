using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Strings = Witcher3StringEditor.Locales.Strings;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for SettingsDialog.xaml
///     This dialog allows users to configure application settings including paths, preferences, and UI options
/// </summary>
public partial class SettingsDialog
{
    /// <summary>
    ///     Initializes a new instance of the SettingsDialog class
    ///     Calls InitializeComponent to set up the UI components defined in the XAML file
    /// </summary>
    public SettingsDialog()
    {
        InitializeComponent();
        RegisterMessageHandler();
    }

    /// <summary>
    ///     Registers a message handler for log cleanup messages
    ///     Shows a message box with the result of the log cleanup operation (success or failure)
    /// </summary>
    private void RegisterMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<SettingsDialog, string, string>(this, MessageTokens.LogsNoNeedToClean,
            static (_, m) =>
            {
                MessageBox.Show(Strings.LogsNoNeedToCleanMessage, Strings.LogCleanupCaption,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        WeakReferenceMessenger.Default.Register<SettingsDialog, string, string>(this, MessageTokens.LogsCleaned,
            static (_, m) =>
            {
                MessageBox.Show(Strings.LogsCleanedMessage, Strings.LogCleanupCaption,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
    }

    /// <summary>
    ///     Unregisters message handlers to prevent memory leaks when the dialog is closed
    /// </summary>
    private void SettingsDialog_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}