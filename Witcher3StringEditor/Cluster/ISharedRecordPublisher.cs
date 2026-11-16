namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Receiver of the local record mutations that must be replicated.
/// </summary>
internal interface ISharedRecordPublisher
{
    void Publish(SharedRecordCommand command);
}
