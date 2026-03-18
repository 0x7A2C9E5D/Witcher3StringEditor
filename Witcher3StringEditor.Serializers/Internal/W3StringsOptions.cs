using CommandLine;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Serializers.Internal;

/// <summary>
///     Represents the command line options for the W3Strings encoder/decoder tool
///     This record defines the parameters that can be passed to the external W3Strings processing tool
/// </summary>
internal record W3StringsOptions
{
    /// <summary>
    ///     Gets the path to the input file to decode
    ///     This option is used when decoding a W3Strings file to CSV format
    /// </summary>
    [UsedImplicitly]
    [Option('d')]
    public string InputFileToDecode { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the path to the input file to encode
    ///     This option is used when encoding a CSV file to W3Strings format
    /// </summary>
    [UsedImplicitly]
    [Option('e')]
    public string InputFileToEncode { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the expected ID space value
    ///     This is used during the encoding process to validate the ID space of the strings
    /// </summary>
    [UsedImplicitly]
    [Option('i')]
    public int ExpectedIdSpace { get; init; }

    /// <summary>
    ///     Gets a value indicating whether to ignore the ID space check
    ///     When true, bypasses the ID space validation during encoding
    /// </summary>
    [UsedImplicitly]
    [Option("force-ignore-id-space-check-i-know-what-i-am-doing")]
    public bool IgnoreIdSpaceCheck { get; init; }
}