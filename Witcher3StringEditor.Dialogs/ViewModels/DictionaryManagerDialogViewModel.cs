using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Serilog;
using Witcher3StringEditor.Dialogs.Models;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public partial class DictionaryManagerDialogViewModel : ObservableObject, IModalDialogViewModel
{
    /// <summary>
    ///     The dialog service.
    /// </summary>
    private readonly IDialogService dialogService;

    /// <summary>
    ///     Provides access to the dictionary management functionality used by this class.
    /// </summary>
    private readonly IDictionaryManager dictionaryManager;

    /// <summary>
    ///     Provides access to the underlying dictionary provider used for retrieving dictionary data.
    /// </summary>
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
        var groups = found.GroupBy(x => x.TargetLanguage);
        foreach (var group in groups) DictionaryGroups.Add(new DictionaryGroup(group.Key, [..group]));
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
                // Try to import the dictionary, if successful, regroup dictionaries to reflect changes
                var dictionaryInfo =
                    await dictionaryManager.Import(storageFile.LocalPath);
                if (dictionaryInfo != null) ReGroupDictionaries();
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
    ///     Regroups all dictionaries by target language.
    /// </summary>
    private void ReGroupDictionaries()
    {
        // Regroup dictionaries by target language
        DictionaryGroups.Clear();
        var groups = dictionaryManager.Find(null)
            .GroupBy(x => x.TargetLanguage);
        foreach (var group in groups) DictionaryGroups.Add(new DictionaryGroup(group.Key, [..group]));

        // Validate selected dictionary and clear if removed
        if (DictionaryGroups.SelectMany(g => g.Dictionaries)
            .Contains(SelectedDictionary)) return;
        SelectedDictionary = null;
        DictionaryTerms = [];
    }

    /// <summary>
    ///     Removes the specified dictionary.
    /// </summary>
    /// <param name="dictionary"></param>
    [RelayCommand]
    private async Task RemoveDictionary(DictionaryInfo? dictionary)
    {
        if (dictionary is null) return; // If no dictionary is selected, do nothing
        if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.RemoveDictionaryConfirm)) // Ask for confirmation before removing the dictionary
        {
            dictionaryManager.Remove(dictionary); // Remove the dictionary
            ReGroupDictionaries(); // Rebuild groups to reflect changes and check if selected dictionary is still valid
        }
    }
}