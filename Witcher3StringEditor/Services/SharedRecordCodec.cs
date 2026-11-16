using System.Text.Json;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Serializable payload of a recently opened file stored in a CRDT map.
/// </summary>
internal sealed record RecentFilePayload(
    string FilePath,
    DateTime OpenedTime,
    bool IsMarked,
    bool IsDeleted = false);

/// <summary>
///     Serializable payload of a backup record stored in a CRDT map.
/// </summary>
internal sealed record BackupPayload(
    string FileName,
    string Hash,
    string OriginPath,
    string BackupPath,
    DateTime BackupTime,
    bool IsDeleted = false);

/// <summary>
///     Point-in-time view of the two CRDT maps, keyed by normalized record keys.
/// </summary>
internal sealed record SharedRecordsSnapshot(
    IReadOnlyDictionary<string, string> RecentFiles,
    IReadOnlyDictionary<string, string> Backups);

/// <summary>
///     Encodes and decodes shared records into the string values stored by DistributedData.
///     The application settings serializer is left untouched.
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
    ///     Tombstone for a deleted recent file. Kept as a normal LWW value so late joiners
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
    ///     LWW clock returning an ever-increasing timestamp: the business time for live records
    ///     and the deletion time for tombstones, so a deletion always wins over an older value.
    /// </summary>
    public static long RecentClock(long currentTimestamp, string value)
    {
        var payload = DecodeRecent(value);
        return payload is null ? currentTimestamp : Timestamp(payload.OpenedTime, payload.IsDeleted, currentTimestamp);
    }

    /// <summary>
    ///     LWW clock for backup records, using the backup time or the deletion time.
    /// </summary>
    public static long BackupClock(long currentTimestamp, string value)
    {
        var payload = DecodeBackup(value);
        return payload is null ? currentTimestamp : Timestamp(payload.BackupTime, payload.IsDeleted, currentTimestamp);
    }

    private static long Timestamp(DateTime businessTime, bool isDeleted, long currentTimestamp)
    {
        var ticks = businessTime.ToUniversalTime().Ticks;
        return isDeleted ? Math.Max(ticks, currentTimestamp + 1) : ticks;
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