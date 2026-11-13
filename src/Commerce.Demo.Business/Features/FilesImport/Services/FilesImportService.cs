using Commerce.Demo.Business.Features.FilesImport.Enums;
using Commerce.Demo.Business.Features.FilesImport.Interfaces;
using Commerce.Demo.Business.Features.FilesImport.Models;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Media;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.Web;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace Commerce.Demo.Business.Features.FilesImport.Services;

public class FilesImportService : IFilesImportService
{
    private const string ImportsFolderName = "Imports";
    private const string VectorExtension = "svg";

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "jpe", "ico", "gif", "bmp", "png"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "flv", "mp4", "webm"
    };

    private readonly IContentRepository _contentRepository;
    private readonly IBlobFactory _blobFactory;
    private readonly ILogger<FilesImportService> _logger;
    private readonly IApplicationResolver _applicationResolver;

    public FilesImportService(
        IContentRepository contentRepository,
        IBlobFactory blobFactory,
        ILogger<FilesImportService> logger,
        IApplicationResolver applicationResolver)
    {
        _contentRepository = contentRepository;
        _blobFactory = blobFactory;
        _logger = logger;
        _applicationResolver = applicationResolver;
    }

    public FilesImportResult Import(Stream zipStream, string originalFileName, string userName)
    {
        var result = new FilesImportResult
        {
            OriginalFileName = originalFileName
        };

        try
        {
            var assetsRoot = ResolveAssetsRoot();
            //var importsFolder = GetOrCreateChildFolder(assetsRoot, ImportsFolderName);
            //var runFolderName = BuildRunFolderName(originalFileName);
            //var runFolder = CreateChildFolder(importsFolder, runFolderName);

            //result.TargetFolderPath = $"/{ImportsFolderName}/{runFolderName}";

            _logger.LogInformation(
                "Files import started. User={User}, File={File}, Target={Target}",
                userName, originalFileName, result.TargetFolderPath);

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

            var directoryRefs = new Dictionary<string, ContentReference>(StringComparer.OrdinalIgnoreCase)
            {
                [string.Empty] = assetsRoot
            };

            var fileEntries = new List<(ZipArchiveEntry Entry, string SafeRelativePath)>();

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                if (entry.FullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileName(entry.FullName), ".DS_Store", StringComparison.OrdinalIgnoreCase))
                {
                    result.Items.Add(new FilesImportItem
                    {
                        FileName = entry.Name,
                        RelativePath = entry.FullName,
                        Status = FilesImportStatus.Skipped,
                        Message = "Excluded macOS metadata.",
                        SizeBytes = entry.Length
                    });
                    continue;
                }

                if (!TryNormaliseRelativePath(entry.FullName, out var safeRelativePath))
                {
                    result.Items.Add(new FilesImportItem
                    {
                        FileName = entry.Name,
                        RelativePath = entry.FullName,
                        Status = FilesImportStatus.Skipped,
                        Message = "Entry rejected: unsafe path.",
                        SizeBytes = entry.Length
                    });
                    continue;
                }

                fileEntries.Add((entry, safeRelativePath));
            }

            foreach (var (_, safeRelativePath) in fileEntries)
            {
                var dir = GetDirectory(safeRelativePath);
                EnsureFolderHierarchy(dir, directoryRefs);
            }

            foreach (var (entry, safeRelativePath) in fileEntries)
            {
                var dir = GetDirectory(safeRelativePath);
                var targetFolder = directoryRefs[dir];
                var item = new FilesImportItem
                {
                    FileName = entry.Name,
                    RelativePath = safeRelativePath,
                    SizeBytes = entry.Length
                };
                result.Items.Add(item);

                try
                {
                    if (entry.Length == 0)
                    {
                        item.Status = FilesImportStatus.Skipped;
                        item.Message = "Empty file (0 bytes).";
                        continue;
                    }

                    if (NameAlreadyExists(targetFolder, entry.Name))
                    {
                        item.Status = FilesImportStatus.Skipped;
                        item.Message = "A file with the same name already exists in the target folder.";
                        continue;
                    }

                    var ext = (Path.GetExtension(entry.Name) ?? string.Empty).TrimStart('.');
                    if (string.IsNullOrEmpty(ext))
                    {
                        item.Status = FilesImportStatus.Skipped;
                        item.Message = "File has no extension.";
                        continue;
                    }

                    var (mediaType, instance) = CreateMediaInstance(ext, targetFolder);
                    instance.Name = entry.Name;

                    var blob = _blobFactory.CreateBlob(instance.BinaryDataContainer, "." + ext);
                    using (var blobStream = blob.OpenWrite())
                    using (var entryStream = entry.Open())
                    {
                        entryStream.CopyTo(blobStream);
                    }
                    instance.BinaryData = blob;

                    _contentRepository.Save(instance, SaveAction.Publish, AccessLevel.NoAccess);

                    item.Status = FilesImportStatus.Imported;
                    item.MediaType = mediaType;
                }
                catch (Exception ex)
                {
                    item.Status = FilesImportStatus.Failed;
                    item.Message = ex.Message;
                    _logger.LogError(ex,
                        "Failed to import zip entry {Entry} from {OriginalFileName}",
                        entry.FullName, originalFileName);
                }
            }

            result.Total = result.Items.Count;
            result.Imported = result.Items.Count(i => i.Status == FilesImportStatus.Imported);
            result.Skipped = result.Items.Count(i => i.Status == FilesImportStatus.Skipped);
            result.Failed = result.Items.Count(i => i.Status == FilesImportStatus.Failed);

            _logger.LogInformation(
                "Files import finished. User={User}, File={File}, Target={Target}, Total={Total}, Imported={Imported}, Skipped={Skipped}, Failed={Failed}",
                userName, originalFileName, result.TargetFolderPath,
                result.Total, result.Imported, result.Skipped, result.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Files import aborted. User={User}, File={File}",
                userName, originalFileName);
            throw;
        }

        return result;
    }

    private (string MediaType, MediaData Instance) CreateMediaInstance(string extension, ContentReference folder)
    {
        if (string.Equals(extension, VectorExtension, StringComparison.OrdinalIgnoreCase))
            return (nameof(VectorImageFile), _contentRepository.GetDefault<VectorImageFile>(folder));

        if (ImageExtensions.Contains(extension))
            return (nameof(ImageFile), _contentRepository.GetDefault<ImageFile>(folder));

        if (VideoExtensions.Contains(extension))
            return (nameof(VideoFile), _contentRepository.GetDefault<VideoFile>(folder));

        return (nameof(GenericMedia), _contentRepository.GetDefault<GenericMedia>(folder));
    }

    private ContentReference ResolveAssetsRoot()
    {
        // CMS 13: per-application assets root replaces SiteDefinition.SiteAssetsRoot.
        var siteAssets = (_applicationResolver.GetByContext() as IResourceableApplication)?.AssetsRoot;
        if (!ContentReference.IsNullOrEmpty(siteAssets))
            return siteAssets!;

        var globalAssets = SystemDefinition.Current?.GlobalAssetsRoot;
        if (!ContentReference.IsNullOrEmpty(globalAssets))
            return globalAssets!;

        return ContentReference.GlobalBlockFolder;
    }

    private ContentReference GetOrCreateChildFolder(ContentReference parent, string name)
    {
        var existing = _contentRepository.GetChildren<ContentFolder>(parent)
            .FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing.ContentLink;

        return CreateChildFolder(parent, name);
    }

    private ContentReference CreateChildFolder(ContentReference parent, string name)
    {
        var folder = _contentRepository.GetDefault<ContentFolder>(parent);
        folder.Name = name;
        return _contentRepository.Save(folder, SaveAction.Publish, AccessLevel.NoAccess);
    }

    private void EnsureFolderHierarchy(string normalisedDir, IDictionary<string, ContentReference> cache)
    {
        if (string.IsNullOrEmpty(normalisedDir))
            return;

        if (cache.ContainsKey(normalisedDir))
            return;

        var parentPath = string.Empty;
        var segments = normalisedDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            var current = string.Join("/", segments.Take(i + 1));
            if (!cache.ContainsKey(current))
            {
                var parentRef = cache[parentPath];
                cache[current] = GetOrCreateChildFolder(parentRef, segments[i]);
            }
            parentPath = current;
        }
    }

    private bool NameAlreadyExists(ContentReference folder, string fileName)
    {
        return _contentRepository.GetChildren<MediaData>(folder)
            .Any(m => string.Equals(m.Name, fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildRunFolderName(string originalFileName)
    {
        var stem = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(stem))
            stem = "import";
        return $"{stem}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    private static bool TryNormaliseRelativePath(string fullName, out string normalisedPath)
    {
        normalisedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        var normalised = fullName.Replace('\\', '/');
        if (normalised.StartsWith('/'))
            return false;

        foreach (var segment in normalised.Split('/'))
        {
            if (segment == "..")
                return false;
        }

        normalisedPath = normalised;
        return true;
    }

    private static string GetDirectory(string normalisedRelativePath)
    {
        var lastSlash = normalisedRelativePath.LastIndexOf('/');
        return lastSlash < 0 ? string.Empty : normalisedRelativePath[..lastSlash];
    }
}
