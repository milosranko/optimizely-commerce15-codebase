using Commerce.Demo.Business.Features.FilesImport.Interfaces;
using Commerce.Demo.Web.Models.Admin;
using EPiServer.Authorization;
using EPiServer.Cms.Shell.UI.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Commerce.Demo.Web.Controllers;

[Authorize(Roles = $"{Roles.Administrators},{Roles.CmsAdmins},{Roles.WebAdmins}")]
[Route("cms/admin/[controller]")]
public class FilesImportController : Controller
{
    private const long UploadAttributeCeilingBytes = 1024L * 1024 * 1024;

    private readonly IFilesImportService _filesImportService;
    private readonly MediaFileOptions _mediaFileOptions;
    private readonly ILogger<FilesImportController> _logger;

    public FilesImportController(
        IFilesImportService filesImportService,
        IOptions<MediaFileOptions> mediaFileOptions,
        ILogger<FilesImportController> logger)
    {
        _filesImportService = filesImportService;
        _mediaFileOptions = mediaFileOptions.Value;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("[action]")]
    public IActionResult Index()
    {
        return View(new FilesImportViewModel());
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadAttributeCeilingBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadAttributeCeilingBytes)]
    public IActionResult Import(IFormFile? file)
    {
        var viewModel = new FilesImportViewModel();

        if (file == null || file.Length == 0)
        {
            viewModel.ErrorMessage = "No file uploaded.";
            return View(nameof(Index), viewModel);
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            viewModel.ErrorMessage = "Only .zip archives are accepted.";
            return View(nameof(Index), viewModel);
        }

        if (file.Length > _mediaFileOptions.FileSizeLimit)
        {
            viewModel.ErrorMessage = $"Uploaded file exceeds the configured limit of {_mediaFileOptions.FileSizeLimit / (1024 * 1024)} MB.";
            return View(nameof(Index), viewModel);
        }

        try
        {
            using var stream = file.OpenReadStream();
            var userName = User.Identity?.Name ?? "unknown";
            viewModel.Result = _filesImportService.Import(stream, file.FileName, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Files import failed at controller level. File={File}, User={User}",
                file.FileName, User.Identity?.Name);
            viewModel.ErrorMessage = "Import failed. Please check the server logs for details.";
        }

        return View(nameof(Index), viewModel);
    }
}
