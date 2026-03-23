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
        var found = dictionaryManager.Find(null).ToList();
        Log.Information("Found {Count} dictionaries in total.", found.Count);
        var groups = found.GroupBy(x => x.TargetLanguage);
        foreach (var group in groups)
            DictionaryGroups.Add(new DictionaryGroup(group.Key, [..group]));
        Log.Information("Grouped dictionaries into {GroupCount} groups based on target language.",
            DictionaryGroups.Count);
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
                if (dictionaryInfo == null) return;

                UpdateOrAddDictionaryToGroups(dictionaryInfo);
                _ = WeakReferenceMessenger.Default.Send(string.Empty, MessageTokens.DictionaryImported);
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
    ///     Updates the DictionaryGroups collection by first removing any existing entry with the same path,
    ///     then adding the new dictionaryInfo to the group corresponding to its TargetLanguage.
    ///     This ensures that each file path exists only once and in the correct language group.
    /// </summary>
    /// <param name="dictionaryInfo">The dictionary info to add or update.</param>
    private void UpdateOrAddDictionaryToGroups(DictionaryInfo dictionaryInfo)
    {
        // Step 1: Remove any existing entry with the same path from any group
        RemoveExistingEntryByPath(dictionaryInfo.Path);

        // Step 2: Add the new entry to the correct target language group
        AddToTargetLanguageGroup(dictionaryInfo);
    }

    /// <summary>
    ///     Removes an existing DictionaryInfo from its group if found, based on the provided file path.
    ///     Also removes the entire group if it becomes empty after removal.
    /// </summary>
    /// <param name="path">The file path of the dictionary to remove.</param>
    private void RemoveExistingEntryByPath(string path)
    {
        foreach (var group in DictionaryGroups.ToList()) // Use ToList() to avoid modification during enumeration
        {
            var existingEntry =
                group.Dictionaries.FirstOrDefault(d => d.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (existingEntry == null) continue;
            group.Dictionaries.Remove(existingEntry);
            Log.Information("Removed duplicate dictionary with same path from {OldLanguage}: {Path}",
                group.TargetLanguage.EnglishName, path);

            // If the group is now empty, remove the group itself
            if (group.Dictionaries.Count == 0) DictionaryGroups.Remove(group);
            break; // Path is unique, so we can exit after finding and removing the first match
        }
    }

    /// <summary>
    ///     Adds the given dictionaryInfo to the group matching its TargetLanguage.
    ///     Creates a new group if no matching group exists.
    /// </summary>
    /// <param name="dictionaryInfo">The dictionary info to add.</param>
    private void AddToTargetLanguageGroup(DictionaryInfo dictionaryInfo)
    {
        var targetGroup = DictionaryGroups.FirstOrDefault(g => Equals(g.TargetLanguage, dictionaryInfo.TargetLanguage));

        if (targetGroup == null)
        {
            // Create a new group for this language
            DictionaryGroups.Add(new DictionaryGroup(dictionaryInfo.TargetLanguage, [dictionaryInfo]));
            Log.Information("Created new group for {Language} and added dictionary: {Path}",
                dictionaryInfo.TargetLanguage.EnglishName, dictionaryInfo.Path);
        }
        else
        {
            // Add to existing group
            targetGroup.Dictionaries.Add(dictionaryInfo);
            Log.Information("Added dictionary to existing group {Language}: {Path}",
                dictionaryInfo.TargetLanguage.EnglishName, dictionaryInfo.Path);
        }
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
            var found = DictionaryGroups
                .FirstOrDefault(x => Equals(x.TargetLanguage, dictionary.TargetLanguage));
            found?.Dictionaries.Remove(dictionary);
            if (found?.Dictionaries.Count == 0)
                DictionaryGroups.Remove(found);
        }
    }
}