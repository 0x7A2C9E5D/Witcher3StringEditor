using System.Globalization;

namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     Manages dictionaries: importing, de-duplicating by file name, removing and querying them by language
/// </summary>
/// <remarks>
///     <para>
///         Implementations must be safe for concurrent use: importing and removing mutate shared registry state
///         while <see cref="ContainsDuplicate" /> and <see cref="Find" /> read it, and the registry is also
///         populated in the background during startup.
///     </para>
///     <para>
///         Registry entries are compared by their normalized file name using an ordinal, case-insensitive
///         comparison, because the file system is case-insensitive on the supported platforms.
///     </para>
/// </remarks>
public interface IDictionaryManager
{
    /// <summary>
    ///     Imports a dictionary from a file, replacing any dictionary already registered under the same file name
    /// </summary>
    /// <param name="filePath">The absolute path of the dictionary file to import</param>
    /// <returns>The registered dictionary information, or null when the file is not a readable dictionary</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    /// <exception cref="InvalidDataException">The file is not a valid dictionary</exception>
    /// <exception cref="IOException">The file could not be copied into the dictionary directory</exception>
    Task<DictionaryInfo?> Import(string filePath);

    /// <summary>
    ///     Determines whether a dictionary with the same file name is already registered
    /// </summary>
    /// <param name="filePath">The path of the dictionary file being imported</param>
    /// <returns><c>true</c> when importing would overwrite an existing dictionary</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    bool ContainsDuplicate(string filePath);

    /// <summary>
    ///     Removes the specified dictionary and deletes its file
    /// </summary>
    /// <param name="dictionary">The dictionary to remove; the registration is located by its path</param>
    /// <remarks>
    ///     A null argument is ignored. When the file cannot be deleted the registration is kept, so the registry
    ///     never references a dictionary that is missing on disk
    /// </remarks>
    void Remove(DictionaryInfo dictionary);

    /// <summary>
    ///     Finds all dictionaries matching the specified language
    /// </summary>
    /// <param name="language">The language to filter by, or null to get every registered dictionary</param>
    /// <returns>
    ///     An immutable snapshot of the matching dictionaries; enumerating it is safe while the registry changes
    /// </returns>
    IEnumerable<DictionaryInfo> Find(CultureInfo? language);
}