using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Serilog;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public partial class DictionaryDialogViewModel : ObservableObject, IModalDialogViewModel
{
    private readonly IDialogService dialogService;
    private readonly IDictionaryMangerService dictionaryMangerService;

    /// <summary>
    ///     Initializes a new instance of the DictionaryDialogViewModel class.
    /// </summary>
    /// <param name="dictionaryMangerService"></param>
    /// <param name="dialogService"></param>
    public DictionaryDialogViewModel(IDictionaryMangerService dictionaryMangerService, IDialogService dialogService)
    {
        this.dialogService = dialogService;
        this.dictionaryMangerService = dictionaryMangerService;
        var found = this.dictionaryMangerService.Find(null);
        found.ForEach(x => Dictionaries.Add(x));
    }

    public ObservableCollection<DictionaryInfo> Dictionaries { get; } = [];

    public bool? DialogResult => true;

    /// <summary>
    ///     Adds a dictionary from a file.
    /// </summary>
    [RelayCommand]
    private async Task ImportDictionary()
    {
        using var storageFile = await dialogService.ShowOpenFileDialogAsync(this, new OpenFileDialogSettings
        {
            Filters =
            [
                new FileFilter(Strings.FileFormatSupported, [".xliff", ".xlf"])
            ]
        });

        try
        {
            if (storageFile is not null &&
                Path.GetExtension(storageFile.LocalPath) is ".xliff" or ".xlf")
            {
                var dictionaryInfo = dictionaryMangerService.Import(storageFile.LocalPath);
                if (dictionaryInfo == null) return;
                Dictionaries.Add(dictionaryInfo);
            }
        }
        catch (Exception e)
        {
            _ = WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.ImportDictionaryFailed);
            Log.Warning(e, "Invalid dictionary file: {Path}", storageFile?.LocalPath);
            Log.Error(e, "Error loading dictionary: {Path}", storageFile?.LocalPath);
        }
    }

    /// <summary>
    ///     Removes the specified dictionary.
    /// </summary>
    /// <param name="dictionary"></param>
    [RelayCommand]
    private void RemoveDictionary(DictionaryInfo dictionary)
    {
        dictionaryMangerService.Remove(dictionary);
        Dictionaries.Remove(dictionary);
    }
}