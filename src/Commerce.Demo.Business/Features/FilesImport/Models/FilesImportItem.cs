using Commerce.Demo.Business.Features.FilesImport.Enums;

namespace Commerce.Demo.Business.Features.FilesImport.Models;

public class FilesImportItem
{
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public FilesImportStatus Status { get; set; }
    public string? MediaType { get; set; }
    public string? Message { get; set; }
    public long SizeBytes { get; set; }
}
