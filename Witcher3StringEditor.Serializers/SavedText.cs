namespace Witcher3StringEditor.Serializers;

/// <summary>
///     The text of a string item as a save writes it
/// </summary>
/// <remarks>
///     <para>
///         A line break of a text is not saved as a line break. A text of The Witcher 3 is markup, and a break
///         in it is the <c>&lt;br&gt;</c> tag the game reads rather than the newline character an editor holds,
///         so the break an item carries is turned into the tag when the item is saved.
///     </para>
///     <para>
///         The text a save writes is handed back to the item it came from rather than to the serializer that
///         writes it: a save gives every one of its items the text of <see cref="NormalizeLineBreaks" />, so
///         every format writes the same text without knowing about the rule, and the items show what a save
///         wrote.
///     </para>
/// </remarks>
internal static class SavedText
{
    /// <summary>
    ///     The markup a line break is saved as
    /// </summary>
    private const string LineBreakMarkup = "<br>";

    /// <summary>
    ///     Turns every line break of a text into the markup a save writes it as
    /// </summary>
    /// <param name="text">The text of an item, as it was edited</param>
    /// <returns>The text, with <c>&lt;br&gt;</c> where each line break was and no line break left in it</returns>
    /// <remarks>
    ///     A carriage return and the line feed after it are one break, so the pair is replaced before either
    ///     of them is: replacing them one at a time would save two tags for one break. The tags an older save
    ///     wrote are not markup to this method, it only knows line breaks, which is what keeps it idempotent.
    ///     A text that holds no break at all is handed back as it is.
    /// </remarks>
    public static string NormalizeLineBreaks(string text)
    {
        if (text.AsSpan().IndexOfAny('\r', '\n') < 0) return text; // No break to turn into markup
        return text.Replace("\r\n", LineBreakMarkup, StringComparison.Ordinal)
            .Replace("\r", LineBreakMarkup, StringComparison.Ordinal)
            .Replace("\n", LineBreakMarkup, StringComparison.Ordinal);
    }
}
