# Visitor group criterion templates

Namespaces use the Wisbar layout. The repo is the authority - these are the shape. Comments stay within the two-line limit from `CLAUDE.md`.

Worked example: a criterion that matches visitors whose request carries a given header, with a condition dropdown.

## A. The enum

`Optimizely.Core/Personalization/HeaderAudienceCriteria/Enums/HeaderValueCondition.cs`

Keep the namespace **short and top-level**. `EnumSelectionFactory` turns the namespace into XML nesting in the translation file, so `Wisbar.Optimizely.Core.Personalization.HeaderAudienceCriteria.Enums` would produce six levels of nesting before the enum name.

```csharp
namespace HeaderAudienceCriteria.Enums;

public enum HeaderValueCondition
{
    Is,
    IsNot,
    Contains,
    DoesNotContain
}
```

## B. The settings model

`.../Models/HeaderValueCriterionModel.cs`

```csharp
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Web.Mvc.VisitorGroups;
using HeaderAudienceCriteria.Enums;
using System.ComponentModel.DataAnnotations;

namespace Wisbar.Optimizely.Core.Personalization.HeaderAudienceCriteria.Models;

public class HeaderValueCriterionModel : CriterionModelBase
{
    [Required]
    [CriterionPropertyEditor(Order = 10, LabelTranslationKey = "/headeraudiencecriteria/properties/headername")]
    public string HeaderName { get; set; } = string.Empty;

    // EnumSelectionFactory here is the VisitorGroups one, not the content-editing type of the same name.
    [Required]
    [CriterionPropertyEditor(Order = 20, SelectionFactoryType = typeof(EnumSelectionFactory))]
    public HeaderValueCondition Condition { get; set; }

    [Required]
    [CriterionPropertyEditor(Order = 30, LabelTranslationKey = "/headeraudiencecriteria/properties/headervalue")]
    public string HeaderValue { get; set; } = string.Empty;

    [CriterionPropertyEditor(Order = 40, LabelTranslationKey = "/headeraudiencecriteria/properties/casesensitive")]
    public bool CaseSensitive { get; set; }

    public override ICriterionModel Copy() => base.ShallowCopy();
}
```

## C. The criterion

`.../Criterion/HeaderValueCriterion.cs`

```csharp
using EPiServer.Personalization.VisitorGroups;
using HeaderAudienceCriteria.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using Wisbar.Optimizely.Core.Personalization.HeaderAudienceCriteria.Models;

namespace Wisbar.Optimizely.Core.Personalization.HeaderAudienceCriteria.Criterion;

[VisitorGroupCriterion(
    Category = "Technical Criteria",
    Description = "Check a request header value",
    DisplayName = "Header value",
    LanguagePath = "/headeraudiencecriteria/enums/headervaluecondition")]
public class HeaderValueCriterion : CriterionBase<HeaderValueCriterionModel>
{
    public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
    {
        if (httpContext?.Request?.Headers is null ||
            !httpContext.Request.Headers.TryGetValue(Model.HeaderName, out var header))
        {
            return false;
        }

        var value = header.ToString();
        var comparison = Model.CaseSensitive
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;

        // Negations are written as !(positive) so the two arms cannot drift apart.
        return Model.Condition switch
        {
            HeaderValueCondition.Is => value.Equals(Model.HeaderValue, comparison),
            HeaderValueCondition.IsNot => !value.Equals(Model.HeaderValue, comparison),
            HeaderValueCondition.Contains => value.Contains(Model.HeaderValue, comparison),
            HeaderValueCondition.DoesNotContain => !value.Contains(Model.HeaderValue, comparison),
            _ => false,
        };
    }
}
```

Resolving the comparison once, above the switch, is what keeps a `DoesNotContain` arm from silently inverting its own case sensitivity the way `CookieValueCriterion` does.

## D. The translations

`Wisbar.Web/Resources/Translations/HeaderAudienceCriteria.xml`. It must be in **Wisbar.Web** - that project's csproj is what embeds `Resources\Translations\**\*`.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<languages>
  <language name="English" id="en">
    <!-- Property labels: absolute path, matching each LabelTranslationKey. -->
    <headeraudiencecriteria>
      <properties>
        <headername>Header name</headername>
        <headervalue>Header value</headervalue>
        <casesensitive>Case sensitive</casesensitive>
      </properties>
    </headeraudiencecriteria>

    <!-- Enum values: /enums + the ENUM'S NAMESPACE, lowercased, + type name. -->
    <enums>
      <headeraudiencecriteria>
        <enums>
          <headervaluecondition>
            <is>Is equal to</is>
            <isnot>Is not equal to</isnot>
            <contains>Contains</contains>
            <doesnotcontain>Does not contain</doesnotcontain>
          </headervaluecondition>
        </enums>
      </headeraudiencecriteria>
    </enums>
  </language>
</languages>
```

The `<enums><headeraudiencecriteria><enums>` double nesting is not a typo: the first `<enums>` is the lookup root, the inner one is the `Enums` segment of the namespace `HeaderAudienceCriteria.Enums`.

## E. Using the built-in string matcher

When the criterion is a plain string test, skip the switch entirely.

```csharp
using EPiServer.Personalization.VisitorGroups;

public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
{
    var value = httpContext?.Request?.Headers[Model.HeaderName].ToString();

    return !string.IsNullOrEmpty(value) &&
           StringMatchHelper.IsMatch(value, Model.HeaderValue, Model.MatchType);
}
```

`Model.MatchType` here is a `MatchStringType` property on the model, editable through the same `EnumSelectionFactory`:

```csharp
[Required]
[CriterionPropertyEditor(Order = 20, SelectionFactoryType = typeof(EnumSelectionFactory))]
public MatchStringType MatchType { get; set; }
```

`MatchStringType` is `Equals`, `Contains`, `StartsWith`, `EndsWith`. Taking it from the framework means you inherit the built-in criteria's behaviour and their translations rather than reimplementing both.

## F. Constructor injection

Discovery is by attribute, but the type is resolved from DI, so dependencies are injected normally. `UserProfileCriterion` in CMS ships with only a parameterized constructor.

```csharp
[VisitorGroupCriterion(Category = "Member", DisplayName = "Signed-in member", ...)]
public class MemberCriterion : CriterionBase<MemberCriterionModel>
{
    private readonly ICurrentIdentity _currentIdentity;

    public MemberCriterion(ICurrentIdentity currentIdentity) => _currentIdentity = currentIdentity;

    // principal.Identity.IsAuthenticated is true for signed-in CMS editors on the storefront,
    // so membership is only ever asked of ICurrentIdentity.
    public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
        => _currentIdentity.IsMember;
}
```

`ICurrentIdentity` lives in `Optimizely.Core/Infrastructure/Identity/`, so a criterion in Optimizely.Core can inject it without an upward project reference. A criterion needing a `Wisbar.Business` service cannot live in Optimizely.Core at all - move the criterion to Business.

## G. Stateful criterion

For "what happened earlier in this visit". Needs `VisitorGroupOptions.EnableSession`.

```csharp
public class ViewedSectionCriterion : CriterionBase<ViewedSectionCriterionModel>
{
    private const string StateKey = "Wisbar.ViewedSections";
    private readonly IStateStorage _stateStorage;

    public ViewedSectionCriterion(IStateStorage stateStorage) => _stateStorage = stateStorage;

    public override void Subscribe(ICriterionEvents criterionEvents)
        => criterionEvents.VisitedPage += OnVisitedPage;

    public override void Unsubscribe(ICriterionEvents criterionEvents)
        => criterionEvents.VisitedPage -= OnVisitedPage;

    private void OnVisitedPage(object? sender, CriterionEventArgs e)
    {
        if (!_stateStorage.IsAvailable)
        {
            return;
        }

        var pageLink = e.GetPageLink();
        // ...accumulate into _stateStorage.Save(StateKey, ...)
    }

    public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
        => _stateStorage.IsAvailable &&
           _stateStorage.Load(StateKey) is HashSet<int> seen &&
           seen.Contains(Model.SectionId);
}
```

For the capture-a-value-at-session-start shape, derive from `SessionStartCookieCriterionBase<T>` instead of rebuilding it.

## H. Hiding a criterion from the picker

```csharp
public class InternalOnlyCriterion : CriterionBase<InternalOnlyCriterionModel>, ICriterionAvailability
{
    // Keeps an obsolete criterion working for saved groups while stopping new uses.
    public bool AvailableForCreation => false;
}
```

## I. Tests

No mocking framework is needed: the criterion is a plain class and `DefaultHttpContext` is real.

```csharp
[TestMethod]
public void IsMatch_WhenHeaderMissing_ReturnsFalse()
{
    var criterion = new HeaderValueCriterion();
    criterion.Initialize(new VisitorGroupCriterion
    {
        Model = new HeaderValueCriterionModel
        {
            HeaderName = "X-Wisbar-Segment",
            Condition = HeaderValueCondition.Is,
            HeaderValue = "member",
        },
    });

    var context = new DefaultHttpContext();

    Assert.IsFalse(criterion.IsMatch(new ClaimsPrincipal(new ClaimsIdentity()), context));
}

[TestMethod]
public void IsMatch_DoesNotContain_IsTheNegationOfContains()
{
    var context = new DefaultHttpContext();
    context.Request.Headers["X-Wisbar-Segment"] = "member-gold";

    Assert.IsTrue(Build(HeaderValueCondition.Contains, "member").IsMatch(Anonymous, context));
    Assert.IsFalse(Build(HeaderValueCondition.DoesNotContain, "member").IsMatch(Anonymous, context));
}
```

Assert both directions of every condition. A negated arm that returns the positive result passes any test that only checks the happy path, which is how the bug in `CookieValueCriterion.DoesNotContain` survives.
