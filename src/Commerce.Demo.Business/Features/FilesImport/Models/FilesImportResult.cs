namespace Commerce.Demo.Business.Features.FilesImport.Models;

public class FilesImportResult
{
    public int Total { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public string TargetFolderPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    public IList<FilesImportItem> Items { get; set; } = new List<FilesImportItem>();
}
