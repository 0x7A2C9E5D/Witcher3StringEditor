namespace Witcher3StringEditor.ViewModels.States;

/// <summary>
///     The mutually exclusive phase of the document shown in the main window.
/// </summary>
internal enum DocumentPhase
{
    /// <summary>
    ///     No file has been opened yet.
    /// </summary>
    Unloaded,

    /// <summary>
    ///     A file is being read.
    /// </summary>
    Loading,

    /// <summary>
    ///     A file is open and holds at least one item.
    /// </summary>
    Loaded,

    /// <summary>
    ///     A file is open but holds no item.
    /// </summary>
    Empty
}