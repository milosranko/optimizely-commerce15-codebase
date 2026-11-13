# Optimizely / EPiServer Commerce Knowledge Base

Condensed Markdown references distilled from the PDF books in [`../pdfs/`](../pdfs/). Each doc is dense, API-name-rich, and written to be grep-friendly — search for a type, interface, or concept name and you should land on the relevant section.

## When to use which doc

| Document | Source PDF | Era / Version | Best for |
|---|---|---|---|
| [pro-optimizely-commerce-cloud.md](pro-optimizely-commerce-cloud.md) | `prooptimizelycommercecloud.pdf` (Apress, 2023) | **Optimizely CMS 12 / Commerce 13+ / .NET** — matches this project | **Primary reference.** Current APIs, DXP/Cloud architecture, `IOrderRepository`, `IPromotionEngine`, `IPriceService`, DI/startup, Find.Commerce, routing, custom payments/shipping. |
| [epi-commerce-recipes.md](epi-commerce-recipes.md) | `epicommercerecipes.pdf` (Quan Mai, Leanpub, 2014–2019) | Classic Episerver Commerce 11 (pre-Optimizely rebrand) | Practical **recipes/code snippets** for common tasks — still mostly applicable to current Optimizely (API names may differ). Catalog routing, price/inventory tweaks, search conventions, promotion processors, cache keys. |
| [pro-episerver-commerce.md](pro-episerver-commerce.md) | `proepiservercommerce.pdf` (Apress, c. 2018) | Episerver Commerce 9–12 (pre-Optimizely rebrand) | **Historical reference** for inherited legacy code: `OrderContext`, `CartHelper`, workflows, WebForms Commerce Manager, old promotion engine, `Mediachase.*` namespaces. Includes a **"Migration Notes for Current Optimizely 14 Codebases"** section mapping old APIs → current replacements. |
| [episerver-commerce-user-guide.md](episerver-commerce-user-guide.md) | `episerver-commerce-user-guide-update-14-1-print.pdf` (Episerver, 2023) | Commerce UI Update 14-1 | **Functional / business user guide** — catalogs, orders, customers, marketing, promotions, reports, admin settings. No code. Use to understand editor/merchandiser workflows, terminology (SKU, meta-class, market, purchase order, etc.), and the Commerce UI surface. |

## Quick topic index

- **Catalog content types** (ProductContent, VariationContent, NodeContent, BundleContent, PackageContent) → `pro-optimizely-commerce-cloud.md`
- **Cart & Order** (`IOrderRepository`, `ICart`, `IOrderGroup`, `ILineItem`, `IShipment`, `IPayment`) → `pro-optimizely-commerce-cloud.md`
- **Pricing** (`IPriceService`, `IPriceDetailService`, `CustomerPricing`, `CatalogKey`) → `pro-optimizely-commerce-cloud.md`
- **Promotions** (`IPromotionEngine`, `PromotionData`, `PromotionProcessorBase<T>`, `RewardDescription`, coupon codes) → `pro-optimizely-commerce-cloud.md` + `epi-commerce-recipes.md`
- **Markets & Currencies** (`IMarket`, `IMarketService`, `ICurrentMarket`) → `pro-optimizely-commerce-cloud.md`
- **Inventory & Warehouses** (`IInventoryService`, `InventoryRequest`, `OperationKey`) → `pro-optimizely-commerce-cloud.md`
- **Search / Find.Commerce** (`SearchClient`, conventions, faceted filters, best bets) → `pro-optimizely-commerce-cloud.md` + `epi-commerce-recipes.md`
- **Payment gateways** (`IPaymentPlugin`, `IPaymentGateway`) → `pro-optimizely-commerce-cloud.md` + `epi-commerce-recipes.md`
- **Shipping providers** (`IShippingPlugin`, `IShippingGateway`) → `pro-optimizely-commerce-cloud.md` + `epi-commerce-recipes.md`
- **Routing** (`HierarchicalCatalogPartialRouter`, `SeoUriRouter`, `UniqueSeoGenerator`) → `pro-optimizely-commerce-cloud.md` + `epi-commerce-recipes.md`
- **Caching** (`ISynchronizedObjectInstanceCache`, `MasterCacheKeys`) → `pro-optimizely-commerce-cloud.md`
- **Catalog events** (`IContentEvents`, `CatalogEventBroadcaster`, `CatalogKeyEventBroadcaster`) → `pro-optimizely-commerce-cloud.md`
- **Business Foundation** (`BusinessManager`, `CustomerContact`, `CustomerAddress`, `IPlugin`) → `pro-episerver-commerce.md`
- **DI & startup** (`AddCommerce`, `IConfigurableModule`, initialization modules) → `pro-optimizely-commerce-cloud.md`
- **Catalog editor workflows, CSV import, admin UI** → `episerver-commerce-user-guide.md`
- **Terminology / glossary (SKU, meta-class, market, campaign, etc.)** → `episerver-commerce-user-guide.md`

## Provenance

Raw extracted text lives in [`../pdfs/extracted/`](../pdfs/extracted/) — one `.txt` per PDF, produced by `pdftotext -layout`. Regenerate by re-running `pdftotext` over the PDFs if source books are updated.

## Notes

- Namespace prefixes `Mediachase.*` (legacy) still appear in current Optimizely Commerce assemblies for back-compat; new code should prefer `EPiServer.Commerce.*` abstractions.
- The Pro EPiServer Commerce doc predates Optimizely Commerce 14 — see its "Migration Notes" section before adopting patterns from it.
- All four KB docs are stand-alone — no circular dependencies — but cross-link where relevant.
