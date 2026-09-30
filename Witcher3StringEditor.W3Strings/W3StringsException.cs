namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     Thrown when a file is not a w3strings container this build can decode, or cannot be written as one
/// </summary>
/// <param name="message">What went wrong, in the words of the codec</param>
/// <param name="innerException">The failure underneath this one, when there was any</param>
public sealed class W3StringsException(string message, Exception? innerException = null)
    : Exception(message, innerException);