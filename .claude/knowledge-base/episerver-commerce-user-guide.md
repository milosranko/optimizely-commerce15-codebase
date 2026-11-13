# EPiServer Commerce User Guide — Knowledge Base

> **Source document:** *EPiServer Commerce User Guide*, Update 14-1 (EPiServer AB, 1996–2014).
> **Scope:** Official end-user / business-user documentation for EPiServer Commerce (now Optimizely Commerce). Covers functionality available in EPiServer 7.5 updates 1–37 and the sample site shipped with that release.
> **Intended audience:** Content editors, marketers, merchandisers, customer service representatives (CSRs), store / system administrators, and — as a cross-reference — developers configuring the platform.
> **Purpose of this file:** Functional/UX reference derived verbatim from the official guide, reorganised and summarised for quick lookup. It is not a developer or API reference.

---

## Table of Contents

1. [Introduction and Platform Concepts](#1-introduction-and-platform-concepts)
2. [Getting Started](#2-getting-started)
3. [User Interface](#3-user-interface)
4. [Roles and Tasks](#4-roles-and-tasks)
5. [Shopping Process Flow](#5-shopping-process-flow)
6. [Catalogs](#6-catalogs)
7. [Orders](#7-orders)
8. [Shopping Carts](#8-shopping-carts)
9. [Shipping and Receiving](#9-shipping-and-receiving)
10. [Returns and Exchanges](#10-returns-and-exchanges)
11. [Customers (Organizations, Contacts, Roles)](#11-customers-organizations-contacts-roles)
12. [Markets](#12-markets)
13. [Marketing (Campaigns, Segments, Promotions, Expressions, Policies)](#13-marketing-campaigns-segments-promotions-expressions-policies)
14. [Reporting](#14-reporting)
15. [Administration — System Settings](#15-administration--system-settings)
16. [Administration — Catalog System](#16-administration--catalog-system)
17. [Administration — Order System (Payments, Shipping, Taxes, Meta Data)](#17-administration--order-system-payments-shipping-taxes-meta-data)
18. [Scheduled Jobs](#18-scheduled-jobs)
19. [Managing Content (CMS + Commerce)](#19-managing-content-cms--commerce)
20. [Personalization](#20-personalization)
21. [Commerce Gadgets](#21-commerce-gadgets)
22. [Access Rights](#22-access-rights)
23. [Glossary](#23-glossary)

---

## 1. Introduction and Platform Concepts

EPiServer Commerce extends the EPiServer (now Optimizely) CMS with a full e-commerce back office. The platform is made of:

- **EPiServer CMS** — content management (pages, blocks, media). Core to every installation.
- **EPiServer Commerce** — adds catalog, orders, customers, marketing, markets, reporting and administration subsystems. Requires a separate license.
- **Add-ons** — extensions (e.g. EPiServer Find, Connect for SharePoint, Live Monitor, CMO). Some free, some licensed.

### Feature summary

| Area | What it delivers |
|---|---|
| Catalogs & product management | Multi-catalog, categories/products/SKUs/packages/bundles, related products, cross-sell/up-sell. |
| Order management | Purchase orders, payment plans, split shipments/payments, shipping, returns/exchanges, one-page checkout, anonymous purchase. |
| Merchandising | Drag-and-drop landing pages, shared blocks, personalization by behaviour/demographics. |
| Customer management | Organizations, contacts, customer groups, wish lists, cross-site order history. |
| Pricing & promotions | Volume/tiered pricing, customer-group pricing, campaigns, coupons, pre- or in-checkout promotions. |
| Multi-site/language | One platform, many localized stores; per-market content, currency, payments, shipping, tax. |
| Multi-channel | Same content for web, mobile, social, email; QR / Facebook promo codes. |
| Security | Access rights per user/group/function; predefined and custom roles; virtual roles via visitor groups. |
| Content management | Editorial workflow, drag-and-drop content blocks, dynamic sites. |
| Administration | Shipping gateways, payment gateways, default language/currency/units, tax configuration, search configuration. |

### Integration with external systems

EPiServer Commerce is designed to exchange data with Financial, CRM, ERP, PIM, DAM, Payment gateways, Shipping gateways, Inventory/warehouse systems and Customer service platforms.

---

## 2. Getting Started

- **Logging in.** Editors/admins log in via a site-specific URL. After login the access menu appears in the upper-right. Selecting **CMS Edit** opens the editing view; **Dashboard** opens the personal dashboard.
- **Accessing features.** What you can do is driven entirely by access rights. Menu items you don’t see are either not licensed or not permitted.
- **Navigation.** The **global menu** (top) switches between products (CMS, Commerce, Find, Add-ons, Admin).
- **Next steps.** Guide suggests reviewing: User interface, Roles and tasks, Managing content, Commerce, Find, Add-ons, Administration interface.

---

## 3. User Interface

Two editing modes: **On-Page Editing view** and **All Properties view**.

### Common UI behaviours

- **Context-sensitive actions** — only relevant buttons are shown.
- **Primary actions** — highlighted (e.g. the Publish button).
- **Action feedback & notifications** — success is confirmed; status icons surface errors.
- **Drag-and-drop** — drag pages, blocks, media, or catalog entries onto content areas.
- **Keyboard commands** — Ctrl/Cmd+C/X/V for copy/cut/paste; Esc to close; Delete / Cmd+Delete to remove.
- **Search** — used to find content instead of navigating folder trees.
- **Customizable UI** — panes can be resized and pinned; gadgets can be added to the dashboard and panes.
- **Time zones** — Editorial interface uses the user's local time zone; the Administrative interface uses server time (contact hosting provider to confirm).

### On-Page Editing view — named parts

Global menu, Toolbar, Content area, Actions menu (context-sensitive combo button), Flyout pane (edit a single property), All Properties button, Pane settings, Pane pin, Notification bar, Autosave, Content status, Context menu, Gadget settings.

### All Properties view — named parts

Page/Block settings (status + key properties), Tab bar, Tab content, On-Page Editing button.

### Navigation pane tabs

- **Pages** — CMS page tree (search by page ID or keyword).
- **Catalogs** — Commerce catalog tree.
- **Sites** — language variants.
- **Tasks** — filterable by Draft, Rejected, Ready to Publish, My Tasks, Marked as Being Edited, Recently Changed.

### Assets pane tabs

- **Blocks** — reusable content.
- **Media** — images, video, documents.
- **Catalogs** — catalog entries for drag-and-drop into pages or blocks.

---

## 4. Roles and Tasks

Typical roles described in the guide and their responsibilities:

| Role | Responsibilities |
|---|---|
| **Visitor** | Browses, optionally buys anonymously or after registration. |
| **Community member** | Adds forum posts, reviews, ratings, comments (when enabled). |
| **Content editor** | Creates/publishes content; tunes search results. |
| **Marketer** | Runs campaigns, banner ads, monitors KPIs, optimizes search for promoted content. |
| **Merchandiser** | Stock, category structure, pricing, cross-sell, supplier and distribution oversight. |
| **Website owner** | Overall responsibility for content and performance; approvals; may install add-ons. |
| **Administrator** | Configures system (search, languages, access rights, visitor groups). |
| **Developer** | Implements templates, content types, Find index, add-ons. |

---

## 5. Shopping Process Flow

Site types: **B2C** (consumer-oriented) and **B2B** (inter-business, procurement). A single site can cover both.

### Typical B2C workflow

1. **Cart created** — adding to basket creates a persistent cart, visible in *Orders > Carts*. Inventory and pricing are checked at add time and on return.
2. **Start checkout** — customer proceeds to checkout.
3. **Add address** — shipping + billing; manual or pulled from registered profile; split-shipping supported.
4. **Add payment** — method chosen (credit card, PayPal, etc.); totals, shipping, and taxes calculated; split payments possible.
5. **Order created** — PO number generated; cart becomes a *Purchase Order* with status **In Progress**.
6. **Order processing** — inventory/warehouse check; order may split into multiple shipments.
7. **Shipment released** — appears in *Shipping/Receiving > Released for Shipping*.
8. **Add to picklist** — generates pick list and packing slip.
9. **Order completed/shipped** — shipment validation & tracking number assigned; order status *Completed*.
10. **Returns/Exchanges** — only on completed orders; refund or exchange; status may become *Awaiting Exchange*.

---

## 6. Catalogs

A **catalog** contains **products** and **variants (SKUs)** organized into **categories** (nodes). Catalogs are independent; multiple per site are supported. Products can exist in several catalogs. Each catalog can have availability dates so future releases can be staged.

### Catalog entry types

- **Category** — grouping of purchasable products (e.g. "Art Books", "Cocktail Dresses").
- **Product** — merchandise; normally a grouping for variants.
- **Variation / SKU** — a concrete, purchasable item with size/color/etc.
- **Package** — multiple products/variants sold as a single shrink-wrapped line item with a single SKU and price.
- **Bundle** — a collection of variants purchased together but listed and priced separately in the basket.
- **Dynamic Package** — a package created on the fly from selectable components.

### Meta classes and fields

Meta classes + meta fields let you enrich entries with custom data (e.g. a "Wine" meta class with *color*, *taste*, *vintage*, *maturity*). Defined in *Administration > Catalog System*.

### Working with catalogs — common tasks

- Create catalogs via import/export or manually.
- Categorize and organize entries.
- Browse and review catalogs.
- Edit catalogs before going live.
- Manage pricing for entries.

### Importing and exporting

Two mechanisms:

1. **Standard import/export** — ZIP files exported from Commerce Manager. Good for moving whole catalogs between sites.
2. **CSV catalog import** — bulk-load or edit entries via Excel-format CSVs; works for entire catalogs or parts.

**CSV import steps:**

1. Create CSV files (categories, entries, entry relations, associations, variation inventory/pricing, sale price).
2. Create any necessary meta fields in Administration.
3. Upload CSV files via *Catalog Management > Import/Export > CSV Import Catalog*.
4. Create a **mapping file** per CSV (type = Category w/ Meta Data, Entry w/ Meta Data, Entry Relation, Entry Association, Variation w/ Inventory, or Sale Price).
5. Import CSV + mapping into a chosen catalog.
6. Create entry relations (product ↔ variants, package ↔ constituents).

**Required CSV attributes:** Code, Name, Entry Type (Variation/Product/Package/Bundle/Dynamic Package), Parent/Child Entry Code (for relation files), Action (Insert / Update / Delete — I/U/D), Category Code (comma-separated), SEO URL (must be unique).

### Browsing and searching catalogs

- *Catalogs* view displays catalogs and drill-down nodes with name, code, price, availability, optional thumbnails.
- Filterable per market; search accepts keywords; quoted phrases do exact-match search.
- Catalog entries are also available from CMS via the **Catalogs gadget** in the assets pane (used for drag-and-drop onto CMS pages).

### Creating / editing a catalog

Catalogs are created at the **Catalog Root** only. Properties:

- **Name** / **Name in URL** / **Default currency** / **Default language** / **Base weight** / **Available languages**.

Catalog properties are **global** — they have the same value for all languages and can only be edited in the default/master language. Changes go through the normal publishing flow (draft → publish). Deleting a catalog may break site functionality — export first.

### Creating catalog entries

Entries can be added from context menus in the catalog tree, from the main listing, or from the **Add** button at the top. The options offered depend on context: from a category you can create a category, product, or variant; from a product, only products or variants. Packages and bundles live under categories.

Creating a **category**: choose "New Category" → name → pick meta class → edit details → publish.

Creating a **variant (SKU)**: navigate to parent → pick New Variation → name → meta class → edit → publish.

**Duplicating** an entry produces a copy with a new code, URL name and SEO URL; for variants the pricing info is also copied. Multiple entries can be duplicated via drag-and-drop then choosing *Duplicate* in the Paste Item dialog.

### Editing catalog entries

Editing views (in addition to All Properties / On-Page): list view, pricing view, relations view.

Tabs on a product/variant:

- **Content** — description, template-specific fields (rich-text editor included).
- **Pricing** — overview of prices by market/customer group; set **Tax Category**; launch *Edit prices*.
- **Inventory** — Max/Min Quantity, Tracked flag, Weight/Length/Height/Width, per-location Quantity & Reserved, Allow backorder/preorder, Backorder/preorder availability, Inventory status (Enabled/Disabled/Ignored).
- **Assets** — images, PDFs, videos; ordering and grouping (e.g. "Banner") supported.
- **Variants** — only for products; edit or remove variants.
- **Relations** — category relations (where the entry appears) and Related Entries (cross-/up-sell).
- **Settings** — default built-in date-related properties.

**Deletion rules.** Deleting a category removes only links for entries that also live in another category; orphaned entries are deleted. Deleting a product with variants leaves the variants in the same category. Variants deleted lose all inbound links. Deletion cannot be undone.

**Translations.** Catalog entries can be multilingual; see [Section 19](#19-managing-content-cms--commerce).

### Editing packages and bundles

- Packages have **Pricing** (unique price for the package) and **Inventory** (because a package is a physical assembly); bundles do not.
- Both have **Assets**, **Relations**, and **Entries** (Package Entries or Bundle Entries). **Manage Entry Groups** lets you tag packages/bundles into groups.

### Product categorization

- **Move** — drag an entry into a new tree location; existing links are preserved.
- **Link** — drag and select *Link here* so the entry appears in multiple categories without duplication.
- **Detach** — remove a link (disabled if it would orphan the entry).
- The **top category** is the entry's "home" location; if URLs are based on *Name in URL*, moving the home changes the URL.

### Managing pricing

Pricing overview: *Catalog > Pricing* view. Filter by Customer Group and Market. Catalog entries without any price do not appear here — add prices first. Changes are **published immediately** (not part of content publishing flow).

Price record fields:

- **Sale Type** — All Customers / Specific Customer / Customer Price Group.
- **Sale Code** — used when Sale Type = Customer Price Group.
- **Market** / **Currency** / **Price** / **Date from–to** / **Min. quantity** (used for tiered pricing).

Customer groups must exist in Commerce Manager before they can be referenced in a price record.

### Catalog batch update

*Catalog Management > Catalog Batch Update* allows bulk edits. Filters: Entry Type (Product/Variation/Bundle/Package) + Meta Class + Field, plus Language, Catalog and Keywords. Editing, for example, "Variation: Display Price" changes the field for every returned entry at once. Save with **Save All**.

---

## 7. Orders

Order Management provides shopping carts, order capture, fulfilment, payment, and returns/exchanges. Most orders arrive from the front-end; manual back-office creation is supported.

### Browsing orders

**Order Search** offers filters: *Class Type* (Purchase Orders / Shopping Carts / Payment Plans), *Status* (OnHold, PartiallyShipped, InProgress, Completed, Cancelled, AwaitingExchange), *Date Range*, *Return #*, *ID* (format `PO####`), *Customer*. Results columns: ID, Customer, Total, Status, Modified. Sortable by ID/Customer/Total/Status.

Left navigation also lets you view Purchase Orders by date (Today/This Week/This Month/All) or by Status.

### Creating orders

Two ways:

- **From the order list.** *Order Management > Purchase Orders > New Order* → choose site + contact.
- **From a contact.** *Customer Management > Contacts > [contact] > New Order*.

When created the order gets status **In Progress**.

**Order header fields:** Customer, Currency, Coupon.

**Adding items:** *New Item* opens a catalog browser. Advanced filters: *Select catalog*, *Select language*, *Select entry type*. On *Configure Selected Entry* you set Price (required), Quantity (required), Discount (value or percentage) — Total is computed.

**Addresses:** enter Billing, then Shipping (or tick *Same as Billing*); optionally *Add to customer's address book*. Choose shipping method.

**Summary:** click *Recalculate* to refresh totals; *OK* to save.

### Processing orders

Adding a **payment** and **releasing to shipping**:

1. Add payment — Amount defaults to order total; can be split.
2. Select Payment Method.
3. Save — payment is processed. Details tab shows shipment status *Awaiting Inventory*.
4. *Release Shipment* → status becomes *Released*; the order appears in *Shipping/Receiving > Released for Shipping*.
5. *Cancel shipment* cancels the entire order and cannot be undone.

### Splitting shipments

Possible when the order has ≥ 2 items. In the Details tab click the *Move* icon on an item and either move it to an existing shipment or create a new one (new shipments can have their own shipping address + shipping method). Save to persist.

### Payment plans (recurring orders)

Used for subscriptions (magazines, groceries, etc.). Handled by the **Subscription payment plan** scheduled job.

Create via *Order Management > Payment Plans (recurring) > New Payment Plan*.

Key fields:

| Field | Meaning |
|---|---|
| Plan Cycle | No Cycle / Daily / Weekly / Monthly / Custom1 / Custom2 (developer-extensible). |
| Cycle Length | Integer multiplier for the cycle (e.g. Monthly + 2 = every two months). |
| Max Cycles | Maximum number of cycles; 0 = run indefinitely. |
| Completed Cycles | Starts at 0. |
| Start Date / End Date | Plan active window. |
| Is Plan Active | True/False. |
| Last Transaction Date | Logged automatically. |

The first purchase order is created either manually (*Create First Purchase Order*) or by the scheduled job. Subsequent POs are numbered `POXXXXX` with the payment-plan parent ID embedded (e.g. plan ID 35 → `PO35XXX`). Adding a payment to the *plan* does not charge the customer — charging happens on each generated PO.

Out-of-the-box support for **Authorize.Net** as recurring gateway. In the payment-method parameters the class name must be set and *Supports Recurring* = Yes.

### Editing, canceling, deleting orders

- **Edit** — change fields under Summary, Details, Payments, Returns, Notes; *Save*.
- **Cancel** — *Summary > Cancel Order*; status becomes *Canceled*; order stays in the list; cancellation is logged in Notes.
- **Delete** — completely removes the order; confirm to commit.

---

## 8. Shopping Carts

A cart is created when a visitor adds an item. Carts can be created manually in *Order Management > Carts*.

**Cart view functions:** New Order, New Payment Plan, **New Shopping Cart** (pick a customer — one cart per customer at a time), Delete Selected, ID (click to open).

Creating a cart manually: pick customer → add items with *New Line Item*. Line items, list price, and totals are visible; anonymous carts expose limited customer info.

**Converting a cart to a purchase order:** open cart → *More Actions > Convert to Purchase Order* → page refreshes into the Order view for further processing.

---

## 9. Shipping and Receiving

Shipping and Receiving splits into **shipments** (outgoing) and **returns** (incoming).

### Viewing shipments

Columns: Order Created, Last Modified, Number # (PO number + shipment tracking number), Customer, Shipping Method, Address, Warehouse. Default warehouse can be renamed in Administration.

### Creating picklists

*Order Management > Shipping/Receiving > Shipments > Released for Shipping*. Pick warehouse → select orders → **Add Shipment to Picklist** → create a new pick list (name defaults to date/time, can't be renamed) or add to an existing one.

Once added the order has status *In Progress* again; it disappears from *Released for Shipping* and appears in *Picklist*.

### Completing shipments

Two methods:

- **Method 1** — tick shipment → *Complete* → validate shipment number (defaults to the last segment of the PO number, e.g. `PO10728-9` → enter `9`) → enter tracking number provided by the shipping provider → *OK*.
- **Method 2** — click the *Complete shipment* icon on the shipment row → tracking number → *OK*.

Once complete the order becomes *Completed* and is eligible for returns.

### Printing picklists and packing slips

*Order Management > Pick Lists*. Filter by warehouse. Inside a picklist, select POs and click **Print Picklist** or **Print Packing Slip** (exportable to Excel/PDF).

### Removing items & deleting picklists

Remove items via *Remove from Pick List*. Picklists must be empty (0 packing shipments remaining) before they can be deleted.

---

## 10. Returns and Exchanges

Available only on **Completed** orders. A return records the physical item coming back. Compensation can be a refund or an **exchange** (separate Exchange Order, prefix `EO`).

### Initiating a return

*Purchase Order > Details > Create Return* → *New Item* → pick line item, Return Quantity, Return Reason (Faulty, Unwanted Gift, Incorrect Item, or admin-added reasons) → Save. Order status: *Completed / Awaiting Return Completion*.

### Completing a return (refund)

*Returns tab > Acknowledge Receipt Items* → *Complete Return* → opens **Create Refund** form. Amount defaults to the item price (not shipping); can be changed. Refund is deposited to the original payment method by default, or to a new credit card/other method via **New Credit**. *OK* to process.

### Initiating an exchange

After *Create Return* & *Acknowledge Receipt Items*, click **Create Exchange** → select exchange item via *Configure Selected Entry* → enter addresses + shipping method → *OK*. An **EO** with status *Awaiting Exchange* is created.

### Processing an exchange

Open the EO → *Payments* tab → add payment (Transaction Type becomes *Authorization*) → *Save*. On the original PO's *Returns* tab click **Complete Return** to release the EO's shipment; the CSR then ships it like a normal PO.

---

## 11. Customers (Organizations, Contacts, Roles)

Customer Management is the platform's built-in **CRM**, split into:

- **Organizations** — parent-child structures grouping users (e.g. *Administration > EU HQ > EU Management* or *Customers > Gold/Silver/Bronze*).
- **Contacts** — individuals; Contacts-with-Accounts (shoppers) vs Contacts-with-User-Permissions (internal staff).
- **Roles** — permission bundles.

Customer groups and organization customer groups drive price eligibility and promotion eligibility. If an organization has a customer group, it overrides any customer group assigned directly to the contact.

### Organizations

**Creation fields:** Name (required), Description, Type (Organization / Organization Unit / custom), Organization Customer Group, Business Category, Parent.

**Viewing/Searching.** *Customer Management > Organization*. Columns: Name, Description, Organization Customer Group, Type, Business Category. Search by name. Selecting an org shows contacts, suborgs, addresses, credit cards.

**Views.** Custom views define visible columns and filter rules (General / Fields / Filters tabs). "Show this view for all users" makes it shared. Example filter: Business Category equals *Business & Professional Services* AND Name equals *Company X*.

**Hierarchies.** Set a Parent on the child org to create a tree; permissions can be inherited from the parent.

**Printing.** *Printer Version* renders a simple table for printing.

**Deleting.** Two modes: *Delete Organizations Only* (keeps children/contacts as orphans, reassignable later) or *Delete Organizations, Child Organizations and Contacts*.

**Dictionaries for organizations.** Types and Business Categories are controlled through **dictionary values**. Edit-Dictionary icon next to each drop-down. Sort-order numbers can be duplicated — they are informational.

### Contacts

**Required fields:** Full Name, First Name, Last Name, Email. Optional but recommended: Parent Organization, Customer Group, Preferred Currency, Preferred Language, Preferred Shipping/Billing Address, Registration Source.

*Registration Source* auto-fills when a visitor registers from the public site (useful to know which site the contact came from).

**Custom views** — same pattern as organizations: General / Fields / Filters tabs.

**Accounts.** A contact can have an Account attached. Required: User Name, Password (≥4 chars). Optional: Description, Email, Approved (Yes/No). Accounts show: Last Activity, Last Lockout, Last Log in, Last Password Changed.

You **cannot change the User Name** — you must remove and recreate the account.

**Deleting a contact or account.** Select contact → *Delete* (or *Remove Account*) → confirm.

**Dictionary values for customer groups.** Default values: Customer, Partner, Distributor. Customer groups drive pricing and promotion eligibility.

### Roles

Out-of-the-box roles (selected):

- **Order Managers** — View/Create/Edit/Delete order elements; can split shipments, process payments, send notifications.
- **Order Supervisors** — full control over Order Management (includes Order Manager rights).
- **Shipping Manager** — view, pack, complete shipments.
- **Receiving Manager** — view shipments and receive returns.
- **Management Users** — access to Commerce Manager.
- **Administrators** — broad administrative rights.
- **WebEditors / WebAdmins** — CMS edit/admin access.
- **Everyone / Registered** — auto-assigned on customer self-registration.
- **Expert** — in the sample site, for external reviewers.

Every Commerce Manager login role must have *Core > Management > Login* enabled; without that the user cannot log in no matter what else is permitted. Roles are additive (cumulative).

Permissions form a tree — selecting a higher node grants all branches beneath it; selecting a leaf only grants that leaf.

**Creating a role:** *Customer Management > Roles > New* → name → check desired permissions → *OK*.

**Assigning roles:** open a contact with an account → *Security > Roles > Assign Role* → pick Security Role; optionally attach to an Organization with Inheritance (*Inherit from Parent* or *Overwrite parent*).

---

## 12. Markets

A market represents a country, group of countries, region, or customer cluster. Markets drive:

- Which products are shown.
- Which payment methods are available.
- Which languages and currencies apply.
- Which promotions run.
- Report filtering.
- Personalization rules.

Each market has: Market ID, Market Name, Market Description, Is Active, Countries, Languages, Currencies (with a default fallback language and currency).

A site can offer many languages; the chosen language does not imply a market. Markets are created in *Market Management > Markets > Create New > New Market*. Default Market is the fallback.

---

## 13. Marketing (Campaigns, Segments, Promotions, Expressions, Policies)

The Marketing system is built from five components:

- **Campaigns** — logical groupings of promotions and/or customer segments with an availability window.
- **Customer segments** — subsets of users, static or expression-driven.
- **Promotions** — discounts/coupons attached to entries, orders, or shipping.
- **Expressions** — XML conditions that extend the system.
- **Policies** — global rules (e.g. "do not allow negative orders").

### Campaigns

Fields: Campaign Name, Comments, Available From, Expires On, Target Segments, Target Markets, Active (Yes/No), Archive (Yes/No — UI placeholder not implemented).

Edit/Delete via the Campaign List. Campaigns without promotions produce no discounts.

### Customer segments

Can be **static** (manually-assigned contacts) or **dynamic** (condition-driven, so new users automatically join if they match). Expressions allow conditions like "customers in Los Angeles" or "emails from companyx.com OR winearts.com". Supported operators include Equals / NotEquals.

Create via *Marketing > Customer Segments > New Segment*. To add conditions: *New Condition* → add clauses → *OK*.

### Promotions

Must be attached to a Campaign. Can be:

- **Visible prior to checkout** (on product/category pages).
- **Applied during checkout** (via coupon or rule match).

Built-in categories:

- **Entry-level** — Catalog Entry: Build Your Own Discount; Buy X Get Y at a Discount; Buy X Get $ Off Discount.
- **Order-level** — Order: Buy X Get N quantity of Y at a Discount; Order: Sub Total Volume Discount; Build Your Own Discount.
- **Shipping-level** — Shipping: Build Your Own Discount.

**Common properties** when creating a promotion:

- Promotion Type, Promotion Name, Display Name per language.
- Campaign, Target Markets, Promotion Group.
- **Combination with other promotions** — Combine / Exclusive within selected groups / Exclusive within all groups.
- **Priority** — higher priority evaluated first.
- **Coupon Code** — optional; redeemable on public site.
- **Status** — Active / Inactive / Suspended / Deleted.
- **Purchase Condition & Reward** — the actual rule.
- **Redemption Limits** — Max. Total Redemption, Max. Redemption Per Order, Max. Redemption Per Customer.
- **Schedule** — Available / Expires date-time.

**Example – 40% off wine decanter** (Build Your Own Discount): set Purchase Condition on item ID/name, Reward = 40%.

**Example – Buy X Get Y at discount:** select entries X and Y, set Quantity X and Quantity Y, set Amount + Percentage-Based or Value-Based.

**Example – Buy X Get $ Off:** Minimum Quantity, Amount (Percentage or Value), select qualifying Variations.

**Example – Sub Total Volume Discount:** set Minimum Order Amount + Amount (Percentage/Value).

**Custom discounts with OR:** add the OR operator **first** because AND is the default, e.g. `And(Or(DisplayName=Bordeaux, DisplayName=Sauternes))`. Otherwise the rule engine collapses to AND and no item matches.

**Customer-group-targeted promotions** are built by creating a Customer Segment with a `CustomerGroup = …` condition, then associating that segment to the Campaign. All promotions in that campaign are then limited to matching contacts (matched via `EffectiveCustomerGroup`).

**Activating/inactivating promotions.** *More Actions > Flip status (active/inactive)* keeps the promotion for reuse.

### Expressions

XML statements that extend the system. Used by promotions, segments, and policies. *Marketing > Expressions > New Expression* → Name, Description, Expression XML, Category (Promotion / Segment / Policy). **Warning:** requires XML knowledge; advanced users only.

### Policies

Rules evaluated whenever promotions run. A policy is **Local** if associated with a promotion group (only those promotions are affected) or **Global** if unassociated (applies to all promotions).

Creation fields: Policy Name, Status, Is Local (Yes/No), Policy Expression, Groups.

> The guide notes that **Policy Status** and **Policy Is Local** are not fully implemented in this release; all policies behave as global.

---

## 14. Reporting

Built-in reports (in *Reporting*):

- **Sales Report** — website sales performance over time.
- **Shipping Report** — shipping method, number of orders, total shipping cost.
- **Best Sellers Report** — products by quantity and total revenue.
- **Low Stock Report** — products below reorder minimum.

Filters: Market, Currencies, Start Date / End Date (+ time), Group by Day / Month / Year. Apply Filter + reload icon to regenerate.

**Exporting.** Excel or PDF via *Select a format > Export*. Printing via the print icon. Custom reports require development work.

---

## 15. Administration — System Settings

*Administration* is where settings that cross subsystems are maintained. It includes:

### Dictionaries

Dictionaries store "data about data" — used to enrich countries, currencies, return reasons, catalog content, customer org types, and customer contact groups.

**Countries.** *Administration > System Settings > Dictionaries > Countries*. New Country fields: Country Name, Code (ISO Short or Long — stay consistent), Sort Order, Visible. Regions (states/provinces/prefectures) added under the Region tab — type a Friendly Name (e.g. "CA"), click Add. Reorder via Edit icon.

**Currencies.** *Dictionaries > Currencies*. New Currency fields: Currency Name, Code (ISO 4217). The Rates tab holds FX rates: To Currency, End of Date Rate, Average Rate, Currency Rate Date.

**Return Reasons.** Built-in: Faulty, Incorrect Item, Unwanted Gift. Add your own via *Dictionaries > Return Reasons > New Return Reason* — Return Reason, Sort Order, Visible. Edit/Delete per row or in bulk.

### Logs

Two logs:

- **System Log** — system activities; filter by Operation, Object Type, Created Before; export or delete.
- **Application Log** — by default tracks catalog changes; filter by Source Type, Operation, Object Type, Created Before.

### Common Settings

*Administration > System Settings > Common Settings* sets defaults for **Language** (from language settings), **Currency** (from currency dictionary), **Length units** (cm / inches), **Weight units** (kg / lbs). Length/weight feeds shipping cost calculations.

### Business Foundation

Lets you extend Customer and Asset forms without code. A no-code data model for adding objects, fields, relationships.

> Current implementation supports Customer and Asset subsystems only. The Asset subsystem in Commerce Manager has been replaced by the EPiServer Asset system.

**Creating business objects.** *System Settings > Business Foundation > Create New > New Business Object*. Fields: System Name, Friendly Name, Plural Name, Supports Extensions (extend existing class with extra properties). Field Info: System Name, Friendly Name, Maximum Length.

**Customising objects.** Out-of-the-box objects (e.g. *Address*, *Organization*) can be edited but not deleted. Locked fields (like Field Name, Field Type) cannot be edited.

**Adding fields.** Select object → *New Field* → Field Name (system name), Friendly Name (auto from Field Name), Description, Allow Nulls, Add Field To (forms), Field Type (String/Date/etc.), Format, Max Length, Unique value.

**Customizing forms.** Forms dictate how fields are displayed (Edit Forms, Short Info Forms, View Forms). Edit layout (View Type: Two Columns 1:1/1:2/2:1 or Three Columns 1:1:1; Cell Padding). Add sections (Title, show border/title, View Type, Cell Padding). Add fields to sections with Item label options (Hide Label / System Label / Custom Label), Label Width, Tab Index, View Type. Rearrange with arrow buttons. Save or *Recreate Form* to start fresh.

**Relating business objects.**

- **1 : Many** — one object to many related objects (e.g. Organization → many Addresses).
- **Many : 1** — many related objects linked to one (e.g. many Contacts → one Organization).
- **Many : Many** — bridge tables (e.g. SKU ↔ Warehouse).

Forms: Primary Object / Related Object, Field Name, Friendly Name, Allow Nulls, Add Field To (which forms), Display region, Display Text, Display Order. Reciprocal relationships are auto-generated on the other side.

**Publishing business objects.** *Publish* exposes the object as a left-menu item in Commerce Manager. Access Permissions are auto-generated and copyable between objects. A browser refresh is needed after publish.

**Deleting business objects.** All customized published objects can be deleted from *Business Foundation*. To fully remove from the left menu, also delete via *System Settings > Left Menu*.

### Updating the search index

*System Settings > Search Index*:

- **Build** — quick incremental re-indexing of only changed items. Use for minor edits.
- **Rebuild** — full indexing. Use for brand-new catalogs or major structural changes (moved categories, renamed products, etc.).

### Customizing the left menu

Add custom menu items (link to a page) directly or after publishing a business object. Fields: Display Region, Display Text, Item Link (URL), Display Order (default 10000 — lower = higher up), Access Permissions (auto-filled when created via Business Foundation, otherwise copy from existing menu), Item Icon. Edit / delete via the notepad and X icons.

---

## 16. Administration — Catalog System

Under *Catalog System*:

### Warehouses

A warehouse is a physical inventory location with an address. Multiple warehouses let you track stock per location and enable in-store pickup / check stock in-store / delivery to store.

Creation fields (Overview tab): Name, Code, Available (Yes/No), Is Primary, Is Fulfillment Center (supports outgoing shipments), Is Pickup Location (in-store pickup), Is Delivery Location (reserved for future in-store pickups). Address tab for address information.

Delete via *More Actions > Delete Selected*.

### Tax categories

A tax category lets you associate entries with specific taxes (e.g. "luxury items", or regional VAT). Works together with **Tax Configuration** under Order System. Create via *New*; delete individually or via *More Actions > Delete Selected*.

### Catalog meta classes and meta fields

Extend product/variant data with custom attributes.

**Create a meta class.** *Meta Classes > Create New > New Meta Class*: Name (no spaces), Friendly Name, Description, Object Type (Catalog Node or Catalog Entry).

**Create a meta field.** Name, Friendly Name, Description, Type (datetime, decimal, float, money, Integer, Boolean, Date, Email, URL, Short String, Long String, Long Html String, String Dictionary, File, Image File, Dictionary). **Search Properties** per field: Allow search, Enable Sorting Search Results, Include Values in Search Results, Tokenize (word-breaking for long text), Include in the Default Search.

**Apply fields to classes.** *Meta Classes > Element = Catalog Entry > Type = {class}* → tick fields, optionally set Sort order → OK.

---

## 17. Administration — Order System (Payments, Shipping, Taxes, Meta Data)

### Payments

Three related concepts:

- **Payment Types** — meta classes defining the properties (e.g. Credit Card has number, expiry, card type). Built-in types: Payment by phone, Exchange payment, Credit card payment, plus a generic "OtherPayment" class.
- **Payment Gateways** — classes that process payments (e.g. PayPal). One payment type per gateway.
- **Payment Methods** — the customer-facing option, each tied to a gateway.

**Creating a payment method.** *Administration > Order System > Payments > {language} > New*. Fields: ID (auto), Name, Description, System Keyword (unique, not editable after creation), Language, Class Name / Payment Class, Sort Order, IsActive, IsDefault, Supports Recurring (for subscriptions), Restricted Shipping Methods (not fully implemented OOTB). Markets tab associates the method with markets.

A payment method needs **all** of: assigned to the market, assigned to a language valid for the market, and assigned to a language valid for the site — otherwise it won't appear.

**nSoftware-provided gateways.** Create a method with System Keyword = `ICharge` and Class Name = `Mediachase.Commerce.Plugins.Payment.ICharge.IChargeGateway`. On the *Parameters* tab the **Configure IBiz E-Payment Integrator Component** lets you pick from dozens of gateways (e.g. Authorize.Net) and enter credentials.

### Shipping

Four related concepts:

- **Shipping Methods** — business-user-facing rules that calculate the fee (Ground Shipping, Fixed Shipping, …). Mapped to a shipping provider.
- **Shipping Providers** — classes that interact with real carriers (USPS, UPS, FedEx) or specific scenarios (e.g. overnight).
- **Shipping Gateways** — the actual class behind a provider. Two out-of-the-box: **Generic Gateway** (flat rate) and **Weight/Jurisdiction Gateway** (base + rules by weight and location).
- **Shipping Jurisdictions / Jurisdiction Groups** — geographic rules used by the Weight/Jurisdiction gateway (required configuration for it).

**Customising shipping methods.** *Administration > Order System > Shipping > Shipping Methods > {language}*. Fields: ID (auto), Name (no spaces/special chars), Friendly Name (customer-facing), Description, Provider, Language, Base Price, IsActive, IsDefault, Sort Order. Settings tab excludes specific Countries/Regions/Payments. Parameters tab (only for Weight/Jurisdiction): Jurisdiction Group, Weight, Price, Start Date, End Date.

**Customising shipping providers.** *Shipping Providers* — set Name, Description, System Keyword (no spaces), Class. Parameters and Packages tabs are not fully implemented and are starting points for devs.

### Tax configuration

Two setup methods:

**1. Manual setup.**

1. Create countries + regions (*System Settings > Dictionaries > Countries*) — ISO Short codes required.
2. Create **Tax Categories** (*Catalog System*) — e.g. "General Sales", "Food", "Luxury Items".
3. Create **Tax Jurisdictions** (*Order System > Tax Configuration*) — zip codes, country codes; must match codes from step 1.
4. Create **Tax Jurisdiction Groups** — collections of jurisdictions.
5. Create **Taxes** — rate + category + jurisdiction group.
6. Associate catalog items with the correct Tax Category (*Catalog Management > Variation/SKU > Pricing/Inventory*).

**2. CSV import.** *Order System > Taxes > Import Taxes > Add New File > Browse > Upload File > Save > Start Import*. Re-importing **edits or adds** but never deletes — deletes must be done manually.

CSV columns: Jurisdiction, Jurisdiction Group, Tax Rate, Tax Category, and optionally **Tax Type** (Sales Tax / Shipping Tax).

Supports single-tax-per-item, multi-tax-per-item (state + federal), and typed taxes.

### Order meta classes and fields

Like catalog meta classes but against order objects. Object Types: Order Group, Order Form, Shipment, LineItem, Order Group Address, Order Form Payment. Field types and search properties mirror the catalog meta fields. Default order meta fields are built into the system and not available for change — only extensions.

---

## 18. Scheduled Jobs

Commerce-specific scheduled jobs run in the CMS admin view. Configurable via the EPiServer admin UI.

| Job | Purpose |
|---|---|
| **Remove Expired Carts** | Purges abandoned carts after their retention period. |
| **Rotate Encryption Keys** | Rotates keys for encrypted meta fields used to protect sensitive customer data. |
| **Full Search Index** | Performs a full rebuild of the search index. |
| **Subscription Payment Plans** | Generates the recurring POs for payment plans. |
| **Incremental Search Index** | Indexes only recent changes. |
| **Serialize Catalog Meta Data** | Serializes catalog metadata to speed catalog search. |
| **Shipment Releasing** | Finds releasable shipments in active orders and flips them to *Released* once the configured time has elapsed. |

---

## 19. Managing Content (CMS + Commerce)

### What is content?

In Commerce, content covers catalog entries (categories, products, variants, packages, bundles) and CMS content (pages, blocks, media). Catalog data is often imported from external systems and enriched by merchandisers; campaign content is authored by marketers.

### Content types

Content types define the properties of an item. Normally created in code, sometimes tweaked in admin. Catalog content is based on catalog-entry content types.

### Editing catalog content

Editing is done from either On-Page Editing or All Properties view. Save-and-publish flow applies to content and assets. Changes to **pricing** and **existing relations** publish immediately and bypass the publishing flow.

### Linking to content

The Link tool in Commerce can link to catalog entries. Useful both in rich-text editors and link properties.

### Catalogs gadget

Available in the assets pane in both CMS and Commerce. Drag catalog items into rich-text editor or content areas to promote products; drag into the Relations tab to create relation links. Rendering templates must be defined by developers for catalog items to display correctly inside CMS content.

### Multi-language management

Languages are set per catalog. Enabling a language for a catalog surfaces translation-ready fields on every entry.

**Enable a new language:**

1. Activate in CMS admin + edit views and ensure the site start page supports the new language.
2. In the Catalog All-Properties view, tick it under *Available languages* and publish.

**Translate an entry:**

1. Select the entry in the catalog tree → Edit.
2. Switch language in the header → translate the fields → publish.

> Once a language is enabled on a catalog, *all* entries are immediately "available" in that language — untranslated descriptions will appear empty on the front-end.

Some properties are **globally shared** and can only be edited in the master language (depends on implementation).

**UI language.** CMS side: *My Settings > Display Options*. Commerce Manager side: *Change Language* menu in the top bar.

---

## 20. Personalization

Commerce ships predefined **visitor group criteria** on top of CMS. Apply them via *CMS > Visitor Groups > Add > Commerce Criteria*. Requires administrative access rights.

| Criterion | What it matches |
|---|---|
| **Customer Properties** | Date of Birth (from/to), Customer group (Customer/Partner/Distributor), Registration source, Country, Region code, Address postal code, State. Only one property per criterion; multiple criteria per visitor group allowed. |
| **Market** | A specific market from Market Management. |
| **Order Frequency** | Customers who placed "X" orders in the last "Y" days. |
| **Product in Cart or Wish List** | Specific product code, product in a category, product with a property + value (e.g. brand = Sony). One property per criterion, multiple criteria per group. |
| **Recent Orders** | Customers who placed an order in the last "X" days. |
| **Total Spent** | Customers who spent ≥ "X" amount in currency Y in the last "Z" days. |

---

## 21. Commerce Gadgets

Dashboard gadgets that come with the Commerce sample site:

- **Commerce Orders gadget** — sales graph and PO list for a chosen time window. Default refresh 30 seconds.
- **Commerce Overview gadget** — sales data grouped by products, categories, orders, customers, promotions.
- **Community Approval gadget** — moderate product review comments (Approve / Mark Pending / Delete; filter by status).
- **Comment Abuse Report gadget** — review abuse reports left by visitors; Remove Comment or Ignore.
- **Commerce Settings for CMO gadget** — integrate with EPiServer CMO (Campaign Monitor and Optimization). Monitor product views or orders by product/category; uses an Application Key + CMO Web Service URL defined in CMO. Requires both CMO and Commerce installed.

All gadgets are added via *Dashboard > Add Gadgets* and configured via the gadget's Edit menu.

---

## 22. Access Rights

Access-right management spans both CMS and Commerce Manager and requires administrative access in both.

### Contacts ↔ Accounts

User administration is based on **contacts**. A contact can be a registered shopper or an internal employee. Only contacts with an **Account** appear in CMS admin for access-rights assignment. Anonymous shopping is supported.

### Managing users

Users must be created in **Commerce Manager first** to be usable in both systems; CMS-only users are not available in Commerce Manager.

### Commerce-specific user-interface groups

In addition to **WebEditors** and **WebAdmins**, Commerce defines:

- **CommerceAdmins** — Commerce Manager + Catalogs UI + Settings (excluding CMS admin view).
- **CommerceSettingsAdmins** — Settings only (no Commerce Manager back-office).
- **CatalogManagers** — Catalogs UI only (no Commerce Manager back-office).

> These groups are **not created automatically** — add them manually in *CMS > Admin > Set Access Rights > Administer Groups* before assigning users.

### Access matrix (summary)

| User needs | Group / role |
|---|---|
| Log in at all | A Commerce Manager contact with an account. |
| CMS/Commerce UI (non-admin) | WebEditors. |
| Catalog UI only | CatalogManagers. |
| Settings only | CommerceSettingsAdmins. |
| Commerce Manager (excl. CMS admin) | CommerceAdmins. |
| Full access everywhere | WebAdmins. |
| Editing in page tree | A "structure" group with tree permissions (e.g. *Site_Editors*, *Product_Editors*, *News_Editors*). |

### Typical setup flow

1. In Commerce Manager → *Customers* → create a contact with an account.
2. In CMS admin → *Search User/Group* → find the user → assign access rights.
3. Verify by logging in as that user.

### Sample-site predefined roles

Admins, Order Managers, Order Supervisors, Shipping Manager, Receiving Manager, Everyone, Registered, Management Users, Administrators, WebAdmins, WebEditors, Expert. At least one role is required to access Commerce Manager. Permissions can be tuned per role; custom roles allowed.

> Note: "Everyone" and "Registered" do **not** grant access to the administrative interface.

---

## 23. Glossary

*Alphabetical, drawn from the guide's terminology.*

- **Account** — User credentials attached to a contact. Enables log-in and role assignment. Cannot change the User Name — delete and recreate to change.
- **Acknowledge Receipt Items** — Return step confirming the physical goods were received before completing a return or starting an exchange.
- **Address book (customer's)** — Per-contact stored addresses reusable at checkout.
- **All Properties view** — Editing mode exposing every property of a content item via a tab bar.
- **Application Log** — Log that tracks catalog-system changes by default; extensible.
- **Assets** — Images, documents, videos attached to a catalog entry or CMS item. Managed via the Assets pane.
- **Autosave** — Background saving of edits to drafts.
- **Awaiting Exchange** — Order status indicating a related exchange order is in progress.
- **Awaiting Inventory** — Shipment status after payment but before the items are confirmed in stock.
- **Base weight** — Default weight unit for a catalog; feeds shipping cost calculations.
- **Basket** — Synonym for shopping cart.
- **Batch update** — Bulk editing of multiple catalog entries through Catalog Batch Update.
- **Best Sellers Report** — Report showing top-selling products by quantity and revenue.
- **Build (index)** — Incremental re-index of recently changed catalog items.
- **Build Your Own Discount** — Configurable promotion type where the user defines purchase conditions and rewards.
- **Bundle** — Collection of variants purchased together but priced and displayed as separate line items.
- **Business Category** — Dictionary-driven classification of an organization.
- **Business Foundation** — No-code data modelling subsystem for extending Customer and Asset forms, objects, fields, and relations.
- **Business Object** — An entity managed by Business Foundation.
- **Campaign** — Logical grouping of promotions and customer segments with a lifecycle window.
- **Cart** — Shopping cart; a pre-order container persisted per customer.
- **Catalog** — Top-level container of categories and entries; independent, multi-site-assignable.
- **Catalog Batch Update** — Tool for editing multiple catalog entries at once, filtered by entry type, meta class, field, language, catalog, keywords.
- **Catalog entry** — A category, product, variant, package, bundle, or dynamic package.
- **Catalog Management** — Subsystem for working with catalogs in Commerce Manager.
- **Category** — A grouping of products in a catalog tree.
- **Class Name / Payment Class** — The implementation class behind a payment method.
- **CMO (Campaign Monitor and Optimization)** — EPiServer add-on for campaign/landing-page measurement; integrated with Commerce via the Commerce Settings for CMO gadget.
- **Combine with other promotions** — Promotion rule allowing it to stack with all other promotions.
- **Commerce Manager** — Back-office UI for Commerce subsystems (Catalog, Order, Customer, Marketing, Market, Reporting, Administration).
- **Community Approval** — Moderation feature + dashboard gadget for product review comments.
- **Contact** — Individual in Customer Management; may have an account.
- **Content breadcrumbs** — Toolbar path showing the tree location of the current content.
- **Coupon code** — Optional promotion code customers redeem for a discount.
- **CSV Catalog Import** — Bulk import feature requiring a CSV file plus an XML mapping file.
- **Currency Rate** — FX rate (End of Day, Average, Date) defined between currencies.
- **Customer group** — Dictionary-driven classification of contacts (e.g. Customer, Partner, Distributor); drives pricing and promotion eligibility.
- **Customer Price Group** — Sale Type used to tie a price to a specific customer group.
- **Customer Segment** — Static or dynamic group of customers targeted by campaigns.
- **Default Market** — Market providing fallback language and currency.
- **Default Warehouse** — Warehouse used when no other is specified; rename-able.
- **Detach** — Remove a link between a catalog entry and one of its categories (disabled if it would orphan the entry).
- **Dictionary** — Structured list of controlled values (countries, currencies, return reasons, customer groups, organization types, business categories).
- **Display Name** — Customer-facing name for a product, variant, or shipping method.
- **Duplicate** — Copy a catalog entry with a new URL/SEO URL/code.
- **Dynamic Package** — Package whose contents are determined at runtime.
- **EO (Exchange Order)** — Exchange order derived from a completed purchase order; prefixed `EO`.
- **Entry Association** — Cross-sell / accessory / "you may also like" relationship, configurable via CSV.
- **Entry Relation** — Parent-child relationship between catalog entries (e.g. Product → Variants).
- **Exclusive within Selected/All Groups** — Promotion rule preventing other promotions in those groups from stacking.
- **Expression** — XML-authored condition used to extend promotions, segments, policies.
- **Fixed Shipping** — Sample shipping method using a flat rate.
- **Flat shipping rate** — See Generic Gateway.
- **Fulfillment Center** — Warehouse flag enabling outgoing shipments.
- **Generic Gateway** — Shipping gateway for a fixed shipping fee.
- **Ground Shipping** — Sample shipping method.
- **IBiz E-Payment Integrator** — nSoftware component offering dozens of payment gateways.
- **Inventory status** — Enabled / Disabled / Ignored — controls whether checkout is blocked when stock runs out.
- **Jurisdiction / Jurisdiction Group** — Geographic rule definitions used by Weight/Jurisdiction shipping and tax configuration.
- **LineItem** — Order meta-class object type representing a line in an order.
- **Low Stock Report** — Built-in report of products below their reorder minimum.
- **Mapping file** — XML file that aligns CSV columns to meta fields during catalog CSV import.
- **Market** — Configurable grouping of countries/languages/currencies/promotions driving per-region behaviour.
- **Market Management** — Subsystem for defining markets.
- **Merchandiser** — Role responsible for stock, categorisation, pricing, cross-selling.
- **Meta Class** — Named data model (Catalog Node, Catalog Entry, Order Group, Order Form, Shipment, LineItem, Order Group Address, Order Form Payment).
- **Meta Field** — Custom data field attached to a meta class.
- **Name in URL** — Auto-generated URL segment from the entry's name.
- **No Cycle** — Payment plan mode meaning a one-time charge, not recurring.
- **Notification bar** — UI element surfacing messages about the current content.
- **nSoftware** — Third-party supplier of payment gateway integrations.
- **On-Page Editing** — Editing view that shows edits in-context on the rendered page.
- **Order Form / Order Form Payment** — Meta-class object types inside the order data model.
- **Order Group / Order Group Address** — Meta-class object types representing the container for an order.
- **Order Management** — Subsystem handling carts, POs, payment plans, shipments, returns, exchanges.
- **Organization** — Parent-child structure grouping contacts.
- **Organization Customer Group** — Customer group assigned to an organization; overrides individual contact groups for pricing and promotion eligibility.
- **Organization Unit** — Child-type organization (e.g. a regional branch).
- **Other Payment** — Generic payment type built into Commerce.
- **Package** — Group of items sold as one SKU with one price.
- **Packing Slip** — Printed sheet attached to the physical package.
- **Partially Shipped** — Order status indicating at least one shipment completed but others pending.
- **Payment gateway** — Class processing a payment type (PayPal, Authorize.Net, etc.).
- **Payment method** — Customer-facing payment option tied to a gateway.
- **Payment plan** — Recurring payment schedule (subscriptions); governed by the Subscription Payment Plan scheduled job.
- **Payment type** — Meta class of a payment (e.g. Credit Card, Payment by Phone, Exchange Payment).
- **Pickup Location** — Warehouse flag enabling in-store pickup orders.
- **Pick List** — List used in the warehouse to collect items for shipment; drives packing slip printing.
- **Policy** — Business rule (global or local) applied to promotions.
- **Preorder / Backorder** — Inventory allowances after current stock is exhausted.
- **Priority (promotion)** — Integer controlling evaluation order — higher first.
- **Product** — Grouping of variations; a purchasable concept.
- **Promotion** — Discount/coupon applied to entries, orders, or shipping, attached to a campaign.
- **Promotion Group** — Grouping of promotions that exclusivity rules can target.
- **Purchase Order (PO)** — Finalized order converted from a cart, prefix `PO`.
- **Rebuild (index)** — Full re-indexing (slower than Build).
- **Receiving Manager** — Role permitted to view shipments and receive returns.
- **Redemption limits** — Max usage counts for promotions (total / per order / per customer).
- **Related Entries** — Cross-/up-sell links (e.g. Accessories, You May Also Like).
- **Release Shipment** — Move an order to *Released for Shipping* so the warehouse can process it.
- **Remove Expired Carts** — Scheduled job purging abandoned carts past retention.
- **Reserved (stock)** — Quantity set aside and subtracted from effective availability.
- **Return** — Incoming goods from a completed order; eligible for refund or exchange.
- **Return reason** — Reason drop-down for a return. Default values: Faulty, Incorrect Item, Unwanted Gift.
- **Role** — Named bundle of permissions; cumulative when multiple assigned.
- **Rotate Encryption Keys** — Scheduled job rotating keys used for encrypted meta fields.
- **Sale Code** — Sale identifier tying a price to a specific group/customer.
- **Sale Type** — All Customers / Specific Customer / Customer Price Group.
- **Sample Site** — Reference implementation shipped with EPiServer Commerce, illustrating typical templates, gadgets, catalog, and roles.
- **Search Index** — Index used by the platform search; Build and Rebuild options available.
- **Search Properties** — Per-meta-field switches: Allow search, Enable Sorting, Include Values, Tokenize, Include in Default Search.
- **SEO URL** — SEO-friendly URL; must be unique.
- **Serialize Catalog Meta Data** — Scheduled job speeding catalog metadata retrieval.
- **Shipment Releasing** — Scheduled job auto-releasing shipments after a delay.
- **Shipping Gateway** — Class behind a shipping provider (Generic or Weight/Jurisdiction).
- **Shipping Manager** — Role that can view, pack, complete shipments.
- **Shipping Method** — Customer-facing shipping choice with a provider.
- **Shipping Provider** — Class interacting with real carriers or shipping scenarios.
- **SKU (Stock Keeping Unit)** — Unique purchasable variant of a product.
- **Split payment** — Multiple payments attached to one order.
- **Split shipment** — An order delivered in multiple shipments (each with its own address/method).
- **Structure group** — CMS group controlling editing access inside the page tree.
- **Subscription Payment Plan** — Scheduled job creating the recurring POs defined by payment plans.
- **System Keyword** — Unique, immutable identifier for a payment method or shipping provider.
- **System Log** — Log of Commerce system events.
- **Target Markets** — Markets a campaign or promotion is restricted to.
- **Target Segments** — Customer segments selected on a campaign.
- **Tax Category** — Catalog-side classification (e.g. General Sales, Luxury Items) used to drive tax calculations.
- **Tax Configuration** — Order-system area defining jurisdictions, jurisdiction groups, and taxes.
- **Tax Type** — Optional CSV column: Sales Tax or Shipping Tax.
- **Tiered pricing** — Differentiated prices using Min. quantity thresholds and customer price groups.
- **Tokenize** — Meta-field search property that breaks text into individual words for indexing.
- **Tracked (variant)** — Inventory flag requiring stock data before orders can complete.
- **Variation / Variant** — See SKU.
- **View (custom)** — Saved filter+column layout for Organizations or Contacts.
- **Visible to** — Access-right setting on a content item.
- **Visitor Group** — Rule-based group used for personalization; can use Commerce Criteria.
- **Warehouse** — Physical stock location; may act as Fulfillment, Pickup, Delivery location.
- **Weight / Jurisdiction Gateway** — Shipping gateway calculating cost from weight and location rules.
- **WebAdmins / WebEditors** — CMS groups granting admin / edit access; prerequisite for the CMS halves of an editor's permissions.
- **Wish list** — Customer-side list that can trigger "Product in Cart or Wish List" personalization.

---

*End of knowledge-base file. Source: EPiServer Commerce User Guide, Update 14-1.*
