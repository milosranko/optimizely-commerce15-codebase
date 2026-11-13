---
name: payflow-payments
description: Work on the PayPal Payflow hosted-checkout integration - the payment attempt state machine, secure-token start, callbacks, authenticated Inquiry verification, order finalization, reconciliation and retention jobs, manual review, cart locking, PCI-safe logging, and the kill switch. Use whenever asked to change, debug, extend, or review anything under Features/Payments/Payflow, PaymentProviders/Payflow, the payflow callback endpoints, the Payflow jobs, or when investigating a customer charged without an order, a stuck or duplicate payment, a declined card, or a payment page problem.
---

# Working on the Payflow integration

This is a mature, safety-critical subsystem: real money, PCI scope, and a customer on the other end who may already have been charged. It is engineered around one principle:

> **Never trust anything the browser or the callback tells you.** Success is decided only by an authenticated server-to-server Inquiry against PayPal, and every state change is a compare-and-swap.

Read `.claude/rules/commerce.md` first. Per CLAUDE.md, **payments changes require a plan before implementing** - present the plan and get agreement before editing.

## Step 1: Orient before changing anything

```bash
ls src/Wisbar.Business/Features/Payments/Payflow/
ls src/Wisbar.Integrations/PaymentProviders/Payflow/
ls src/Wisbar.Tests/Features/Payments/
```

| Layer | Location | Role |
|---|---|---|
| Gateway / protocol | `Wisbar.Integrations/PaymentProviders/Payflow/` | NVP codec, HTTP client, options, URL and field rules, safe logging |
| Domain services | `Wisbar.Business/Features/Payments/Payflow/` | State machine, start, callback, verification, finalization, jobs |
| Store | `SqlPayflowPaymentAttemptStore` + `PayflowPaymentAttempt` | Durable attempt rows in a dedicated SQL table |
| Callbacks | `Wisbar.Web/Api/PayflowCallbackEndpoints.cs` | Anonymous, rate-limited, cookie-independent |
| Operator UI | `Wisbar.Web/Controllers/Cms/PaymentsController.cs` | Admin-gated manual review and dismissal |
| Tests | `Wisbar.Tests/Features/Payments/` | ~20 files. The safety net - keep it green |

There are ~20 test files here for a reason. Any change to this subsystem ships with tests.

## Step 2: The state machine is the contract

`PayflowAttemptState`: `Created` → `TokenReady` → `AwaitingResult` → `Verified` → `Finalizing` → `OrderPlaced`, with terminals `Declined`, `Cancelled`, `Failed`, `Expired`, `ManualReview`, and `Dismissed`.

**Every transition goes through `IPayflowPaymentAttemptStore.TryTransitionAsync`**, which is a compare-and-swap on `RowVersion` plus an allowed-source-state guard. That is the single mechanism making duplicate callbacks and concurrent jobs safe no-ops. A `null` return means you lost the race - that is a normal outcome to handle, not an error to log loudly.

Never write a state change any other way, and never widen an `allowedFromStates` set without working out which double-advance it now permits.

Three groupings encode real money rules - change them only with a specific reason:

- **Charged states** (`Verified`, `Finalizing`, `OrderPlaced`) - a hosted Sale has already taken money. Such an attempt must finalize; it must never be superseded by a fresh attempt, or the same cart gets paid twice.
- **Charge-in-flight states** (`AwaitingResult`, `Verified`, `Finalizing`, `ManualReview`) - the cart is locked against edits while these hold.
- **`ManualReview`** - charged but unplaced. Never purged by retention, cart stays locked, an operator must resolve it.

## Step 3: The flow

1. **Start** (`PaymentStartService`) - the page POSTs to `/api/checkout/payflow/start`, so the gateway call is off the render path. A distributed lock serializes concurrent starts for one customer, so a double-submit or second tab resumes the existing attempt rather than minting a second payable token for the same cart. An existing charged attempt is resumed, never replaced.
2. **Hosted page** - the customer pays on PayPal's page in an iframe. Card data never touches this application, which is the whole point of hosted checkout and the reason PCI scope stays small.
3. **Callbacks** (`PayflowCallbackEndpoints`) - Return, Silent Post, Cancel, Error. Anonymous, rate-limited, body-size-capped, correlated only by an opaque GUID path segment and durable server state. **They are triggers, not results.** An "error" callback is not authoritative - in this hosted configuration a decline (and possibly an approval) also arrives there - so it triggers verification rather than marking the attempt failed.
4. **Verification** (`PayflowVerificationService`) - an authenticated Inquiry confirms amount, currency and result. This is the only thing that decides success.
5. **Finalization** (`PayflowFinalizationService`) - a distributed lock plus the `Finalizing` claim ensures the Commerce order is placed exactly once.
6. **Reconciliation** (`PayflowReconciliationJob`, every 15 min) - re-drives dropped callbacks, resumes crashed finalizations, expires abandoned attempts. Everything it calls is idempotent, so overlapping runs are safe.
7. **Retention** (`PayflowRetentionJob`, daily 03:30) - scrubs expired secure tokens, purges old terminal attempts, never touches `ManualReview`.

## Step 4: Rules that exist because breaking them cost money

Each of these is load-bearing. If a change requires violating one, stop and raise it.

- **Only the authenticated Inquiry decides success.** Never mark an attempt paid from callback fields, query strings, or anything the browser supplied.
- **Timeouts must not shorten a token's life.** `TokenLifetimeMinutes` must stay at or above PayPal's secure-token lifetime; expiring early once dropped real captures.
- **Expire retains the token.** An expired attempt keeps its token so a late capture is still recoverable through the recovery path.
- **Request ids are deterministic per operation** (`PayflowRequestId`) so a Hangfire retry reuses the same `X-VPS-REQUEST-ID` and Payflow treats it as the same request, not a new charge. Distinct suffixes (`t`, `i`, `b`, `f`) keep separate operations from being deduped against each other.
- **The kill switch is `AcceptNewPayments`, not `Enabled`.** Setting it false stops new attempts while in-flight callbacks, verification, finalization and reconciliation keep running, so already-charged customers still get their orders. Never stop payments by turning `Enabled` off.
- **Reconciliation's repeated alerts are intentional.** A stuck attempt re-alerts every run to stay visible; that is not a throttling bug to fix.

## Step 5: PCI and logging

- **Log through `PayflowSafeLog` only.** It is an **allowlist by key**, in both directions, deliberately so a newly added secret or PII field cannot leak. Never replace it with "serialize everything and blank a denylist", and never add a field to the allowlist without checking it carries no PAN, credential, or PII. `PWD`, `USER`, `VENDOR`, `PARTNER`, `SECURETOKEN`, `CUSTREF` and all `BILLTO*`/`SHIPTO*` are absent by design.
- **Credentials live in the DXP App Settings portal**, read at runtime through `IConfiguration`. Never commit them, never echo them into logs, tests, or a status endpoint.
- `PaymentPageIntegrityMonitorJob` verifies served payment scripts against an approved baseline every three days (PCI DSS 4.0 Req 11.6.1). It is skipped only in Development, where a bare checkout has no built `wwwroot/js`. It is a security control, not an opt-in feature.
- Adding a script or an external resource to the payment page changes the baseline - update it deliberately and say so.

## Step 6: Configuration

`PayflowOptions` (section `Payflow`), validated at startup by `PayflowOptionsValidator`:

`Enabled`, `AcceptNewPayments`, `Partner`, `Vendor`, `User`, `Password`, `ApiBaseUrl`, `HostedPageBaseUrl`, `Mode`, `PublicBaseUrl`, `CurrencyCode`, `ClientTimeoutSeconds`.

The one that surprises people: **the hosted host is always `payflowlink.paypal.com`; `MODE` selects TEST or LIVE, not the host.** The pilot hosted host rejects the token. `ApiBaseUrl` does switch between pilot and live. `PublicBaseUrl` must be absolute HTTPS - it builds the callback URLs PayPal calls back on.

## Step 7: Testing

```bash
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

`InMemoryPayflowPaymentAttemptStore` exists so services can be tested without SQL. Cover, at minimum:

- The duplicate-callback no-op (Return and Silent Post both arriving).
- The lost compare-and-swap race (`TryTransitionAsync` returning null).
- A charged attempt not being superseded by a new start.
- Retry reusing the same request id.
- Anything you touched in `PayflowSafeLog` still dropping every non-allowlisted key.

## Step 8: Verify and report honestly

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Be explicit about what you did and did not exercise. You cannot place a real transaction from here, so say so - do not imply an end-to-end payment was tested. Note anything that needs pilot verification against PayPal, and never suggest testing with real card numbers.

## Diagnostic map

| Symptom | Look at |
|---|---|
| Charged but no order | Attempt state - `Verified`/`Finalizing` stuck, or `ManualReview`. Reconciliation should re-drive it; the admin Payments screen resolves it |
| Cart won't unlock | An attempt in a charge-in-flight state; `ManualReview` holds it until an operator resolves |
| Duplicate charge risk | Charged-states handling in `PaymentStartService`, and the start distributed lock |
| Decline looks like an error | Expected - the error callback is not authoritative; check the Inquiry result and `RESPMSG`/`HOSTCODE` |
| Attempt expired too early | `TokenLifetimeMinutes` versus PayPal's token lifetime |
| Field rejected by gateway | `PayflowFieldRules` - `&`/`=` stripped, max lengths, country coerced to ISO alpha-2 or sent empty |

## Checklist

- [ ] Plan presented and agreed before implementing (payments rule in CLAUDE.md)
- [ ] All state changes go through `TryTransitionAsync` with a deliberate `allowedFromStates`
- [ ] Success determined only by authenticated Inquiry, never callback data
- [ ] Idempotent: duplicate callbacks and overlapping job runs are no-ops
- [ ] Charged states never superseded; `ManualReview` never auto-purged
- [ ] Deterministic request id preserved on retry paths
- [ ] Logging goes through `PayflowSafeLog`; no secrets or PII added to the allowlist
- [ ] No credentials in code, config, tests, or logs
- [ ] Payment-page script changes reflected in the PCI baseline
- [ ] Tests added or updated; full suite green
- [ ] Report states plainly what was not exercised against the real gateway

Details and code: `references/architecture.md`.
