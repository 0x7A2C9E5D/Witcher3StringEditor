using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for DictionaryDialog.xaml
/// </summary>
public partial class DictionaryManagerDialog
{
    public DictionaryManagerDialog()
    {
        InitializeComponent(); // InitializeComponent
        SetupSearchHelper(); // Setup search helper
        RegisterMessageHandlers(); // Register message handlers
        
    }

    /// <summary>
    ///     Sets up the search helper for the data grid
    ///     Enables filtering and disables case-sensitive search
    /// </summary>
    private void SetupSearchHelper()
    {
        SfDataGrid.SearchHelper.AllowFiltering = true;
        SfDataGrid.SearchHelper.AllowCaseSensitiveSearch = false;
        SfDataGrid.SearchHelper.CanHighlightSearchText = false;
    }
    
    /// <summary>
    ///     Registers message handlers for the DictionaryDialog
    /// </summary>
    private void RegisterMessageHandlers()
    {
        WeakReferenceMessenger.Default.Register<DictionaryManagerDialog, AsyncRequestMessage<bool>, string>(this,
            MessageTokens.ImportDictionaryFailed,
            (_, _) =>
            {
                MessageBox.Show(I18NExtension.Translate(LangKeys.ImportDictionaryFailedMessage),
                    I18NExtension.Translate(LangKeys.ImportDictionaryFailedCaption), MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
    }

    /// <summary>
    ///     Unregisters message handlers when the DictionaryDialog is closed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DictionaryDialog_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}