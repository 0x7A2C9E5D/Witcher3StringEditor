using System.Diagnostics;
using System.IO;
using CommunityToolkit.Diagnostics;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides functionality to play the game
///     Implements the IPlayGameService interface to handle starting the game process
/// </summary>
internal class PlayGameService(IAppSettings appSettings) : IPlayGameService
{
    /// <summary>
    ///     Starts the game process using the configured game executable path
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task PlayGame()
    {
        try
        {
            Log.Information("Starting the game process."); // Log start of game process
            using var process = new Process(); // Create new process
            process.EnableRaisingEvents = true; // Enable event raising
            process.StartInfo = new ProcessStartInfo // Configure process start info
            {
                FileName = appSettings.GameExePath, // Set game executable path
                WorkingDirectory = Path.GetDirectoryName(appSettings.GameExePath), // Set working directory
                RedirectStandardError = true, // Redirect standard error
                RedirectStandardOutput = true // Redirect standard output
            };
            process.ErrorDataReceived += Process_ErrorDataReceived; // Register error handler
            process.OutputDataReceived += Process_OutputDataReceived; // Register output handler
            process.Start(); // Start the process
            process.BeginErrorReadLine(); // Begin reading error output
            process.BeginOutputReadLine(); // Begin reading standard output
            await process.WaitForExitAsync(); // Wait for process to exit
            Guard.IsEqualTo(process.ExitCode, 0); // Ensure exit code is 0
            Log.Information("Game process exited with code {ExitCode}.", process.ExitCode); // Log exit code
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start the game process."); // Log any errors
        }
    }

    /// <summary>
    ///     Handles the error data received event from the game process
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">A DataReceivedEventArgs that contains the event data</param>
    private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        // Log any error output from the game process
        if (!string.IsNullOrWhiteSpace(e.Data))
            Log.Error("Error: {Data}.", e.Data);
    }

    /// <summary>
    ///     Handles the output data received event from the game process
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">A DataReceivedEventArgs that contains the event data</param>
    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        // Log any standard output from the game process
        if (!string.IsNullOrWhiteSpace(e.Data))
            Log.Information("Output: {Data}.", e.Data);
    }
}