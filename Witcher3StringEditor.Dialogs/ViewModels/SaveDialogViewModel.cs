using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Serializers;
using Witcher3StringEditor.Serializers.Abstractions;

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
    ///     The serializer used to save The Witcher 3 string items
    /// </summary>
    private readonly IW3Serializer serializer;

    /// <summary>
    ///     The collection of The Witcher 3 string items to save
    /// </summary>
    private readonly IReadOnlyList<IW3StringItem> w3StringItems;

    /// <summary>
    ///     Gets or sets the ID space value for the items being saved
    /// </summary>
    [ObservableProperty] private int idSpace;

    /// <summary>
    ///     Gets or sets a value indicating whether to ignore ID space checking during save
    /// </summary>
    [ObservableProperty] private bool isIgnoreIdSpaceCheck;

    /// <summary>
    ///     Gets or sets the output directory where the file will be saved
    /// </summary>
    [ObservableProperty] private string outputDirectory;

    /// <summary>
    ///     Gets or sets the target file type for the save operation
    /// </summary>
    [ObservableProperty] private W3FileType targetFileType;

    /// <summary>
    ///     Gets or sets the target language for the save operation
    /// </summary>
    [ObservableProperty] private W3Language targetLanguage;

    /// <summary>
    ///     Initializes a new instance of the SaveDialogViewModel class
    /// </summary>
    /// <param name="appSettings">Application settings to get preferred language and file type</param>
    /// <param name="serializer">The serializer to use for saving the items</param>
    /// <param name="w3StringItems">The collection of The Witcher 3 string items to save</param>
    /// <param name="outputDirectory">The initial output directory for saving</param>
    public SaveDialogViewModel(IAppSettings appSettings, IW3Serializer serializer,
        IReadOnlyList<IW3StringItem> w3StringItems, string outputDirectory)
    {
        OutputDirectory = outputDirectory;
        this.w3StringItems = w3StringItems;
        this.serializer = serializer;
        IdSpace = FindIdSpace(w3StringItems[0]);
        TargetLanguage = appSettings.PreferredLanguage;
        TargetFileType = appSettings.PreferredW3FileType;
    }

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
        Log.Information("Target filetype: {FileType}.", TargetFileType); // Log target file type
        Log.Information("Target language: {Language}.", TargetLanguage); // Log target language
        Log.Information("Output directory: {Directory}.", OutputDirectory); // Log output directory
        if (TargetFileType == W3FileType.W3Strings)
        {
            if (IsIgnoreIdSpaceCheck)
                Log.Information("Ignore ID space check."); // Log ignore ID space check
            else
                Log.Information("ID space: {IdSpace}", IdSpace);
        }

        var saveResult = await serializer.Serialize(w3StringItems, new W3SerializationContext // Serialize items
        {
            OutputDirectory = OutputDirectory, // Set output directory
            ExpectedIdSpace = IdSpace, // Set ID space
            TargetFileType = TargetFileType, // Set file type
            TargetLanguage = TargetLanguage, // Set language
            IgnoreIdSpaceCheck = IsIgnoreIdSpaceCheck // Set ID space check flag
        });
        Log.Information("Sve result: {Result}.", saveResult); // Log save result
        _ = WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(saveResult),
            MessageTokens.Save); // Send result via messaging
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

    /// <summary>
    ///     Finds the ID space from The Witcher 3 string item's StrId
    /// </summary>
    /// <param name="iw3StringItem">The Witcher 3 string item to extract the ID space from</param>
    /// <returns>The ID space value, or -1 if not found</returns>
    private static int FindIdSpace(IW3StringItem iw3StringItem)
    {
        var match = IdSpaceRegex().Match(iw3StringItem.StrId); // Apply regex to extract ID space
        if (!match.Success) return -1; // Return -1 if no match found
        var foundIdSpace = match.Groups[1].Value; // Get the captured group
        return int.Parse(foundIdSpace, CultureInfo.InvariantCulture); // Parse and return as integer
    }

    /// <summary>
    ///     Regular expression to match and extract the ID space from a string ID
    /// </summary>
    /// <returns>A Regex object for matching ID space patterns</returns>
    [GeneratedRegex(@"^211(\d{4})\d{3}$")]
    private static partial Regex IdSpaceRegex();
}