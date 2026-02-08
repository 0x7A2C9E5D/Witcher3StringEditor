using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for SaveDialog.xaml
///     This dialog allows users to configure save settings and save The Witcher 3 string items to a file
/// </summary>
public partial class SaveDialog
{
    /// <summary>
    ///     Initializes a new instance of the SaveDialog class
    ///     Sets up the UI components and registers message handlers
    /// </summary>
    public SaveDialog()
    {
        InitializeComponent(); // Initialize the UI components
        RegisterMessageHandler(); // Register message handler for save operation results
    }

    /// <summary>
    ///     Registers a message handler for save operation results
    ///     Shows a message box with the result of the save operation (success or failure)
    /// </summary>
    private void RegisterMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<SaveDialog, ValueChangedMessage<bool>, string>(
            this, MessageTokens.Save, static (_, m) =>
            {
                MessageBox.Show(m.Value ? Strings.SaveSuccess : Strings.SaveFailure,
                    Strings.SaveResult,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
    }

    /// <summary>
    ///     Handles the closed event of the save dialog
    ///     Unregisters message handlers to prevent memory leaks when the dialog is closed
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="e">The event arguments</param>
    private void SaveDialog_OnClosed(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}