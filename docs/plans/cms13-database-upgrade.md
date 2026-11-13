# CMS 13 / Commerce 15 — upgrading an existing database

Status: **not yet verified** — the local test used empty databases (fresh install).
Run every step below against a **restored copy** of production first. Never point a dev machine at a shared or production database.

## How the upgrade runs

On first start, CMS 13 / Commerce 15 upgrade the CMS (`EPiServerDB`) and Commerce (`EcfSqlConnection`) schemas automatically when schema updates are enabled. This site currently sets `EPiServer:Cms:DataAccess:CreateDatabaseSchema = true` (fresh databases). For upgrading an existing database, also enable updates:

```json
"EPiServer": { "Cms": { "DataAccess": { "CreateDatabaseSchema": true, "UpdateDatabaseSchema": true } } }
```

Expected result: `sp_DatabaseVersion` = **21007** (CMS 13.6.0, observed on the fresh install).

## Why it can fail

Long-lived databases often contain objects Optimizely didn't create — SQL Server Tuning Advisor `_dta_stat_*` / `_dta_index_*`, `missing_index_*`, hand-made indexes on `tbl*` tables. The CMS 13 scripts `ALTER COLUMN` on tables such as `tblWorkContent`, `tblContentLanguage`, `tblScheduledItem(Log)`, `tblSystemBigTable`, and SQL Server refuses while a statistic or index depends on the column ("The statistics '…' is dependent on column '…'").

## Step 1 — discover custom objects (read-only)

Run against the production copy (and ask DBAs to run it on production — dev copies often lack these objects):

```sql
SELECT t.name AS TableName, s.name AS ObjectName,
       CASE WHEN i.index_id IS NULL THEN 'STATISTICS' ELSE 'INDEX' END AS Kind
FROM sys.stats s
JOIN sys.tables t ON t.object_id = s.object_id
LEFT JOIN sys.indexes i ON i.object_id = s.object_id AND i.index_id = s.stats_id
WHERE t.name LIKE 'tbl%'
  AND s.auto_created = 0
  AND (s.name LIKE '\_dta\_%' ESCAPE '\'
       OR s.name LIKE 'missing\_index\_%' ESCAPE '\'
       OR (i.index_id IS NOT NULL AND i.is_primary_key = 0 AND i.is_unique_constraint = 0
           AND s.name NOT LIKE 'IX\_tbl%' ESCAPE '\' AND s.name NOT LIKE 'PK\_%' ESCAPE '\'))
ORDER BY t.name, s.name;
```

Review the list before dropping anything; save the DDL of indexes that exist for performance so they can be re-created after the upgrade.

## Step 2 — only if Step 1 finds blocking objects: cleanup inside the schema update

A migration step runs too late. Hook into the schema update with an `ISchemaValidator` (its `BeforeUpdating` runs only when an update is pending, once per connection string), registered **before** `AddCms()`:

```csharp
services.AddSingleton<ISchemaValidator, Cms13PreUpgradeCleanup>();
```

The validator executes an embedded SQL script through `ScriptExecutor`. The script starts with a validating query so it is idempotent:

```sql
--beginvalidatingquery
IF OBJECT_ID(N'[dbo].[sp_DatabaseVersion]', N'P') IS NULL
    SELECT 0, 'Not an existing CMS database'
ELSE
BEGIN
    DECLARE @ver INT
    EXEC @ver = sp_DatabaseVersion
    IF (@ver < 21007) SELECT 1, 'Updating' ELSE SELECT 0, 'Already Done'
END
--endvalidatingquery
GO
DECLARE @Objects TABLE (TableName sysname NOT NULL, ObjectName sysname NOT NULL);
INSERT INTO @Objects (TableName, ObjectName) VALUES
    -- paste the reviewed list from Step 1
    ('tblWorkContent', '_dta_stat_example');
DECLARE @Sql nvarchar(max) = N'';
SELECT @Sql += CASE WHEN i.index_id IS NOT NULL
        THEN N'DROP INDEX ' + QUOTENAME(o.ObjectName) + N' ON [dbo].' + QUOTENAME(o.TableName) + N';'
        ELSE N'DROP STATISTICS [dbo].' + QUOTENAME(o.TableName) + N'.' + QUOTENAME(o.ObjectName) + N';' END
FROM @Objects o
JOIN sys.stats s ON s.object_id = OBJECT_ID(N'[dbo].' + QUOTENAME(o.TableName)) AND s.name = o.ObjectName
LEFT JOIN sys.indexes i ON i.object_id = s.object_id AND i.index_id = s.stats_id;
EXEC sp_executesql @Sql;
```

Use an explicit, reviewed list — never drop "everything that looks custom" by pattern.

## Step 3 — verify on the copy

1. Start the site; watch the log for schema-update messages.
2. `EXEC @v = sp_DatabaseVersion; SELECT @v` in both databases.
3. Log in to the CMS and Commerce UI; open a page, a block, a catalog item and a cart.
4. Start again: the cleanup must be skipped ("Already Done").

## Data that will not carry over (decided, ADR-008)

- `FashionProduct.AvailableSizes`, `AvailableColors`, `EventQuestionProperty.PurchaseClaims` — storage type changes (LongString → Json); re-enter values.
- `EventTuitionVariant.ColorPicker` (DoubleJay `IColor`) → new `DisplayColor` (`#RRGGBB`); old values stay in the DB for an optional migration.
- Tab access rights / ordering for "Aside content", "Site Settings", "Cookies Consent" (renamed tab identifiers) — re-apply in Admin mode.
- `CookieConsentAuditPurgeJob` history (new job GUID).
- Geta NotFoundHandler 8 upgrades its own schema at startup — the DB user needs ALTER on `NotFoundHandler.*`.
