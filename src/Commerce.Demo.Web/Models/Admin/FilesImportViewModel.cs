using Commerce.Demo.Business.Features.FilesImport.Models;

namespace Commerce.Demo.Web.Models.Admin;

public class FilesImportViewModel
{
    public FilesImportResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
