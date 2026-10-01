namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Thrown when a file is not a w3strings container this build can decode, or cannot be written as one
/// </summary>
/// <param name="message">What went wrong, in the words of the codec</param>
/// <param name="innerException">The failure underneath this one, when there was any</param>
/// <remarks>
///     The codec answers with this one type and every caller of it sits in this assembly: the serializers
///     facing the rest of the application catch what it throws, log it and report it themselves, so it never
///     reaches a caller outside. It is internal for that reason
/// </remarks>
public sealed class W3StringsException(string message, Exception? innerException = null)
    : Exception(message, innerException);