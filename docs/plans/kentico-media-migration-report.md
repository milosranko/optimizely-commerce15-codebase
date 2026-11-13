# Kentico CMS → Optimizely DXP Media Migration — Manager Report

## Executive Summary

Migrate all images and files from Kentico CMS to Optimizely DXP. There is **no built-in migration tool** between the two platforms, so a custom bulk import tool must be built using Optimizely's content and blob storage APIs.

| Aspect | Detail |
|--------|--------|
| **Source** | Kentico CMS (Media Libraries + Page Attachments) |
| **Target** | Optimizely DXP (Azure Blob Storage) |
| **Approach** | Custom bulk importer using `IContentRepository` + `IBlobFactory` |
| **Alternative** | Direct Azure-to-Azure blob copy via AzCopy (for 10k+ files) |

---

## Migration Workflow

| Step | Description | Estimate |
|------|-------------|----------|
| 1. **Export from Kentico** | Dump media files to staging folder + CSV metadata (SQL queries against `Media_File` / `CMS_Attachment` tables) | **3–5 hours** |
| 2. **Transform & Clean** | Normalize filenames, clean paths, deduplicate files | **2–3 hours** |
| 3. **Build Bulk Importer** | Custom tool: folder structure mapping, blob creation, metadata mapping, batch processing | **8–12 hours** |
| 4. **Build Redirect Map** | Map old Kentico URLs → new Optimizely URLs for Geta NotFoundHandler | **3–4 hours** |
| 5. **Update Content References** | Remap rich text / link fields from Kentico GUIDs to Optimizely ContentReferences | **4–6 hours** |
| 6. **Run Import** | Execute bulk import (batched, off-hours), monitor progress | **2–4 hours** |
| 7. **Verify & QA** | Compare file counts, spot-check files, validate URLs and redirects | **3–4 hours** |

---

## Effort Summary

| Category | Estimate |
|----------|----------|
| **Export & Data Prep** (steps 1–2) | **5–8 hours** (~1 dev day) |
| **Tool Development** (steps 3–5) | **15–22 hours** (~2–3 dev days) |
| **Execution & QA** (steps 6–7) | **5–8 hours** (~1 dev day) |
| **Redirect Deployment & Validation** | **2–3 hours** |
| | |
| **Total Estimated Effort** | **27–41 hours (~4–6 dev days)** |

**Note:** Estimates scale with volume. Add 1–2 days for catalogs exceeding 10,000 files.

---

## What Gets Migrated

| Kentico Source | Optimizely Target | Metadata Mapped |
|----------------|-------------------|-----------------|
| Media Library files (`Media_File`) | `ImageData` / `GenericMedia` under folder structure | Title → Name, Description, Path → RouteSegment |
| Page Attachments (`CMS_Attachment`) | Media assets in content tree | Filename, MIME type, binary data |
| Folder structure | Optimizely `ContentFolder` hierarchy | Recreated programmatically |

---

## Key Technical Decisions

1. **Batch processing** — import in chunks of 100–500 files to avoid memory and timeout issues
2. **Events disabled during import** — `EventsDisabledScope` prevents per-item indexing and cache invalidation overhead
3. **Geta NotFoundHandler for redirects** — already installed in the project; old Kentico URLs (`/getmedia/{guid}/...`) redirected to new Optimizely paths
4. **GUID-to-ContentReference lookup** — built during import to enable content reference remapping in rich text fields
5. **Azure Blob Storage** — files automatically go to Azure blob (already configured); no additional storage infrastructure needed

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Broken links after migration | High | Redirect map via Geta NotFoundHandler; verify coverage before go-live |
| Large file volume causing timeouts | Medium | Batch processing (100–500 per chunk), run during off-hours |
| Content reference mismatches | High | GUID-to-ContentReference lookup table built during import; post-migration scan |
| Duplicate files in Kentico | Low | Deduplication step before import |
| Missing metadata (descriptions, alt text) | Medium | Map available Kentico fields; flag gaps for manual review |
| Azure Blob throughput limits | Low | Verify `AzureBlobProviderOptions` configuration; increase if needed |

## Dependencies & Prerequisites

- Access to Kentico database (read-only) for media file export
- Kentico media files accessible on disk or via SQL (depending on storage config)
- Azure Blob Storage configured with sufficient throughput
- Geta NotFoundHandler already installed (confirmed in project)
- Scheduled maintenance window for running the import against production

## Deliverables

- Bulk media import tool (scheduled job or admin controller action)
- Redirect map (old Kentico URLs → new Optimizely URLs)
- Content reference remapping utility
- Post-migration verification report (file counts, broken link check)

## Alternative Approach: AzCopy (Azure-to-Azure)

If both Kentico and Optimizely use Azure Blob Storage, a **direct blob copy via AzCopy** is significantly faster for large volumes (10k+ files). However, it requires separate metadata registration in Optimizely's content database, adding complexity. Recommended only if file count exceeds 10,000.

---

*Estimates assume a single senior .NET developer familiar with both Kentico and Optimizely APIs. Volume-dependent — actual import execution time varies with file count and average file size. Plan for off-hours execution window.*
