using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Serializers;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Shared.Extensions;
using Witcher3StringEditor.W3Strings;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the save dialog window
///     Handles saving The Witcher 3 string items to a file with specified settings
///     Implements IModalDialogViewModel for dialog result handling and ICloseable for close notifications
/// </summary>
public partial class SaveDialogViewModel
    : ObservableObject, IModalDialogViewModel, ICloseable
{
    /// <summary>
    ///     The application settings used to read and remember the save preferences
    /// </summary>
    private readonly IAppSettings appSettings;

    /// <summary>
    ///     The dialog service used to inform the user about the result of the save operation
    /// </summary>
    private readonly IDialogService dialogService;

    /// <summary>
    ///     The serializer used to save The Witcher 3 string items
    /// </summary>
    private readonly IW3Serializer serializer;

    /// <summary>
    ///     The collection of The Witcher 3 string items to save
    /// </summary>
    private readonly IReadOnlyList<IW3StringItem> w3StringItems;

    /// <summary>
    ///     Initializes a new instance of the SaveDialogViewModel class
    /// </summary>
    /// <param name="appSettings">Application settings to get preferred language and file type</param>
    /// <param name="serializer">The serializer to use for saving the items</param>
    /// <param name="dialogService">The dialog service used to report the result of the save operation</param>
    /// <param name="w3StringItems">The collection of The Witcher 3 string items to save</param>
    /// <param name="outputDirectory">The initial output directory for saving</param>
    public SaveDialogViewModel(IAppSettings appSettings, IW3Serializer serializer, IDialogService dialogService,
        IReadOnlyList<IW3StringItem> w3StringItems, string outputDirectory)
    {
        OutputDirectory = outputDirectory;
        this.w3StringItems = w3StringItems;
        this.serializer = serializer;
        this.dialogService = dialogService;
        this.appSettings = appSettings;
        TargetLanguage = appSettings.PreferredLanguage;
        TargetFileType = appSettings.PreferredW3FileType;
        TargetEncoding = appSettings.PreferredW3StringsEncoding;
    }

    /// <summary>
    ///     Gets the payload encodings the user can choose from
    /// </summary>
    public IReadOnlyList<Encoding> Encodings { get; } = [Encoding.Unicode, Encoding.UTF8];

    /// <summary>
    ///     Gets or sets the payload encoding written for W3Strings files
    /// </summary>
    [ObservableProperty]
    public partial Encoding TargetEncoding { get; set; }

    /// <summary>
    ///     Gets or sets the output directory where the file will be saved
    /// </summary>
    [ObservableProperty]
    private partial string OutputDirectory { get; set; }

    /// <summary>
    ///     Gets or sets the target file type for the save operation
    /// </summary>
    [ObservableProperty]
    public partial W3FileType TargetFileType { get; set; }

    /// <summary>
    ///     Gets or sets the target language for the save operation
    /// </summary>
    [ObservableProperty]
    public partial W3Language TargetLanguage { get; set; }

    /// <summary>
    ///     Event that is raised when the dialog requests to be closed
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///     Gets the dialog result value
    ///     True if the save operation was successful, false if canceled
    /// </summary>
    public bool? DialogResult { get; private set; }

    /// <summary>
    ///     Handles the save action
    ///     Serializes The Witcher 3 string items to a file with the specified settings
    /// </summary>
    [RelayCommand]
    private async Task Save()
    {
        Log.Information("Saving {Count} item(s) to {Directory} as {FileType} ({Language})",
            w3StringItems.Count, OutputDirectory, TargetFileType, TargetLanguage);
        if (TargetFileType == W3FileType.W3Strings)
        {
            Log.Information("W3Strings payload encoding: {Encoding}",
                TargetEncoding.WebName); // Log the chosen encoding
            appSettings.PreferredW3StringsEncoding = TargetEncoding; // Remember the chosen encoding
        }

        var saveResult = await serializer.Serialize(w3StringItems, new W3SerializationContext // Serialize items
        {
            OutputDirectory = OutputDirectory, // Set output directory
            TargetFileType = TargetFileType, // Set file type
            TargetLanguage = TargetLanguage, // Set language
            Encoding = TargetEncoding // Set the payload encoding
        });
        if (saveResult)
            Log.Information("Save completed successfully");
        else
            Log.Warning("Save failed, see the serializer error log above for details");
        await dialogService.MessageBoxNotifyAsync(this, saveResult ? Strings.SaveSuccess : Strings.SaveFailure,
            Strings.SaveResult, saveResult ? MessageBoxIcon.Information : MessageBoxIcon.Error); // Report the result
        DialogResult = true; // Set dialog result
        RequestClose?.Invoke(this, EventArgs.Empty); // Close the dialog
    }

    /// <summary>
    ///     Handles the cancel action
    ///     Sets the dialog result to false and requests the dialog to close
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}