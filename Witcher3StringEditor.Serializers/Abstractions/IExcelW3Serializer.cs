namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Marker contract used to disambiguate the Excel serializer in the dependency injection container
/// </summary>
/// <remarks>
///     The marker declares no additional members: the Excel specific behavior is expressed by
///     <see cref="ExcelW3Serializer" /> itself, and all members are inherited from <see cref="IW3Serializer" />
/// </remarks>
public interface IExcelW3Serializer : IW3Serializer;