using EPiServer.Authorization;
using EPiServer.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Shared.Categories;

[ContentType(
    DisplayName = "Protected Catalog Category",
    AvailableInEditMode = false,
    GUID = "{98B719BE-7B0C-4B66-A663-737710AA519A}")]
//[Access(Roles = Roles.ContentAdmins)]
[Access(Roles = Roles.CmsAdmins)]
public class ProtectedCatalogCategory : CatalogCategory
{ }
