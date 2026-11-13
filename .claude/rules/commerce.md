---
globs: src/Wisbar.Business/Features/Cart/**,src/Wisbar.Web/Api/**,**/*Cart*,**/*Order*,**/*Catalog*,**/*Commerce*
---

# Commerce rules

## Cart & Order APIs

- Use `IOrderRepository` for all cart and order persistence.
- Core abstractions: `ICart`, `IOrderGroup`, `ILineItem`, `IShipment`, `IPayment`.
- Never use legacy `CartHelper` or `OrderContext` — those are pre-Optimizely patterns.

## Catalog content types

- Products are `ProductContent`; variations are `VariationContent`.
- Catalog routing uses `HierarchicalCatalogPartialRouter`.
- Cache keys follow `MasterCacheKeys` conventions; Commerce catalog cache is 5-minute expiration in this repo.

## Pricing & inventory

- Use `IPriceService` / `IPriceDetailService` for pricing.
- Use `IInventoryService` / `InventoryRequest` for stock lookups.
- Always go through these abstractions — never query pricing or inventory directly from the database.

## Promotions

- Use `IPromotionEngine` — do not invoke promotion logic outside the engine.

## DXP secrets

- Connection strings (`EPiServerDB`, `EcfSqlConnection`) and API keys must live in the DXP App Settings portal.
- Never commit secrets to `appsettings.*.json`.
- Environments promote Integration → Preproduction → Production with manual approval gates.
- Read secrets at runtime through `IConfiguration` only.

## API reference

For Commerce and Optimizely API details, check:
- `.claude/knowledge-base/pro-optimizely-commerce-cloud.md` — primary reference (current APIs)
- `.claude/knowledge-base/epi-commerce-recipes.md` — recipes and code snippets
