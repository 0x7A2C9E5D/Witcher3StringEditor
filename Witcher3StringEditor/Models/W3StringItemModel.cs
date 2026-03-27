using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Models;

/// <summary>
///     Represents The Witcher 3 string item model
///     Implements the ITrackableW3StringItem interface and provides observable properties for data binding
///     This class extends the basic The Witcher 3 string item with tracking capabilities and cloning functionality
/// </summary>
public partial class W3StringItemModel : ObservableObject, ITrackableW3StringItem
{
    /// <summary>
    ///     Gets or sets the hexadecimal key of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string keyHex = string.Empty;

    /// <summary>
    ///     Gets or sets the key name of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string keyName = string.Empty;

    /// <summary>
    ///     Gets or sets the original text of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    [NotifyCanExecuteChangedFor(nameof(ResetTextCommand))]
    private string oldText = string.Empty;

    /// <summary>
    ///     Gets or sets the string ID of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string strId = string.Empty;

    /// <summary>
    ///     Gets or sets the current text of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    ///     When this property changes, if OldText is empty, it will be set to the previous Text value
    /// </summary>
    [ObservableProperty] private string text = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the W3StringItemModel class by copying values from another IW3StringItem
    /// </summary>
    /// <param name="iw3StringItem">The source IW3StringItem to copy values from</param>
    public W3StringItemModel(IW3StringItem iw3StringItem)
    {
        StrId = iw3StringItem.StrId;
        KeyHex = iw3StringItem.KeyHex;
        KeyName = iw3StringItem.KeyName;
        OldText = iw3StringItem.OldText;
        Text = iw3StringItem.Text;
    }

    /// <summary>
    ///     Initializes a new instance of the W3StringItemModel class
    ///     Creates an empty W3StringItemModel with default values
    /// </summary>
    public W3StringItemModel()
    {
    }

    /// <summary>
    ///     Gets a value indicating whether the Text property has been modified from its original value
    /// </summary>
    public bool IsModified => !string.IsNullOrEmpty(OldText);

    /// <summary>
    ///     Gets the unique tracking identifier for this item
    ///     Used to track and identify specific instances of The Witcher 3 string items throughout the application
    /// </summary>
    public Guid TrackingId { get; } = Guid.NewGuid();

    /// <summary>
    ///     Creates a shallow copy of the current W3StringItemModel
    /// </summary>
    /// <returns>A shallow copy of the current object</returns>
    public object Clone()
    {
        return MemberwiseClone();
    }

    /// <summary>
    ///     Called when the Text property is changing
    ///     If OldText is empty, it sets OldText to the current Text value
    /// </summary>
    /// <param name="value">The new value of the Text property</param>
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnTextChanging(string value)
    {
        if (string.IsNullOrWhiteSpace(OldText)) OldText = Text;
    }

    /// <summary>
    ///     Determines whether the ResetText command can be executed
    /// </summary>
    /// <returns></returns>
    private bool CanResetText()
    {
        return IsModified;
    }

    /// <summary>
    ///     Resets the Text property to the value of OldText and clears OldText
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetText))]
    private void ResetText()
    {
        Text = OldText;
        OldText = string.Empty;
    }
}