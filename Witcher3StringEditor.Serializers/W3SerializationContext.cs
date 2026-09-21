using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Represents the context information required for The Witcher 3 serialization operations
///     This record contains all the necessary parameters and settings needed to serialize The Witcher 3 string items
/// </summary>
public record W3SerializationContext
{
    /// <summary>
    ///     Backing field of the <see cref="OutputDirectory" /> property
    /// </summary>
    private readonly string outputDirectory = string.Empty;

    /// <summary>
    ///     Gets the output directory where the serialized files will be saved
    ///     Must be a rooted, non-white-space path; the value is validated and normalized on assignment because every
    ///     serializer feeds it straight into <see cref="System.IO.Path.Combine(string, string)" />
    /// </summary>
    /// <exception cref="ArgumentException">The value is null, empty, white-space or not a rooted path</exception>
    public required string OutputDirectory
    {
        get => outputDirectory;
        init => outputDirectory = ValidateOutputDirectory(value);
    }

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

    /// <summary>
    ///     Validates and normalizes the output directory
    /// </summary>
    /// <param name="outputDirectory">The directory to validate</param>
    /// <returns>The full path of the directory</returns>
    /// <exception cref="ArgumentException">
    ///     The value is null, empty, white-space, not rooted or not a valid path
    /// </exception>
    private static string ValidateOutputDirectory(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        if (!Path.IsPathRooted(outputDirectory))
            throw new ArgumentException("The output directory must be an absolute path.", nameof(outputDirectory));
        try
        {
            return Path.GetFullPath(outputDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ArgumentException($"The output directory '{outputDirectory}' is not a valid path.",
                nameof(outputDirectory), ex);
        }
    }
}