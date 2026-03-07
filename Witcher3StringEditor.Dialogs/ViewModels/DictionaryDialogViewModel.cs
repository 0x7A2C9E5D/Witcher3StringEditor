using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public partial class DictionaryDialogViewModel : ObservableObject, IModalDialogViewModel
{
    private readonly IDialogService dialogService;
    private readonly IDictionaryService dictionaryService;

    public DictionaryDialogViewModel(IDictionaryService dictionaryService, IDialogService dialogService)
    {
        this.dialogService = dialogService;
        this.dictionaryService = dictionaryService;
        dictionaryService.Dictionaries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Dictionaries));
    }

    public ObservableCollection<XliffInfo> Dictionaries => dictionaryService.Dictionaries;

    public bool? DialogResult => true;

    [RelayCommand]
    private async Task AddDictionaryFromFile()
    {
        using var storageFile = await dialogService.ShowOpenFileDialogAsync(this, new OpenFileDialogSettings
        {
            Filters =
            [
                new FileFilter(Strings.FileFormatSupported, [".xliff", ".xlf"])
            ]
        });
        if (storageFile is not null &&
            Path.GetExtension(storageFile.LocalPath) is ".xliff" or ".xlf")
        {
            dictionaryService.AddDictionaryFromFile(storageFile.LocalPath);
        }
    }
}