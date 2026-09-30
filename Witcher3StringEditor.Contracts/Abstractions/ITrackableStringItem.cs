namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for trackable The Witcher 3 string items
///     Extends the basic IStringItem interface with tracking capabilities and cloning functionality
/// </summary>
public interface ITrackableStringItem : IStringItem, ICloneable
{
    /// <summary>
    ///     Gets the unique tracking identifier for this item
    ///     Used to track and identify specific instances of The Witcher 3 string items throughout the application
    /// </summary>
    public Guid TrackingId { get; }
}