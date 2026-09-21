using GTranslate.Translators;
using HanumanInstitute.MvvmDialogs;
using Microsoft.Extensions.DependencyInjection;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Serializers.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Creates dialog view models by resolving their service dependencies from the DI container
///     and passing through the runtime arguments provided by the caller
///     Explicit construction is used instead of <c>ActivatorUtilities</c> so that nullable
///     runtime arguments (e.g. the optional dictionary service of the translation dialog)
///     are passed through unchanged instead of being resolved by the container
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve view model dependencies</param>
internal sealed class DialogViewModelFactory(IServiceProvider serviceProvider) : IDialogViewModelFactory
{
    /// <summary>
    ///     Creates a new instance of the EditDataDialogViewModel class
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public EditDataDialogViewModel CreateEditDialog(ITrackableW3StringItem item)
    {
        return new EditDataDialogViewModel(item);
    }

    /// <summary>
    ///     Creates a new instance of the DeleteDataDialogViewModel class
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public DeleteDataDialogViewModel CreateDeleteDialog(IEnumerable<IW3StringItem> items)
    {
        return new DeleteDataDialogViewModel(items);
    }

    /// <summary>
    ///     Creates a new instance of the BackupDialogViewModel class
    /// </summary>
    /// <returns></returns>
    public BackupDialogViewModel CreateBackupDialog()
    {
        return new BackupDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IBackupService>(),
            serviceProvider.GetRequiredService<IDialogService>());
    }

    /// <summary>
    ///     Creates a new instance of the SaveDialogViewModel class
    /// </summary>
    /// <param name="items"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public SaveDialogViewModel CreateSaveDialog(IReadOnlyList<IW3StringItem> items, string outputDirectory)
    {
        return new SaveDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IW3Serializer>(),
            serviceProvider.GetRequiredService<IDialogService>(),
            items,
            outputDirectory);
    }

    /// <summary>
    ///     Creates a new instance of the LogDialogViewModel class
    /// </summary>
    /// <returns></returns>
    public LogDialogViewModel CreateLogDialog()
    {
        return new LogDialogViewModel(serviceProvider.GetRequiredService<ILogAccessService>());
    }

    /// <summary>
    ///     Creates a new instance of the SettingsDialogViewModel class
    /// </summary>
    /// <param name="translatorNames"></param>
    /// <returns></returns>
    public SettingDialogViewModel CreateSettingsDialog(IEnumerable<string> translatorNames)
    {
        return new SettingDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IDialogService>(),
            serviceProvider.GetRequiredService<IShellOpenService>(),
            translatorNames,
            serviceProvider.GetRequiredService<ICultureResolver>().SupportedCultures);
    }

    /// <summary>
    ///     Creates a new instance of the AboutDialogViewModel class
    /// </summary>
    /// <param name="aboutInfo"></param>
    /// <returns></returns>
    public AboutDialogViewModel CreateAboutDialog(IReadOnlyDictionary<string, object?> aboutInfo)
    {
        return new AboutDialogViewModel(aboutInfo);
    }
    
    /// <summary>
    ///     Creates a new instance of the RecentDialogViewModel class
    /// </summary>
    /// <returns></returns>
    public RecentDialogViewModel CreateRecentDialog()
    {
        return new RecentDialogViewModel(
            serviceProvider.GetRequiredService<IRecentFilesService>(),
            serviceProvider.GetRequiredService<IDialogService>());
    }
    
    /// <summary>
    ///     Creates a new instance of the TranslationDialogViewModel class
    /// </summary>
    /// <param name="translator"></param>
    /// <param name="items"></param>
    /// <param name="index"></param>
    /// <param name="dictionaryService"></param>
    /// <returns></returns>
    public TranslationDialogViewModel CreateTranslationDialog(ITranslator translator,
        IReadOnlyList<ITrackableW3StringItem> items, int index, IDictionaryService? dictionaryService)
    {
        return new TranslationDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            translator,
            items,
            index,
            serviceProvider.GetRequiredService<IDialogService>(),
            dictionaryService);
    }

    /// <summary>
    ///     Creates a new instance of the BatchItemsTranslationViewModel class
    /// </summary>
    /// <returns></returns>
    public DictionaryManagerDialogViewModel CreateDictionaryManagerDialog()
    {
        return new DictionaryManagerDialogViewModel(
            serviceProvider.GetRequiredService<IDictionaryManager>(),
            serviceProvider.GetRequiredService<IDictionaryProvider>(),
            serviceProvider.GetRequiredService<IDialogService>());
    }
}