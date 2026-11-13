# Visitor group criterion API reference

Reflected out of the assemblies this solution builds against: `EPiServer.CMS.Core 12.24.0` (package reference `EPiServer.CMS 12.34.5`). Types live across four assemblies, which is why the `using` list is longer than it looks like it should be.

## Where each type comes from

| Type | Namespace | Assembly |
|---|---|---|
| `VisitorGroupCriterionAttribute` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.Framework.AspNetCore` |
| `CriterionBase<T>`, `ICriterion` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.Framework.AspNetCore` |
| `ICriterionEvents`, `CriterionEventArgs` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.Framework.AspNetCore` |
| `IStateStorage`, `SessionStartCookieCriterionBase<T>` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.Framework.AspNetCore` |
| `CriterionModelBase`, `ICriterionModel` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.ApplicationModules` |
| `CriterionPropertyEditorAttribute` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.ApplicationModules` |
| `ICriterionAvailability`, `VisitorGroupOptions` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.ApplicationModules` |
| `VisitorGroupCriterion` (the persisted DTO) | `EPiServer.Personalization.VisitorGroups` | `EPiServer.ApplicationModules` |
| `EnumSelectionFactory` | **`EPiServer.Web.Mvc.VisitorGroups`** | `EPiServer.Cms.AspNetCore` |
| `StringMatchHelper`, `MatchStringType` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.Cms.AspNetCore` |
| `CriterionEventArgsExtensions` | `EPiServer.Personalization.VisitorGroups` | `EPiServer.Cms.AspNetCore` |

## `CriterionBase<TModel>`

```
virtual  Initialize(VisitorGroupCriterion visitorGroupCriterion)
abstract IsMatch(IPrincipal principal, HttpContext httpContext) -> bool
virtual  Subscribe(ICriterionEvents criterionEvents)
virtual  Unsubscribe(ICriterionEvents criterionEvents)
         Model : TModel  { get; }
```

`IsMatch` is the only required override. `Model` is read-only and populated by `Initialize`, which is also the seam a unit test uses:

```csharp
criterion.Initialize(new VisitorGroupCriterion { Model = new MyModel { ... } });
```

`VisitorGroupCriterion` has a public parameterless constructor, so that object initializer works in tests.

## `VisitorGroupCriterionAttribute`

Derives from `ServicePlugInAttributeBase`, so an attributed type is discovered and registered automatically - **never add a DI registration for a criterion**. Constructor injection still works; `UserProfileCriterion` in CMS ships with only a parameterized constructor.

| Property | Purpose |
|---|---|
| `DisplayName` | Name in the criteria picker and on the saved criterion |
| `Description` | Hover text in the picker |
| `Category` | Group in the picker. This repo uses `"Technical Criteria"` |
| `LanguagePath` | Root path for this criterion's own translated strings |
| `TypeGuid` | Stable identity, distinct from the type name |
| `ScriptUrl`, `ScriptShellModuleName` | Custom client-side editor, rarely needed |

## `CriterionModelBase`

```
abstract Copy() -> ICriterionModel      // required override; ShallowCopy() is the standard body
protected ShallowCopy() -> ICriterionModel
         Id : Identity { get; set; }
protected LocalizationService { get; }
```

## `CriterionPropertyEditorAttribute`

| Property | Purpose |
|---|---|
| `SelectionFactoryType` | An `ISelectionFactory` implementation; `EnumSelectionFactory` for enums |
| `LabelTranslationKey` | Absolute translation path for the field label |
| `Order` | Field order in the editor |
| `DefaultValue` | Initial value for a new criterion |
| `PreTextTranslationKey`, `AfterTextTranslationKey` | Text rendered around the widget |
| `AdditionalPropsJson` | Extra props passed to the client widget |
| `Ignore` | Hide the property from the editor |

`DojoWidgetAttribute` (same namespace) is the older equivalent, with `WidgetType`, `WidgetPackage`, `AdditionalOptions`. Prefer `CriterionPropertyEditor` in new code.

`CriterionPropertyCultureAttribute` marks a property as culture-specific, with `PropertyCulture`.

## Translation path rules

Two different rules, and they are the usual cause of raw keys in the editor.

**Enum values** - `/enums` + the enum type's **namespace**, lowercased, one XML element per namespace segment, + the type name, + the member name:

| Enum | Path |
|---|---|
| `CookieAudienceCriteria.Enums.CookieValueCondition.Is` | `/enums/cookieaudiencecriteria/enums/cookievaluecondition/is` |

Keep criteria enum namespaces short for this reason. The shipped enums use the top-level `CookieAudienceCriteria.Enums`, not the criterion's own long namespace.

**Property labels** - the literal absolute path given in `LabelTranslationKey`, unrelated to any namespace.

**File location** - `Wisbar.Web/Resources/Translations/*.xml`, embedded by `<EmbeddedResource Include="Resources\Translations\**\*" />` in `Wisbar.Web.csproj`. Translations for a criterion that lives in `Wisbar.Optimizely.Core` still go here.

## `ISelectionFactory` and `EnumSelectionFactory`

```
ISelectionFactory.GetSelectListItems(Type property) -> IEnumerable<SelectListItem>
```

Three types are named `EnumSelectionFactory` in this solution. Only the first implements `ISelectionFactory` and belongs on a criterion model:

| Type | Use |
|---|---|
| `EPiServer.Web.Mvc.VisitorGroups.EnumSelectionFactory` | **Visitor group criteria** |
| `EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors.SelectionFactories.EnumSelectionFactory` | Content type property editors |
| `Wisbar.Optimizely.Core.Infrastructure.EditorDescriptors.EnumSelectionFactory<T>` | This repo's content-editing helper |

Write your own `ISelectionFactory` for non-enum option lists, the way `UserProfileSelectionFactory` does in CMS.

## `StringMatchHelper`

```
IsMatch(string criteriaValue, string settingsValue, MatchStringType matchType) -> bool
IsMatch(string criteriaValue, string settingsValue, string matchType) -> bool
GetMatchStringType(string matchStringType) -> MatchStringType
```

`MatchStringType`: `Equals`, `Contains`, `StartsWith`, `EndsWith`. Used by the built-in string criteria; prefer it over a hand-rolled comparison switch.

## Stateful criteria

`ICriterionEvents`:

| Event | Fires |
|---|---|
| `StartSession` | New visitor session |
| `StartRequest` | Each request, at the start |
| `PreRequestHandlerExecute` | Before the handler runs |
| `VisitedPage` | A content page was visited |
| `EndRequest` | Each request, at the end |

All are `EventHandler<CriterionEventArgs>`. `CriterionEventArgs` exposes `HttpContext`; `CriterionEventArgsExtensions.GetPageLink(args)` gives the current page's `ContentReference`.

`IStateStorage`: `Load(string key)`, `Save(string key, object value)`, `Delete(string key)`, `IsAvailable`. Session-backed; always check `IsAvailable`. Injected by the built-in `ViewedPagesCriterion`.

`SessionStartCookieCriterionBase<T>` already implements capture-at-session-start into a cookie: `CookieKey`, `CookieExpires`, `GetCookie`, `SetCookieValue`, plus `Subscribe`/`Unsubscribe`.

`VisitorGroupOptions`: `EnableSession`, `EnableStatistics`, `StatisticsPersistanceInterval`. Session-backed criteria need `EnableSession`.

## Other interfaces worth knowing

- `ICriterionAvailability` - `AvailableForCreation`; return `false` to keep a criterion working for saved groups while hiding it from the picker.
- `CriterionValidationResult` - `IsValid`, `ErrorMessage`, `ErrorProperty`; `CriterionValidationException` carries `ValidationErrors`.
- `IVisitorGroupEvents` (`EPiServer.Personalization.VisitorGroups`, `EPiServer.ApplicationModules`) - `SavingVisitorGroup`, `SavedVisitorGroup`, `DeletingVisitorGroup`, `DeletedVisitorGroup`. Subscribe from an initializable module; see the `optimizely-content-events` skill for that lifecycle.

## Built-in criteria worth reading before writing your own

All in `EPiServer.Personalization.VisitorGroups.Criteria` (`EPiServer.Cms.AspNetCore`), unless noted.

| Criterion | Shows |
|---|---|
| `TimeOfDayCriterion` | Injecting a provider (`ICurrentTimeProvider`) for testability |
| `ViewedPagesCriterion` | `IStateStorage` plus `ICriterionEvents` |
| `ReferrerCriterion`, `SearchWordReferrerCriterion` | `StringMatchHelper` usage |
| `GeographicLocationCriterion` | Multiple constructors, optional dependencies |
| `UserProfileCriterion` (`EPiServer.Cms.UI.AspNetIdentity`) | Constructor-only injection, custom `ISelectionFactory` |
| `VisitorGroupMembershipCriterion` | A criterion that references other visitor groups |

## This repo

| File | Notes |
|---|---|
| `Optimizely.Core/Personalization/CookieAudienceCriteria/Criterion/CookieExistsCriterion.cs` | Minimal criterion, clean reference |
| `.../Criterion/CookieValueCriterion.cs` | Dropdown plus checkbox. Class is named `CookieValueCriteria` while the file is `CookieValueCriterion.cs`, and its `DoesNotContain` arm is inverted - read it for structure, not for correctness |
| `.../Models/`, `.../Enums/` | Model and enum shapes |
| `Wisbar.Web/Resources/Translations/CookieAudienceCriteria.xml` | Both translation path rules in one file |
| `Optimizely.Core/Infrastructure/Identity/ICurrentIdentity.cs` | The membership check a criterion must use |

`Wisbar.Optimizely.Core` has no project references. A criterion needing a `Wisbar.Business` service belongs in Business, not here.
