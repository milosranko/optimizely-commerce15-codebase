using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Commerce.Demo.Optimizely.Core.CookieConsentAudit.Models;

[EPiServerDataStore(
    AutomaticallyRemapStore = true,
    AutomaticallyCreateStore = true,
    StoreName = "CookieConsentAuditStore")]
[EPiServerDataContract]
public class CookieConsentAuditEntry : IDynamicData
{
    [EPiServerDataMember]
    public Identity Id { get; set; } = Identity.NewIdentity();

    [EPiServerDataMember]
    public DateTime Timestamp { get; set; }

    [EPiServerDataMember]
    public string Action { get; set; } = string.Empty;

    [EPiServerDataMember]
    public string CategoriesSelected { get; set; } = string.Empty;

    [EPiServerDataMember]
    public string? VisitorId { get; set; }

    [EPiServerDataMember]
    public bool IsAuthenticated { get; set; }

    [EPiServerDataMember]
    public string? PolicyVersion { get; set; }

    [EPiServerDataMember]
    public string? IpAddress { get; set; }

    [EPiServerDataMember]
    public string? UserAgent { get; set; }

    [EPiServerDataMember]
    public string? CookieName { get; set; }
}
