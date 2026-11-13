---
name: github-actions-ci-cd
description: Set up or change GitHub Actions CI/CD for the Wisbar solution - the feature-branch CI workflow, the build-once deploy pipeline to Optimizely DXP (Integration, Preproduction) through the EpiCloud PowerShell module, .nupkg packaging and versioning, the PCI 6.4.3(b) package checksum handoff, frontend-before-publish ordering, NuGet and npm caching, Linux vs Windows runners, bounded deployment polling, secrets and token permissions, and what is and is not actually gated. Use whenever asked to add or edit a workflow, job, or step, add a CI check or make CI a merge gate, deploy to a new environment, speed up or cut the cost of Actions, debug a failing build or deploy run, or explain how code reaches DXP.
---

# GitHub Actions CI/CD

Code reaches Optimizely DXP through two workflows and one composite action. Two facts drive every rule below:

> **One package is built once and promoted unchanged.** Its SHA-256 travels as a job output and is verified before every deploy. That chain is the documented PCI 6.4.3(b) integrity method for payment-page assets, so a pipeline change is a compliance-evidence change.

> **Every runner minute is billed.** The repo is private and Windows runners cost 2x. The org has already hit ~90% of its included minutes once (`docs/analysis/github-actions-minutes.md`).

**Scope check.** Almost every request is a change to the existing pipeline. Extend `build-deploy-github.yml` or `branch-protection-pipeline.yml` rather than adding a parallel workflow that rebuilds or redeploys. Deployment changes are plan-first (CLAUDE.md).

## Step 1: Orient

```bash
ls .github/workflows .github/actions
gh api repos/State-Bar-of-Wisconsin/wisbar/environments --jq '.environments[] | {name, protection_rules, deployment_branch_policy}'
gh api repos/State-Bar-of-Wisconsin/wisbar/branches/develop/protection --jq '{checks: .required_status_checks.contexts, reviews: .required_pull_request_reviews.required_approving_review_count}'
gh secret list -R State-Bar-of-Wisconsin/wisbar
gh run list -R State-Bar-of-Wisconsin/wisbar --limit 10
```

Repository settings live outside git and drift. Re-run these before stating what is gated. In Windows PowerShell 5.1, `gh --jq` expressions containing double quotes get mangled - run `gh` through Bash.

| File | Actions tab name | Trigger | Runner | Does |
|---|---|---|---|---|
| `workflows/branch-protection-pipeline.yml` | `.NET Core CI` | push to `feature/*`, `release/*`, `hotfix/*`, `bugfix/*`; skips `**/*.md`, `docs/**`, `.claude/**` | `ubuntu-latest` | restore sln, build Web, test. No frontend |
| `workflows/build-deploy-github.yml` | `Optimizely CI/CD Pipeline` | push to `develop`, `main` | build `ubuntu-latest`; deploys `windows-2025-vs2026` | frontend + CMS build, test, publish, package, checksum, deploy |
| `actions/verify-package-checksum/action.yml` | - | composite | caller's | fail-closed hash compare |

Recent timings: CI ~2 min, deploy build ~3.5 min, Integration deploy ~10 min.

## Step 2: What is actually gated

Verified 2026-09-10. Several docs claim more than this; trust the Step 1 commands over any doc, including this one.

| Claim you will see | Reality |
|---|---|
| "Manual approval gates" | No GitHub gate. The `integration` and `preproduction` environments have **no protection rules and no branch policy**; the job-level `if: github.ref == ...` is the only branch guard. The human gate is DXP's: Preproduction stops in `AwaitingVerification` and someone Completes or Resets it in the DXP portal |
| The pipeline deploys Production | No job targets Production. It is promoted outside this workflow |
| DXP credentials are environment secrets | They are **repository** secrets (`OPTIMIZELY_DEPLOYMENT_KEY`, `OPTIMIZELY_DEPLOYMENT_SECRET`, `OPTIMIZELY_PROJECT_ID`); the environments hold none |
| CI is a merge gate | `develop` and `main` require one review and **no status checks**. CI triggers on push to working branches, not `pull_request` |
| CI builds on Windows | Build jobs moved to Ubuntu in August 2026; only the deploy jobs are Windows |
| Version is SEMVER from `GlobalAssemblyInfo.cs` | That file is linked into no project. Every `.csproj` sets `AssemblyVersion` `1.0.*`, giving packages like `wisbar.cms.app.1.0.9749.15635.nupkg` |

Adding required reviewers, a branch policy, or a required status check is a repository-settings change: confirm with the user, and make it in settings, not YAML.

## Step 3: Invariants of the deploy pipeline

Break none of these without saying so explicitly.

1. **Build once.** Deploy jobs never restore, build, or publish. They download the `cms-nuget` artifact.
2. **Checksum as run metadata.** The build job emits `package-sha256` as a job output. Never put the hash in a file inside the artifact - that check is self-referential and detects corruption, not tampering. Every deploy job starts: sparse-checkout `.github/actions` → download → **verify** → everything else.
3. **Frontend before publish.** `wwwroot/css`, `js`, `ReactComponents`, `assets`, `fonts` are gitignored build output. Publishing without `npm run build` ships a site whose payment page 404s its scripts. `npm run build` runs ESLint first, so a lint error fails the deploy build.
4. **Tests before publish.** A red test must stop packaging.
5. **Version from `AssemblyName.Version`.** Win32 `FileVersion` is empty on Linux because `GenerateAssemblyFileVersionAttribute` is false. Keep the `wisbar.cms.app.<version>.nupkg` shape; `.cms.app.` is the app-type segment DXP expects in a code package name.
6. **Integration is `-DirectDeploy`; Preproduction is not.** Omitting `-DirectDeploy` is what creates the verification slot. The Preproduction wait step treats `AwaitingVerification` as success (exit 0) and writes the slot URL to the step summary.
7. **Bounded everything.** `timeout-minutes` on every job, a 30-minute deadline inside each poll loop, a 30s poll. A shorter poll saves nothing - billing follows the deployment's real duration - and adds DXP API calls.
8. **Package artifact `retention-days: 1`.** It is ~200 MB and only a same-run handoff; default retention overflowed the storage quota.

## Step 4: Runners

Default to `ubuntu-latest`. Windows only with a validated reason - today only the deploy jobs, which have not had their own Linux validation pass (a deferred item in the minutes analysis; EpiCloud itself is cross-platform).

Linux jobs that run tests need:

```yaml
env:
  LANG: en_US.UTF-8
```

Without it the runner's invariant locale makes `CurrentUICulture` Invariant and localized-string tests resolve empty strings. The other Linux trap met here: `Uri.TryCreate("/path", UriKind.Absolute)` succeeds as a `file://` URI on Linux. Fix code like that in source, not in the workflow.

`shell: pwsh` works on Ubuntu runners; Win32 file metadata and Windows-only cmdlets do not.

## Step 5: Restore, cache, test

- **Feeds.** `src/nuget.config` already lists nuget.org and the Optimizely feed, which needs no credentials; CI restores with no extra step. Do not copy the deploy build's "Add Optimizely NuGet source" step into new workflows.
- **NuGet cache.** No `packages.lock.json` exists, so `setup-dotnet`'s `cache: true` is unavailable. Use `actions/cache` on `~/.nuget/packages` keyed on `hashFiles('**/*.csproj', 'src/nuget.config')` with a `${{ runner.os }}-nuget-` restore key. Versions are pinned inline in every `.csproj`, so the key is complete; introducing central package management or lock files means re-keying.
- **npm.** `actions/setup-node` with `cache: 'npm'` and `cache-dependency-path: src/Wisbar.Frontend/package-lock.json`. Always `npm ci`, never `npm install` - the lockfile's integrity hashes are part of the PCI evidence. `NODE_VERSION` is `20.x`, past upstream end of life (April 2026); bumping it is its own tested change.
- **Tests.** `dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj --configuration Release`. `setup-dotnet` with `8.0.x` still leaves SDK 10.x on the runner and `global.json` pins no SDK, so CI runs in **Microsoft.Testing.Platform mode**. VSTest-era flags (`--logger`, `--nologo`, `-v q`) are forwarded to the test executable and fail with "Zero tests ran", exit code 5 - a flag error, not a test failure. Try any new test flag locally on SDK 10 first.

## Step 6: Adding a job or step

**Build-side checks** (lint, analyzers, extra tests) belong in **both** workflows if they should gate feature branches and deploys; in the deploy build, place them before `Publish CMS project`. CI does not build the frontend, so today an ESLint error first surfaces as a failed `develop` deploy build - a `frontend-lint` job in CI is the fix (`references/templates.md` A).

**Making CI a merge gate** needs a `pull_request` trigger on `develop`/`main` plus the job name (`build-and-test`) as a required status check in settings. Two traps: a required check from a workflow skipped by `paths-ignore` stays Pending and blocks the merge, and keeping the `push` trigger as well runs CI twice per PR.

**A new deploy target** copies the job anatomy in `references/templates.md` C:

- Its own `environment:`, `needs: build`, an explicit `if:` branch guard, and the checksum verify step first.
- Choose `-DirectDeploy` or a verification slot deliberately. Anything customer-facing gets the slot.
- If it must not run unattended, that needs required reviewers on the GitHub environment - a settings change to confirm with the user.
- Production is plan-first. Promoting the verified Preproduction code (`Start-EpiDeployment -SourceEnvironment Preproduction -TargetEnvironment Production -SourceApp cms`) keeps build-once intact; confirm the parameters against the installed EpiCloud module before relying on them.

**Concurrency.** Feature-branch CI uses `cancel-in-progress: true`. Never set that on anything that deploys: cancelling the runner does not cancel the DXP deployment, which carries on server-side, and DXP will not start a second deployment to an environment that already has one in progress. If the deploy workflow gets a concurrency group, it is `cancel-in-progress: false`.

## Step 7: Security

- The repo default `GITHUB_TOKEN` permission is read-only. A job needing more declares a minimal `permissions:` block; never widen the repo default.
- Only first-party `actions/*` are used, pinned to major tags. **Pin any third-party action to a full commit SHA.**
- No `pull_request_target`. Never echo a secret or write one to `GITHUB_ENV`, `GITHUB_OUTPUT`, or the step summary. In new steps, pass secrets through step-level `env:` instead of interpolating `${{ secrets.X }}` into script text.
- App secrets (connection strings, API keys) live in the DXP App Settings portal, never in workflow YAML or committed `appsettings.*.json`.

## Step 8: Keep the evidence in step

A change to `build-deploy-github.yml` or the checksum action changes compliance evidence. Update in the same PR:

- `docs/pci/first-party-asset-integrity-method.md` - its controls cite workflow line numbers, runner OS, Node version, and where secrets live, and it asks for re-review whenever the pipeline changes.
- `docs/analysis/github-actions-minutes.md` - for runner, trigger, caching, or retention changes.
- `CLAUDE.md` "CI/CD & branches" - if the pipeline's shape changes.

## Step 9: Validate before it runs

`dotnet build` never touches workflow YAML. In rough order of cost:

1. **Lint.** `actionlint` is not installed locally; Docker Desktop is, but its engine must be started first (`references/templates.md` F). If it cannot run, say the workflow was not linted.
2. **Reproduce Linux behavior** for a build or test change in a container before pushing - that is how the Ubuntu move was validated.
3. **CI changes:** push a `feature/*` branch. A push that touches only `.md`, `docs/**`, or `.claude/**` is skipped; a workflow file change is not.
4. **Deploy-pipeline changes** run only from `develop` or `main`. Land them on `develop` first so Integration exercises them, follow with `gh run watch`, and never let `main` be the first run of a changed deploy job.

Pushing, merging, and re-running a deploy are outward-facing - confirm with the user first.

## Step 10: Debugging a red run

```bash
gh run list -R State-Bar-of-Wisconsin/wisbar --workflow "Optimizely CI/CD Pipeline" --limit 5
gh run view <id> -R State-Bar-of-Wisconsin/wisbar --log-failed
```

| Symptom | Usual cause |
|---|---|
| Passes locally, fails in CI with empty strings | `LANG: en_US.UTF-8` missing on a Linux job |
| "Zero tests ran", exit code 5 | A VSTest flag passed to `dotnet test` under MTP |
| `NETSDK1152` on publish | Duplicate publish path; see the `module.config` `Content Remove` in `Wisbar.Web.csproj` |
| `DLL not found` or empty version | Publish output path changed, or Win32 `FileVersion` read on Linux |
| Checksum mismatch or empty expected hash | Deploy job lost `needs: build`, the output was renamed, or something rewrote `./nupkg` |
| Payment page 404s its scripts after deploy | Frontend build skipped or moved after publish |
| ESLint error fails the `develop` build only | CI has no frontend step (Step 6) |
| Wait step timed out | The DXP deployment is still running server-side - check the portal before re-running, or the next deploy collides with it |
| Preproduction job green, site unchanged | It is sitting in `AwaitingVerification`; Complete it in the DXP portal |

## Verify

Report which of these you actually checked:

- actionlint is clean on every changed workflow.
- Changed CI ran green on a feature branch; changed deploy steps ran green on `develop` into Integration.
- Every deploy job still runs download → verify checksum → deploy, and none builds.
- `timeout-minutes` is on every job and poll loops are still bounded.
- No secret is echoed; any third-party action is SHA-pinned.
- The PCI integrity-method doc and minutes analysis match the new workflow.

## Checklist

- [ ] Extends an existing workflow rather than duplicating build or deploy
- [ ] Gating claims re-verified against live settings, not docs
- [ ] Build once; deploy jobs only download the `cms-nuget` artifact
- [ ] Checksum emitted as a job output and verified first in every deploy job
- [ ] `npm ci` + `npm run build` before `dotnet publish`; tests before publish
- [ ] Linux by default; `LANG: en_US.UTF-8` on Linux test jobs
- [ ] NuGet and npm caches keyed on `.csproj` + `nuget.config` and `package-lock.json`
- [ ] No VSTest-only flags on `dotnet test`
- [ ] `timeout-minutes` on every job; bounded poll loops; `retention-days: 1` on the package
- [ ] No `cancel-in-progress: true` on anything that deploys
- [ ] Minimal `permissions:`; third-party actions SHA-pinned; secrets via `env:`, never echoed
- [ ] `-DirectDeploy` vs verification slot chosen deliberately for any new target
- [ ] PCI evidence doc, minutes analysis, and CLAUDE.md updated where affected
- [ ] Settings changes (reviewers, required checks) confirmed with the user and made in settings

Workflow and job templates: `references/templates.md`.
