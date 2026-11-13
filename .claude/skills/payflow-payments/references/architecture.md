# Payflow architecture reference

Companion to `SKILL.md`. File paths are as they stand in this repo; verify against the code before relying on a detail.

## File map

### `Wisbar.Integrations/PaymentProviders/Payflow/` - protocol layer

| File | Role |
|---|---|
| `PayflowHostedGateway.cs` / `Interfaces/IPayflowHostedGateway.cs` | HTTP calls to Payflow: secure-token creation, Inquiry |
| `PayflowNvpCodec.cs` | Name-value pair encode/decode |
| `PayflowModels.cs` | Request/response shapes |
| `PayflowOptions.cs` / `PayflowOptionsValidator.cs` | Configuration + startup validation |
| `PayflowHostRules.cs` | Validates API host / hosted host / MODE combination |
| `PayflowUrls.cs` | Builds the hosted-page URL |
| `PayflowFieldRules.cs` | Normalizes customer data to Payflow constraints |
| `PayflowSafeLog.cs` | Allowlist projection for logging, both directions |
| `PayflowRetentionOptions.cs` | Retention windows |

### `Wisbar.Business/Features/Payments/Payflow/` - domain layer

| File | Role |
|---|---|
| `PayflowAttemptState.cs` | The state machine |
| `PayflowPaymentAttempt.cs` | The attempt row |
| `IPayflowPaymentAttemptStore.cs` / `SqlPayflowPaymentAttemptStore.cs` | Durable store, compare-and-swap transitions |
| `PaymentStartService.cs` | Creates/resumes an attempt, gets the secure token |
| `PayflowCallbackService.cs` | Turns callbacks into verification triggers |
| `PayflowVerificationService.cs` | Authenticated Inquiry - the only authority on success |
| `PayflowFinalizationService.cs` | Places the Commerce order exactly once |
| `PayflowReconciliationJob.cs` | 15-minute safety net |
| `PayflowRetentionJob.cs` | Daily token scrub and purge |
| `PayflowRequestId.cs` | Deterministic `X-VPS-REQUEST-ID` per operation |
| `PayflowRoutes.cs` | Canonical callback/status route bases |
| `PayflowBillingMapper.cs` | Billing address to Payflow fields |
| `PayflowResolutionOutcome.cs` | How an operator resolved a `ManualReview` |

Elsewhere: `Features/Cart/Services/PayflowCartLock.cs`, `BackgroundJobs/Jobs/PayflowVerifyJob.cs`, `Web/Api/PayflowCallbackEndpoints.cs`, `Web/Controllers/Cms/PaymentsController.cs`.

## State machine

```
Created ──► TokenReady ──► AwaitingResult ──► Verified ──► Finalizing ──► OrderPlaced
                                                                              (terminal-success)

Terminals: Declined · Cancelled · Failed · Expired · ManualReview · Dismissed
```

| State | Meaning |
|---|---|
| `Created` | Row inserted with a fresh SECURETOKENID, before the token comes back |
| `TokenReady` | Secure token stored; the hosted iframe can render |
| `AwaitingResult` | A callback arrived; authenticated Inquiry pending |
| `Verified` | Inquiry confirmed the sale (amount/currency/result matched) |
| `Finalizing` | Exclusive claim while the Commerce order is placed |
| `OrderPlaced` | Purchase order created and linked |
| `Declined` | Gateway declined the card |
| `Cancelled` | Customer cancelled from the hosted page |
| `Failed` | Unrecoverable processing error |
| `Expired` | Token/attempt expired before completion (**token retained**) |
| `ManualReview` | Ambiguous or mismatched - needs an operator |
| `Dismissed` | Operator resolved a `ManualReview` (soft delete) |

### State groupings

```csharp
// PaymentStartService - a hosted Sale has already been charged in these states; the attempt must
// finalize, never be superseded by a fresh attempt/iframe (that would let the same cart be paid twice).
private static readonly PayflowAttemptState[] ChargedStates =
    [PayflowAttemptState.Verified, PayflowAttemptState.Finalizing, PayflowAttemptState.OrderPlaced];

// PayflowCartLock - lock only when a charge may exist. Created/TokenReady carry none (superseded on
// next payment); ManualReview is charged-but-unplaced, so the cart stays locked until resolved.
private static readonly PayflowAttemptState[] ChargeInFlightStates =
    [PayflowAttemptState.AwaitingResult, PayflowAttemptState.Verified, PayflowAttemptState.Finalizing,
     PayflowAttemptState.ManualReview];

// PayflowCallbackService / PayflowVerificationService
private static readonly PayflowAttemptState[] ResolvableFrom =
    [PayflowAttemptState.TokenReady, PayflowAttemptState.AwaitingResult];
```

## The transition primitive

```csharp
/// Atomically move the attempt to toState only if its current state is one of allowedFromStates
/// and its RowVersion still matches. Returns the updated attempt on success, or null if the row
/// changed underneath (lost race, stale RowVersion, wrong source state, or attempt gone).
Task<PayflowPaymentAttempt?> TryTransitionAsync(
    Guid attemptId,
    byte[] expectedRowVersion,
    IReadOnlyCollection<PayflowAttemptState> allowedFromStates,
    PayflowAttemptState toState,
    PayflowAttemptMutation mutation,
    CancellationToken ct = default);
```

A `null` return is the normal way a duplicate callback or a losing concurrent job exits. Handle it as a no-op; do not retry into a second advance and do not log it as an error.

**This is the only sanctioned way to change attempt state.** Widening `allowedFromStates` is a money-safety decision.

## Callbacks

```csharp
group.MapMethods($"{PayflowRoutes.SilentBase}/{{correlation:guid}}", ["POST"], SilentPost).AllowAnonymous();
group.MapMethods($"{PayflowRoutes.ReturnBase}/{{correlation:guid}}", ["GET", "POST"], Return).AllowAnonymous();
group.MapMethods($"{PayflowRoutes.CancelBase}/{{correlation:guid}}", ["GET", "POST"], Cancel).AllowAnonymous();
group.MapMethods($"{PayflowRoutes.ErrorBase}/{{correlation:guid}}", ["GET", "POST"], Error).AllowAnonymous();
```

Properties that must be preserved:

- **Anonymous and cookie-independent.** PayPal calls these server-to-server and cross-site; correlation is the opaque GUID path segment plus durable state.
- **Rate-limited** via the `payflow-callbacks` policy.
- **Body-size capped** at 16 KB, rejecting chunked POSTs (a null `ContentLength` would otherwise skip the cap).
- **Content-type checked**, allowing empty for Return GET→POST hops.
- **Nothing callback-supplied is trusted.**

The error callback is deliberately not treated as failure:

```csharp
// An "error" callback is NOT authoritative: in this hosted config a decline (and potentially an
// approval) also arrives here. Trust only the authenticated Inquiry - trigger verification instead
// of marking Failed, which would mislabel a decline as an error and could drop a captured charge.
public Task NotifyErrorAsync(Guid correlation, CancellationToken ct = default)
    => TriggerVerificationAsync(correlation, null, ct);
```

## Deterministic request ids

```csharp
public static string Token(Guid attemptId) => $"{attemptId:N}t";
public static string Inquiry(Guid attemptId) => $"{attemptId:N}i";
public static string BindingInquiry(Guid attemptId) => $"{attemptId:N}b";
public static string TokenFallbackInquiry(Guid attemptId) => $"{attemptId:N}f";
```

Stable per attempt so a Hangfire retry reuses the same `X-VPS-REQUEST-ID` and Payflow treats it as the same request rather than a new charge. The suffixes keep distinct operations from being deduped against each other. 33 characters, inside Payflow's 36-character limit.

Adding a new gateway operation means adding a new suffix - never reuse one.

## Timing constants

| Constant | Value | Why |
|---|---|---|
| `PaymentStartService.TokenLifetimeMinutes` | 30 | Must be **>=** PayPal's secure-token lifetime. Expiring earlier once dropped real captures |
| `PaymentStartService.StartLockTimeout` | 30s | Serializes concurrent starts per customer; held across the gateway call so the loser resumes |
| `PayflowVerificationService.GiveUpAfter` | 5 min | No queryable transaction by then, expire so the cart unlocks; token retained for late recovery |
| `PayflowFinalizationService.FinalizeLockTimeout` | 30s | Placement is sub-second; only outlasts a concurrent worker |
| `PayflowReconciliationJob.StaleAfter` | 5 min | Give the live flow time before reconciliation intervenes |
| `PayflowReconciliationJob.AlertAfter` | 60 min | Stuck attempt needs operator eyes |
| `PayflowReconciliationJob.BatchLimit` | 100 | Per-run cap |

Reconciliation runs `*/15 * * * *`, retention `30 3 * * *`. Both registered in `Startup.Configure`.

## PCI-safe logging

`PayflowSafeLog` is an allowlist in both directions.

```csharp
// Response allowlist: protocol/result identity plus decline diagnostics.
"TRXTYPE", "TENDER", "RESULT", "RESPMSG", "PNREF", "ORIGRESULT", "ORIGPNREF", "TRANSSTATE", "INVNUM",
"HOSTCODE", "RESPTEXT", "ADDLMSGS",
"AVSADDR", "AVSZIP", "IAVS", "CVV2MATCH", "PROCAVS", "PROCCVV2",
"PREFPSMSG", "POSTFPSMSG",
"DUPLICATE", "AUTHCODE", "CARDTYPE", "AMT", "CURRENCY"

// Request allowlist: opt-in by key so credentials (PWD/USER/VENDOR/PARTNER), the SECURETOKEN bearer
// credential, CUSTREF, and BILLTO*/SHIPTO* PII cannot leak - they are simply absent and dropped.
"TRXTYPE", "TENDER", "AMT", "CURRENCY", "VERBOSITY", "CREATESECURETOKEN", "CAPTURECOMPLETE",
"DISABLERECEIPT", "TEMPLATE", "SECURETOKENID", "ORIGID", "INVNUM",
"RETURNURL", "SILENTPOSTURL", "ERRORURL", "CANCELURL",
"PAGEBUTTONBGCOLOR", "PAGEBUTTONTEXTCOLOR", "LABELTEXTCOLOR",
"PAGECOLLAPSEBGCOLOR", "PAGECOLLAPSETEXTCOLOR"
```

Note `SECURETOKENID` is allowlisted but `SECURETOKEN` is not - the id is a correlation handle, the token is a bearer credential.

Never invert this to a denylist. Adding a key requires confirming it carries no PAN, credential, or PII.

## Configuration

```csharp
public sealed class PayflowOptions
{
    public const string SectionName = "Payflow";

    // Master switch. false -> payment page keeps pre-Payflow behavior, nothing validated.
    public bool Enabled { get; set; }

    // Kill switch (default true). false -> no new attempts start; in-flight callbacks, verification,
    // finalization, and reconciliation keep running so already-charged customers still get orders.
    public bool AcceptNewPayments { get; set; } = true;

    public string Partner { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://pilot-payflowpro.paypal.com";
    public string HostedPageBaseUrl { get; set; } = "https://payflowlink.paypal.com";

    // Hosted-page MODE: TEST (pilot API) or LIVE (live API). Required - the hosted host is always
    // payflowlink.paypal.com and the pilot hosted host rejects the token.
    public string Mode { get; set; } = "TEST";

    public string PublicBaseUrl { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public int ClientTimeoutSeconds { get; set; } = 45;
}
```

Credentials belong in the DXP App Settings portal, read through `IConfiguration`. Never in committed config.

Validation at startup: credentials present, host/MODE combination valid (`PayflowHostRules`), `PublicBaseUrl` absolute HTTPS, `ClientTimeoutSeconds` in 1-120.

## Field normalization

```csharp
/// Payflow name-value fields reject '&' and '=' (they break parsing) and enforce documented max
/// lengths. Country is coerced to a 2-letter ISO code via a known map; anything unrecognized is sent
/// empty rather than fabricated.
public static string Clean(string? value, int maxLength)
```

Sending empty over a guess is deliberate - a fabricated country code produces a wrong AVS result.

## Operator tooling

`PaymentsController` at `/ui/payments`, gated on `Administrators`, `CmsAdmins`, `WebAdmins`. It lists locked (`ManualReview`) attempts and lets an operator record a resolution and dismiss:

```csharp
public enum PayflowResolutionOutcome
{
    Refunded,            // The charge was refunded in PayPal Manager.
    HandledExternally,   // Reconciled outside this tool (e.g. the order was placed manually).
    NoRealCharge         // Investigation showed no real charge was captured.
}
```

Dismissal is a soft delete (`Dismissed`), preserving the audit trail. It uses `IOutOfRequestExecutor` for the work it drives off the request thread.

## DI registration

```csharp
services.AddSingleton<IValidateOptions<PayflowOptions>>(new PayflowOptionsValidator());
services.AddOptions<PayflowOptions>().BindConfiguration(PayflowOptions.SectionName)...;

services.AddScoped<IPayflowPaymentAttemptStore, SqlPayflowPaymentAttemptStore>();
services.AddScoped<IPayflowCartLock, PayflowCartLock>();
services.AddScoped<IPayflowVerificationService, PayflowVerificationService>();
services.AddScoped<IPayflowFinalizationService, PayflowFinalizationService>();
services.AddScoped<IPayflowCallbackService, PayflowCallbackService>();
services.AddScoped<PayflowVerifyJob>();
services.AddScoped<PayflowReconciliationJob>();
services.AddScoped<PayflowRetentionJob>();

// Typed client; inert unless PayflowOptions.Enabled and the start service calls it.
services.AddHttpClient<IPayflowHostedGateway, PayflowHostedGateway>(...);
```

Schema comes from migrations 19 (table), 21 (indexes), 22 (order link), 23 (callback PNREF), 24 (PNREF unique), 38 (resolution). Add schema changes as new steps - see `optimizely-commerce-migration-step`.

## Testing

`Wisbar.Tests/Features/Payments/` plus `Features/BackgroundJobs/PayflowVerifyJobTests.cs`. `InMemoryPayflowPaymentAttemptStore` lets services be tested without SQL.

Existing coverage to extend rather than duplicate: billing mapper, callback service, cart lock, field rules, finalization, gateway inquiry, manual review store, migration 38, NVP codec, options validator, reconciliation alerts, request id, retention job, safe log, token fields, URLs, verification service, payment start.

Highest-value cases for any change here:

1. Duplicate callback (Return **and** Silent Post) is a no-op.
2. `TryTransitionAsync` returning null is handled as a lost race.
3. A charged attempt is resumed, never superseded, by a new start.
4. A retry reuses the same request id.
5. `PayflowSafeLog` still drops every non-allowlisted key.
