using CommunityToolkit.Diagnostics;
using Serilog;
using Syncfusion.XlsIO;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.Internal;

namespace Witcher3StringEditor.Serializers.Implementation;

/// <summary>
///     Provides Excel serialization functionality for The Witcher 3 string items
///     Implements the IExcelW3Serializer interface to handle reading from and writing to Excel files
/// </summary>
public class ExcelW3Serializer(IBackupService backupService) : IExcelW3Serializer
{
    /// <summary>
    ///     Deserializes The Witcher 3 string items from an Excel file
    /// </summary>
    /// <param name="filePath">The path to the Excel file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items, or an empty list if an error occurred
    /// </returns>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath)
    {
        try
        {
            return await Task.Run(() => // Async deserialize
            {
                using var excelEngine = new ExcelEngine(); // Auto-cleanup engine
                var worksheet = excelEngine.Excel.Workbooks.Open(filePath).Worksheets[0]; // Get 1st sheet
                var usedRange = worksheet.UsedRange; // Get data range
                return worksheet.ExportData<W3StringStringItem>(1, 1, usedRange.LastRow,
                    usedRange.LastColumn); // Export data
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deserializing Excel worksheets file: {Path}.",
                filePath); // Log error
            return []; // Empty on fail
        }
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to an Excel file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing output directory and target language information</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context)
    {
        try
        {
            // Run the serialization process on a background thread to prevent UI blocking
            return await Task.Run(() =>
            {
                Guard.IsGreaterThan(w3StringItems.Count, 0); // Require items to serialize
                var filePath = Path.Combine(context.OutputDirectory,
                    $"{Enum.GetName(context.TargetLanguage)!.ToLowerInvariant()}.xlsx"); // Build language-specific Excel path
                if (File.Exists(filePath))
                    Guard.IsTrue(backupService.Backup(filePath)); // Backup existing file if exists
                GenerateExcelFile(filePath, w3StringItems); // Generate Excel file
                return true; // Return success
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while serializing the Excel worksheets file."); // Log serialization error
            return false; // Return failure
        }
    }

    /// <summary>
    ///     Generates an Excel file with the provided The Witcher 3 string items
    /// </summary>
    /// <param name="path">The path where the Excel file will be created</param>
    /// <param name="w3StringItems">The Witcher 3 string items to include in the Excel file</param>
    private static void GenerateExcelFile(string path, IReadOnlyList<IW3StringItem> w3StringItems)
    {
        using var fileStream = File.Create(path); // Create output file stream (auto-disposed)
        using var excelEngine = new ExcelEngine(); // Initialize Excel engine (auto-cleanup)
        var application = excelEngine.Excel; // Get Excel application instance
        application.DefaultVersion = ExcelVersion.Xlsx; // Set default format to XLSX
        var workbook = application.Workbooks.Create(1); // Create workbook with 1 sheet
        var worksheet = workbook.Worksheets[0]; // Get first worksheet
        FormatWorksheet(worksheet, w3StringItems); // Format worksheet (headers/styles/widths)
        WriteDataToWorksheet(worksheet, w3StringItems); // Write data to worksheet
        workbook.SaveAs(fileStream); // Save workbook to stream
    }

    /// <summary>
    ///     Formats the worksheet with headers, styles, and column widths
    /// </summary>
    /// <param name="worksheet">The worksheet to format</param>
    /// <param name="w3StringItems">The Witcher 3 string items used to determine row count</param>
    private static void FormatWorksheet(IWorksheet worksheet, IReadOnlyList<IW3StringItem> w3StringItems)
    {
        SetTableHeaders(worksheet); // Set table headers
        SetTableStyles(worksheet, w3StringItems.Count); // Apply table styles (align/borders/freeze)
        SetColumnWidths(worksheet); // Adjust column widths
    }

    /// <summary>
    ///     Sets the table headers in the first row of the worksheet
    /// </summary>
    /// <param name="worksheet">The worksheet to set headers on</param>
    private static void SetTableHeaders(IWorksheet worksheet)
    {
        // Set the header values for the worksheet columns:
        worksheet["A1"].Value = "StrId"; // Column A: String ID (StrId)
        worksheet["B1"].Value = "KeyHex"; // Column B: Key in hexadecimal format (KeyHex)
        worksheet["C1"].Value = "KeyName"; // Column C: Key name (KeyName)
        worksheet["D1"].Value = "OldText"; // Column D: Original text (OldText)
        worksheet["E1"].Value = "Text"; // Column E: Translated/new text (Text)
    }

    /// <summary>
    ///     Applies formatting styles to the worksheet
    /// </summary>
    /// <param name="worksheet">The worksheet to apply styles to</param>
    /// <param name="rowCount">The number of data rows in the worksheet</param>
    private static void SetTableStyles(IWorksheet worksheet, int rowCount)
    {
        ApplyHeaderRowStyle(worksheet); // Style header row
        ApplyDataCellAlignment(worksheet, rowCount); // Style data cells
        ApplyTableBorders(worksheet, rowCount); // Add table borders
        ApplyTextCellWrapping(worksheet, rowCount); // Style text cells
        FreezeHeaderRow(worksheet); // Freeze header row
    }

    /// <summary>
    ///     Formats the header row with bold text, center alignment, and color
    /// </summary>
    /// <param name="worksheet">The worksheet containing the header row</param>
    private static void ApplyHeaderRowStyle(IWorksheet worksheet)
    {
        var headerRange = worksheet["A1:E1"]; // Header range (A1-E1)
        headerRange.CellStyle.Font.Bold = true; // Bold text
        headerRange.HorizontalAlignment = ExcelHAlign.HAlignCenter; // Horizontal center
        headerRange.VerticalAlignment = ExcelVAlign.VAlignCenter; // Vertical center
        headerRange.CellStyle.ColorIndex = ExcelKnownColors.Grey_80_percent; // Dark gray background
        headerRange.CellStyle.Font.Color = ExcelKnownColors.White; // White font
    }

    /// <summary>
    ///     Applies center alignment to all data cells in the worksheet
    /// </summary>
    /// <param name="worksheet">The worksheet containing the cells to format</param>
    /// <param name="rowCount">The number of data rows in the worksheet</param>
    private static void ApplyDataCellAlignment(IWorksheet worksheet, int rowCount)
    {
        var normalRange = worksheet[$"A2:C{rowCount + 1}"]; // Data range (A2:C{n}, excludes header)
        normalRange.HorizontalAlignment = ExcelHAlign.HAlignCenter; // Horizontal center alignment
        normalRange.VerticalAlignment = ExcelVAlign.VAlignCenter; // Vertical center alignment
    }

    /// <summary>
    ///     Applies borders to the entire table
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table</param>
    /// <param name="rowCount">The number of data rows in the worksheet</param>
    private static void ApplyTableBorders(IWorksheet worksheet, int rowCount)
    {
        var tableRange = worksheet[$"A1:E{rowCount + 1}"]; // Full table range (headers + data)
        tableRange.Borders.LineStyle = ExcelLineStyle.Thin; // Thin border on all edges
        tableRange.Borders[ExcelBordersIndex.DiagonalUp].ShowDiagonalLine = false; // Disable diagonal up
        tableRange.Borders[ExcelBordersIndex.DiagonalDown].ShowDiagonalLine = false; // Disable diagonal down
        tableRange.NumberFormat = "@"; // Text format (preserve leading zeros)
    }

    /// <summary>
    ///     Applies text wrapping to text cells
    /// </summary>
    /// <param name="worksheet">The worksheet containing the text cells</param>
    /// <param name="rowCount">The number of data rows in the worksheet</param>
    private static void ApplyTextCellWrapping(IWorksheet worksheet, int rowCount)
    {
        var textRange = worksheet[$"D2:E{rowCount + 1}"]; // Text range (D2-E{n+1})
        textRange.WrapText = true; // Enable text wrapping
    }

    /// <summary>
    ///     Freezes the header row so it remains visible when scrolling
    /// </summary>
    /// <param name="worksheet">The worksheet to freeze the header row on</param>
    private static void FreezeHeaderRow(IWorksheet worksheet)
    {
        worksheet["A2:E2"].FreezePanes(); // Freeze header
    }

    /// <summary>
    ///     Sets appropriate column widths for optimal viewing
    /// </summary>
    /// <param name="worksheet">The worksheet to set column widths on</param>
    private static void SetColumnWidths(IWorksheet worksheet)
    {
        worksheet["A:B"].ColumnWidth = 15; //Columns A-B (15 units): Short ID/hex values
        worksheet["C:C"].ColumnWidth = 30; //Column C (30 units): Longer key names
        worksheet["D:E"].ColumnWidth = 50; //Columns D-E (50 units): Multi-line text content
    }

    /// <summary>
    ///     Writes The Witcher 3 string items data to the worksheet
    /// </summary>
    /// <param name="worksheet">The worksheet to write data to</param>
    /// <param name="w3StringItems">The Witcher 3 string items to write</param>
    private static void WriteDataToWorksheet(IWorksheet worksheet, IReadOnlyList<IW3StringItem> w3StringItems)
    {
        // Iterate through each string item to write data to the worksheet
        for (var i = 0; i < w3StringItems.Count; i++)
        {
            var rowIndex = i + 2; //Row 1 is header, data starts from row 2
            worksheet[$"A{rowIndex}"].Value = w3StringItems[i].StrId; //Column A: StrId
            worksheet[$"B{rowIndex}"].Value = w3StringItems[i].KeyHex; //Column B: KeyHex
            worksheet[$"C{rowIndex}"].Value = w3StringItems[i].KeyName; //Column C: KeyName
            worksheet[$"D{rowIndex}"].Value = w3StringItems[i].OldText; //Column D: OldText
            worksheet[$"E{rowIndex}"].Value = w3StringItems[i].Text; //Column E: Text
        }
    }
}