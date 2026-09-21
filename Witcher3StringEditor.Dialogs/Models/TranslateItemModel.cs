using CommunityToolkit.Mvvm.ComponentModel;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///     A model class representing an item to be translated
///     Contains the original text, translated text, and metadata about the translation state
///     Implements ObservableObject to support property change notifications in MVVM pattern
/// </summary>
public partial class TranslateItemModel : ObservableObject
{
    /// <summary>
    ///     Gets or sets a value indicating whether the translation has been saved
    /// </summary>
    [ObservableProperty] private bool isSaved;

    /// <summary>
    ///     Gets or sets the translated text
    /// </summary>
    [ObservableProperty] private string translatedText = string.Empty;

    /// <summary>
    ///     Gets the unique identifier for this translation item
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the original text that needs to be translated
    /// </summary>
    public required string Text { get; init; }
}