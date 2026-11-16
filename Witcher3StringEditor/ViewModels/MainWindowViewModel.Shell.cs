using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Witcher3StringEditor.Services;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Shell commands that start the game or open folders and links, together with the state that decides
///     whether each one is available.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Gets a value indicating whether the working folder can be opened
    /// </summary>
    private bool CanOpenWorkingFolder
        => Directory.Exists(OutputFolder);

    /// <summary>
    ///     Gets a value indicating whether the game can be played
    /// </summary>
    private bool CanPlayGame => File.Exists(AppSettings.GameExePath);

    /// <summary>
    ///     Starts the game process
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlayGame))]
    private async Task PlayGame()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPlayGameService>().PlayGame();
    }

    /// <summary>
    ///     Opens the working folder in Windows Explorer
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenWorkingFolder))]
    private void OpenWorkingFolder()
    {
        shellOpenService.Open(OutputFolder); // Open the working folder
    }

    /// <summary>
    ///     Opens the NexusMods page in the default browser
    /// </summary>
    [RelayCommand]
    private void OpenNexusMods()
    {
        shellOpenService.Open(AppSettings.NexusModUrl); // Open the NexusMods page
    }
}