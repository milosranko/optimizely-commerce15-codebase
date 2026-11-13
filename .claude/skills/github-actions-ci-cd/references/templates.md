# GitHub Actions templates

Companion to `SKILL.md`. The live files under `.github/` are the authority; these are their shapes, trimmed, plus the pieces a new job needs.

## A. Feature-branch CI (`branch-protection-pipeline.yml`)

```yaml
name: .NET Core CI

on:
  push:
    branches:
      - feature/*
      - release/*
      - hotfix/*
      - bugfix/*
    # Skip the build for changes that cannot affect it (docs, editor rules).
    paths-ignore:
      - '**/*.md'
      - 'docs/**'
      - '.claude/**'

# Cancel superseded runs on the same branch so rapid pushes do not stack up.
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    # Linux runners default to an invariant locale; force en-US so culture-sensitive
    # tests resolve the same strings they do on Windows.
    env:
      LANG: en_US.UTF-8

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    # Version-addressed cache; a version bump rotates a .csproj and the key.
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', 'src/nuget.config') }}
        restore-keys: ${{ runner.os }}-nuget-

    - name: Restore dependencies
      run: dotnet restore src/Wisbar.sln

    - name: Build solution
      run: dotnet build src/Wisbar.Web/Wisbar.Web.csproj --configuration Release --no-restore

    - name: Run tests
      run: dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj --configuration Release --no-restore
```

### Optional: frontend lint job

Catches ESLint errors on the feature branch instead of in the `develop` deploy build. Runs in parallel with `build-and-test`.

```yaml
  frontend-lint:
    runs-on: ubuntu-latest
    timeout-minutes: 10

    steps:
    - uses: actions/checkout@v4

    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '20.x'
        cache: 'npm'
        cache-dependency-path: src/Wisbar.Frontend/package-lock.json

    - name: Install frontend dependencies
      working-directory: src/Wisbar.Frontend
      run: npm ci

    - name: Lint frontend
      working-directory: src/Wisbar.Frontend
      run: npm run lint
```

### Optional: CI as a required PR check

```yaml
on:
  pull_request:
    branches: [ "develop", "main" ]
```

Then add `build-and-test` as a required status check on the branch in repository settings (confirm with the user). Drop `paths-ignore` from any workflow whose check is required - a path-skipped required check stays Pending and blocks the merge.

## B. Build job (`build-deploy-github.yml`)

Order matters: setup → cache → restore → **frontend build** → CMS build → **tests** → publish → package + checksum → upload.

```yaml
env:
  DOTNET_VERSION: "8.0.x"
  NODE_VERSION: "20.x"
  CMS_PROJECT: "src/Wisbar.Web"
  FRONTEND_PROJECT: "src/Wisbar.Frontend"

jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    env:
      LANG: en_US.UTF-8
    outputs:
      # The package hash travels as run metadata, never inside the artifact.
      package-sha256: ${{ steps.package.outputs.sha256 }}

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: src/Wisbar.Frontend/package-lock.json

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', 'src/nuget.config') }}
          restore-keys: ${{ runner.os }}-nuget-

      - name: Restore .NET dependencies
        run: dotnet restore ${{ env.CMS_PROJECT }}

      - name: Install frontend dependencies
        working-directory: ${{ env.FRONTEND_PROJECT }}
        run: npm ci

      - name: Build frontend
        working-directory: ${{ env.FRONTEND_PROJECT }}
        run: npm run build

      - name: Build CMS project
        run: dotnet build ${{ env.CMS_PROJECT }} --configuration Release --no-restore

      - name: Run tests
        run: dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj --configuration Release

      - name: Publish CMS project
        run: dotnet publish ${{ env.CMS_PROJECT }} --configuration Release --output ./publish

      - name: Create NuGet-style deployment package (.nupkg)
        id: package
        shell: pwsh
        run: |
          $dllPath = "./publish/Wisbar.Web.dll"
          if (-Not (Test-Path $dllPath)) { Write-Error "DLL not found at $dllPath"; exit 1 }

          # Metadata-only read; Win32 FileVersion is empty on Linux runners.
          $version = [System.Reflection.AssemblyName]::GetAssemblyName((Resolve-Path $dllPath)).Version.ToString()
          $packageName = "wisbar.cms.app.$version.nupkg"

          New-Item -ItemType Directory -Force -Path "./nupkg" | Out-Null
          Compress-Archive -Path "./publish/*" -DestinationPath "./nupkg/$packageName" -Force

          $hash = (Get-FileHash "./nupkg/$packageName" -Algorithm SHA256).Hash.ToLower()
          "sha256=$hash"         | Out-File -FilePath $env:GITHUB_OUTPUT -Append
          "package=$packageName" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append
          "sha256=$hash"         | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append

      - name: Upload NuGet package artifact
        uses: actions/upload-artifact@v4
        with:
          name: cms-nuget
          path: nupkg
          # Same-run handoff to the deploy jobs only - no need to retain past the run.
          retention-days: 1
```

## C. Deploy job anatomy

Shape for a **new** deploy job. The existing jobs interpolate `${{ secrets.* }}` inline; this template passes them through step-level `env:` instead. Do not churn the existing jobs just to match.

Each `run` step is a new pwsh process, so a `Connect-EpiCloud` context does not carry to the next step - later steps pass credentials again.

```yaml
  deploy_<target>:
    runs-on: windows-2025-vs2026
    timeout-minutes: 45
    needs: build
    if: github.ref == 'refs/heads/<branch>'
    environment:
      name: <target>

    steps:
      - name: Checkout composite actions
        uses: actions/checkout@v4
        with:
          sparse-checkout: .github/actions
          sparse-checkout-cone-mode: false

      - name: Download NuGet package artifact
        uses: actions/download-artifact@v4
        with:
          name: cms-nuget
          path: ./nupkg

      # Must run before anything touches DXP.
      - name: Verify package checksum (PCI 6.4.3b)
        uses: ./.github/actions/verify-package-checksum
        with:
          expected-sha256: ${{ needs.build.outputs.package-sha256 }}

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Install Optimizely DXP PowerShell module
        shell: pwsh
        run: Install-Module -Name EpiCloud -Force -AllowClobber -Scope CurrentUser

      - name: Upload package to DXP
        shell: pwsh
        env:
          EPI_CLIENT_KEY: ${{ secrets.OPTIMIZELY_DEPLOYMENT_KEY }}
          EPI_CLIENT_SECRET: ${{ secrets.OPTIMIZELY_DEPLOYMENT_SECRET }}
          EPI_PROJECT_ID: ${{ secrets.OPTIMIZELY_PROJECT_ID }}
        run: |
          Connect-EpiCloud -ClientKey $env:EPI_CLIENT_KEY -ClientSecret $env:EPI_CLIENT_SECRET -ProjectId $env:EPI_PROJECT_ID

          $package = Get-ChildItem "./nupkg/*.nupkg" | Select-Object -First 1
          $sasUrl = Get-EpiDeploymentPackageLocation -ProjectId $env:EPI_PROJECT_ID
          Add-EpiDeploymentPackage -SasUrl $sasUrl -Path $package.FullName
          Write-Host "Uploaded package: $($package.Name)"

      - name: Start deployment to <Target>
        shell: pwsh
        env:
          EPI_CLIENT_KEY: ${{ secrets.OPTIMIZELY_DEPLOYMENT_KEY }}
          EPI_CLIENT_SECRET: ${{ secrets.OPTIMIZELY_DEPLOYMENT_SECRET }}
          EPI_PROJECT_ID: ${{ secrets.OPTIMIZELY_PROJECT_ID }}
        run: |
          $package = Get-ChildItem "./nupkg/*.nupkg" | Select-Object -First 1
          # Add -DirectDeploy only for a target that needs no verification slot (Integration).
          $deployment = Start-EpiDeployment `
            -ClientKey $env:EPI_CLIENT_KEY `
            -ClientSecret $env:EPI_CLIENT_SECRET `
            -ProjectId $env:EPI_PROJECT_ID `
            -TargetEnvironment <Target> `
            -DeploymentPackage $package.Name

          Write-Host "Deployment started with ID: $($deployment.Id)"
          "DEPLOYMENT_ID=$($deployment.Id)" | Out-File -FilePath $env:GITHUB_ENV -Append

      - name: Wait for deployment
        shell: pwsh
        env:
          EPI_CLIENT_KEY: ${{ secrets.OPTIMIZELY_DEPLOYMENT_KEY }}
          EPI_CLIENT_SECRET: ${{ secrets.OPTIMIZELY_DEPLOYMENT_SECRET }}
          EPI_PROJECT_ID: ${{ secrets.OPTIMIZELY_PROJECT_ID }}
        run: |
          $deploymentId = $env:DEPLOYMENT_ID

          # Bounded wait: stop after 30 min so a stuck or queued deploy cannot
          # burn runner minutes up to GitHub's 6h job ceiling.
          $deadline = (Get-Date).AddMinutes(30)

          do {
            Start-Sleep -Seconds 30
            $status = Get-EpiDeployment `
              -Id $deploymentId `
              -ClientKey $env:EPI_CLIENT_KEY `
              -ClientSecret $env:EPI_CLIENT_SECRET `
              -ProjectId $env:EPI_PROJECT_ID

            Write-Host "Status: $($status.Status)"

            if ((Get-Date) -gt $deadline -and $status.Status -in @("InProgress", "Queued")) {
              Write-Error "Timed out after 30 min; deployment $deploymentId still $($status.Status). The DXP deployment continues server-side - verify in the portal."
              exit 1
            }
          } while ($status.Status -in @("InProgress", "Queued"))

          # Slot targets only: the package is live on the slot and waits for a human.
          if ($status.Status -eq "AwaitingVerification") {
            $slotUrl = $status.ValidationLinks -join ", "
            @(
              "### <Target> deployment awaiting verification",
              "",
              "- Deployment ID: ``$deploymentId``",
              "- Verification slot: $(if ($slotUrl) { $slotUrl } else { '(none reported)' })",
              "",
              "Verify the slot, then **Complete** or **Reset** the deployment in the Optimizely DXP portal."
            ) | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
            exit 0
          }

          if ($status.Status -ne "Succeeded") {
            Write-Error "Deployment failed with status: $($status.Status)"
            exit 1
          }

          Write-Host "Deployment completed successfully."
```

## D. Choosing a deploy mode

| Target | `-DirectDeploy` | Ends in | Human step |
|---|---|---|---|
| Integration (`develop`) | yes | `Succeeded` | none |
| Preproduction (`main`) | no | `AwaitingVerification` | Complete or Reset in the DXP portal |
| Anything customer-facing | no | `AwaitingVerification` | as above, plus GitHub environment reviewers if it must not start unattended |

A job that uses a slot must keep the `AwaitingVerification` branch in its wait step, or it fails on a successful deployment.

## E. Settings audit

Run through Bash; PowerShell 5.1 mangles the quoted `--jq` arguments.

```bash
R=State-Bar-of-Wisconsin/wisbar

# Environments: protection rules (required reviewers, wait timers) and branch policy
gh api repos/$R/environments --jq '.environments[] | {name, protection_rules, deployment_branch_policy}'

# Branch protection: required checks and reviews
for b in develop main; do
  echo "--- $b"
  gh api repos/$R/branches/$b/protection --jq '{checks: .required_status_checks.contexts, reviews: .required_pull_request_reviews.required_approving_review_count}'
done

# Secret names (never values) at repo and environment level
gh secret list -R $R
gh secret list -R $R --env integration
gh secret list -R $R --env preproduction

# Default GITHUB_TOKEN permission
gh api repos/$R/actions/permissions/workflow

# Recent runs and a failing run's log
gh run list -R $R --limit 10 --json workflowName,headBranch,conclusion,createdAt --jq '.[] | [.workflowName,.headBranch,(.conclusion // "running"),.createdAt] | @tsv'
gh run view <id> -R $R --log-failed
```

## F. Local validation

PowerShell, from the repo root. Start Docker Desktop first; otherwise these fail with "failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine".

```powershell
# Lint every workflow (actionlint is not installed locally)
docker run --rm -v "${PWD}:/repo" -w /repo rhysd/actionlint:latest -color

# Reproduce Linux build and test behavior (locale, paths) before pushing
docker run --rm -v "${PWD}:/repo" -w /repo -e LANG=en_US.UTF-8 mcr.microsoft.com/dotnet/sdk:8.0 `
  dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj --configuration Release
```

The container uses SDK 8, where `dotnet test` runs through the VSTest bridge; CI resolves SDK 10 and runs in MTP mode. The container proves Linux behavior, not test-CLI flags - check new flags on a local SDK 10 run as well.

Container builds write `bin/` and `obj/` into the working tree as root-owned Linux output; run `dotnet clean` or delete them before the next Windows build.
