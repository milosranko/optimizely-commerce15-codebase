# Optimizely Commerce Demo

> Enterprise e-commerce platform built on Optimizely CMS 13 and Commerce 15 (.NET 10), with a React frontend.

## Quick Context

Optimizely Commerce Demo is an enterprise-grade e-commerce application for the fashion retail domain. Built on .NET 10 with Optimizely CMS 13 and Commerce 15, it demonstrates a layered architecture with a React 19 SPA frontend. The platform supports product catalog management, search (via Optimizely Graph, which replaced EPiServer Find — ADR-008), PayPal payment integration, and content scaffolding for demo purposes.

## F3 Framework Reference

This project uses F3 (Foundation-First Framework). The complete framework
documentation is in the project root: `F3_FRAMEWORK.md`

**Claude: Read @F3_FRAMEWORK.md when you need:**
- Complete templates for any F3 document
- Detailed methodology explanations
- Implementation prompts and examples
- Troubleshooting guidance

**Current F3 Version:** 2.3.2

## Tech Stack

- **Runtime:** .NET 10, C# 14 (SDK pinned via `/global.json`)
- **CMS:** Optimizely CMS 13 (EPiServer.CMS 13.6.0)
- **Commerce:** Optimizely Commerce 15 (EPiServer.Commerce 15.5.0)
- **Search:** Optimizely Graph / Content Graph (enabled only when keys are configured; replaced EPiServer Find)
- **Optional CMS 13 features:** Opti ID (back office), Content Manager, Opal — credentials via user-secrets / DXP App Settings
- **Frontend:** React 19 + Vite 7 (JSX)
- **Database:** SQL Server (via EPiServer)
- **ORM/Data:** Optimizely Content Repository
- **Mapping:** Mapster
- **DI:** Microsoft.Extensions.DependencyInjection + Scrutor
- **Payments:** PayPal (PayPalServerSDK v2.2.0)
- **Testing:** MSTest 4 on Microsoft.Testing.Platform
- **Infrastructure:** Azure Blob Storage, Optimizely DXP (non-dev)

## Architecture Overview

See /docs/architecture.md for full system design.

Key Components:
- **Commerce.Demo.Web**: ASP.NET Core host, controllers, API endpoints, Startup.cs
- **Commerce.Demo.Business**: Business logic — search, site settings, sitemaps, caching
- **Commerce.Demo.Optimizely.Core**: Optimizely infrastructure — all NuGet packages, rendering, editor descriptors
- **Commerce.Demo.Optimizely.ContentTypes**: Content type definitions (pages, products, categories, variants, blocks)
- **Commerce.Demo.Integrations**: External integrations (PayPal payment gateway)
- **Commerce.Demo.Frontend**: React 19 + Vite 7 SPA
- **Commerce.Demo.Tests**: MSTest unit tests

## Build & Test Commands

```bash
# Build the full solution (includes npm install/build for frontend)
dotnet build src\Commerce.Demo.slnx -c Release

# Run tests (MSTest on Microsoft.Testing.Platform; .NET 10 SDK needs --project, see /global.json)
dotnet test --project src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj

# Run a single test by fully qualified name
dotnet test --project src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj --filter "FullyQualifiedName~MyTestMethod"
# note: a filter that matches zero tests exits with an error

# Frontend only (from src\Commerce.Demo.Frontend)
npm install
npm run build       # production build → dist/
npm run dev         # dev server
npm run lint        # ESLint
```

The Web project's pre-build step automatically runs `npm install` and `npm run build` in the Frontend project, then copies output to `wwwroot/dist`.

## Current Development Phase

Phase: Active Development
Version: 2.2.0

Active Work:
- [x] Upgrade to .NET 10 / CMS 13.6 / Commerce 15.5 (2026-11-11, ADR-008)
- [ ] Provision Graph / Opti ID / Opal credentials; run CMS 13 schema upgrade on a prod DB copy (see docs/todo.md)

Blocked By: None

Last Session: 2026-11-11 - Upgrade to .NET 10 / CMS 13 / Commerce 15 (see devlog Session 3, ADR-008)

## Critical Conventions

### Code Patterns
- **Layered architecture**: Web → Business → ContentTypes → Core (strict dependency flow)
- **Decorator pattern**: Used for cross-cutting concerns (e.g., `SiteSettingsManagerCacheDecorator`)
- **Feature folders**: Business logic organized by feature (`Features/Search`, `Features/SiteSettings`)
- **Base classes**: All content types inherit from project base classes (`PageBase`, `ProductBase`, `CategoryBase`, `VariantBase`, `BlockBase`)
- **Controllers**: Inherit from `PageControllerBase<T>` or `CommerceControllerBase<T>`
- **Minimal API**: API endpoints in `Endpoints.cs`, mounted at `/api`
- **Object mapping**: Mapster configured in `MapsterConfig.cs`

### NuGet Feeds
Defined in `src\nuget.config`:
- `nuget.org`
- `optimizely` — `https://nuget.optimizely.com/v3/index.json` (CMS 13 feed)
- Local packages — `D:\Packages` (required: CmsContentScaffolding.Optimizely 2.0.0 for CMS 13 is only published there)

### Versioning
Semantic Versioning in `src\GlobalAssemblyInfo.cs`: `[Breaking].[Features].[BugFixes].[Dev]`

### Git Workflow
- Branch naming: feature/, bugfix/, hotfix/
- Main branch: `main`
- Development branch: `develop`

## Production Standards

**Every implementation MUST include these. No exceptions.**

### Logging
- Log key user actions and system events (INFO level)
- Log all errors with stack traces and context (ERROR level)
- Use structured format: timestamp, user/session ID, action, outcome
- NEVER log passwords, tokens, PII, or secrets

### Error Handling
- Never show raw errors or stack traces to users
- Provide clear, actionable error messages
- Implement error boundaries (don't let failures crash the app)
- Include recovery paths (retry options, fallback behavior)

### Security
- Validate and sanitize ALL user inputs
- Use parameterized queries (never string concatenation for SQL)
- Escape output to prevent XSS
- Check permissions before every protected action
- Never hardcode secrets (use environment variables)

**Claude: Before marking any component complete, verify:**
- [ ] Logging implemented for key actions and errors?
- [ ] Error handling with user-friendly messages?
- [ ] Security considerations addressed?

## Operational Guardrails

### Instruction Precedence (when sources conflict)
1. System/platform constraints → 2. CLAUDE.md → 3. SPEC.md → 4. PLAN.md → 5. /docs/*

**If code conflicts with SPEC:** Treat as bug; fix code (preferred) or update SPEC with approval + ADR.

### Component Sizing
- **Max:** ~5 files, ~300 lines per component
- **Exclusions:** Only tests/** and docs/** don't count toward limits
- **Risky changes (auth, billing, migrations):** Single-layer only, explicit rollback notes required
- **If exceeds limits:** Break down and get approval before proceeding

### Definition of Done (required for every component)
- [ ] Scope matches SPEC/PLAN (no scope creep)
- [ ] Tests added/updated and passing
- [ ] Lint/typecheck passes (if configured) or TODOs logged
- [ ] Logging, error handling, security implemented
- [ ] Required docs updated (see Doc Triggers)
- [ ] Proof: Commands run + outcomes captured

### Doc Update Triggers
| Change | Update |
|--------|--------|
| API endpoint | api.md |
| Module/data flow | architecture.md |
| Tech decision | decisions.md (ADR) |
| Dependency | stack.md |
| Schema (if affects API) | api.md + architecture.md |
| Component complete | devlog.md + todo.md |

### Security Gate (for new endpoints/inputs)
1. Inputs identified 2. Validated 3. Authorized 4. Output escaped 5. Logging redacted 6. Abuse prevented

## Session Protocol

### F3 Methodology Reminder

This project follows F3 (Foundation-First Framework) v2.3.2:

**Area 1: Foundation** - Spec + Plan defined upfront
**Area 2: Implementation** - Build <-> Guide continuous cycle (Claude builds, I guide)
**Area 3: Documentation** - Continuous logging for persistent context
**Production Standards** - Security, logging, error handling in every implementation

**Critical F3 Behaviors:**
- Show me each component before moving on (Build <-> Guide cycle)
- **After completing each component: STOP, announce completion, wait for approval**
- Never proceed to next component without explicit permission
- Checkpoint progress to devlog.md after every component completion
- Document blockers immediately when they occur
- Never assume - always ask for approval before major changes
- Update documentation continuously, not just at session end
- **Include logging, error handling, and security in EVERY component**

**CRITICAL:** If a requested change is NOT in PLAN.md, do NOT implement it.
Stop and ask: "This isn't in our plan yet. Should we run Feature Discovery first?"

### Pre-Implementation Checkpoint

**Before starting ANY significant work, verify:**

1. [ ] Is this feature/change in SPEC.md?
2. [ ] Is there an implementation plan in PLAN.md?
3. [ ] If NO to either: **STOP** and use Feature Discovery Prompt first

**Before completing ANY component, verify Production Standards:**

4. [ ] Logging implemented for key actions and errors?
5. [ ] Error handling with user-friendly messages?
6. [ ] Security considerations addressed (input validation, auth, etc.)?

### Build <-> Guide Cycle Protocol

**After completing EVERY component, you MUST:**

1. **STOP** - Do not continue to the next component
2. **ANNOUNCE:** "I've completed [COMPONENT_NAME]"
3. **SHOW:** Summary of what was built (files created/modified, key functionality)
4. **INCLUDE:** DoD checklist + acceptance verification + commands run
5. **ASK:** "Would you like to review this before I continue to [NEXT_COMPONENT]?"
6. **WAIT:** Do not proceed until I give explicit approval

### Progress Checkpointing
**During Build <-> Guide Cycle:**
- After completing each component, immediately update /docs/devlog.md
- Update /docs/todo.md marking items complete as you go
- **Don't wait for session end** - log progress continuously

### Before Risky Operations
Before deployments, database migrations, or major refactors:
1. Document current state in /docs/devlog.md
2. Commit current code
3. Create restore point

### Blocker Protocol
If you hit a blocker that halts progress:
1. STOP immediately
2. Document in /docs/devlog.md using BLOCKER template
3. Include: issue, impact, context, resolution plan
4. Pause work until resolved

## Important Warnings

- Always run tests before committing
- Checkpoint progress after completing each component
- Never edit Optimizely core/platform files
- All Optimizely NuGet packages are referenced only in Commerce.Demo.Optimizely.Core

## Documentation Map

| Document | Purpose |
|----------|---------|
| /docs/architecture.md | System design, components, data flow |
| /docs/decisions.md | Architecture Decision Records (ADRs) |
| /docs/stack.md | Technology choices and configuration |
| /docs/api.md | API contracts and endpoints |
| /docs/devlog.md | Session-by-session progress tracking |
| /docs/todo.md | Current tasks and priorities |
| /docs/changelog.md | Release history |

## Session Start Checklist

1. Read this file (automatic)
2. Check /docs/todo.md for priorities
3. Review last /docs/devlog.md entry (including BLOCKERs)
4. Verify no unexpected shutdowns left incomplete work

## Session End Protocol

Before closing:
1. Summarize accomplishments and append to /docs/devlog.md
2. Update "Current Development Phase" above
3. Update /docs/todo.md with completed/new items
4. Document any decisions in /docs/decisions.md
