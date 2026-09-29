using System.Text;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.W3Strings;

namespace Witcher3StringEditor.Serializers.Model;

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
    ///     Gets the target file format for serialization
    ///     This determines the format in which The Witcher 3 string items will be serialized (e.g., CSV, Excel, W3Strings)
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required W3FileFormat TargetFileFormat { get; init; }

    /// <summary>
    ///     Gets the target language for serialization
    ///     This specifies the language that The Witcher 3 string items are in or should be translated to
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required W3Language TargetLanguage { get; init; }

    /// <summary>
    ///     Gets the payload encoding to write for W3Strings files
    ///     This is used during W3Strings serialization to select the text encoding and the
    ///     size of one offset/length unit of the written file
    ///     This is a required property that must be specified during context creation
    /// </summary>
    public required Encoding Encoding { get; init; }
}