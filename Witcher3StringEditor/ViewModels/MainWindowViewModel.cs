using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using GTranslate.Translators;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Serilog;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Models;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Services;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Main window view model for the Witcher 3 String Editor application
///     Handles file operations, UI commands, and coordination between services and dialogs
/// </summary>
internal partial class MainWindowViewModel : ObservableObject
{
    // Dependency services
    private readonly IAppSettings appSettings; // Get application settings service
    private readonly IBackupService backupService; // Get backup service
    private readonly IDialogService dialogService; // Get dialog service
    private readonly IDictionaryManager dictionaryManager; // Get dictionary manager
    private readonly IDictionaryProvider dictionaryProvider; // Get dictionary provider
    private readonly IServiceProvider serviceProvider; // Get service provider
    private readonly ISettingsManagerService settingsManagerService; // Get settings manager service
    private readonly IW3Serializer w3Serializer; // Get serializer service

    /// <summary>
    ///     Gets or sets the data from dropped files
    /// </summary>
    [ObservableProperty] private string[]? dropFileData;

    /// <summary>
    ///     Gets the filtered collection of W3String items
    /// </summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ShowTranslateDialogCommand))]
    private ObservableCollection<W3StringItemModel>? filteredW3StringItems;

    /// <summary>
    ///     Registers a message handler to listen for translator changes and update dictionary support status
    /// </summary>
    [ObservableProperty] private bool isSupportDictionary;

    /// <summary>
    ///     Gets or sets a value indicating whether an update is available
    /// </summary>
    [ObservableProperty] private bool isUpdateAvailable;

    /// <summary>
    ///     Gets or sets the output folder path
    ///     Notifies OpenWorkingFolderCommand when this property changes
    /// </summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OpenWorkingFolderCommand))]
    private string outputFolder = string.Empty;

    /// <summary>
    ///     Gets or sets the page size
    /// </summary>
    [ObservableProperty] private int pageSize;

    /// <summary>
    ///     Gets or sets the current search text used for filtering W3String items
    /// </summary>
    [ObservableProperty] private string searchText = string.Empty;

    /// <summary>
    ///     Gets or sets the collection of The Witcher 3 string items
    ///     Notifies multiple commands when this property changes
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(MergeDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowSaveDialogCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowTranslateDialogCommand))]
    private ObservableCollection<W3StringItemModel>? w3StringItems;

    /// <summary>
    ///     Initializes a new instance of the MainWindowViewModel class
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies</param>
    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider; // Store the service provider
        appSettings = serviceProvider.GetRequiredService<IAppSettings>(); // Get application settings service
        backupService = serviceProvider.GetRequiredService<IBackupService>(); // Get backup service
        dialogService = serviceProvider.GetRequiredService<IDialogService>(); // Get dialog service
        w3Serializer = serviceProvider.GetRequiredService<IW3Serializer>(); // Get serializer
        dictionaryManager = serviceProvider.GetRequiredService<IDictionaryManager>(); // Get dictionary manager
        dictionaryProvider = serviceProvider.GetRequiredService<IDictionaryProvider>(); // Get dictionary provider
        settingsManagerService =
            serviceProvider.GetRequiredService<ISettingsManagerService>(); // Get settings manager service
        PageSize = appSettings.PageSize; // Set page size
        IsSupportDictionary =
            appSettings.Translator == "MicrosoftTranslator"; // Set dictionary support based on translator
        RegisterMessengerHandlers(); // Register all message handlers
    }

    /// <summary>
    ///     Gets a value indicating whether there are The Witcher 3 string items
    /// </summary>
    private bool HasW3StringItems => W3StringItems?.Any() == true;

    /// <summary>
    ///     Gets a value indicating whether the working folder can be opened
    /// </summary>
    private bool CanOpenWorkingFolder
        => Directory.Exists(OutputFolder);

    /// <summary>
    ///     Gets a value indicating whether the game can be played
    /// </summary>
    private bool CanPlayGame => File.Exists(appSettings.GameExePath);

    /// <summary>
    ///     Gets a value indicating whether a file can be opened
    /// </summary>
    private bool CanOpenFile => File.Exists(appSettings.W3StringsPath);

    /// <summary>
    ///     Handles changes to the W3StringItems collection
    /// </summary>
    partial void OnW3StringItemsChanged(ObservableCollection<W3StringItemModel>? oldValue,
        ObservableCollection<W3StringItemModel>? newValue)
    {
        // Unsubscribe from the old collection's CollectionChanged event to prevent memory leaks
        if (oldValue is not null) oldValue.CollectionChanged -= W3StringItems_CollectionChanged;
        // Subscribe to the new collection's CollectionChanged event to handle item additions/removals
        if (newValue is not null) newValue.CollectionChanged += W3StringItems_CollectionChanged;
    }

    /// <summary>
    ///     Handles collection change events for the W3StringItems collection
    /// </summary>
    /// <param name="sender">The ObservableCollection that raised the event</param>
    /// <param name="e">The collection change event arguments</param>
    private void W3StringItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Return early if filteredW3StringItems is not initialized
        if (FilteredW3StringItems is null) return;
        // Handle item additions to the collection
        if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null })
        {
            // Add new items to the filtered results
            FilterW3StringItems(e.NewItems.OfType<W3StringItemModel>(), SearchText)
                .ForEach(x => FilteredW3StringItems.Add(x));
            // Log the number of items added
            Log.Information("Added {Count} items to filtered results", e.NewItems.Count);
        }

        // Handle item removals from the collection
        if (e is not { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }) return;
        // Remove deleted items from the filtered results by matching TrackingId
        e.OldItems.OfType<W3StringItemModel>()
            .ForEach(x => FilteredW3StringItems.Remove(x));
        // Log the number of items removed
        Log.Information("Removed {Count} items from filtered results", e.OldItems.Count);
    }

    /// <summary>
    ///     Registers message handlers for settings-related messages
    /// </summary>
    private void RegisterSettingsMessageHandlers()
    {
        // Register handler for W3Strings path change notifications
        WeakReferenceMessenger.Default
            .Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(this, MessageTokens.W3StringsPathChanged,
                (_, _) => OpenFileCommand
                    .NotifyCanExecuteChanged()); // Update OpenFile command state when W3Strings path changes
        // Register handler for GameExe path change notifications
        WeakReferenceMessenger.Default
            .Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(this, MessageTokens.GameExePathChanged,
                (_, _) => PlayGameCommand
                    .NotifyCanExecuteChanged()); // Update PlayGame command state when GameExe path changes
    }

    /// <summary>
    ///     Registers all message handlers for the view model
    /// </summary>
    private void RegisterMessengerHandlers()
    {
        RegisterFileMessageHandlers(); // Register file message handlers
        RegisterSettingsMessageHandlers(); // Register settings message handlers
        RegisterSearchMessageHandlers(); // Register search message handlers
        RegisterDictionarySupportMessageHandler(); // Register dictionary support message handler
    }

    /// <summary>
    ///     Registers a handler to monitor changes to the translator setting and updates dictionary support accordingly.
    /// </summary>
    private void RegisterDictionarySupportMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<string>, string>(
            this,
            MessageTokens.TranslatorChanged,
            (_, m) => { IsSupportDictionary = m.Value == "MicrosoftTranslator"; });
    }

    /// <summary>
    ///     Registers message handlers for search-related messages
    /// </summary>
    private void RegisterSearchMessageHandlers()
    {
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(
            this,
            MessageTokens.SearchRequested,
            (_, m) => { HandleSearchRequested(m.Value); });
    }

    /// <summary>
    ///     Handles search request
    /// </summary>
    private void HandleSearchRequested(bool isSearched)
    {
        if (isSearched)
        {
            FilteredW3StringItems = FilterW3StringItems(W3StringItems, SearchText).ToObservableCollection();
            Log.Information("Search completed. Found {Count} items matching {SearchText}",
                FilteredW3StringItems.Count, SearchText);
        }
        else
        {
            FilteredW3StringItems = null;
            Log.Information("Search cleared.");
        }
    }

    /// <summary>
    ///     Applies filter to W3String items based on search text
    /// </summary>
    /// <param name="items">The items to filter</param>
    /// <param name="searchText">The search text to filter by</param>
    /// <returns>Filtered collection of W3String items</returns>
    private static IEnumerable<W3StringItemModel> FilterW3StringItems(IEnumerable<W3StringItemModel>? items,
        string searchText)
    {
        // Return early if search text is empty or items are null
        if (string.IsNullOrWhiteSpace(searchText) || items is null)
            return items ?? [];
        // Filter items based on search text
        return items.Where(item =>
            item.StrId.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            item.KeyHex.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            item.KeyName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            item.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Registers message handlers for file-related messages
    /// </summary>
    private void RegisterFileMessageHandlers()
    {
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, AsyncRequestMessage<string, bool>, string>(
            // ReSharper disable once AsyncVoidMethod
            this,
            MessageTokens.RecentFileOpened,
            async void (_, m) => { await OpenFile(m.Request); }); // Open the requested file
    }

    /// <summary>
    ///     Handles the window loaded event
    ///     Logs application startup information and checks for updates
    /// </summary>
    [RelayCommand]
    private async Task WindowLoaded()
    {
        LogApplicationStartupInfo(); // Log application startup information
        await settingsManagerService.CheckSettings(); // Check application settings
        IsUpdateAvailable =
            await serviceProvider.GetRequiredService<ICheckUpdateService>().CheckUpdate(); // Check for updates
    }

    /// <summary>
    ///     Logs application startup information including version, OS, and other diagnostic data
    /// </summary>
    private void LogApplicationStartupInfo()
    {
        Log.Information("Application started."); // Log application start
        Log.Information("Application Version: {Version}", ThisAssembly.AssemblyFileVersion); // Log application version
        Log.Information("OS Version: {Version}", // Log OS version
            $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Log.Information(".Net Runtime: {Runtime}", RuntimeInformation.FrameworkDescription); // Log .NET runtime version
        Log.Information("Is Debug: {IsDebug}", !BuildInformation.IsReleaseBuild); // Log debug mode status
        Log.Information("Current Directory: {Directory}", Environment.CurrentDirectory); // Log current directory
        Log.Information("AppData Folder: {Folder}", AppPaths.AppDataDirectory); // Log AppData folder path
        var supportedCultures = serviceProvider.GetRequiredService<ICultureResolver>().SupportedCultures;
        Log.Information("Installed Language Packs: {Languages}",
            string.Join(", ", supportedCultures.Select(x => x.Name))); // Log installed language packs
        Log.Information("Current Language: {Language}", appSettings.Language); // Log current language
    }

    /// <summary>
    ///     Handles the window closing event
    ///     Cancels the close if there are unsaved changes
    /// </summary>
    /// <param name="e">The event arguments for the cancel event</param>
    [RelayCommand]
    private async Task WindowClosing(CancelEventArgs e)
    {
        if (W3StringItems?.Any() == true && // Check if there are any W3String items
            await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.MainWindowClosing)) // Send close request
            e.Cancel = true; // Cancel window closing if requested
    }

    /// <summary>
    ///     Handles the window closed event
    ///     Unregisters all message handlers for this view model
    /// </summary>
    [RelayCommand]
    private void WindowClosed()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    ///     Handles dropped files
    ///     Opens the file if it has a supported extension
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task DropFile()
    {
        if (DropFileData?.Length > 0) // Check if any files were dropped
        {
            var file = DropFileData[0]; // Get the first dropped file
            var ext = Path.GetExtension(file); // Get the file extension
            if (ext is ".csv" or ".w3strings" or ".xlsx") await OpenFile(file); // Open file if extension is supported
        }
    }

    /// <summary>
    ///     Opens a file using a file dialog
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenFile()
    {
        // Show open file dialog
        using var storageFile = await dialogService.ShowOpenFileDialogAsync(this, new OpenFileDialogSettings
        {
            Filters =
            [
                new FileFilter(Strings.FileFormatSupported, [".csv", ".xlsx", ".w3strings"]),
                new FileFilter(Strings.FileFormatTextFile, ".csv"),
                new FileFilter(Strings.FileFormatExcelWorkbook, ".xlsx"),
                new FileFilter(Strings.FileFormatWitcher3StringsFile, ".w3strings")
            ]
        });
        if (storageFile is not null &&
            Path.GetExtension(storageFile.LocalPath) is ".csv" or ".w3strings"
                or ".xlsx") // Check if file has a supported extension
            await OpenFile(storageFile.LocalPath); // Open file
    }

    /// <summary>
    ///     Opens the specified file and loads its contents
    /// </summary>
    /// <param name="fileName">The path to the file to open</param>
    private async Task OpenFile(string fileName)
    {
        try
        {
            if (!await HandleReOpenFile(fileName)) return; // Handle reopening file logic
            W3StringItems = await DeserializeW3StringItems(fileName); // Deserialize file contents
            SetOutputFolder(fileName, folder => OutputFolder = folder); // Set output folder based on file location
            UpdateRecentItems(fileName); // Update recent items list
            SearchText = string.Empty; // Clear search text
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open file: {FileName}.", fileName); // Log any errors during file opening
        }
    }

    /// <summary>
    ///     Deserializes The Witcher 3 string items from the specified file
    /// </summary>
    /// <param name="fileName">The path to the file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the deserialized The Witcher 3
    ///     string items
    /// </returns>
    private async Task<ObservableCollection<W3StringItemModel>> DeserializeW3StringItems(string fileName)
    {
        Log.Information("The file {FileName} is being opened...", fileName); // Log file opening
        var items = await w3Serializer.Deserialize(fileName); // Deserialize file contents
        var orderedItems = items.OrderBy(x => x.StrId); // Order items by ID
        return orderedItems.Select(x => new W3StringItemModel(x))
            .ToObservableCollection(); // Convert to observable collection
    }

    /// <summary>
    ///     Handles reopening a file when another file is already open
    /// </summary>
    /// <param name="fileName">The path to the file to reopen</param>
    /// <returns>True if the file can be reopened, false otherwise</returns>
    private async Task<bool> HandleReOpenFile(string fileName)
    {
        if (W3StringItems?.Any() != true) return true; // Return true if no items currently loaded
        return await WeakReferenceMessenger.Default.Send(
            new AsyncRequestMessage<string, bool>(fileName), // Send reopen file request
            MessageTokens.ReOpenFile); // Wait for response
    }

    /// <summary>
    ///     Sets the output folder based on the specified file name
    /// </summary>
    /// <param name="fileName">The file name to extract the directory from</param>
    /// <param name="onOutputFolderChanged">The action to call when the output folder changes</param>
    private static void SetOutputFolder(string fileName, Action<string> onOutputFolderChanged)
    {
        var folder = Path.GetDirectoryName(fileName); // Extract directory from file name
        if (folder is null) return; // Return if directory is null
        onOutputFolderChanged(folder); // Notify of folder change
        Log.Information("Working directory set to {Folder}.", folder); // Log folder change
    }

    /// <summary>
    ///     Updates the recent items list with the specified file name
    ///     If the file is already in the list, updates its opened time; otherwise, adds it to the list
    /// </summary>
    /// <param name="fileName">The file name to add or update in the recent items list</param>
    private void UpdateRecentItems(string fileName)
    {
        var foundItem = appSettings.RecentItems.FirstOrDefault(x => x.FilePath == fileName); // Find existing item
        if (foundItem is null) // If item not found
        {
            appSettings.RecentItems.Add(new RecentItem(fileName, DateTime.Now)); // Add new recent item
            Log.Information("Added {FileName} to recent items.", fileName); // Log addition
        }
        else // If item found
        {
            foundItem.OpenedTime = DateTime.Now; // Update opened time
            Log.Information("The last opened time for file {FileName} has been updated.", fileName); // Log update
        }
    }

    /// <summary>
    ///     Adds a new The Witcher 3 string item
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task Add()
    {
        var dialogViewModel = new EditDataDialogViewModel(new W3StringItemModel()); // Create new item view model
        if (await dialogService.ShowDialogAsync(this, dialogViewModel) == true // Show add dialog
            && dialogViewModel.Item is not null) // Check if user confirmed
        {
            W3StringItems!.Add(dialogViewModel.Item.Cast<W3StringItemModel>()); // Add new item to collection
            Log.Information("New W3Item added."); // Log successful addition
        }
        else
        {
            Log.Information("The W3Item has not been added."); // Log canceled addition
        }
    }

    /// <summary>
    ///     Edits the selected The Witcher 3 string item
    /// </summary>
    /// <param name="selectedItem">The item to edit</param>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task Edit(W3StringItemModel selectedItem)
    {
        var dialogViewModel = new EditDataDialogViewModel(selectedItem); // Create edit dialog view model
        if (await dialogService.ShowDialogAsync(this, // Show edit dialog
                dialogViewModel) == true && dialogViewModel.Item is not null) // Check if user confirmed changes
        {
            var found = W3StringItems! // Find the item in the collection
                .First(x => x.TrackingId == selectedItem.TrackingId);
            var index = W3StringItems!.IndexOf(found); // Get the item index
            // Update the item properties
            W3StringItems[index].StrId = dialogViewModel.Item.StrId;
            W3StringItems[index].KeyHex = dialogViewModel.Item.KeyHex;
            W3StringItems[index].KeyName = dialogViewModel.Item.KeyName;
            W3StringItems[index].Text = dialogViewModel.Item.Text;
            Log.Information("The W3Item has been updated."); // Log successful update
        }
        else
        {
            Log.Information("The W3Item has not been updated."); // Log canceled update
        }
    }

    /// <summary>
    ///     Deletes the selected The Witcher 3 string items
    /// </summary>
    /// <param name="selectedItems">The items to delete</param>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task Delete(IEnumerable<object> selectedItems)
    {
        var w3Items = selectedItems
            .OfType<ITrackableW3StringItem>().ToArray(); // Filter and convert to trackable items
        if (w3Items.Length > 0 &&
            await dialogService.ShowDialogAsync(this, new DeleteDataDialogViewModel(w3Items)) ==
            true) // Show delete confirmation dialog

            foreach (var item in w3Items)
            {
                var stringItem = item.Cast<W3StringItemModel>(); // Cast to string item model
                W3StringItems!.Remove(stringItem); // Remove from main collection
            }
    }

    /// <summary>
    ///     Shows the backup dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowBackupDialog()
    {
        await dialogService.ShowDialogAsync(this,
            new BackupDialogViewModel(appSettings, backupService));
    }

    /// <summary>
    ///     Shows the save dialog
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task ShowSaveDialog()
    {
        await dialogService.ShowDialogAsync(this,
            new SaveDialogViewModel(appSettings,
                serviceProvider.GetRequiredService<IW3Serializer>(),
                W3StringItems!,
                OutputFolder));
    }

    /// <summary>
    ///     Shows the log dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowLogDialog()
    {
        using var logDialogViewModel =
            new LogDialogViewModel(serviceProvider
                .GetRequiredService<ILogAccessService>()); // Create log dialog view model
        await dialogService.ShowDialogAsync<LogDialogViewModel>(this, logDialogViewModel); // Show the log dialog
    }

    /// <summary>
    ///     Shows the settings dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowSettingsDialog()
    {
        var translators = serviceProvider.GetServices<ITranslator>().ToArray(); // Get all available translators
        var names = translators.Select(x => x.Name.Replace("2", string.Empty)); // Extract translator names
        translators.ForEach(x => x.Cast<IDisposable>().Dispose()); // Dispose of translator instances
        await dialogService.ShowDialogAsync(this,
            new SettingDialogViewModel(appSettings, dialogService,
                serviceProvider.GetRequiredService<IExplorerService>(), names,
                serviceProvider.GetRequiredService<ICultureResolver>().SupportedCultures)); // Show the settings dialog
    }

    /// <summary>
    ///     Starts the game process
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlayGame))]
    private async Task PlayGame()
    {
        await serviceProvider.GetRequiredService<IPlayGameService>().PlayGame();
    }

    /// <summary>
    ///     Shows about dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowAbout()
    {
        await dialogService.ShowDialogAsync(this, // Show about dialog
            new AboutDialogViewModel(new Dictionary<string, object?> // Create view model with application information
            {
                { "Version", ThisAssembly.AssemblyInformationalVersion }, // Application version
                { "BuildTime", BuildInformation.BuildAt.ToLocalTime() }, // Build time
                { "OS", $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})" }, // OS information
                { "Runtime", RuntimeInformation.FrameworkDescription }, // Runtime information
                {
                    "Package", DependencyContext.Default?
                        .RuntimeLibraries.Where(static x => x.Type == "package")
                        .OrderBy(x => x.Name) // Package information
                }
            }));
    }

    /// <summary>
    ///     Opens the working folder in Windows Explorer
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenWorkingFolder))]
    private void OpenWorkingFolder()
    {
        serviceProvider.GetRequiredService<IExplorerService>().Open(OutputFolder); // Open the working folder
        Log.Information("Working folder opened."); // Log successful opening
    }

    /// <summary>
    ///     Opens the NexusMods page in the default browser
    /// </summary>
    [RelayCommand]
    private void OpenNexusMods()
    {
        serviceProvider.GetRequiredService<IExplorerService>()
            .Open(appSettings.NexusModUrl); // Open the NexusMods page
        Log.Information("NexusMods opened."); // Log successful opening
    }

    /// <summary>
    ///     Shows the recent files dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowRecentDialog()
    {
        using var recentDialogViewModel = new RecentDialogViewModel(appSettings); // Create recent dialog view model
        await dialogService.ShowDialogAsync(this, recentDialogViewModel); // Show the recent files dialog
    }

    /// <summary>
    ///     Determines whether translate dialog can be shown
    /// </summary>
    /// <returns>True if translate dialog can be shown, false otherwise</returns>
    private bool CanShowTranslateDialog()
    {
        if (FilteredW3StringItems is not null) // Check if we have search results
            return FilteredW3StringItems.Any(); // Return true if search results exist
        return W3StringItems?.Any() == true; // Otherwise check if we have any W3String items
    }

    /// <summary>
    ///     Shows translation dialog
    /// </summary>
    /// <param name="selectedItem">The initially selected item in the dialog</param>
    [RelayCommand(CanExecute = nameof(CanShowTranslateDialog))]
    private async Task ShowTranslateDialog(IW3StringItem? selectedItem)
    {
        var itemsToUse = FilteredW3StringItems ?? W3StringItems!; // Use filtered items if available
        var selectedIndex =
            selectedItem is not null ? itemsToUse.IndexOf(selectedItem) : 0; // Get the index of the selected item
        var translator = serviceProvider.GetServices<ITranslator>() // Get the configured translator
            .First(x => x.Name.Contains(appSettings.Translator));
        var isUseDictionary = appSettings.Translator == "MicrosoftTranslator";
        var dictionaryService = isUseDictionary ? serviceProvider.GetRequiredService<IDictionaryService>() : null;
        var translationDialogViewModel = new TranslationDialogViewModel(appSettings, translator, itemsToUse,
            selectedIndex, dictionaryService);
        await dialogService.ShowDialogAsync(this, translationDialogViewModel); // Show translation dialog
        if (translator is IDisposable disposable) disposable.Dispose(); // Dispose of the translator if it's disposable
    }

    /// <summary>
    ///     Shows the dictionary dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowDictionaryDialog()
    {
        await dialogService.ShowDialogAsync(this,
            new DictionaryManagerDialogViewModel(dictionaryManager, dictionaryProvider,
                dialogService)); // Show the dictionary dialog
    }

    /// <summary>
    ///     Determines whether merge data can be executed
    /// </summary>
    /// <returns></returns>
    private bool CanMergeData()
    {
        return W3StringItems?.Any() == true;
    }

    /// <summary>
    ///     Merges data from a file
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMergeData))]
    private async Task MergeData()
    {
        // Show open file dialog
        using var storageFile = await dialogService.ShowOpenFileDialogAsync(this, new OpenFileDialogSettings
        {
            Filters =
            [
                new FileFilter(Strings.FileFormatSupported, [".csv", ".xlsx", ".w3strings"]),
                new FileFilter(Strings.FileFormatTextFile, ".csv"),
                new FileFilter(Strings.FileFormatExcelWorkbook, ".xlsx"),
                new FileFilter(Strings.FileFormatWitcher3StringsFile, ".w3strings")
            ]
        });
        if (storageFile is not null &&
            Path.GetExtension(storageFile.LocalPath) is ".csv" or ".w3strings"
                or ".xlsx" &&
            await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.MergeDataConfirm))
        {
            // Check if file has a supported extension
            var mergeData =
                await w3Serializer.Deserialize(storageFile.LocalPath);

            var matchedPairs = W3StringItems!.Join(
                mergeData,
                oldItem => oldItem.StrId,
                newItem => newItem.StrId,
                (oldItem, newItem) => new { oldItem, newItem }
            );

            foreach (var pair in matchedPairs)
                pair.oldItem.Text = pair.newItem.Text;
        }
    }
}