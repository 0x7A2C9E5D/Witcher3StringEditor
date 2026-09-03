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
    /// <inheritdoc />
    public EditDataDialogViewModel CreateEditDialog(ITrackableW3StringItem item)
    {
        return new EditDataDialogViewModel(item);
    }

    /// <inheritdoc />
    public DeleteDataDialogViewModel CreateDeleteDialog(IEnumerable<IW3StringItem> items)
    {
        return new DeleteDataDialogViewModel(items);
    }

    /// <inheritdoc />
    public BackupDialogViewModel CreateBackupDialog()
    {
        return new BackupDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IBackupService>(),
            serviceProvider.GetRequiredService<IDialogService>());
    }

    /// <inheritdoc />
    public SaveDialogViewModel CreateSaveDialog(IReadOnlyList<IW3StringItem> items, string outputDirectory)
    {
        return new SaveDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IW3Serializer>(),
            serviceProvider.GetRequiredService<IDialogService>(),
            items,
            outputDirectory);
    }

    /// <inheritdoc />
    public LogDialogViewModel CreateLogDialog()
    {
        return new LogDialogViewModel(serviceProvider.GetRequiredService<ILogAccessService>());
    }

    /// <inheritdoc />
    public SettingDialogViewModel CreateSettingsDialog(IEnumerable<string> translatorNames)
    {
        return new SettingDialogViewModel(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IDialogService>(),
            serviceProvider.GetRequiredService<IShellOpenService>(),
            translatorNames,
            serviceProvider.GetRequiredService<ICultureResolver>().SupportedCultures);
    }

    /// <inheritdoc />
    public AboutDialogViewModel CreateAboutDialog(IReadOnlyDictionary<string, object?> aboutInfo)
    {
        return new AboutDialogViewModel(aboutInfo);
    }

    /// <inheritdoc />
    public RecentDialogViewModel CreateRecentDialog()
    {
        return new RecentDialogViewModel(
            serviceProvider.GetRequiredService<IRecentFilesService>(),
            serviceProvider.GetRequiredService<IDialogService>());
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public DictionaryManagerDialogViewModel CreateDictionaryManagerDialog()
    {
        return new DictionaryManagerDialogViewModel(
            serviceProvider.GetRequiredService<IDictionaryManager>(),
            serviceProvider.GetRequiredService<IDictionaryProvider>(),
            serviceProvider.GetRequiredService<IDialogService>());
    }
}
