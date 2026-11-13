---
name: optimizely-scheduled-job
description: Add an Optimizely CMS 12 scheduled job - a [ScheduledPlugIn] class deriving from ScheduledJobBase, run and monitored from the CMS admin Scheduled Jobs screen. Covers the attribute and its GUID, interval and start time, the status string, stop and progress support, running without a request context, long catalog walks, operator settings, and reading execution history via IScheduledJobLogRepository. Use whenever asked for an Optimizely scheduled job, a CMS admin job, an import/export or digest job, or a periodic task an administrator needs to see, run on demand, and check the history of.
---

# Adding an Optimizely scheduled job

An Optimizely scheduled job is a `[ScheduledPlugIn]` class deriving from `ScheduledJobBase`, discovered by attribute and managed from **CMS admin → Scheduled Jobs**. Administrators can run it on demand, change its interval, stop it, and read its execution history.

**Scope check before you start.** This repo also runs Hangfire (`Wisbar.Business/Features/BackgroundJobs/`) for unattended cron maintenance and for request-triggered fire-and-forget work. Use an Optimizely scheduled job when an operator needs to *see and drive* it in the CMS. If the work is triggered by a user action, or is invisible plumbing on a cron, it belongs in Hangfire instead - see the `hangfire-background-jobs` skill, and stop and say so rather than building the wrong one.

## Step 1: Learn this repo's conventions first

```bash
grep -rln "ScheduledPlugIn" --include=*.cs .
find . -path "*ScheduledJobs*" -name "*.cs"
```

Read one existing job end to end before writing. `references/templates.md` has the full code; the repo is the authority.

| Part | Location |
|---|---|
| Jobs | `Wisbar.Business/ScheduledJobs/` |
| Operator settings | `SiteSettingsPage`, tab `Globals.Tabs.ScheduleJobSettings` |
| Services the jobs call | `Wisbar.Business/Synchronization/Services/` |

Existing jobs, worth knowing before you add another: `ProfilesImportJob` (SortIndex 1, 00:30, every 6h), `ProductsImportJob` (SortIndex 2, 00:00, every 6h), `ImportLogsNotificationJob` (SortIndex 3, 06:00, daily).

## Step 2: Write the job

- `[ScheduledPlugIn]` on a class deriving from `ScheduledJobBase`. **No DI registration** - Optimizely discovers it by attribute - but constructor injection works normally.
- **Stable GUID.** It keys the job's schedule and its history rows; changing it orphans both and re-registers the job as new. Expose it as `internal const string JobGuid` when anything else needs to read this job's history.
- `DisplayName` is prefixed `[Wisbar]` here so site jobs sort away from Optimizely's built-ins. Match the host project's prefix.
- Keep the job class thin. It should own scheduling concerns - status, stop signal, error reporting - and delegate the real work to an injected service that can be unit tested without the job shell.

## Step 3: Set the schedule deliberately

`SortIndex`, `InitialTime`, `IntervalType`, `IntervalLength`, `DefaultEnabled`, `Restartable`.

- **Stagger against sibling jobs.** Products starts at `00:00:00` and Profiles at `00:30:00` precisely so two six-hourly imports never hammer the same external system at once. Pick a start time that does not collide.
- **The attribute values are only defaults.** Once the job is registered in a database, the admin UI owns its interval and enabled state - editing the attribute will not move an existing schedule on Integration, Preproduction, or Production. Say so when the change was meant to alter a deployed schedule; someone has to change it in each environment's admin UI.
- `DefaultEnabled = false` for anything expensive or destructive, so it does not start running the moment it deploys.

## Step 4: The status string is the deliverable

`Execute()` returns the text the admin UI shows and stores in history. It is the only thing an operator sees a week later.

```csharp
return $"Import complete. Processed: {result.Processed}, Created: {result.Created}, " +
       $"Updated: {result.Updated}, Skipped: {result.Skipped}, Failed: {result.Failed}";
```

Return real counts, never "Done" or "OK". When the job did nothing, say why - `ImportLogsNotificationJob` returns "No notification recipients configured in Site Settings. Email not sent." rather than reporting success.

**Log the exception and rethrow.** The throw is what marks the run failed; swallowing it makes a broken job look healthy in the history.

`Execute()` is synchronous. Bridge async work with `.GetAwaiter().GetResult()`, as the existing jobs do.

## Step 5: Stop and progress

Long jobs must be interruptible:

- Set `IsStoppable = true` in the constructor.
- Override `Stop()` to set a `_stopSignaled` flag.
- Pass the flag into the work as a callback (`() => _stopSignaled`) so it can bail at a safe point, between items rather than mid-write.
- Report progress with `OnStatusChanged(status)` so the admin screen is not silent for twenty minutes.

## Step 6: A job has no request context

This is the trap that makes jobs behave differently from everything else in the site. There is no `HttpContext`, so anything ambient is unreliable:

- **`SiteDefinition.Current` may be null or wrong.** Resolve the site explicitly - `ProductsImportService` injects `ISiteDefinitionResolver` and calls `GetByHostname(new Uri(options.SiteHost).Authority, false)`, guarding the null it returns when the configured host matches no registered site.
- **`ContentLanguage.PreferredCulture` is not the visitor's language.** Pass the culture explicitly.
- **The current principal is not a member.** Anything member-aware needs its identity passed in, not inferred.

Reading Site Settings from a job goes through the resolved start page, not `SiteDefinition.Current`.

## Step 7: Long catalog walks

A job that walks the whole catalog can fail with `Cannot create a command without an existing connection`, `Internal connection fatal error`, or `There is already an open DataReader associated with this Connection`. The database executor is cached against an ambient request cache, which dies mid-walk.

```csharp
using (ExecutionContext.SuppressFlow())
    return Task.Run(() => Sweep()).GetAwaiter().GetResult();
```

Also wrap per-item loads in a per-item try/catch, so one bad row logs and continues instead of aborting the pass. `ServiceLocator.SetScopedServiceProvider` does **not** fix this - it swaps the DI provider, not the connection context. **Do not** enable MARS; `MultipleActiveResultSets=False` is deliberate for CMS 12.

## Step 8: Operator settings, not constants

Start dates, recipient lists, and thresholds an administrator may want to change belong on `SiteSettingsPage` under `Globals.Tabs.ScheduleJobSettings` - not in code, not in appsettings. Existing examples: `ProductsImportStartDate`, `ProfilesImportStartDate`, `NotificationEmailAddresses` (read through `INotificationRecipientProvider`).

## Step 9: Reading execution history

Inject `IScheduledJobLogRepository` - it is in the **`EPiServer.Scheduler`** namespace, not `EPiServer.DataAbstraction`:

```csharp
var page = await _logRepository.GetAsync(jobGuid, startIndex: 0, maxRows: 4);
var logs = page.PagedResult.OrderByDescending(l => l.CompletedUtc).ToList();
```

`ScheduledJobLogItem` gives `Status`, `Message`, `CompletedUtc`, `Duration`, `Trigger`, `Server`. HTML-encode `Message` before putting it in an email - it is arbitrary text from a return value or an exception. This is how `ImportLogsNotificationJob` builds its daily digest.

## Step 10: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Test the injected service, not the job shell - the shell is deliberately thin. Then report which of these you actually checked:

- Job appears under CMS admin → Scheduled Jobs, with the intended display name, interval, and enabled state.
- A manual run finishes and returns a status string that would still be useful a week later.
- Stop actually stops it, at a safe point, without half-written data.
- A forced failure shows as Failed in history with a usable message.
- It behaves correctly with no request context - the right site and language, not whatever was ambient.
- Nothing operator-tunable got hardcoded that belongs on the Site Settings jobs tab.

## Checklist

- [ ] Optimizely scheduled job is genuinely the right system (not Hangfire)
- [ ] `[ScheduledPlugIn]` with a fresh stable GUID and `[Wisbar]`-prefixed display name
- [ ] Start time staggered against sibling jobs; interval and `DefaultEnabled` deliberate
- [ ] Real work delegated to an injected, testable service
- [ ] `Execute()` returns a summary with counts; exceptions logged **and** rethrown
- [ ] `IsStoppable`, `Stop()`, and `OnStatusChanged` for anything long-running
- [ ] Site and language resolved explicitly, never from ambient request state
- [ ] Long walks use `ExecutionContext.SuppressFlow()` and per-item try/catch
- [ ] Operator-tunable values on the Site Settings jobs tab
- [ ] Deployed-schedule change flagged as needing an admin UI edit per environment
- [ ] Solution builds and tests pass

Full code templates: `references/templates.md`.
