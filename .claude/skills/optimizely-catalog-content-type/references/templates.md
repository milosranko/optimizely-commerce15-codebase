# Catalog content type templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces and base classes - the repo you are working in is the authority, these are the shape.

## A. Product type

```csharp
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using Wisbar.Optimizely.ContentTypes.Commerce.Products.Base;
using Wisbar.Optimizely.ContentTypes.Commerce.Products.Interfaces;
using Wisbar.Optimizely.ContentTypes.Commerce.Variants;
using Wisbar.Optimizely.Core.Attributes;
using static Wisbar.Optimizely.Core.Constants.Globals;

namespace Wisbar.Optimizely.ContentTypes.Commerce.Products;

[CatalogContentType(
    GUID = "{PASTE-A-FRESH-GUID-HERE}",
    GroupName = GroupNames.Products,
    DisplayName = "Course Product",
    Description = "A course product from the iMIS catalog.")]
[AvailableContentTypes(Include = [typeof(CourseVariant), typeof(AddOnTuitionVariant)])]
[SiteImageUrl("course-product.png")]
public class CourseProduct : ProductBase, IDetails
{
    #region Content tab

    [Display(
        Name = "Details",
        GroupName = SystemTabNames.Content,
        Order = 120)]
    [CultureSpecific]
    public virtual string? Details { get; set; }

    #endregion
}
```

Property `Order` values start at 100 on concrete products - `ProductBase` already occupies 5 through 90 on the Content tab.

### Subclassing instead of writing a new type

`EventProduct` adds no properties. It exists only to carry a different variant allow-list and thumbnail:

```csharp
[CatalogContentType(
    GUID = "{FRESH-GUID}",
    GroupName = GroupNames.Products,
    DisplayName = "Event Product",
    Description = "An event product from the iMIS catalog.")]
[AvailableContentTypes(Include = [typeof(EventVariant), typeof(AddOnTuitionVariant), typeof(AddOnOtherVariant)])]
[SiteImageUrl("event-product.png")]
public class EventProduct : MeetingProduct
{
}
```

It reuses the Meeting controller, builder, and view.

## B. Variant type

The variant is where price, inventory, and purchasability live.

```csharp
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;
using Wisbar.Optimizely.ContentTypes.Commerce.Variants.Base;
using Wisbar.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Wisbar.Optimizely.Core.Attributes;
using static Wisbar.Optimizely.Core.Constants.Globals;

namespace Wisbar.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{PASTE-A-FRESH-GUID-HERE}",
    GroupName = GroupNames.Products,
    DisplayName = "Course Variant",
    Description = "A course variant/SKU from the iMIS catalog.")]
[SiteImageUrl("course-variant.png")]
public class CourseVariant : VariantBase, ILanguage, ISubscription
{
    #region Content tab

    [Display(
        Name = "Language Name",
        GroupName = SystemTabNames.Content,
        Order = 90)]
    public virtual string? LanguageName { get; set; }

    [Display(
        Name = "Seat Limit",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    public virtual int? SeatLimit { get; set; }

    #endregion
}
```

`VariantBase` occupies Order 10-80 on the Content tab, so concrete variants start at 90. It also already hides SEO:

```csharp
[ScaffoldColumn(false)]
public override SeoInformation SeoInformation { get; set; }
```

Repeat that only on a variant deriving directly from `VariationContent`.

## C. Category / catalog node

```csharp
[CatalogContentType(
    GUID = "{FRESH-GUID}",
    GroupName = GroupNames.GenericCategory,
    DisplayName = "Generic Category",
    Description = "Generic Category.")]
[AvailableContentTypes(Include = [typeof(ProductBase), typeof(GenericCategory)])]
[SiteImageUrl("generic-category.png")]
public class GenericCategory : CategoryBase
{ }
```

`CategoryBase : NodeContent, ILayoutPageBase`. Including `ProductBase` in the allow-list admits every product type at once.

## D. Custom-editor property

Three attributes always travel together. Drop one and the property renders as a plain string list.

```csharp
[Display(
    Name = "Tabs",
    GroupName = SystemTabNames.Content,
    Order = 80)]
[BackingType(typeof(TabDropDownLabelXhtmlPropertyMetaData))]
[EditorDescriptor(EditorDescriptorType = typeof(LabelXhtmlCollectionEditorDescriptor<TabDropDownLabelXhtmlProperty>))]
[UIHint(Globals.SiteUIHints.TabsItemCollection)]
public virtual IList<TabDropDownLabelXhtmlProperty>? Tabs { get; set; }
```

Simple collection, no custom UI hint:

```csharp
[BackingType(typeof(IconLabelPropertyMetaData))]
[EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<IconLabelProperty>))]
public virtual IList<IconLabelProperty>? ProductHighlights { get; set; }
```

Categories via Geta:

```csharp
[Categories]
[AllowedTypes(typeof(DefaultCategory))]
[BackingType(typeof(PropertyContentReferenceList))]
public virtual IList<ContentReference>? PracticeAreas { get; set; }

[UIHint(CategoryUIHint.Category)]
[AllowedTypes(typeof(IconOrderedCategory))]
public virtual ContentReference? Format { get; set; }
```

**Changing a `[BackingType]` on a List/Json property that already exists in a database has no effect.** It needs a migration - see section H.

## E. Import wiring

`ProductsImportService`:

```csharp
private static readonly Dictionary<string, (Type ProductType, Type VariantType)> DefinitionMap = new(StringComparer.OrdinalIgnoreCase)
{
    [ProductFamily.BookFamily]   = (typeof(BookProduct),   typeof(BookVariant)),
    [ProductFamily.CourseFamily] = (typeof(CourseProduct), typeof(CourseVariant)),   // new entry
    // ...
};
```

Also required:

- `ProductFamily.CourseFamily` in `WisbarConstants.ProductFamily`, matching the external system's family name exactly (the dictionary is case-insensitive, nothing else is).
- A branch in the variant field-mapping switch:

```csharp
case CourseVariant v: MapCourseVariantFields(v, dto); break;
```

- The `MapCourseVariantFields` method itself. Where a mapping writes back to the parent product, follow the existing pattern of `Get<T>(...).CreateWritableClone()` before mutating.

Constraint documented in that file: **variant types must be unique across the map** (they reverse-resolve a definition 1:1), while a product type may back several families and is only reverse-resolvable when unique. Reusing an existing variant type for a new family breaks `DefinitionByVariantType`.

## F. Find indexing

`FindIndexingConventionsModule` configures `ForInstancesOf<ProductBase>()` once; new products inherit it. Add a type-specific block only to exclude:

```csharp
client.Conventions.ForInstancesOf<CourseProduct>()
    .ExcludeField(x => x.AccordionAnchor);
```

Computed fields are extension methods (`ProductSearchIndexExtensions`) included by method group:

```csharp
.IncludeField(x => x.ProductTypeName())
.IncludeField(x => x.LowestListPrice())
```

Nested fields must be registered in the nested conventions too:

```csharp
var nested = client.Conventions.NestedConventions.ForInstancesOf<ProductBase>();
nested.Add(x => x.IndexedVariants());
```

Product queries go through `IProductSearchService`.

## G. Product page - the four pieces

### View model

```csharp
namespace Wisbar.Web.Models.Commerce;

public record CourseProductViewModel : ProductViewModel<CourseProduct>
{
    // Chains to the record's generated copy constructor, so base properties
    // the builder already set are carried over.
    public CourseProductViewModel(ProductViewModel<CourseProduct> baseModel) : base(baseModel)
    { }

    public string? SeatsRemainingLabel { get; init; }
}
```

`ProductViewModel<T> : PageViewModel<T>, IProductViewModel<T>`, which is what gets `Layout`, `IsMember`, and `LoginUrl` stamped by `PageContextActionFilter`.

### Builder interface and registration

```csharp
public interface ICourseProductViewModelBuilder : IProductViewModelBuilder
{
    Task<CourseProductViewModel> BuildAsync(CourseProduct currentPage, string memberId, CancellationToken ct = default);
}
```

```csharp
// Startup.ConfigureServices
.AddScoped<ICourseProductViewModelBuilder, CourseProductViewModelBuilder>()
```

The implementation builds the base model through `ProductViewModelBuilder` first, then wraps it in the derived record.

### Controller

```csharp
public class CourseProductController : CommerceControllerBase<CourseProduct>
{
    private readonly ICourseProductViewModelBuilder _builder;

    public CourseProductController(
        ICourseProductViewModelBuilder builder,
        IContentLoader contentLoader,
        IPublishedStateAssessor publishedStateAssessor,
        ICurrentIdentity identity)
        : base(builder, contentLoader, publishedStateAssessor, identity)
    {
        _builder = builder;
    }

    [CommerceTracking(TrackingType.Product)]
    public async Task<IActionResult> Index(CourseProduct currentPage)
    {
        var model = await _builder.BuildAsync(currentPage, Identity.MemberId, HttpContext.RequestAborted);
        return View(model);
    }
}
```

`CommerceControllerBase` already 404s a product whose variants are all unpublished or expired, and skips that check in edit mode. Do not duplicate it.

### View

`Views/CourseProduct/Index.cshtml`, typed to `CourseProductViewModel`. Identity questions go through the globally injected `Identity` (`Identity.IsMember`), never `User.Identity.IsAuthenticated`.

## H. Migration templates

### Forcing a backing type change onto an existing database

```csharp
[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration44ChangeCourseTabsBackingType : IMigrationStep
{
    // Higher than every existing step. Confirm the current maximum first:
    //   grep -h "public int Order =>" src/Wisbar.Business/Migrations/*.cs
    public int Order => 1440;
    public string Name => "Change Course Tabs backing type";
    public string Description => "Forces the Tabs PropertyDefinition on CourseProduct onto the new metadata type.";

    private static readonly Type[] ProductTypes = [typeof(CourseProduct)];

    public bool Execute(IProgressMessenger progressMessenger)
    {
        // Resolve each content type, find the PropertyDefinition by name, repoint its
        // PropertyDefinitionType, and save. See Migration36 for the full implementation.
    }
}
```

### Registering an order-system metafield

```csharp
var context = OrderContext.MetaDataContext;
var lineItemMetaClass = OrderContext.Current.LineItemMetaClass
    ?? throw new InvalidOperationException("LineItem meta-class is unavailable; order system not configured.");

OrderMetaFieldHelper.EnsureField(context, lineItemMetaClass, LineItemProperties.MyKey,
    "My Key", MetaDataType.ShortString, 100);
```

Use `MetaDataType.LongString` for anything that can exceed a short string. Key names come from `Globals.LineItemProperties` and are prefixed, because the LineItem meta-class is shared and generic names collide with native fields.

`Order` values are global across the migration set - take the next free one, and never renumber an existing step.

## Base class reference

| Class | CLR base | Adds |
|---|---|---|
| `ProductBase` | `ProductContent` | Marketing copy, practice areas, highlights, tabs, accordions, sponsors, SEO/Open Graph, snippets, sitemap and search flags, `AllowOrdering` |
| `VariantBase` | `VariationContent` | Category, format, tags, deliverable/pinnacle/upass flags, restricted purchase, external purchase link, product message; hides SEO |
| `AddOnVariantBase` | `VariationContent` | Dates, location, registration questions, add-on group and description; hides SEO |
| `CategoryBase` | `NodeContent` | `ILayoutPageBase` |
| `BundleBase` / `PackageBase` | `BundleContent` / `PackageContent` | Grouping types |

## Marker interfaces

Products: `IDetails`, `ITabs`, `IAccordions`, `ISponsors`, `IScheduleAndFaculty`.

Variants: `ICredits`, `ICreditExpirationDate`, `ISubscription`, `ILanguage`, `IRegistration`, `IEventDetails`, `IAutoSupplementation`, `IEndorsedMessage`, `IOriginalPresentationDate`, `IAddOnVariant`.

Check these before adding a property that already has a contract - services and search extensions dispatch on them.
