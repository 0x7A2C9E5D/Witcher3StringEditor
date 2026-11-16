using System.Text.Json;
using Serilog;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Serializable payload of a recently opened file stored in the replicated log.
/// </summary>
internal sealed record RecentFilePayload(
    string FilePath,
    DateTime OpenedTime,
    bool IsMarked,
    bool IsDeleted = false);

/// <summary>
///     Serializable payload of a backup record stored in the replicated log.
/// </summary>
internal sealed record BackupPayload(
    string FileName,
    string Hash,
    string OriginPath,
    string BackupPath,
    DateTime BackupTime,
    bool IsDeleted = false);

/// <summary>
///     Point-in-time view of the replicated records, keyed by normalized record keys.
/// </summary>
internal sealed record SharedRecordsSnapshot(
    IReadOnlyDictionary<string, string> RecentFiles,
    IReadOnlyDictionary<string, string> Backups);

/// <summary>
///     A record mutation broadcast to the other application instances. Upserts and tombstones
///     share the same shape; tombstones carry an <c>IsDeleted</c> flag inside the encoded value.
/// </summary>
internal sealed record SharedRecordCommand(string Key, string Value, bool IsBackup);

/// <summary>
///     Conflict resolution metadata of an encoded record.
/// </summary>
internal readonly record struct SharedRecordVersion(long Timestamp, bool IsDeleted)
{
    /// <summary>
    ///     Returns <see langword="true" /> when this version supersedes <paramref name="other" />.
    ///     A tombstone wins against a live record created at the same instant.
    /// </summary>
    public bool Supersedes(SharedRecordVersion other)
    {
        return Timestamp > other.Timestamp || (Timestamp == other.Timestamp && IsDeleted && !other.IsDeleted);
    }
}

/// <summary>
///     Encodes and decodes shared records into the payloads exchanged between instances.
///     The application settings iw3StringSerializer is left untouched.
/// </summary>
internal static class SharedRecordCodec
{
    private const string RecentPrefix = "recent:";
    private const string BackupPrefix = "backup:";

    public static string RecentKey(string filePath)
    {
        return RecentPrefix + filePath.Trim().ToLowerInvariant();
    }

    public static string BackupKey(string backupPath)
    {
        return BackupPrefix + backupPath.Trim().ToLowerInvariant();
    }

    public static string EncodeRecent(IRecentFileEntry entry)
    {
        return JsonSerializer.Serialize(new RecentFilePayload(
            entry.FilePath, entry.OpenedTime, entry.IsMarked));
    }

    public static string EncodeBackup(IBackupItem item)
    {
        return JsonSerializer.Serialize(new BackupPayload(
            item.FileName, item.Hash, item.OrginPath, item.BackupPath, item.BackupTime));
    }

    /// <summary>
    ///     Tombstone for a deleted recent file. Kept as a normal value so late joiners
    ///     receive a versioned deletion instead of re-seeding a stale record from disk.
    /// </summary>
    public static string EncodeDeletedRecent(string filePath)
    {
        return JsonSerializer.Serialize(new RecentFilePayload(
            filePath, DateTime.UtcNow, false, true));
    }

    /// <summary>
    ///     Tombstone for a deleted backup record.
    /// </summary>
    public static string EncodeDeletedBackup(IBackupItem item)
    {
        return JsonSerializer.Serialize(new BackupPayload(
            item.FileName, item.Hash, item.OrginPath, item.BackupPath, DateTime.UtcNow, true));
    }

    public static RecentFilePayload? DecodeRecent(string value)
    {
        return TryDecode<RecentFilePayload>(value);
    }

    public static BackupPayload? DecodeBackup(string value)
    {
        return TryDecode<BackupPayload>(value);
    }

    /// <summary>
    ///     Conflict resolution version: the business time for live records and the deletion
    ///     time for tombstones.
    /// </summary>
    public static SharedRecordVersion GetVersion(bool isBackup, string value)
    {
        if (isBackup)
        {
            var payload = DecodeBackup(value);
            return payload is null
                ? new SharedRecordVersion(0L, false)
                : new SharedRecordVersion(payload.BackupTime.ToUniversalTime().Ticks, payload.IsDeleted);
        }

        var recent = DecodeRecent(value);
        return recent is null
            ? new SharedRecordVersion(0L, false)
            : new SharedRecordVersion(recent.OpenedTime.ToUniversalTime().Ticks, recent.IsDeleted);
    }

    private static T? TryDecode<T>(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Ignoring an unreadable shared record of type {RecordType}", typeof(T).Name);
            return default;
        }
    }
}