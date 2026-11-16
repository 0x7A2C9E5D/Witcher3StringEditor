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
using Witcher3StringEditor.Cluster;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dialogs.Views;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Factories;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Models;
using Witcher3StringEditor.Providers;
using Witcher3StringEditor.Serializers;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Services;
using Witcher3StringEditor.ViewModels;
using Witcher3StringEditor.Views;

namespace Witcher3StringEditor;

/// <summary>
///     Interaction logic for App.xaml
///     Main application class that handles startup, initialization, and shutdown processes
/// </summary>
public sealed partial class App : IDisposable
{
    private bool disposedValue; // Flag to indicate whether the object has been disposed
    private ObserverBase<LogEvent>? logObserver; // Observer for log events

    /// <summary>
    ///     Disposes of the resources used by the application
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    ///     Handles the startup of the application.
    ///     Every launch is an independent instance; a file passed on the command line is opened
    ///     in this instance, while shared records are converged with other instances via the cluster.
    /// </summary>
    /// <param name="e">Startup event arguments</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        InitializeServices();
        var startupFileService = Ioc.Default.GetRequiredService<IStartupFileService>();
        var filePath = startupFileService.GetStartupFile(e.Args);
        InitializeApplication();
        if (filePath is not null)
            startupFileService.OpenFile(filePath);
    }

    /// <summary>
    ///     Initializes the application components
    ///     Sets up exception handling, settings, logging, and culture
    /// </summary>
    private void InitializeApplication()
    {
        InitializeLogging(); // Setup logging system
        SetupExceptionHandling(); // Setup global exception handling
        InitializeCulture(); // Initialize culture
        RegisterSyncfusionLicense(); // Register Syncfusion license for UI components
        LogStartupInfo(); // Log startup information
        StartClusterRuntime(); // Join the local cluster for shared record replication
        ShowMainWindow(); // Show the main window
    }

    /// <summary>
    ///     Starts the peer runtime that synchronizes recent files and backups between instances.
    /// </summary>
    private static void StartClusterRuntime()
    {
        try
        {
            Ioc.Default.GetRequiredService<IClusterRuntime>().Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start the cluster runtime; records will remain local to this instance");
        }
    }

    /// <summary>
    ///     Logs startup information
    /// </summary>
    private static void LogStartupInfo()
    {
        Ioc.Default.GetRequiredService<IAppDiagnosticsService>().LogStartupInfo();
    }

    /// <summary>
    ///     Initializes the culture for the application
    /// </summary>
    private static void InitializeCulture()
    {
        var appSettings = Ioc.Default.GetRequiredService<IAppSettings>();
        var cultureInfo = appSettings.Language == string.Empty
            ? Ioc.Default.GetRequiredService<ICultureResolver>().ResolveSupportedCulture()
            : new CultureInfo(appSettings.Language);
        if (appSettings.Language == string.Empty)
            appSettings.Language = cultureInfo.Name;
        I18NExtension.Culture = cultureInfo;
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

        // Create observer to forward log events through the messaging system
        logObserver = new AnonymousObserver<LogEvent>(logAccessService.Logs.Add);

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
            .GetManifestResourceStream("Witcher3StringEditor.Syncfusion.lic")!;
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
            .AddSingleton<ISettingsPersistenceService, SettingsPersistenceService>()
            .AddSingleton<IAppSettings, AppSettings>(_ => Ioc.Default
                .GetRequiredService<ISettingsPersistenceService>().Load<AppSettings>())
            .AddSingleton<ICultureResolver, CultureResolver>()
            .AddSingleton<IBackupService, BackupService>()
            .AddSingleton<IW3StringFileSerializer, CsvSerializer>()
            .AddSingleton<IW3StringFileSerializer, ExcelSerializer>()
            .AddSingleton<IW3StringFileSerializer, W3StringsSerializer>()
            .AddSingleton<IW3StringSerializerCoordinator, W3StringIw3StringSerializerCoordinator>()
            .AddSingleton<IDialogManager, DialogManager>()
            .AddSingleton<IDialogService, DialogService>()
            .AddSingleton<ILogAccessService, LogAccessService>()
            .AddSingleton<IDictionaryManager, DictionaryManager>()
            .AddSingleton<IDictionaryProvider, DictionaryProvider>()
            .AddSingleton<IShellOpenService, ShellOpenService>()
            .AddSingleton<IFileAssociationService, FileAssociationService>()
            .AddSingleton<IStartupFileService, StartupFileService>()
            .AddSingleton<IInstanceRegistry, InstanceRegistry>()
            .AddSingleton<IClusterLeadership, ClusterLeadership>()
            .AddSingleton<ISharedRecordsService, SharedRecordsService>()
            .AddSingleton<IClusterRuntime, ClusterRuntime>()
            .AddScoped<IPlayGameService, PlayGameService>()
            .AddScoped<ICheckUpdateService, CheckUpdateService>()
            .AddTransient<ICultureMatcher, CultureMatcher>()
            .AddTransient<ISettingsManagerService, SettingsManagerService>()
            .AddTransient<ITranslator, MicrosoftTranslator>()
            .AddTransient<ITranslator, GoogleTranslator2>()
            .AddTransient<ITranslator, YandexTranslator>()
            .AddSingleton<ITranslatorProvider, TranslatorProvider>()
            .AddSingleton<IDialogViewModelFactory, DialogViewModelFactory>()
            .AddTransient<IDynamicDictionaryReplacer, AcDynamicDictionaryReplacer>()
            .AddTransient<IDictionaryService, DictionaryService>()
            .AddSingleton<IAppDiagnosticsService, AppDiagnosticsService>()
            .AddSingleton<IRecentFilesService, RecentFilesService>()
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
        // Save before the node stops: the persistence service writes the file only on the leader,
        // and the node is no longer a leader once it has been stopped.
        SaveAppSettings(); // Save application settings
        StopClusterRuntime(); // Flush shared records and leave the cluster
        Log.Information("Application exited"); // Log application exit
        Log.CloseAndFlush(); // Flush logs
        Dispose(); // Dispose of resources
    }

    /// <summary>
    ///     Stops the peer runtime, flushing local record writes first.
    /// </summary>
    private static void StopClusterRuntime()
    {
        try
        {
            var clusterRuntime = Ioc.Default.GetRequiredService<IClusterRuntime>();
            // Run the shutdown off the UI thread: awaiting on the UI thread would deadlock,
            // because the continuations of StopAsync need the dispatcher that is blocked here
            Task.Run(clusterRuntime.StopAsync).Wait(TimeSpan.FromSeconds(20));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop the cluster runtime gracefully");
        }
    }

    /// <summary>
    ///     Saves application settings to the configuration file. Only the leader writes the file,
    ///     because every instance shares it.
    /// </summary>
    private static void SaveAppSettings()
    {
        if (!Ioc.Default.GetRequiredService<IClusterLeadership>().IsLeader) return;
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
        if (disposing)
            logObserver?.Dispose();
        disposedValue = true;
    }
}