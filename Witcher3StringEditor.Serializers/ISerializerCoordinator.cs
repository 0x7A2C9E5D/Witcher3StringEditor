namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Defines a contract for the serialization facade the rest of the application depends on
///     It delegates every request to the serializer able to handle the requested file format, so callers
///     never need to know which file formats exist
/// </summary>
public interface ISerializerCoordinator : ISerializer;