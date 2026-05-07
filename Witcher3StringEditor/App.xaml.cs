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
    private IAppSettings? appSettings; // Application settings
    private IConfigService? configService; // Configuration service
    private bool disposedValue; // Flag to indicate whether the object has been disposed
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
        SetupExceptionHandling(); // Setup global exception handling
        InitializeServices(); // Initialize dependency injection services
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
        Log.Logger = new LoggerConfiguration().WriteTo.File(Path.Combine(AppPaths.LogDirectory, "log.txt"),
                rollingInterval: RollingInterval.Day, formatProvider: CultureInfo.InvariantCulture)
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
    ///     Initializes the dependency injection services
    ///     Registers all services, view models, and other dependencies with the IoC container
    /// </summary>
    private static void InitializeServices()
    {
        // Configure the IoC container with all required services
        Ioc.Default.ConfigureServices(new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog())
            .AddSingleton<IViewLocator, StrongViewLocator>(_ => CreatStrongViewLocator())
            .AddSingleton<IConfigService, ConfigService>()
            .AddSingleton<IAppSettings, AppSettings>()
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
            .AddScoped<IExplorerService, ExplorerService>()
            .AddScoped<IPlayGameService, PlayGameService>()
            .AddScoped<ICheckUpdateService, CheckUpdateService>()
            .AddTransient<ICultureMatcher, CultureMatcher>()
            .AddTransient<ISettingsManagerService, SettingsManagerService>()
            .AddTransient<ITranslator, MicrosoftTranslator>()
            .AddTransient<ITranslator, GoogleTranslator2>()
            .AddTransient<ITranslator, YandexTranslator>()
            .AddTransient<IDynamicDictionaryReplacer, AcDynamicDictionaryReplacer>()
            .AddTransient<IDictionaryService, DictionaryService>()
            .AddSingleton<IAppDiagnostics,AppDiagnostics>()
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
            logObserver?.Dispose();
        }

        // Mark as disposed
        disposedValue = true;
    }
}