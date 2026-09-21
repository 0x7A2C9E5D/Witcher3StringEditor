using System.Diagnostics;
using CommandLine;
using CommunityToolkit.Diagnostics;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;

namespace Witcher3StringEditor.Serializers.Implementation;

/// <summary>
///     Provides W3Strings serialization functionality for The Witcher 3 string items
///     Implements the IW3StringsSerializer interface to handle reading from and writing to W3Strings files
///     This serializer uses an external tool (W3Strings encoder/decoder) to perform the actual serialization
/// </summary>
public class W3StringsSerializer(
    IAppSettings appSettings,
    IBackupService backupService,
    ICsvW3Serializer csvSerializer)
    : IW3StringsSerializer
{
    /// <summary>
    ///     Deserializes The Witcher 3 string items from a W3Strings file
    ///     This method uses an external tool to decode the W3Strings file into a CSV format,
    ///     then uses the CSV serializer to read the data
    /// </summary>
    /// <param name="filePath">The path to the W3Strings file to deserialize</param>
    /// <param name="cancellationToken">A token used to abort the decoding</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items, or an empty list if an error occurred
    /// </returns>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var tempDirectory = Directory.CreateTempSubdirectory().FullName; // Create temporary directory

        try
        {
            // The copy happens inside the try so a failed copy cannot orphan the temporary directory
            var tempFilePath = CreateTemporaryCopy(filePath, tempDirectory);
            // Execute the external W3Strings decoder tool with the file to decode
            using var process = ExecuteExternalProcess(appSettings.W3StringsPath,
                Parser.Default.FormatCommandLine(W3StringsOptions.CreateDecodingOptions(tempFilePath)));
            await process.WaitForExitAsync(cancellationToken);
            Guard.IsEqualTo(process.ExitCode, 0); // Ensure the process completed successfully (exit code 0)
            return await csvSerializer.Deserialize($"{tempFilePath}.csv", cancellationToken); // Read decoded data
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deserializing W3Strings file: {Path}",
                filePath); // Log any errors that occur during deserialization
            return []; // Return an empty list in case of errors
        }
        finally
        {
            DeleteTemporaryDirectory(tempDirectory); // Delete the temporary directory
        }
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a W3Strings file
    ///     This method first creates a temporary CSV file, then uses an external tool to encode it into a W3Strings file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">
    ///     The serialization context containing output directory, target language,
    ///     and other serialization parameters
    /// </param>
    /// <param name="cancellationToken">A token used to abort the encoding</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(w3StringItems);
        ArgumentNullException.ThrowIfNull(context);
        var tempDirectory =
            Directory.CreateTempSubdirectory().FullName; // Create a temporary directory for intermediate files

        try
        {
            var saveLang =
                GetLanguageName(context.TargetLanguage); // Get the lowercase language name for file naming

            // Create a temporary context with the temp directory as output
            var tempContext = context with
            {
                OutputDirectory = tempDirectory
            };

            // Define paths for temporary CSV and W3Strings files
            var tempCsvPath = Path.Combine(tempDirectory, $"{saveLang}.csv");
            var outputW3StringsPath =
                Path.Combine(context.OutputDirectory, $"{saveLang}.w3strings");
            Guard.IsTrue(await csvSerializer.Serialize(w3StringItems, tempContext,
                cancellationToken)); // Serialize the items to a temporary CSV file
            Guard.IsTrue(await StartSerializationProcess(tempContext, tempCsvPath,
                cancellationToken)); // Encode the temporary CSV file to W3Strings format
            Guard.IsTrue(await ReplaceFileWithBackup(Path.ChangeExtension(tempCsvPath, ".w3strings"),
                outputW3StringsPath)); // Replace the destination file with backup if needed
            return true; // Return true to indicate successful serialization
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "An error occurred while serializing W3Strings"); // Log any errors that occur during serialization
            return false; // Return false to indicate serialization failure
        }
        finally
        {
            DeleteTemporaryDirectory(tempDirectory); // Delete the temporary directory
        }
    }

    /// <summary>
    ///     Gets the lower-case language name used in the file name
    /// </summary>
    /// <param name="language">The target language</param>
    /// <returns>The lower-case name of the language</returns>
    /// <exception cref="ArgumentOutOfRangeException">The language value is not defined</exception>
    private static string GetLanguageName(W3Language language)
    {
        return Enum.GetName(language)?.ToLowerInvariant()
               ?? throw new ArgumentOutOfRangeException(nameof(language), language,
                   "The target language is not defined.");
    }

    /// <summary>
    ///     Creates a temporary copy of the specified file inside the given temporary directory
    /// </summary>
    /// <param name="filePath">The path to the file to be copied</param>
    /// <param name="tempDirectory">The temporary directory that owns the copy</param>
    /// <returns>The path to the temporary copy of the file</returns>
    private static string CreateTemporaryCopy(string filePath, string tempDirectory)
    {
        var tempFilePath = Path.Combine(tempDirectory, Path.GetFileName(filePath)); // Build temporary file path
        File.Copy(filePath, tempFilePath, true); // Copy file to temporary location
        return tempFilePath; // Return temporary file path
    }

    /// <summary>
    ///     Deletes the temporary directory without letting a cleanup failure escape the method
    /// </summary>
    /// <param name="tempDirectory">The directory to delete</param>
    private static void DeleteTemporaryDirectory(string tempDirectory)
    {
        try
        {
            Directory.Delete(tempDirectory, true);
            Log.Debug("Temporary directory deleted: {Directory}", tempDirectory);
        }
        catch (Exception ex)
        {
            // A locked handle (the external tool may still hold one) must not replace the operation result
            Log.Warning(ex, "Failed to delete the temporary directory: {Directory}", tempDirectory);
        }
    }

    /// <summary>
    ///     Replaces a destination file with a source file, creating a backup of the destination file if it exists
    /// </summary>
    /// <param name="sourceFilePath">The path to the source file</param>
    /// <param name="destinationFilePath">The path to the destination file</param>
    /// <returns>True if the replacement was successful, false otherwise</returns>
    private async Task<bool> ReplaceFileWithBackup(string sourceFilePath, string destinationFilePath)
    {
        if (File.Exists(destinationFilePath) &&
            !await backupService.BackupAsync(destinationFilePath)) // Backup existing file before overwrite
            return false; // Return false if backup creation failed
        File.Copy(sourceFilePath, destinationFilePath, true); // Copy with overwrite
        return true; // Return true to indicate successful replacement
    }

    /// <summary>
    ///     Starts the serialization process by executing the external W3Strings encoder tool
    /// </summary>
    /// <param name="context">The serialization context containing serialization parameters</param>
    /// <param name="path">The path to the temporary CSV file to encode</param>
    /// <param name="cancellationToken">A token used to abort the encoding</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result indicates whether the process completed successfully (exit code 0)
    /// </returns>
    private async Task<bool> StartSerializationProcess(W3SerializationContext context, string path,
        CancellationToken cancellationToken)
    {
        // Execute the external W3Strings encoder tool with appropriate arguments based on context
        // If ignoring ID space check, pass the ignore flag, otherwise pass the expected ID space
        var options = W3StringsOptions.CreateEncodingOptions(path,
            context.IgnoreIdSpaceCheck ? null : context.ExpectedIdSpace, context.IgnoreIdSpaceCheck);
        using var process = ExecuteExternalProcess(appSettings.W3StringsPath,
            Parser.Default.FormatCommandLine(options));
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0; // Return true if the process completed successfully (exit code 0)
    }

    /// <summary>
    ///     Executes an external process with the specified filename and arguments
    ///     Captures and logs both standard output and error output
    /// </summary>
    /// <param name="filename">The filename of the executable to run</param>
    /// <param name="arguments">The arguments to pass to the executable</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the completed Process object
    /// </returns>
    private static Process ExecuteExternalProcess(string filename, string arguments)
    {
        // Create a new process with the specified filename and arguments
        var process = new Process
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = filename,
                Arguments = arguments
            }
        };

        // Attach event handlers for error and output data
        process.ErrorDataReceived += Process_ErrorDataReceived;
        process.OutputDataReceived += Process_OutputDataReceived;
        process.Start(); // Start the process
        process.BeginErrorReadLine(); // Begin reading error output
        process.BeginOutputReadLine(); // Begin reading standard output
        return process; // Return the completed process
    }

    /// <summary>
    ///     Handles the ErrorDataReceived event of the external process
    ///     Logs error output for the process
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The DataReceivedEventArgs instance containing the event data</param>
    private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        // The encoder reports problems on stderr; treat these as warnings rather than application errors
        if (!string.IsNullOrWhiteSpace(e.Data))
            Log.Warning("External encoder error output: {Data}", e.Data);
    }

    /// <summary>
    ///     Handles the OutputDataReceived event of the external process
    ///     Logs standard output from the process
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The DataReceivedEventArgs instance containing the event data</param>
    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        // Standard output of the encoder is diagnostic detail only
        if (!string.IsNullOrWhiteSpace(e.Data))
            Log.Debug("External encoder output: {Data}", e.Data);
    }
}