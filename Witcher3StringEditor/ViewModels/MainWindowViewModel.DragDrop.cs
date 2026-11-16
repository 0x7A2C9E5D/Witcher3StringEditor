using System.IO;
using System.Windows;
using iNKORE.UI.WPF.DragDrop;
using Serilog;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Drop target: accepts files dragged onto the main window and opens the first one it supports.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Handles the drag over event
    /// </summary>
    /// <param name="dropInfo"></param>
    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.Effects = DragDropEffects.Copy;
    }

    /// <summary>
    ///     Handles the drop event
    /// </summary>
    /// <param name="dropInfo"></param>
    public async void Drop(IDropInfo dropInfo)
    {
        try
        {
            if (dropInfo.Data is not IDataObject data) return; // Check if data is null
            var dropData = data.GetData(DataFormats.FileDrop) as string[]; // Get the file drop data
            var file = dropData?.FirstOrDefault(); // Get the first file
            if (file is null) return; // Check if file is null
            var ext = Path.GetExtension(file); // Get the file extension
            if (ext is ".csv" or ".w3strings" or ".xlsx")
                await LoadFileAsync(file); // Open file if extension is supported
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while dropping file");
        }
    }
}