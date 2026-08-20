using GTranslate.Translators;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dictionary.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Centralizes the creation of dialog view models
///     View models are created here so that the main window view model only depends on this factory
///     instead of knowing the concrete dialog view model types and their dependencies
/// </summary>
internal interface IDialogViewModelFactory
{
    /// <summary>
    ///     Creates a view model for adding or editing a string item
    /// </summary>
    /// <param name="item">The item to edit, or a new item template for adding</param>
    EditDataDialogViewModel CreateEditDialog(ITrackableW3StringItem item);

    /// <summary>
    ///     Creates a view model for the delete confirmation dialog
    /// </summary>
    /// <param name="items">The items to delete</param>
    DeleteDataDialogViewModel CreateDeleteDialog(IEnumerable<IW3StringItem> items);

    /// <summary>
    ///     Creates a view model for the backup management dialog
    /// </summary>
    BackupDialogViewModel CreateBackupDialog();

    /// <summary>
    ///     Creates a view model for the save dialog
    /// </summary>
    /// <param name="items">The items to save</param>
    /// <param name="outputDirectory">The initial output directory</param>
    SaveDialogViewModel CreateSaveDialog(IReadOnlyList<IW3StringItem> items, string outputDirectory);

    /// <summary>
    ///     Creates a view model for the log viewer dialog
    /// </summary>
    LogDialogViewModel CreateLogDialog();

    /// <summary>
    ///     Creates a view model for the settings dialog
    /// </summary>
    /// <param name="translatorNames">The display names of the available translators</param>
    SettingDialogViewModel CreateSettingsDialog(IEnumerable<string> translatorNames);

    /// <summary>
    ///     Creates a view model for the about dialog
    /// </summary>
    /// <param name="aboutInfo">The application information to display</param>
    AboutDialogViewModel CreateAboutDialog(IReadOnlyDictionary<string, object?> aboutInfo);

    /// <summary>
    ///     Creates a view model for the recent files dialog
    /// </summary>
    RecentDialogViewModel CreateRecentDialog();

    /// <summary>
    ///     Creates a view model for the translation dialog
    /// </summary>
    /// <param name="translator">The translator to use; the caller is responsible for disposing it</param>
    /// <param name="items">The items to translate</param>
    /// <param name="index">The index of the initially selected item</param>
    /// <param name="dictionaryService">The dictionary service, or null if dictionaries are not supported</param>
    TranslationDialogViewModel CreateTranslationDialog(ITranslator translator,
        IReadOnlyList<ITrackableW3StringItem> items, int index, IDictionaryService? dictionaryService);

    /// <summary>
    ///     Creates a view model for the dictionary management dialog
    /// </summary>
    DictionaryManagerDialogViewModel CreateDictionaryManagerDialog();
}
