# Changelog

## [Unreleased]
> Breaking platform upgrade — suggested version 3.0.0 (`src/GlobalAssemblyInfo.cs` not changed yet).

### Added
- F3 documentation system (CLAUDE.md, /docs/*)
- Optimizely Graph (Content Graph) for CMS + catalog content, Opti ID (back office), Content Manager, Opal — each enabled once credentials are configured
- Hex colour editor (`HexColorPicker`) for `EventTuitionVariant.DisplayColor`
- `/global.json` (.NET 10 SDK, Microsoft.Testing.Platform test runner); Web project user-secrets
- `docs/plans/cms13-database-upgrade.md`

### Changed
- .NET 8 → .NET 10; Optimizely CMS 12.35 → 13.6, Commerce 14.48 → 15.5; NuGet feed → nuget.optimizely.com
- Add-ons: Geta NotFoundHandler 8, Sitemaps 5, Categories 2.2; Stott Security 7.1, RobotsHandler 7.0; Forms 6.0; LanguageManager 6.0; CmsContentScaffolding 2.0
- SiteDefinition APIs → CMS 13 Applications (`ISiteSettingsManager.SpecificSiteSettings` now takes `IRoutableApplication`)
- Tab names without spaces (`Aside_content`, `Site_Settings`, `Cookies_Consent`)
- `AvailableSizes`/`AvailableColors`/`PurchaseClaims` use `PropertyStringDictionaryMultiple` (existing values not migrated)
- `dotnet test` now requires `--project`

### Removed
- EPiServer Find (search service, catalog indexing, Commerce search provider), Labs ContentManager, DoubleJay ColorPicker/EnhancedPropertyList, Geta DdsAdmin

### Fixed
- `CookieConsentAuditPurgeJob` GUID collided with Commerce 15's webhooks job (blocked all scheduled-job sync)
- Forms 6.0.2 startup crash on fresh databases (`FormsAclJobRegistrationInitialization`)
- Site-settings cache returned the current site's settings for `SpecificSiteSettings` of another site
- Start page 500 for visitors when no user named "admin" existed (fixed by commenting out the demo notification)

---

## [2.2.0] - [TODO: Date]
### Added
- Changed projects structure, optimized project references
- Adjusted namespaces, changed folders structure
- Updated gitignore file
- Refactored initialization modules, added catalog caching and CI/CD pipeline
- Fixed null issue, early method exit implemented

---

## Version History

| Version | Date | Highlights |
|---------|------|------------|
| 2.2.0 | [TODO] | Project restructure, namespace adjustments, caching, CI/CD |
| [Earlier] | [TODO] | [TODO: Document earlier versions if available] |
