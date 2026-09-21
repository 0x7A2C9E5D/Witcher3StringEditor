namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for backup item information
///     Represents metadata about a backed up file including its location, hash, and backup time
/// </summary>
public interface IBackupItem
{
    /// <summary>
    ///     Gets the name of the backed up file
    ///     Must be a non-null, non-empty file name; a null/empty value indicates a corrupt backup record
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    string FileName { get; }

    /// <summary>
    ///     Gets the hash of the backed up file
    ///     Used to verify file integrity and detect changes
    ///     Producers must emit an SHA-256 digest encoded as a 64 character, lower-case hexadecimal string
    ///     (invariant culture) so that all consumers can compare values reliably
    /// </summary>
    string Hash { get; }

    /// <summary>
    ///     Gets the original path of the file before backup
    ///     Must be a non-null, non-whitespace path
    /// </summary>
    string OrginPath { get; }

    /// <summary>
    ///     Gets the path where the backup file is stored
    ///     Must be a non-null, non-whitespace path
    /// </summary>
    string BackupPath { get; }

    /// <summary>
    ///     Gets the time when the backup was created
    ///     Expressed as a <see cref="DateTime" /> so the instant stays unambiguous across time zones and
    ///     daylight saving changes, which keeps ordering of the backup list stable
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    DateTime BackupTime { get; }
}