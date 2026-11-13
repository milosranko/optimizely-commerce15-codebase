# Kentico CMS to Optimizely DXP — Media & File Migration Plan

> Migrating a large number of images and files from Kentico CMS to Optimizely DXP.

## Overview

There's no built-in migration tool between Kentico and Optimizely, so a custom bulk import approach is needed using Optimizely's `IContentRepository` and `IBlobFactory` APIs.

---

## Recommended Approach: Bulk Import via Optimizely Blob API

### Step 1: Export from Kentico

Export media files from Kentico's media library or attachments:

- **Media Libraries** — files on disk or DB, accessible via Kentico API or direct SQL queries against `Media_File` / `Media_Library` tables
- **Page Attachments** — stored in `CMS_Attachment` table (binary in DB or on disk depending on config)

```sql
-- Example: Extract media file metadata from Kentico
SELECT MediaFileGUID, MediaFileName, MediaFilePath, MediaFileMimeType, MediaFileSize
FROM Media_File
WHERE MediaLibraryID = @LibraryId
```

### Step 2: Import into Optimizely

Use Optimizely's `IContentRepository` and `IBlobFactory` to programmatically create media content:

```csharp
public class BulkMediaImporter
{
    private readonly IContentRepository _contentRepository;
    private readonly IBlobFactory _blobFactory;
    private readonly ContentAssetHelper _contentAssetHelper;

    public void ImportFile(string filePath, string fileName, string mimeType,
                           ContentReference parentFolder)
    {
        var mediaData = _contentRepository
            .GetDefault<ImageData>(parentFolder); // or GenericMedia for non-images

        mediaData.Name = fileName;

        var blob = _blobFactory.CreateBlob(mediaData.BinaryDataContainer,
                                           Path.GetExtension(fileName));
        using (var stream = blob.OpenWrite())
        using (var fileStream = File.OpenRead(filePath))
        {
            fileStream.CopyTo(stream);
        }

        mediaData.BinaryData = blob;
        mediaData.RouteSegment = UrlSegment.GetUrlFriendlySegment(fileName);

        _contentRepository.Save(mediaData, SaveAction.Publish, AccessLevel.NoAccess);
    }
}
```

### Step 3: Folder Structure Mapping

Map Kentico's media library structure to Optimizely's content asset folders:

```csharp
private ContentReference GetOrCreateFolder(string path, ContentReference root)
{
    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    var current = root;

    foreach (var segment in segments)
    {
        var existing = _contentRepository
            .GetChildren<ContentFolder>(current)
            .FirstOrDefault(f => f.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            current = existing.ContentLink;
        }
        else
        {
            var folder = _contentRepository.GetDefault<ContentFolder>(current);
            folder.Name = segment;
            current = _contentRepository.Save(folder, SaveAction.Publish, AccessLevel.NoAccess);
        }
    }

    return current;
}
```

---

## Key Considerations

### Performance

- **Batch processing** — import in chunks of 100-500 files, not all at once
- **Disable events temporarily** during bulk import to avoid triggering indexing/cache invalidation per item:
  ```csharp
  using (var scope = new EventsDisabledScope()) { /* bulk import */ }
  ```
- **Run during off-hours** if importing into a live environment

### URL Preservation

- Build a **redirect map** (old Kentico URLs to new Optimizely URLs) to avoid broken links
- Use Geta NotFoundHandler (already in the project) to handle 404s with redirects:
  ```
  /getmedia/{guid}/{filename}  →  /globalassets/migrated/{path}/{filename}
  ```

### Azure Blob Storage

Since this project uses Azure Blob Storage, files will automatically go to blob storage. Ensure `AzureBlobProviderOptions` are configured with sufficient throughput.

### Metadata Mapping

| Kentico Field | Optimizely Property |
|---|---|
| `MediaFileTitle` | `Name` |
| `MediaFileDescription` | `Description` (if media type has it) |
| `MediaFilePath` | Folder structure + `RouteSegment` |
| `MediaFileMimeType` | Auto-detected from extension |

### Content References

After importing, **update content references** — any rich text or link fields that pointed to Kentico media GUIDs need remapping to Optimizely `ContentReference` values. Build a GUID-to-ContentReference lookup during import.

---

## Practical Workflow

1. **Export** — Dump Kentico media to a staging folder (files + CSV metadata)
2. **Transform** — Clean filenames, normalize paths, deduplicate
3. **Import** — Run bulk importer as a scheduled job or admin controller action
4. **Verify** — Compare counts, spot-check files, validate URLs
5. **Redirect** — Deploy redirect rules for old URLs

---

## Alternative: Direct Blob Copy (Azure-to-Azure)

If both Kentico and Optimizely use Azure Blob Storage, use **AzCopy** to transfer blobs directly, then register them in Optimizely's content database. This is faster for very large volumes (10k+ files) but requires more careful metadata handling.

---

## Status

- **Created:** 2026-03-10
- **Status:** Draft / For Review
- **Next Steps:** Decide on approach and scope before implementation
