using Serilog;
using Witcher3StringEditor.W3Strings.Model;

namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     Checks the container that is about to be written to a w3strings file
///     The check runs on the save path only. Reading a file never fails over content that still decodes,
///     and the other file formats keep their own semantics, so neither of them comes through here
/// </summary>
public static class W3StringsValidator
{
    /// <summary>
    ///     Checks a container that is about to be written and writes every anomaly to the log
    /// </summary>
    /// <param name="file">The container to check</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when the container may be written, false when one of its entries is unreachable</returns>
    /// <exception cref="ArgumentNullException">Thrown when the container or the logger is null</exception>
    public static bool Validate(W3StringsFile file, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(logger);

        if (file.Strings.Count == 0)
        {
            logger.Error("The container holds no string entry at all");
            return false;
        }

        var mayWrite = ValidateStrings(file, logger);
        if (!ValidateKeys(file, logger)) mayWrite = false;

        return mayWrite;
    }

    /// <summary>
    ///     Checks the readable key names of the items that are about to be written
    ///     Two items that share a key name end up sharing a key hash while pointing at different ids,
    ///     which makes one of the two texts unreachable. Only a name collision can be reported here,
    ///     because the container itself no longer holds the names
    /// </summary>
    /// <param name="entries">The string id and readable key name of every item that carries a name</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when every name belongs to a single id, false when one of them is shared</returns>
    /// <exception cref="ArgumentNullException">Thrown when the entries or the logger is null</exception>
    public static bool ValidateKeyNames(IEnumerable<(uint Id, string KeyName)> entries, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(logger);

        var idsByKeyName = new Dictionary<string, HashSet<uint>>(StringComparer.Ordinal);
        foreach (var (id, keyName) in entries)
        {
            if (string.IsNullOrWhiteSpace(keyName)) continue;
            if (!idsByKeyName.TryGetValue(keyName, out var ids))
                idsByKeyName[keyName] = ids = [];

            ids.Add(id);
        }

        var mayWrite = true;
        foreach (var (keyName, ids) in idsByKeyName.Where(pair => pair.Value.Count > 1))
        {
            var relatedIds = ids.Order().ToArray();
            logger.Error(
                "The key name {KeyName} is used by {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                keyName, relatedIds.Length, string.Join(", ", relatedIds));
            mayWrite = false;
        }

        return mayWrite;
    }

    /// <summary>
    ///     Reports the anomalies of the string block
    /// </summary>
    /// <param name="file">The container to check</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when no string entry is unreachable</returns>
    private static bool ValidateStrings(W3StringsFile file, ILogger logger)
    {
        var mayWrite = true;

        // Two entries under one id means the game resolves the id to a single text while the other
        // one is silently unreachable, which is why such a container is refused.
        foreach (var group in file.Strings.GroupBy(entry => entry.Id).Where(group => group.Count() > 1))
        {
            logger.Error(
                "String ID {StringId} appears {Count} time(s) in the container, so only one of its texts can be reached",
                group.Key, group.Count());
            mayWrite = false;
        }

        foreach (var entry in file.Strings.Where(entry => entry.Value.Contains('\0')))
            logger.Warning("String ID {StringId} contains a null character, the game cuts the text there", entry.Id);

        var keyedIds = file.Keys.Select(key => key.Id).ToHashSet();
        var orphanCount = file.Strings.Select(entry => entry.Id).Distinct().Count(id => !keyedIds.Contains(id));
        if (orphanCount > 0)
            logger.Information("{Count} string(s) have no key and cannot be reached through a key lookup",
                orphanCount);

        return mayWrite;
    }

    /// <summary>
    ///     Reports the anomalies of the key block
    /// </summary>
    /// <param name="file">The container to check</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when every key resolves to a single string entry</returns>
    private static bool ValidateKeys(W3StringsFile file, ILogger logger)
    {
        foreach (var group in file.Keys.GroupBy(key => (key.KeyHash, key.Id)).Where(group => group.Count() > 1))
            logger.Warning("Key hash 0x{KeyHash:X8} of string ID {StringId} is listed {Count} time(s)",
                group.Key.KeyHash, group.Key.Id, group.Count());

        var mayWrite = true;
        foreach (var group in file.Keys.GroupBy(key => key.KeyHash)
                     .Where(group => group.Select(key => key.Id).Distinct().Count() > 1))
        {
            var relatedIds = group.Select(key => key.Id).Distinct().Order().ToArray();
            logger.Error(
                "Key hash 0x{KeyHash:X8} points at {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                group.Key, relatedIds.Length, string.Join(", ", relatedIds));
            mayWrite = false;
        }

        foreach (var group in file.Keys.GroupBy(key => key.Id)
                     .Where(group => group.Select(key => key.KeyHash).Distinct().Count() > 1))
            logger.Warning("String ID {StringId} is referenced by {Count} different keys, only the first one is kept",
                group.Key, group.Select(key => key.KeyHash).Distinct().Count());

        return mayWrite;
    }
}