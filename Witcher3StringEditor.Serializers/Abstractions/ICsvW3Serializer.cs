namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Marker contract used to disambiguate the CSV serializer in the dependency injection container and by the
///     W3Strings serializer, which round-trips through an intermediate CSV file
/// </summary>
/// <remarks>
///     The marker declares no additional members: the CSV specific behavior is expressed by
///     <see cref="CsvW3Serializer" /> itself. Registering it separately keeps a CSV implementation from being
///     injected where the coordinator is expected
/// </remarks>
public interface ICsvW3Serializer : IW3Serializer;