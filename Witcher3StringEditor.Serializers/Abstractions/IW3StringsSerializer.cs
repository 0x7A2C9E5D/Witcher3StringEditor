namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Marker contract used to disambiguate the W3Strings serializer in the dependency injection container
/// </summary>
/// <remarks>
///     The marker declares no additional members: the W3Strings specific behavior is expressed by
///     <see cref="W3StringsSerializer" /> itself, and all members are inherited from <see cref="IW3Serializer" />
/// </remarks>
public interface IW3StringsSerializer : IW3Serializer;