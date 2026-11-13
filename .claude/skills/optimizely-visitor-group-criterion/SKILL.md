---
name: optimizely-visitor-group-criterion
description: Add a custom visitor group criterion to an Optimizely CMS 12 (.NET) site - the [VisitorGroupCriterion] class deriving from CriterionBase<TModel>, its CriterionModelBase settings model, the editor widgets driven by [CriterionPropertyEditor], the enum selection factory, and the translation XML that gives the criterion and its dropdowns readable labels. Covers which project each file belongs in, why the translations must live in Wisbar.Web while the criterion lives in Optimizely.Core, how EnumSelectionFactory derives its lookup path from the enum namespace, constructor injection into a criterion, writing IsMatch safely on the per-request hot path, why principal.Identity.IsAuthenticated is the wrong membership check in this codebase, stateful criteria via ICriterionEvents and IStateStorage, and testing. Use whenever asked to add, change or debug a visitor group criterion, an audience rule, or personalization targeting, or when a criterion does not appear in the visitor group editor, shows raw translation keys, or matches the wrong visitors.
---

# Adding a custom visitor group criterion

A criterion is the rule an editor drags into a visitor group in **CMS admin -> Visitor Groups**. It is four artefacts, not one, and it is not usable until all four exist:

| Artefact | This repo | Purpose |
|---|---|---|
| Criterion class | `Optimizely.Core/Personalization/CookieAudienceCriteria/Criterion/` | `[VisitorGroupCriterion]` + `CriterionBase<TModel>`; implements `IsMatch` |
| Settings model | `.../Models/` | `CriterionModelBase`; the fields the editor fills in |
| Enum (if any) | `.../Enums/` | The dropdown options |
| Translations | **`Wisbar.Web/Resources/Translations/`** | Labels for the criterion and every enum value |

Read the two shipped examples end to end before writing: `CookieExistsCriterion` (minimal) and `CookieValueCriterion` (dropdown plus a checkbox). Full code in `references/templates.md`; the API surface in `references/reference.md`.

## Step 0: Decide which project it lives in

`Wisbar.Optimizely.Core` has **no project references at all** - it is a leaf. That bounds what a criterion placed there can do.

| The criterion needs | Put it in |
|---|---|
| `HttpContext`, cookies, headers, the principal, `ICurrentIdentity` | `Optimizely.Core/Personalization/` - alongside the cookie criteria. `ICurrentIdentity` lives in Optimizely.Core, so identity questions stay in-project. |
| A Business service - iMIS, site settings, cart, search, pricing | `Wisbar.Business/Features/<Feature>/` | 
| Anything Web-only | `Wisbar.Web/` |

Getting this wrong means an upward project reference, which per `CLAUDE.md` means the code is in the wrong project. Decide before you create the file.

The translation XML is the exception that always crosses projects: it must be in `Wisbar.Web/Resources/Translations/`, because that is the only project whose csproj ships them (`<EmbeddedResource Include="Resources\Translations\**\*" />`). A criterion in Optimizely.Core with its translations anywhere else renders as raw keys.

## Step 1: The settings model

```csharp
public class CookieExistsCriterionModel : CriterionModelBase
{
    [Required]
    public string CookieName { get; set; } = string.Empty;

    [Required]
    [CriterionPropertyEditor(SelectionFactoryType = typeof(EnumSelectionFactory))]
    public CookieExistsCondition Condition { get; set; }

    public override ICriterionModel Copy() => base.ShallowCopy();
}
```

- **`Copy()` is required** and `ShallowCopy()` is the standard implementation. It is how the editor clones settings; omit it and editing a saved criterion misbehaves.
- `[Required]` drives client-side validation in the visitor group editor.
- `[CriterionPropertyEditor]` controls the widget: `SelectionFactoryType` for a dropdown, `LabelTranslationKey` for the field label, plus `Order`, `DefaultValue`, `PreTextTranslationKey`, `AfterTextTranslationKey`, `Ignore`.
- **`EnumSelectionFactory` here is `EPiServer.Web.Mvc.VisitorGroups.EnumSelectionFactory`.** Two other types share that name - the content-editing one in `EPiServer.Cms.Shell.UI...SelectionFactories`, and this repo's own `Wisbar.Optimizely.Core.Infrastructure.EditorDescriptors.EnumSelectionFactory<T>`. Only the `Web.Mvc.VisitorGroups` one implements `ISelectionFactory`. Importing the wrong one is a compile error at best and an empty dropdown at worst.
- `Nullable` is enabled in Optimizely.Core, so initialize reference-typed properties (`= string.Empty;`). The two shipped models do not, and produce CS8618 warnings; `CLAUDE.md` says not to add more.

## Step 2: The criterion class

```csharp
[VisitorGroupCriterion(
    Category = "Technical Criteria",
    Description = "Check if cookie exist (or not)",
    DisplayName = "Cookie exists",
    LanguagePath = "/cookieaudiencecriteria/enums/cookieexistscondition")]
public class CookieExistsCriterion : CriterionBase<CookieExistsCriterionModel>
{
    public override bool IsMatch(IPrincipal principal, HttpContext httpContext) { ... }
}
```

- **No DI registration.** `VisitorGroupCriterionAttribute` derives from `ServicePlugInAttributeBase`, so the type is discovered and registered by assembly scanning.
- **Constructor injection works.** Optimizely's own `UserProfileCriterion` has only a parameterized constructor. If you inject, keep a parameterless constructor too unless every dependency is guaranteed registered.
- `Category` groups the criterion in the editor's picker. Reuse an existing string (`"Technical Criteria"` here) rather than inventing a near-duplicate.
- `Model` is the typed settings instance, populated before `IsMatch` runs.
- Other attribute properties: `TypeGuid`, `ScriptUrl`, `ScriptShellModuleName`. **Do not rename or move a criterion class that is already used** - saved visitor groups store the criterion by type name, so a rename orphans them in every environment.

## Step 3: Translations

One XML file per criteria family under `Wisbar.Web/Resources/Translations/`. Two different path rules apply, and mixing them up is the usual cause of raw keys in the UI.

**Enum values** are looked up by the enum's **namespace**, lowercased, nested under `<enums>`. `CookieValueCondition` in namespace `CookieAudienceCriteria.Enums` becomes:

```xml
<enums>
  <cookieaudiencecriteria><enums><cookievaluecondition>
    <is>Is equal to</is>
    <contains>Contains</contains>
  </cookievaluecondition></enums></cookieaudiencecriteria>
</enums>
```

This is why the shipped enums sit in a short top-level `CookieAudienceCriteria.Enums` namespace instead of the full `Wisbar.Optimizely.Core.Personalization...` one: the namespace becomes the XML nesting, so a long namespace means a deeply nested, unreadable file. Keep new criteria enums in a short namespace for the same reason.

**Property labels** use `LabelTranslationKey`, an absolute path from the document root, independent of any namespace:

```csharp
[CriterionPropertyEditor(LabelTranslationKey = "/cookieaudiencecriteria/properties/casesensitive")]
```

```xml
<cookieaudiencecriteria><properties><casesensitive>Case sensitive</casesensitive></properties></cookieaudiencecriteria>
```

Add every enum member. A missing one renders as its key, which editors read as a broken criterion.

## Step 4: Write `IsMatch` carefully

`IsMatch` runs **for every visitor group, for every criterion in it, on every request** that evaluates personalization. It is a hot path with no error containment.

- **Null-guard everything.** `httpContext`, `httpContext.Request.Cookies`, and the values you read. `CookieValueCriterion` guards; `CookieExistsCriterion` does not need to because the indexer tolerates it.
- **No I/O.** No HTTP calls, no database queries, no content loads without a cache. A slow criterion slows every personalized page.
- **Return `false` on anything unexpected**, including the `_ => false` default arm on a switch. Never throw: an exception here surfaces as a failed page render, not a skipped rule.
- **`principal.Identity.IsAuthenticated` does not mean "signed-in member" in this codebase.** `AddOptimizelyIdentity(useAsDefault: true)` makes the CMS scheme the default, so that check is true for any signed-in CMS editor browsing the storefront. Membership questions go through `ICurrentIdentity` (`Optimizely.Core/Infrastructure/Identity/`) - `IsMember`, `MemberId`, `ContactId`. Injecting it into a criterion is fine; it lives in the same project.
- **Prefer `StringMatchHelper.IsMatch(criteriaValue, settingsValue, MatchStringType)`** over hand-rolled comparison chains when the criterion is a string test. `MatchStringType` is `Equals`, `Contains`, `StartsWith`, `EndsWith`, and the built-in criteria use it.

**Do not copy `CookieValueCriterion`'s switch as a pattern.** Its `DoesNotContain` arm selects the comparison with `!Model.CaseSensitive` (inverted against every other arm) and then returns `>= 0`, which is "contains". That arm currently returns true when the cookie *does* contain the value. Write negations as `!(positive)`, the way the `IsNot` arm does, or use `StringMatchHelper`.

## Step 5: Stateful criteria, only if you need them

For rules about what happened earlier in the visit (pages viewed, session-start values), override `Subscribe`/`Unsubscribe` and store state:

- `ICriterionEvents` exposes `StartRequest`, `EndRequest`, `PreRequestHandlerExecute`, `StartSession`, `VisitedPage`. `CriterionEventArgs` carries `HttpContext`, and `args.GetPageLink()` gives the current page.
- `IStateStorage` (`Load`, `Save`, `Delete`, `IsAvailable`) is the session-backed store the built-in `ViewedPagesCriterion` injects. Always check `IsAvailable`.
- `SessionStartCookieCriterionBase<T>` already implements the capture-at-session-start cookie pattern.
- Session-backed criteria need `VisitorGroupOptions.EnableSession`. Say so when you add one, because it is a configuration change, not just code.

## Step 6: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

A criterion is plain testable code: construct the model, construct the criterion, call `IsMatch` with a `DefaultHttpContext` and a `ClaimsPrincipal`. There are no criterion tests in `Wisbar.Tests` yet - add them for the match logic, which is the part that can be silently wrong.

A green build says nothing about the editor. Report which of these you checked:

- The criterion appears in CMS admin -> Visitor Groups, under the intended category, with a readable display name and description.
- Every field renders with its label, and every dropdown shows translated text, not keys.
- `[Required]` fields block saving when empty.
- Saving, reopening and editing the criterion preserves the settings (this is what `Copy()` breaks).
- A visitor who should match does, and one who should not does not - test both directions, especially for any negated condition.
- The criterion behaves when the underlying data is absent: no cookie, anonymous visitor, no session.

## Checklist

- [ ] Project chosen by what the criterion needs; no upward reference out of Optimizely.Core
- [ ] Model derives `CriterionModelBase` and overrides `Copy()` with `ShallowCopy()`
- [ ] Reference-typed model properties initialized; no new nullable warnings
- [ ] `[CriterionPropertyEditor]` with `EPiServer.Web.Mvc.VisitorGroups.EnumSelectionFactory` for enums
- [ ] `[VisitorGroupCriterion]` with `Category`, `DisplayName`, `Description`, `LanguagePath`; no DI registration added
- [ ] Translation XML in `Wisbar.Web/Resources/Translations/`, enum path matching the enum's namespace, every member present
- [ ] Enum namespace kept short so the XML nesting stays readable
- [ ] `IsMatch` null-guards, does no I/O, returns false on the default arm, never throws
- [ ] Membership checked through `ICurrentIdentity`, never `principal.Identity.IsAuthenticated`
- [ ] Negated conditions written as `!(positive)` or via `StringMatchHelper`
- [ ] Existing criterion classes not renamed or moved
- [ ] Unit tests for the match logic; editor behaviour verified by hand

Full code templates: `references/templates.md`. API surface: `references/reference.md`.
