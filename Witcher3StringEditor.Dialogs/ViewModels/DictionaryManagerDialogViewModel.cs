using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using MoreLinq.Extensions;
using Serilog;
using Witcher3StringEditor.Dialogs.Models;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public partial class DictionaryManagerDialogViewModel : ObservableObject, IModalDialogViewModel
{
    private readonly IDialogService dialogService;
    private readonly IDictionaryMangerService dictionaryMangerService;

    [ObservableProperty] private string selectedDictionary = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the DictionaryDialogViewModel class.
    /// </summary>
    /// <param name="dictionaryMangerService"></param>
    /// <param name="dialogService"></param>
    public DictionaryManagerDialogViewModel(IDictionaryMangerService dictionaryMangerService,
        IDialogService dialogService)
    {
        this.dialogService = dialogService;
        this.dictionaryMangerService = dictionaryMangerService;
        var found = this.dictionaryMangerService.Find(null);
        found.GroupBy(x => x.TargetLanguage).ForEach(g =>
        {
            var group = new DictionaryGroup(g.Key, g.Select(x => x).ToList());
            DictionaryGroups.Add(group);
        });
    }

    public ObservableCollection<DictionaryGroup> DictionaryGroups { get; } = [];

    /// <summary>
    ///     Dialog result.
    /// </summary>
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
                new FileFilter(Strings.FileFormatXliffFile, [".xliff", ".xlf"])
            ]
        });

        try
        {
            if (storageFile is not null &&
                Path.GetExtension(storageFile.LocalPath) is ".xliff" or ".xlf")
            {
                var dictionaryInfo = dictionaryMangerService.Import(storageFile.LocalPath);
                if (dictionaryInfo == null) return;
                var found = DictionaryGroups.Where(x => Equals(x.TargetLanguage, dictionaryInfo.TargetLanguage))
                    .ToList();
                if (found.Count != 0)
                {
                    found[0].Dictionaries.Add(dictionaryInfo);
                }
                else
                {
                    var group = new DictionaryGroup(dictionaryInfo.TargetLanguage, [dictionaryInfo]);
                    DictionaryGroups.Add(group);
                }
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
        var found = DictionaryGroups
            .FirstOrDefault(x => x.Dictionaries.Contains(dictionary));
        if (found is null) return;
        if (found.Dictionaries.Count == 1)
            DictionaryGroups.Remove(found);
        else
            found.Dictionaries.Remove(dictionary);
    }
}