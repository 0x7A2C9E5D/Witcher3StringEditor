using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using ZLinq;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public partial class DictionaryManagerDialogViewModel : ObservableObject, IModalDialogViewModel
{
    /// <summary>
    ///     The dialog service.
    /// </summary>
    private readonly IDialogService dialogService;

    private readonly IDictionaryManager dictionaryManager;

    private readonly IDictionaryProvider dictionaryProvider;

    /// <summary>
    ///     The dictionary terms.
    /// </summary>
    [ObservableProperty] private Dictionary<string, string>? dictionaryTerms;

    /// <summary>
    ///     The selected dictionary.
    /// </summary>
    [ObservableProperty] private DictionaryInfo? selectedDictionary;

    /// <summary>
    ///     Initializes a new instance of the DictionaryDialogViewModel class.
    /// </summary>
    /// <param name="dictionaryManager"></param>
    /// <param name="dictionaryProvider"></param>
    /// <param name="dialogService"></param>
    public DictionaryManagerDialogViewModel(IDictionaryManager dictionaryManager,
        IDictionaryProvider dictionaryProvider,
        IDialogService dialogService)
    {
        this.dialogService = dialogService;
        this.dictionaryManager = dictionaryManager;
        this.dictionaryProvider = dictionaryProvider;
        var found = dictionaryManager.Find(null);
        found.GroupBy(x => x.TargetLanguage).ForEach(g =>
        {
            var group = new DictionaryGroup(g.Key, g.Select(x => x).ToList());
            DictionaryGroups.Add(group);
        });
    }

    /// <summary>
    ///     Groups of dictionaries.
    /// </summary>
    public ObservableCollection<DictionaryGroup> DictionaryGroups { get; } = [];

    /// <summary>
    ///     Dialog result.
    /// </summary>
    public bool? DialogResult => true;

    /// <summary>
    ///     Selected dictionary.
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedDictionaryChanged(DictionaryInfo? value)
    {
        _ = Task.Run(async () =>
        {
            var terms = value is null ? [] : await dictionaryProvider.GetEntries(value);

            await Application.Current.Dispatcher.InvokeAsync(() => { DictionaryTerms = terms; });
        });
    }

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
                new FileFilter(Strings.FileFormatTextFile, ".txt")
            ]
        });

        try
        {
            if (storageFile is not null &&
                Path.GetExtension(storageFile.LocalPath) is ".txt") // If file is a text file
            {
                var dictionaryInfo =
                    await dictionaryManager.Import(storageFile.LocalPath);
                if (dictionaryInfo == null) return; // No dictionary found
                var matchingGroup = DictionaryGroups
                    .AsValueEnumerable()
                    .Where(x => Equals(x.TargetLanguage, dictionaryInfo.TargetLanguage))
                    .ToList(); // Find existing group
                if (matchingGroup.Count != 0) // If group exists, remove existing entries
                {
                    var group = matchingGroup[0]; // Get group
                    var fileName = Path.GetFileName(dictionaryInfo.Path); // Get file name
                    var existingEntries = group.Dictionaries
                        .AsValueEnumerable()
                        .Where(x => Path.GetFileName(x.Path) == fileName).ToArray(); // Find existing entries
                    existingEntries.ForEach(x => group.Dictionaries.Remove(x)); // Remove existing entries
                    matchingGroup[0].Dictionaries.Add(dictionaryInfo); // Add new entry
                }
                else
                {
                    var group = new DictionaryGroup(dictionaryInfo.TargetLanguage,
                        [dictionaryInfo]); // Create new group
                    DictionaryGroups.Add(group); // Add new group
                }
            }
        }
        catch (Exception e)
        {
            _ = WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.ImportDictionaryFailed);
            Log.Error(e, "Error loading dictionary: {Path}", storageFile?.LocalPath);
        }
    }

    /// <summary>
    ///     Removes the specified dictionary.
    /// </summary>
    /// <param name="dictionary"></param>
    [RelayCommand]
    private async Task RemoveDictionary(DictionaryInfo? dictionary)
    {
        if (dictionary is null) return;
        if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.RemoveDictionaryConfirm))
        {
            dictionaryManager.Remove(dictionary);
            var found = DictionaryGroups
                .FirstOrDefault(x => x.Dictionaries.Contains(dictionary));
            if (found is null) return;
            if (found.Dictionaries.Count == 1)
                DictionaryGroups.Remove(found);
            else
                found.Dictionaries.Remove(dictionary);
            SelectedDictionary = null;
            DictionaryTerms = [];
        }
    }
}