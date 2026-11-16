using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HanumanInstitute.MvvmDialogs;
using iNKORE.UI.WPF.DragDrop;
using Microsoft.Extensions.DependencyInjection;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Factories;
using Witcher3StringEditor.Models;
using Witcher3StringEditor.Providers;
using Witcher3StringEditor.Serializers;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Main window view model for the Witcher 3 String Editor application
///     Handles file operations, UI commands, and coordination between services and dialogs
/// </summary>
/// <remarks>
///     Split by responsibility into sibling partial files: file commands (FileOperations), the file dialog and the
///     readable formats (FileDialog), editing and merging (Editing), paging and search (Paging), dialogs (Dialogs),
///     shell commands (Shell), drag and drop (DragDrop), messenger wiring (Messages) and window events (Window)
/// </remarks>
internal partial class MainWindowViewModel : ObservableObject, IDropTarget
{
    // Dependency services
    private readonly IDialogService dialogService; // Get dialog service
    private readonly IDialogViewModelFactory dialogViewModelFactory; // Get dialog view model factory
    private readonly IW3StringSerializerCoordinator iw3StringSerializer; // Get iw3StringSerializer service
    private readonly IRecentFilesService recentFilesService; // Get recent files service
    private readonly IServiceProvider serviceProvider; // Get service provider
    private readonly ISettingsManagerService settingsManagerService; // Get settings manager service
    private readonly IShellOpenService shellOpenService; // Get explorer service
    private readonly ITranslatorProvider translatorProvider; // Get translator provider


    /// <summary>
    ///     Initializes a new instance of the MainWindowViewModel class
    /// </summary>
    /// <param name="dialogService"></param>
    /// <param name="dialogViewModelFactory"></param>
    /// <param name="shellOpenService"></param>
    /// <param name="recentFilesService"></param>
    /// <param name="settingsManagerService"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="translatorProvider"></param>
    /// <param name="iw3StringSerializer"></param>
    public MainWindowViewModel(
        IDialogService dialogService,
        IDialogViewModelFactory dialogViewModelFactory,
        IShellOpenService shellOpenService,
        IRecentFilesService recentFilesService,
        ISettingsManagerService settingsManagerService,
        IServiceProvider serviceProvider,
        ITranslatorProvider translatorProvider,
        IW3StringSerializerCoordinator iw3StringSerializer)
    {
        this.dialogService = dialogService;
        this.dialogViewModelFactory = dialogViewModelFactory;
        this.shellOpenService = shellOpenService;
        this.recentFilesService = recentFilesService;
        this.settingsManagerService = settingsManagerService;
        this.serviceProvider = serviceProvider;
        this.translatorProvider = translatorProvider;
        this.iw3StringSerializer = iw3StringSerializer;
        // Resolve the dictionary manager at startup so the dictionary directory is scanned while the window is opening
        _ = serviceProvider.GetRequiredService<IDictionaryManager>();
        PageSize = AppSettings.PageSize; // Set page size
        IsSupportDictionary =
            AppSettings.Translator == "MicrosoftTranslator"; // Set dictionary support based on translator
        RegisterMessengerHandlers(); // Register all message handlers
    }

    /// <summary>
    ///     Gets or sets the collection of The Witcher 3 string items
    ///     Notifies multiple commands when this property changes
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(MergeDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowSaveDialogCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowTranslateDialogCommand))]
    public partial ObservableCollection<W3StringItem>? W3StringItems { get; set; }

    /// <summary>
    ///     Registers a message handler to listen for translator changes and update dictionary support status
    /// </summary>
    [ObservableProperty]
    public partial bool IsSupportDictionary { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether an update is available
    /// </summary>
    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    /// <summary>
    ///     Gets or sets the output folder path
    ///     Notifies OpenWorkingFolderCommand when this property changes
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenWorkingFolderCommand))]
    // Resharper disable once MemberCanBeMadeStatic.Local
    private partial string OutputFolder { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the page size
    /// </summary>
    [ObservableProperty]
    public partial int PageSize { get; private set; }

    /// <summary>
    ///     Gets or sets the application settings
    /// </summary>
    private IAppSettings AppSettings => settingsManagerService.AppSettings;

    /// <summary>
    ///     Gets a value indicating whether there are The Witcher 3 string items
    /// </summary>
    private bool HasW3StringItems => W3StringItems?.Count > 0;
}