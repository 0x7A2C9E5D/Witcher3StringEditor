using CommunityToolkit.Mvvm.ComponentModel;
using Witcher3StringEditor.Dialogs.States;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///     A model class representing an item to be translated
///     Contains the original text, translated text, and metadata about the translation state
///     Implements ObservableObject to support property change notifications in MVVM pattern
/// </summary>
public partial class TranslateItemModel : ObservableObject
{
    private readonly TranslationItemStateMachine itemStateMachine = new(); // Tracks the item's translation state

    /// <summary>
    ///     Initializes a new instance of the TranslateItemModel class
    /// </summary>
    public TranslateItemModel()
    {
        itemStateMachine.StateChanged += OnItemStateChanged;
    }

    /// <summary>
    ///     Gets a value indicating whether the translation has been saved
    /// </summary>
    public bool IsSaved => itemStateMachine.Current == TranslationItemState.Saved;

    /// <summary>
    ///     Gets a value indicating whether the item holds translation text that was not saved yet
    /// </summary>
    public bool HasUnsavedTranslation =>
        itemStateMachine.Current != TranslationItemState.Saved &&
        !string.IsNullOrWhiteSpace(TranslatedText);

    /// <summary>
    ///     Gets or sets the translated text
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnsavedTranslation))]
    public partial string TranslatedText { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the unique identifier for this translation item
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the original text that needs to be translated
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    ///     Moves the item into the translating state
    /// </summary>
    internal void BeginTranslation()
    {
        itemStateMachine.BeginTranslation();
    }

    /// <summary>
    ///     Marks the item as translated
    /// </summary>
    internal void MarkTranslated()
    {
        itemStateMachine.MarkTranslated();
    }

    /// <summary>
    ///     Marks the item as failed
    /// </summary>
    internal void MarkFailed()
    {
        itemStateMachine.MarkFailed();
    }

    /// <summary>
    ///     Marks the item as cancelled
    /// </summary>
    internal void MarkCancelled()
    {
        itemStateMachine.MarkCancelled();
    }

    /// <summary>
    ///     Marks the item as saved
    /// </summary>
    internal void MarkSaved()
    {
        itemStateMachine.MarkSaved();
    }

    /// <summary>
    ///     Applies a manual translation edit, so an already saved item becomes unsaved again
    /// </summary>
    /// <param name="value">The new translated text</param>
    partial void OnTranslatedTextChanged(string value)
    {
        itemStateMachine.TextChanged(!string.IsNullOrWhiteSpace(value));
    }

    /// <summary>
    ///     Notifies the derived state properties when the item state changed
    /// </summary>
    private void OnItemStateChanged()
    {
        OnPropertyChanged(nameof(IsSaved));
        OnPropertyChanged(nameof(HasUnsavedTranslation));
    }
}
