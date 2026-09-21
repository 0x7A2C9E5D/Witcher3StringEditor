namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for trackable The Witcher 3 string items
///     Extends the basic IW3StringItem interface with tracking capabilities and cloning functionality
/// </summary>
/// <remarks>
///     Cloning must produce a new, independent instance that copies the content of the original but receives its
///     own <see cref="TrackingId" />. Sharing the tracking id between a clone and its source would make
///     identity-based lookups (and edit tracking) resolve to the wrong instance.
/// </remarks>
public interface ITrackableW3StringItem : IW3StringItem, ICloneable
{
    /// <summary>
    ///     Gets the unique tracking identifier for this item
    ///     Used to track and identify specific instances of The Witcher 3 string items throughout the application
    ///     Must be globally unique and never <see cref="Guid.Empty" />; two distinct instances must never share a value
    /// </summary>
    Guid TrackingId { get; }
}