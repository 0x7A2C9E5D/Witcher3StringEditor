using System.Globalization;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using GTranslate.Translators;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Syncfusion.Licensing;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dialogs.Views;
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Dictionary.Implementation;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Models;
using Witcher3StringEditor.Serializers;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.Implementation;
using Witcher3StringEditor.Services;
using Witcher3StringEditor.ViewModels;
using Witcher3StringEditor.Views;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor;

/// <summary>
///     Interaction logic for App.xaml
///     Main application class that handles startup, initialization, and shutdown processes
/// </summary>
public sealed partial class App : IDisposable
{
    private IServiceProvider? appServiceProvider; // The provider owning the registered services
    private bool disposedValue; // Flag to indicate whether the object has been disposed
    private bool initialized; // Flag to indicate whether InitializeApplication completed
    private ObserverBase<LogEvent>? logObserver; // Observer for log events
    private SingleInstanceManager? singleInstanceManager; // Single instance manager

    /// <summary>
    ///     Disposes of the resources used by the application
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    ///     Handles the startup of the application
    ///     Checks for existing instances and initializes the application if none is running
    /// </summary>
    /// <param name="e">Startup event arguments</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        singleInstanceManager = new SingleInstanceManager(DebugHelper.IsDebug);
        // Check if another instance is already running
        if (singleInstanceManager.IsAnotherInstanceRunning())
        {
            // If another instance is running, ask user if they want to activate it
            if (MessageBox.Show(Strings.MultipleInstanceMessage, Strings.MultipleInstanceCaption,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information) == MessageBoxResult.Yes)
                singleInstanceManager.ActivateExistingInstance();
            Shutdown();
        }
        else
        {
            // If no other instance is running, initialize the application
            InitializeApplication();
        }
    }

    /// <summary>
    ///     Initializes the application components
    ///     Sets up exception handling, services, settings, logging, and culture
    /// </summary>
    private void InitializeApplication()
    {
        AppPaths.EnsureDirectoriesExist(); // Create the folders the application writes to
        InitializeServices(); // Initialize dependency injection services
        initialized = true; // Mark the container as ready for the shutdown path
        InitializeLogging(); // Setup logging system
        SetupExceptionHandling(); // Setup global exception handling
        InitializeCulture(); // Initialize culture
        RegisterSyncfusionLicense(); // Register Syncfusion license for UI components
        LogStartupInfo(); // Log startup information
        ShowMainWindow(); // Show the main window
    }

    /// <summary>
    ///     Logs startup information
    /// </summary>
    private static void LogStartupInfo()
    {
        Ioc.Default.GetRequiredService<IAppDiagnostics>().LogStartupInfo();
    }

    /// <summary>
    ///     Initializes the culture for the application
    ///     A stale or hand-edited language setting falls back to the resolved supported culture
    /// </summary>
    private static void InitializeCulture()
    {
        var appSettings = Ioc.Default.GetRequiredService<IAppSettings>();
        var cultureInfo = ResolveCulture(appSettings);
        if (appSettings.Language != cultureInfo.Name)
            appSettings.Language = cultureInfo.Name; // Keep the setting in sync with the applied culture
        I18NExtension.Culture = cultureInfo;
    }

    /// <summary>
    ///     Resolves the culture to apply at startup
    /// </summary>
    /// <param name="appSettings">The application settings</param>
    /// <returns>The configured culture, or the resolved supported culture when the setting is unusable</returns>
    private static CultureInfo ResolveCulture(IAppSettings appSettings)
    {
        if (string.IsNullOrWhiteSpace(appSettings.Language))
            return Ioc.Default.GetRequiredService<ICultureResolver>().ResolveSupportedCulture();

        try
        {
            return CultureInfo.GetCultureInfo(appSettings.Language);
        }
        catch (Exception ex) when (ex is CultureNotFoundException or ArgumentException)
        {
            Log.Warning(ex, "The configured language '{Language}' is not supported; using the system culture",
                appSettings.Language);
            return Ioc.Default.GetRequiredService<ICultureResolver>().ResolveSupportedCulture();
        }
    }

    /// <summary>
    ///     Shows the main window
    /// </summary>
    private static void ShowMainWindow()
    {
        var window = new MainWindow
        {
            DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>()
        };
        window.Show();
    }

    /// <summary>
    ///     Initializes the logging system
    ///     Sets up observers to capture log events
    /// </summary>
    private void InitializeLogging()
    {
        // Get log access service from the IoC container
        var logAccessService = Ioc.Default.GetRequiredService<ILogAccessService>();

        // Create observer to forward log events through the messaging system. The service marshals the entries
        // onto the UI thread because the sink can be invoked from arbitrary background threads
        logObserver = new AnonymousObserver<LogEvent>(logAccessService.Add);

        // Configure Serilog with multiple outputs: file, debug, and observer
        Log.Logger = new LoggerConfiguration().WriteTo.File(Path.Combine(AppPaths.LogDirectory, "log.txt"),
                rollingInterval: RollingInterval.Day, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Observers(observable => observable.Subscribe(logObserver))
            .CreateLogger();
    }

    /// <summary>
    ///     Registers the Syncfusion license
    ///     Reads the license from embedded resources and registers it with Syncfusion
    /// </summary>
    private static void RegisterSyncfusionLicense()
    {
        // Read the license from embedded resources
        using var stream = Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("Witcher3StringEditor.License.txt")
                           ?? throw new InvalidOperationException(
                               "The embedded Syncfusion license resource 'Witcher3StringEditor.License.txt' is missing.");
        using var reader = new StreamReader(stream);
        // Register the license with Syncfusion
        SyncfusionLicenseProvider.RegisterLicense(reader.ReadToEnd());
    }

    /// <summary>
    ///     Sets up global exception handling
    ///     Registers handlers for unhandled exceptions and unobserved task exceptions
    /// </summary>
    private void SetupExceptionHandling()
    {
        // Handle unhandled exceptions on the UI thread
        DispatcherUnhandledException += static (_, e) =>
        {
            var exception = e.Exception;
            Log.Error(exception, "Unhandled exception: {ExceptionMessage}", exception.Message);
            // Unconditionally suppressing the exception would hide UI-thread crashes entirely, so the user is
            // asked instead: confirm to keep going, cancel to let the process terminate
            var result = MessageBox.Show(Strings.OperationFailureMessage, Strings.OperationResultCaption,
                MessageBoxButton.OKCancel, MessageBoxImage.Error);
            e.Handled = result == MessageBoxResult.OK;
        };
        // Handle unobserved task exceptions (background tasks)
        TaskScheduler.UnobservedTaskException += static (_, e) =>
        {
            e.SetObserved();
            var exception = e.Exception;
            Log.Error(exception, "Unobserved task exception: {ExceptionMessage}", exception.Message);
        };
    }

    /// <summary>
    ///     Initializes the dependency injection services
    ///     Registers all services, view models, and other dependencies with the IoC container
    /// </summary>
    private void InitializeServices()
    {
        // Configure the IoC container with all required services. Every registration is a singleton because the
        // application always resolves through the static root provider
        appServiceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog())
            .AddSingleton<IViewLocator, StrongViewLocator>(_ => CreatStrongViewLocator())
            .AddSingleton<ISettingsPersistenceService, SettingsPersistenceService>()
            .AddSingleton<IAppSettings, AppSettings>(_ => Ioc.Default
                .GetRequiredService<ISettingsPersistenceService>().Load<AppSettings>())
            .AddSingleton<ICultureResolver, CultureResolver>()
            .AddSingleton<IBackupService, BackupService>()
            .AddSingleton<ICsvW3Serializer, CsvW3Serializer>()
            .AddSingleton<IExcelW3Serializer, ExcelW3Serializer>()
            .AddSingleton<IW3StringsSerializer, W3StringsSerializer>()
            .AddSingleton<IW3Serializer, W3SerializerCoordinator>()
            .AddSingleton<IDialogManager, DialogManager>()
            .AddSingleton<IDialogService, DialogService>()
            .AddSingleton<ILogAccessService, LogAccessService>()
            .AddSingleton<IDictionaryManager, DictionaryManager>()
            .AddSingleton<IDictionaryProvider, DictionaryProvider>()
            .AddSingleton<IShellOpenService, ShellOpenService>()
            .AddSingleton<IPlayGameService, PlayGameService>()
            .AddSingleton<ICheckUpdateService, CheckUpdateService>()
            .AddTransient<ICultureMatcher, CultureMatcher>()
            .AddSingleton<ISettingsManagerService, SettingsManagerService>()
            .AddTransient<ITranslator, MicrosoftTranslator>()
            .AddTransient<ITranslator, GoogleTranslator2>()
            .AddTransient<ITranslator, YandexTranslator>()
            .AddSingleton<ITranslatorProvider, TranslatorProvider>()
            .AddSingleton<IDialogViewModelFactory, DialogViewModelFactory>()
            .AddTransient<IDynamicDictionaryReplacer, AcDynamicDictionaryReplacer>()
            .AddTransient<IDictionaryService, DictionaryService>()
            .AddSingleton<IAppDiagnostics, AppDiagnostics>()
            .AddSingleton<IRecentFilesService, RecentFilesService>(_ =>
                new RecentFilesService(Ioc.Default.GetRequiredService<IAppSettings>().RecentItems))
            .AddTransient<MainWindowViewModel>()
            .BuildServiceProvider();
        Ioc.Default.ConfigureServices(appServiceProvider);
    }

    /// <summary>
    ///     Creates and configures the strong view locator
    ///     Registers view models with their corresponding views
    /// </summary>
    /// <returns>The configured StrongViewLocator</returns>
    private static StrongViewLocator CreatStrongViewLocator()
    {
        // Create and configure the view locator
        var viewLocator = new StrongViewLocator();
        // Register all view model to view mappings
        viewLocator.Register<EditDataDialogViewModel, EditDataDialog>();
        viewLocator.Register<DeleteDataDialogViewModel, DeleteDataDialog>();
        viewLocator.Register<BackupDialogViewModel, BackupDialog>();
        viewLocator.Register<SaveDialogViewModel, SaveDialog>();
        viewLocator.Register<LogDialogViewModel, LogDialog>();
        viewLocator.Register<SettingDialogViewModel, SettingsDialog>();
        viewLocator.Register<TranslationDialogViewModel, TranslationDialog>();
        viewLocator.Register<RecentDialogViewModel, RecentDialog>();
        viewLocator.Register<AboutDialogViewModel, AboutDialog>();
        viewLocator.Register<DictionaryManagerDialogViewModel, DictionaryManagerDialog>();
        return viewLocator;
    }

    /// <summary>
    ///     Handles the application exit event
    ///     Saves settings, flushes logs, and disposes resources
    /// </summary>
    /// <param name="e">Exit event arguments</param>
    protected override void OnExit(ExitEventArgs e)
    {
        // OnExit is also raised when startup shut down early (another instance is running) and the container was
        // never configured, so the save must be guarded by the initialization flag
        if (initialized) SaveAppSettings(); // Save application settings
        Log.Information("Application exited"); // Log application exit
        Log.CloseAndFlush(); // Flush logs
        Dispose(); // Dispose of resources
        base.OnExit(e);
    }

    /// <summary>
    ///     Saves application settings to the configuration file
    /// </summary>
    private static void SaveAppSettings()
    {
        var appSettings = Ioc.Default.GetRequiredService<IAppSettings>();
        var configService = Ioc.Default.GetRequiredService<ISettingsPersistenceService>();
        configService.Save(appSettings);
    }

    /// <summary>
    ///     Disposes of the resources used by the application
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    private void Dispose(bool disposing)
    {
        if (disposedValue) return;
        disposedValue = true;
        if (!disposing) return;
        logObserver?.Dispose(); // Detach the log observer
        singleInstanceManager?.Dispose(); // Release the single instance mutex
        (appServiceProvider as IDisposable)?.Dispose(); // Release the container and its singletons
    }
}