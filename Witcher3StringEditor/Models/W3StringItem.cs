using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Models;

/// <summary>
///     Represents The Witcher 3 string item model
///     Implements the ITrackableW3StringItem interface and provides observable properties for data binding
///     This class extends the basic The Witcher 3 string item with tracking capabilities and cloning functionality
/// </summary>
public partial class W3StringItem : ObservableObject, ITrackableW3StringItem
{
    /// <summary>
    ///     The text this item was loaded or cloned with
    ///     Used as the baseline for <see cref="IsModified" /> and deliberately never cleared, so resetting the text
    ///     cannot corrupt the modified state and the original text stays available to serializers
    /// </summary>
    private readonly string baselineText;

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
    [ObservableProperty] private string oldText = string.Empty;

    /// <summary>
    ///     Gets or sets the string ID of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string strId = string.Empty;

    /// <summary>
    ///     Gets or sets the current text of The Witcher 3 string item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    [NotifyCanExecuteChangedFor(nameof(ResetTextCommand))]
    private string text = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the W3StringItem class by copying values from another IW3StringItem
    /// </summary>
    /// <param name="iw3StringItem">The source IW3StringItem to copy values from</param>
    /// <exception cref="ArgumentNullException"><paramref name="iw3StringItem" /> is null</exception>
    public W3StringItem(IW3StringItem iw3StringItem)
    {
        ArgumentNullException.ThrowIfNull(iw3StringItem);
        StrId = iw3StringItem.StrId;
        KeyHex = iw3StringItem.KeyHex;
        KeyName = iw3StringItem.KeyName;
        OldText = iw3StringItem.OldText;
        Text = iw3StringItem.Text;
        baselineText = Text;
    }

    /// <summary>
    ///     Initializes a new instance of the W3StringItem class
    ///     Creates an empty W3StringItem with default values
    /// </summary>
    public W3StringItem()
    {
        baselineText = Text;
    }

    /// <summary>
    ///     Gets a value indicating whether the Text property differs from the text the item was loaded with
    /// </summary>
    public bool IsModified => !string.Equals(Text, baselineText, StringComparison.Ordinal);

    /// <summary>
    ///     Gets the unique tracking identifier for this item
    ///     Used to track and identify specific instances of The Witcher 3 string items throughout the application
    /// </summary>
    public Guid TrackingId { get; } = Guid.NewGuid();

    /// <summary>
    ///     Creates a copy of the current W3StringItem
    /// </summary>
    /// <returns>A new instance with the same content and its own tracking identifier</returns>
    public object Clone()
    {
        return new W3StringItem(this);
    }

    /// <summary>
    ///     Determines whether the ResetText command can be executed
    /// </summary>
    /// <returns>True when the text differs from the text the item was loaded with</returns>
    private bool CanResetText()
    {
        return IsModified;
    }

    /// <summary>
    ///     Resets the Text property to the text the item was loaded with
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetText))]
    private void ResetText()
    {
        Text = baselineText;
    }
}