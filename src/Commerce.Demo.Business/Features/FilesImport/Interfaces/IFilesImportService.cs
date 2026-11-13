using Commerce.Demo.Business.Features.FilesImport.Models;

namespace Commerce.Demo.Business.Features.FilesImport.Interfaces;

public interface IFilesImportService
{
    FilesImportResult Import(Stream zipStream, string originalFileName, string userName);
}
