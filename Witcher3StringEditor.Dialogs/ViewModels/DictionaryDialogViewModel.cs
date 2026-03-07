using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HanumanInstitute.MvvmDialogs;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public class DictionaryDialogViewModel : ObservableObject, IModalDialogViewModel
{
    private readonly IDictionaryService dictionaryService;

    public DictionaryDialogViewModel(IDictionaryService dictionaryService)
    {
        this.dictionaryService = dictionaryService;
        Dictionaries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Dictionaries));
    }

    public ObservableCollection<XliffInfo> Dictionaries => dictionaryService.Dictionaries;

    public bool? DialogResult => true;
}