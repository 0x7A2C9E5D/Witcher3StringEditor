using CommandLine;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Serializers.Implementation;

/// <summary>
///     Represents the command line options for the W3Strings encoder/decoder tool
///     This record defines the parameters that can be passed to the external W3Strings processing tool
/// </summary>
internal record W3StringsOptions
{
    /// <summary>
    ///     The mutually exclusive set the encode/decode input options belong to
    /// </summary>
    private const string InputSet = "input";

    /// <summary>
    ///     Gets the path to the input file to decode
    ///     This option is used when decoding a W3Strings file to CSV format and is mutually exclusive with
    ///     <see cref="InputFileToEncode" />
    /// </summary>
    [UsedImplicitly]
    [Option('d', SetName = InputSet)]
    public string? InputFileToDecode { get; init; }

    /// <summary>
    ///     Gets the path to the input file to encode
    ///     This option is used when encoding a CSV file to W3Strings format and is mutually exclusive with
    ///     <see cref="InputFileToDecode" />
    /// </summary>
    [UsedImplicitly]
    [Option('e', SetName = InputSet)]
    public string? InputFileToEncode { get; init; }

    /// <summary>
    ///     Gets the expected ID space value
    ///     This is used during the encoding process to validate the ID space of the strings.
    ///     Nullable on purpose: an unset value is omitted from the formatted command line instead of being sent as
    ///     a misleading <c>-i 0</c>
    /// </summary>
    [UsedImplicitly]
    [Option('i')]
    public int? ExpectedIdSpace { get; init; }

    /// <summary>
    ///     Gets a value indicating whether to ignore the ID space check
    ///     When true, bypasses the ID space validation during encoding. This must never be combined with
    ///     <see cref="ExpectedIdSpace" />, which is enforced by <see cref="CreateEncodingOptions" />
    /// </summary>
    [UsedImplicitly]
    [Option("force-ignore-id-space-check-i-know-what-i-am-doing")]
    public bool IgnoreIdSpaceCheck { get; init; }

    /// <summary>
    ///     Creates the options for a decode operation and validates the input
    /// </summary>
    /// <param name="inputFileToDecode">The path of the file to decode</param>
    /// <returns>The command line options</returns>
    /// <exception cref="ArgumentException"><paramref name="inputFileToDecode" /> is null, empty or white-space</exception>
    public static W3StringsOptions CreateDecodingOptions(string inputFileToDecode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFileToDecode);
        return new W3StringsOptions { InputFileToDecode = inputFileToDecode };
    }

    /// <summary>
    ///     Creates the options for an encode operation and validates the ID space arguments
    /// </summary>
    /// <param name="inputFileToEncode">The path of the file to encode</param>
    /// <param name="expectedIdSpace">The expected ID space, or null when it should not be supplied</param>
    /// <param name="ignoreIdSpaceCheck">Whether the ID space check must be skipped</param>
    /// <returns>The command line options</returns>
    /// <exception cref="ArgumentException"><paramref name="inputFileToEncode" /> is null, empty or white-space</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expectedIdSpace" /> is negative</exception>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="ignoreIdSpaceCheck" /> is combined with a non-null <paramref name="expectedIdSpace" />
    /// </exception>
    public static W3StringsOptions CreateEncodingOptions(string inputFileToEncode, int? expectedIdSpace,
        bool ignoreIdSpaceCheck)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFileToEncode);
        if (ignoreIdSpaceCheck && expectedIdSpace is not null)
            throw new InvalidOperationException(
                "The ID space check cannot be ignored and validated at the same time.");
        if (expectedIdSpace is < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedIdSpace), expectedIdSpace,
                "The expected ID space must not be negative.");

        return new W3StringsOptions
        {
            InputFileToEncode = inputFileToEncode,
            ExpectedIdSpace = ignoreIdSpaceCheck ? null : expectedIdSpace,
            IgnoreIdSpaceCheck = ignoreIdSpaceCheck
        };
    }
}