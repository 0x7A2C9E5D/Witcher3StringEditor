using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     DictionaryDialog.xaml 的交互逻辑
/// </summary>
public partial class DictionaryDialog
{
    public DictionaryDialog()
    {
        InitializeComponent();
        RegisterMessageHandlers();
    }

    private void RegisterMessageHandlers()
    {
        WeakReferenceMessenger.Default.Register<DictionaryDialog, AsyncRequestMessage<bool>, string>(this,
            MessageTokens.ImportDictionaryFailed,
            (_, _) =>
            {
                MessageBox.Show(I18NExtension.Translate(LangKeys.ImportDictionaryFailedMessage),
                    I18NExtension.Translate(LangKeys.ImportDictionaryFailedCaption), MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
    }

    private void DictionaryDialog_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}