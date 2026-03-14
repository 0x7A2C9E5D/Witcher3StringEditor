using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using CommunityToolkit.Mvvm.DependencyInjection;
using GTranslate.Translators;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Syncfusion.Licensing;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dialogs.Views;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Dictionary.Providers;
using Witcher3StringEditor.Dictionary.Services;
using Witcher3StringEditor.Locales;
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
    private IAppSettings? appSettings; // Application settings
    private IConfigService? configService; // Configuration service
    private bool disposedValue; // Flag to indicate whether the object has been disposed

    private ObserverBase<LogEvent>? logObserver; // Observer for log events

    // Private fields
    private Mutex? mutex; // Mutex to prevent multiple instances

    /// <summary>
    ///     Gets a value indicating whether the application is running in debug mode
    /// </summary>
#if DEBUG
    private static bool IsDebug => true;
#else
    private static bool IsDebug => false;
#endif

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
        // Check if another instance is already running
        if (IsAnotherInstanceRunning())
        {
            // If another instance is running, ask user if they want to activate it
            if (MessageBox.Show(Strings.MultipleInstanceMessage, Strings.MultipleInstanceCaption,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information) == MessageBoxResult.Yes) ActivateExistingInstance();
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
        SetupExceptionHandling(); // Setup global exception handling
        InitializeServices(GetAppSettingsPath()); // Initialize dependency injection services
        InitializeAppSettings(); // Load application settings
        InitializeLogging(); // Setup logging system
        RegisterSyncfusionLicense(); // Register Syncfusion license for UI components
        InitializeCulture(); // Set application culture (language)
        new MainWindow().Show(); // Show the main window
    }

    /// <summary>
    ///     Initializes the logging system
    ///     Sets up observers to capture log events
    /// </summary>
    private void InitializeLogging()
    {
        // Get log access service from the IoC container
        var logAccessService = Ioc.Default.GetRequiredService<ILogAccessService>();

        // Create observer to forward log events through the messaging system
        logObserver = new AnonymousObserver<LogEvent>(logAccessService.Logs.Add);

        // Configure Serilog with multiple outputs: file, debug, and observer
        Log.Logger = new LoggerConfiguration().WriteTo.File(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Logs", "log.txt"),
                rollingInterval: RollingInterval.Day, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Observers(observable => observable.Subscribe(logObserver))
            .CreateLogger();
    }

    /// <summary>
    ///     Initializes the application settings
    ///     Loads configuration service and application settings from the IoC container
    /// </summary>
    private void InitializeAppSettings()
    {
        // Get configuration service and application settings from the IoC container
        configService = Ioc.Default.GetRequiredService<IConfigService>();
        appSettings = Ioc.Default.GetRequiredService<IAppSettings>();
    }

    /// <summary>
    ///     Gets the path to the application settings file
    ///     Creates the configuration folder if it doesn't exist
    /// </summary>
    /// <returns>The full path to the application settings file</returns>
    private static string GetAppSettingsPath()
    {
        // Determine the configuration folder path based on debug/release mode
        var configFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor");
        var configPath = Path.Combine(configFolderPath, "AppSettings.Json");
        // Create the configuration folder if it doesn't exist
        if (!Directory.Exists(configFolderPath))
            Directory.CreateDirectory(configFolderPath);
        return configPath;
    }

    /// <summary>
    ///     Initializes the application culture (language)
    ///     Sets the culture based on saved settings or resolves the supported culture
    /// </summary>
    private void InitializeCulture()
    {
        // Determine culture based on saved settings or resolve supported culture
        var cultureInfo = appSettings!.Language == string.Empty
            ? Ioc.Default.GetRequiredService<ICultureResolver>().ResolveSupportedCulture()
            : new CultureInfo(appSettings.Language);
        // Save the resolved culture if it wasn't previously set
        if (appSettings.Language == string.Empty)
            appSettings.Language = cultureInfo.Name;
        // Apply the culture to the application
        I18NExtension.Culture = cultureInfo;
    }

    /// <summary>
    ///     Registers the Syncfusion license
    ///     Reads the license from embedded resources and registers it with Syncfusion
    /// </summary>
    private static void RegisterSyncfusionLicense()
    {
        // Read the license from embedded resources
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Witcher3StringEditor.License.txt")!;
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
            e.Handled = true;
            var exception = e.Exception;
            Log.Error(exception, "Unhandled exception: {ExceptionMessage}", exception.Message);
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
    ///     Checks if another instance of the application is already running
    ///     Uses a mutex to determine if this is the only instance
    /// </summary>
    /// <returns>True if another instance is running, false otherwise</returns>
    private bool IsAnotherInstanceRunning()
    {
        // Create a mutex with a unique name based on debug/release mode
        mutex = new Mutex(true, IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor",
            out var createdNew);
        return !createdNew;
    }

    /// <summary>
    ///     Activates an existing instance of the application
    ///     Finds the existing process and brings its window to the foreground
    /// </summary>
    private static void ActivateExistingInstance()
    {
        // Find the existing process instance
        using var existingProcess = FindExistingProcessInstance();
        // Activate the window of the existing instance
        var mainWindowHandle = new HWND(existingProcess.MainWindowHandle);
        ActivateExistingInstanceWindow(mainWindowHandle);
    }

    /// <summary>
    ///     Activates the window of an existing application instance
    ///     Restores the window if minimized and brings it to the foreground
    /// </summary>
    /// <param name="mainWindowHandle">The handle to the main window of the existing instance</param>
    private static void ActivateExistingInstanceWindow(HWND mainWindowHandle)
    {
        // Get the current window placement
        var placement = new WINDOWPLACEMENT();
        placement.length = (uint)Marshal.SizeOf(placement);
        if (PInvoke.GetWindowPlacement(mainWindowHandle, ref placement).Value == 0) return;
        // Restore the window if it's minimized
        if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
            PInvoke.ShowWindow(mainWindowHandle, SHOW_WINDOW_CMD.SW_RESTORE);
        // Bring the window to the foreground
        PInvoke.SetForegroundWindow(mainWindowHandle);
    }

    /// <summary>
    ///     Finds an existing process instance of the application
    /// </summary>
    /// <returns>The existing process instance</returns>
    private static Process FindExistingProcessInstance()
    {
        // Get the current process to find processes with the same name
        using var currentProcess = Process.GetCurrentProcess();
        return Process.GetProcessesByName(currentProcess.ProcessName).First(p => p.Id != currentProcess.Id);
    }

    /// <summary>
    ///     Initializes the dependency injection services
    ///     Registers all services, view models, and other dependencies with the IoC container
    /// </summary>
    /// <param name="configPath">The path to the configuration file</param>
    private static void InitializeServices(string configPath)
    {
        // Configure the IoC container with all required services
        Ioc.Default.ConfigureServices(new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog())
            .AddSingleton<IViewLocator, StrongViewLocator>(_ => CreatStrongViewLocator())
            .AddSingleton<IConfigService, ConfigService>(_ => new ConfigService(configPath))
            .AddSingleton<IAppSettings, AppSettings>(_ =>
                Ioc.Default.GetRequiredService<IConfigService>().Load<AppSettings>())
            .AddSingleton<ICultureResolver, CultureResolver>()
            .AddSingleton<IBackupService, BackupService>()
            .AddSingleton<ICsvW3Serializer, CsvW3Serializer>()
            .AddSingleton<IExcelW3Serializer, ExcelW3Serializer>()
            .AddSingleton<IW3StringsSerializer, W3StringsSerializer>()
            .AddSingleton<IW3Serializer, W3SerializerCoordinator>()
            .AddSingleton<IDialogManager, DialogManager>()
            .AddSingleton<IDialogService, DialogService>()
            .AddSingleton<ILogAccessService, LogAccessService>()
            .AddSingleton<IDictionaryMangerService, DictionaryMangerService>()
            .AddSingleton<IDictionaryProvider, XliffDictionaryProvider>()
            .AddScoped<IExplorerService, ExplorerService>()
            .AddScoped<IPlayGameService, PlayGameService>()
            .AddScoped<ICheckUpdateService, CheckUpdateService>()
            .AddTransient<ICultureMatcher, CultureMatcher>()
            .AddTransient<ISettingsManagerService, SettingsManagerService>()
            .AddTransient<ITranslator, MicrosoftTranslator>()
            .AddTransient<ITranslator, GoogleTranslator>()
            .AddTransient<ITranslator, YandexTranslator>()
            .AddTransient<IDynamicDictionaryService, DynamicDictionaryService>()
            .AddTransient<IDictionaryService, DictionaryService>()
            .AddTransient<MainWindowViewModel>()
            .BuildServiceProvider());
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
        // Save application settings before exiting
        configService?.Save(appSettings);
        // Log application exit and flush logs
        Log.Information("Application exited.");
        Log.CloseAndFlush();
        // Dispose resources
        Dispose();
    }

    /// <summary>
    ///     Disposes of the resources used by the application
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    private void Dispose(bool disposing)
    {
        // Dispose managed resources
        if (disposedValue) return;
        if (disposing)
        {
            mutex?.Dispose();
            logObserver?.Dispose();
        }

        // Mark as disposed
        disposedValue = true;
    }
}