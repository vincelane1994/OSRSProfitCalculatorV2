namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Service for exporting calculator data to external formats (Excel, CSV, Google Sheets).
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports data to an Excel file and returns the file path.
    /// </summary>
    Task<string> ExportToExcelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs data to Google Sheets.
    /// </summary>
    Task SyncToGoogleSheetsAsync(CancellationToken cancellationToken = default);
}
