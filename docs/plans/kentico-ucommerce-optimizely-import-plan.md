# Plan: Kentico uCommerce → Optimizely Commerce Import

## Context

**Problem:** Products in Optimizely Commerce are created by the iMIS sync (skeleton data: SKU, prices, claims, format). However, iMIS does NOT provide rich content — descriptions, images, SEO metadata, event schedules, addon rules, etc. This rich product data currently lives in Kentico uCommerce and needs to be imported into Optimizely to complete the product catalog.

**Scope:**
- **(a)** Import product details not set by iMIS (rich descriptions, SEO, event schedules, addon relationships, etc.)
- **(b)** Import files and images related to existing products

**Constraints:**
- **Data source:** JSON export files from uCommerce (not direct SQL or API)
- **Correlation key:** Same SKU/product code used in both iMIS and uCommerce
- **Frequency:** Recurring import (not one-time)
- **Media source:** Product images/files on filesystem (disk path)
- **Prerequisite:** Products must already exist in Optimizely (created by iMIS import) before uCommerce enrichment runs

---

## What iMIS Provides vs What uCommerce Provides

| Data | iMIS | uCommerce |
|------|------|-----------|
| Product structure (families, SKUs) | YES | - |
| Prices, claims, pricing groups | YES | - |
| Format, credits, dates, locations | YES | - |
| Member profiles + purchaseClaims | YES | - |
| Rich descriptions (XhtmlString) | - | **YES** |
| Product images & files | - | **YES** |
| SEO metadata (title, keywords, description) | - | **YES** |
| OpenGraph (social media preview) | - | **YES** |
| Event schedules (sessions, speakers) | - | **YES** |
| Event questions (registration forms) | - | **YES** |
| Addon relationships & conflicts | - | **YES** |
| Sizing guides, product teasers | - | **YES** |
| Category intro text | - | **YES** |

---

## Proposed uCommerce Export JSON Format

Since the export format is flexible, we define a JSON structure that maps directly to Optimizely content type properties. The export tool (outside this project) generates files matching this schema.

### Product Enrichment File (`products.json`)
```json
[
  {
    "sku": "CA3778R14/TUITION",
    "description": "<p>Rich HTML description...</p>",
    "longDescription": "<p>Extended HTML content...</p>",
    "productTeaser": "<p>Short marketing copy...</p>",
    "sizing": "<p>Sizing guide HTML...</p>",
    "brand": "State Bar of Wisconsin",
    "availableSizes": ["S", "M", "L"],
    "availableColors": ["Red", "Blue"],
    "seo": {
      "metaTitle": "Page title for search engines",
      "metaDescription": "Meta description text",
      "metaKeywords": "keyword1, keyword2",
      "noRobots": false
    },
    "openGraph": {
      "title": "Social share title",
      "description": "Social share description",
      "imagePath": "images/products/CA3778R14_og.jpg"
    },
    "images": [
      {
        "fileName": "CA3778R14_main.jpg",
        "altText": "Product main image",
        "copyright": "© 2025 SBW",
        "isPrimary": true
      }
    ],
    "files": [
      {
        "fileName": "CA3778R14_brochure.pdf",
        "description": "Product brochure"
      }
    ],
    "eventSchedule": [
      {
        "time": "2026-04-15T09:00:00",
        "heading": "Opening Session",
        "description": "<p>Session description...</p>",
        "speakerNames": ["Speaker 1", "Speaker 2"]
      }
    ],
    "questions": [
      {
        "question": "Dietary requirements?",
        "type": "MultipleChoice",
        "required": true,
        "purchaseClaims": ["DIETARY"]
      }
    ],
    "addonGroupName": "Optional Materials",
    "addonRelations": ["CA3778R14/ADDON1", "CA3778R14/ADDON2"],
    "addonConflicts": ["CA3778R14/ADDON3"],
    "dependentAddons": ["CA3778R14/ADDON1"],
    "excludeFromSitemap": false,
    "excludeFromSearch": false
  }
]
```

### Category Enrichment File (`categories.json`)
```json
[
  {
    "categoryId": "EventCA3778",
    "heading": "CLE Seminars",
    "introText": "<p>Category introduction...</p>",
    "seo": {
      "metaTitle": "CLE Seminars",
      "metaDescription": "Continuing Legal Education seminars",
      "metaKeywords": "CLE, legal education",
      "noRobots": false
    },
    "openGraph": {
      "title": "CLE Seminars",
      "description": "Browse our CLE catalog",
      "imagePath": "images/categories/cle_og.jpg"
    }
  }
]
```

**Note:** Null/missing fields are skipped during import — only provided fields overwrite existing values. This allows partial updates.

---

## Architecture: Where Code Lives

| Concern | Project | Rationale |
|---------|---------|-----------|
| Import file DTOs, file reader | `Commerce.Demo.Integrations` | External data parsing, no Optimizely deps |
| Import services, content mapping, media upload | `Commerce.Demo.Business` | Needs IContentRepository, IBlobFactory |
| Scheduled job, configuration | `Commerce.Demo.Business` + `Commerce.Demo.Web` | Job registration + DI wiring |

---

## Implementation Components (5 total)

### Component 1: Import File DTOs & Reader (Integrations project)

**New files in `Commerce.Demo.Integrations/uCommerce/`:**

```
uCommerce/
  Models/
    UCommerceOptions.cs                  - Config: ImportPath, MediaSourcePath
    DTOs/
      UCommerceProductEnrichmentDto.cs   - Matches products.json schema above
      UCommerceCategoryEnrichmentDto.cs  - Matches categories.json schema
      UCommerceImageDto.cs               - Image metadata (fileName, altText, copyright)
      UCommerceFileDto.cs                - File metadata (fileName, description)
      UCommerceSeoDto.cs                 - SEO fields
      UCommerceOpenGraphDto.cs           - OpenGraph fields
      UCommerceEventScheduleDto.cs       - Event schedule entry
      UCommerceQuestionDto.cs            - Registration question
  Services/
    IUCommerceFileReader.cs              - Interface: reads + deserializes export files
    UCommerceFileReader.cs               - Implementation: reads JSON from configured path
```

**`UCommerceOptions`** (follows `PayPalOptions` pattern):
```
ImportPath       - Directory containing products.json, categories.json
MediaSourcePath  - Root directory containing product images/files
```

**`UCommerceFileReader`:**
- Reads `products.json` and `categories.json` from `ImportPath`
- Deserializes to DTO arrays using `System.Text.Json`
- Validates required fields (SKU must be non-empty)
- Returns `IReadOnlyList<UCommerceProductEnrichmentDto>`

**Files to modify:**
- `src/Commerce.Demo.Integrations/Startup/StartupExtensions.cs` — add `AddUCommerceImport()`
- `src/Commerce.Demo.Integrations/Commerce.Demo.Integrations.csproj` — no new packages needed

### Component 2: Media Import Service (Business project)

**New files in `Commerce.Demo.Business/Features/UCommerceImport/`:**

```
UCommerceImport/
  Interfaces/
    IMediaImportService.cs
  Services/
    MediaImportService.cs
```

**`MediaImportService`** responsibilities:
- Reads image/file bytes from `MediaSourcePath + fileName`
- Creates `ImageFile` or `GenericMedia` in Optimizely's media library under a structured folder (e.g., `Products/{SKU}/`)
- Uses `IContentRepository` + `IBlobFactory` to create media content:
  1. Create folder if not exists: `ContentFolder` under `SiteBlockFolder` or `GlobalAssetsRoot`
  2. Create media content with `IContentRepository.GetDefault<ImageFile>(parentFolder)`
  3. Write binary data via `IBlobFactory.CreateBlob()` → assign to `BinaryData` property
  4. Set `AlternateText`, `Copyright` from DTO
  5. Save + publish
- Returns `ContentReference` for linking to products
- **Idempotent:** Checks if media with same name exists in the target folder before creating

### Component 3: Product & Category Enrichment Service (Business project)

**New files:**

```
UCommerceImport/
  Interfaces/
    IProductEnrichmentService.cs
    ICategoryEnrichmentService.cs
  Services/
    ProductEnrichmentService.cs
    CategoryEnrichmentService.cs
  Mappings/
    UCommerceToOptimizelyMapper.cs
```

**`ProductEnrichmentService`** flow:
1. Read enrichment DTOs via `IUCommerceFileReader`
2. For each product DTO:
   a. Find existing variant/product by `Code = dto.sku` using content loader
   b. If not found → log warning, skip (iMIS hasn't created it yet)
   c. If found → `CreateWritableClone()`
   d. Map non-null DTO fields to content properties:
      - `description` → `Description` (XhtmlString)
      - `longDescription` → `LongDescription` (XhtmlString)
      - `productTeaser` → `ProductTeaser` (XhtmlString)
      - `sizing` → `Sizing` (XhtmlString)
      - `brand` → `Brand`
      - etc.
   e. Import images via `IMediaImportService` → link to product
   f. Import files via `IMediaImportService`
   g. Set SEO block properties (MetaTitle, MetaDescription, etc.)
   h. Set OpenGraph block properties
   i. For EventProduct: map `eventSchedule[]` → `IList<EventScheduleProperty>`
   j. For EventAddonVariant: resolve `addonRelations`/`addonConflicts`/`dependentAddons` by SKU → `ContentReference` lists
   k. For variants with questions: map `questions[]` → `IList<EventQuestionProperty>`
   l. `Save(SaveAction.Publish)`
3. Track results in `SyncResult` (created/updated/skipped/failed)

**`CategoryEnrichmentService`** — same pattern but for categories:
- Find by category ID (the iMIS family id stored as `Code`)
- Enrich: Heading, IntroText (XhtmlString), SeoBlock, OpenGraphBlock

**`UCommerceToOptimizelyMapper`** — mapping logic:
- HTML strings → `new XhtmlString(html)`
- Image DTOs → call `IMediaImportService` → get `ContentReference`
- Schedule DTOs → `new EventScheduleProperty { Time, Heading, Description }`
- SKU references (addon relations) → resolve via content loader to `ContentReference`
- Null/missing fields → skip (don't overwrite existing values)

### Component 4: Scheduled Job (Business project)

**New file:**

```
ScheduledJobs/
  UCommerceImportScheduledJob.cs
```

**`UCommerceImportScheduledJob`:**
- `[ScheduledPlugIn]` with `DefaultEnabled = false`
- Runs after iMIS import (e.g., 4 AM if iMIS runs at 2-3 AM)
- Sequence: Categories first → Products second (so category enrichment is ready when products reference it)
- Supports `Stop()` between phases
- Returns summary: "Products enriched: 150 updated, 3 skipped, 2 failed. Categories: 12 updated."
- Logs per-product results at DEBUG, summary at INFO

### Component 5: Configuration & DI Wiring (Web project)

**Files to modify:**

- `src/Commerce.Demo.Web/appsettings.json`:
```json
{
  "UCommerceImport": {
    "ImportPath": "D:\\Imports\\uCommerce",
    "MediaSourcePath": "D:\\Imports\\uCommerce\\media"
  }
}
```

- `src/Commerce.Demo.Web/Startup.cs` — register in `#region Other Services`:
```csharp
services.AddUCommerceImport(o => _configuration.GetSection("UCommerceImport").Bind(o));
services.AddScoped<IMediaImportService, MediaImportService>();
services.AddScoped<IProductEnrichmentService, ProductEnrichmentService>();
services.AddScoped<ICategoryEnrichmentService, CategoryEnrichmentService>();
```

---

## Import Execution Order

The three imports (iMIS + uCommerce) run in this sequence:

```
2:00 AM  →  iMIS Import Job (creates product skeleton: families, SKUs, prices, members)
3:00 AM  →  iMIS Export Job (pushes orders/registrations back to iMIS)
4:00 AM  →  uCommerce Import Job (enriches products with descriptions, images, SEO, schedules)
```

This ensures products exist before uCommerce tries to enrich them.

---

## Key Design Decisions

1. **Null-skip pattern:** Only non-null/non-empty fields from the JSON overwrite Optimizely content. This prevents uCommerce from blanking out fields that were manually edited in Optimizely CMS.

2. **Media deduplication:** Before uploading an image, check if one with the same filename already exists in the product's media folder. If yes, update metadata only (alt text, copyright) without re-uploading the binary.

3. **SKU as correlation key:** Both iMIS `product.SKU` and uCommerce `sku` map to Optimizely `Code` property. This is the single join key across all three systems.

4. **XhtmlString from HTML:** uCommerce export provides raw HTML strings. These are wrapped in `new XhtmlString(html)` for Optimizely. No HTML sanitization needed since this is trusted internal data, but malformed HTML is logged as a warning.

5. **Addon relations by SKU:** `addonRelations`, `addonConflicts`, `dependentAddons` contain SKU strings. The mapper resolves these to `ContentReference` by searching for variants with matching `Code`. Unresolved SKUs are logged and skipped.

---

## Error Handling

- **Missing product:** If SKU not found in Optimizely → log WARNING, skip, continue
- **Missing media file:** If image file not found on disk → log WARNING, skip image, continue with other fields
- **Malformed JSON:** If export file can't be parsed → log ERROR, abort that file, continue with other files
- **Per-product isolation:** Each product enrichment is independent; one failure doesn't stop the batch

---

## Critical Files Reference

| File | Role |
|------|------|
| `src/Commerce.Demo.Integrations/Startup/StartupExtensions.cs` | Extend with `AddUCommerceImport()` |
| `src/Commerce.Demo.Integrations/PaymentProviders/PayPal/PayPalOptions.cs` | Pattern for `UCommerceOptions` |
| `src/Commerce.Demo.Web/Startup.cs` | DI wiring (line 178-188 `#region Other Services`) |
| `src/Commerce.Demo.Web/appsettings.json` | Add UCommerceImport config section |
| `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Products/EventProduct.cs` | Target for event schedule enrichment |
| `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Variants/EventAddonVariant.cs` | Target for addon relations |
| `src/Commerce.Demo.Optimizely.ContentTypes/Cms/Media/ImageFile.cs` | Media type for imported images |
| `src/Commerce.Demo.Optimizely.ContentTypes/Cms/Media/GenericMedia.cs` | Media type for imported files |

---

## Verification Plan

1. **Build:** `dotnet build src\Commerce.Demo.sln -c Release`
2. **Unit tests:** Mock `IUCommerceFileReader` → test enrichment services with sample DTOs
3. **Unit tests:** Mock `IContentRepository` + `IBlobFactory` → test media import service
4. **Integration test:** Place sample `products.json` + test images in import path → trigger job from CMS Admin → verify:
   - Products have rich descriptions populated
   - Images appear in media library under correct folders
   - Event schedules populated on EventProducts
   - Addon relations resolved on EventAddonVariants
   - SEO/OpenGraph metadata set
5. **Tests:** `dotnet test src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj`
