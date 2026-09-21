using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Represents the context information required for The Witcher 3 serialization operations
///     This record contains all the necessary parameters and settings needed to serialize The Witcher 3 string items
/// </summary>
public record W3SerializationContext
{
    /// <summary>
    ///     Gets the output directory where the serialized files will be saved
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required string OutputDirectory { get; init; }

    /// <summary>
    ///     Gets the target file type for serialization
    ///     This determines the format in which The Witcher 3 string items will be serialized (e.g., CSV, Excel, W3Strings)
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required W3FileType TargetFileType { get; init; }

    /// <summary>
    ///     Gets the target language for serialization
    ///     This specifies the language that The Witcher 3 string items are in or should be translated to
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required W3Language TargetLanguage { get; init; }

    /// <summary>
    ///     Gets the expected ID space for The Witcher 3 string items
    ///     This is used during W3Strings serialization to validate that the string IDs are within the expected range
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required int ExpectedIdSpace { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether to ignore the ID space check during serialization
    ///     When true, bypasses the ID space validation during W3Strings encoding
    /// </summary>
    public bool IgnoreIdSpaceCheck { get; init; }
}