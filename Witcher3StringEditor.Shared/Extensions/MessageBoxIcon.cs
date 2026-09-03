namespace Witcher3StringEditor.Shared.Extensions;

/// <summary>
///     How important a notification is. View models use this instead of a platform icon enum;
///     the dialog layer maps it to the icon rendered by the current theme.
/// </summary>
public enum MessageBoxIcon
{
    /// <summary>Neutral information.</summary>
    Information,

    /// <summary>Something is off, but the flow can continue.</summary>
    Warning,

    /// <summary>An operation failed.</summary>
    Error
}