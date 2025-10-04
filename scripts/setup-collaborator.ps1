#!/usr/bin/env pwsh
<#!
.SYNOPSIS
  One-shot collaborator setup for this repo (cross-platform via PowerShell Core).

.DESCRIPTION
  - Verifies prerequisites (.NET SDK 9+)
  - Restores local dotnet tools from .config/dotnet-tools.json
  - Restores solution packages
  - Optionally formats code, builds, runs tests
  - Optionally installs local git hooks (pre-commit) to enforce formatting and tests

.PARAMETER NoTools
  Skip restoring local dotnet tools.

.PARAMETER NoFormat
  Skip running code formatter.

.PARAMETER NoBuild
  Skip building the solution.

.PARAMETER NoTest
  Skip running tests.

.PARAMETER InstallGitHooks
  Install a local pre-commit hook that runs dotnet format verification and tests.

.PARAMETER CI
  CI-friendly output (less chatter, non-interactive).

.EXAMPLES
  pwsh -File scripts/setup-collaborator.ps1
  pwsh -File scripts/setup-collaborator.ps1 -InstallGitHooks
  pwsh -File scripts/setup-collaborator.ps1 -NoFormat -NoTest
#>

[CmdletBinding()]
param(
  [switch]$NoTools,
  [switch]$NoFormat,
  [switch]$NoBuild,
  [switch]$NoTest,
  [switch]$InstallGitHooks,
  [switch]$CI
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($CI) { Write-Host $msg } else { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
}
function Write-Warn($msg) {
  if ($CI) { Write-Warning $msg } else { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
}
function Write-Err($msg) {
  if ($CI) { Write-Error $msg } else { Write-Host "[ERROR] $msg" -ForegroundColor Red }
}

# Determine repo root as parent of this script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path

Write-Info "Repo root: $RepoRoot"

# Sanity: Verify solution file presence (optional)
$SolutionPath = Join-Path $RepoRoot 'DotNetCliMcp.sln'
if (-not (Test-Path $SolutionPath)) {
  Write-Warn "Solution file 'DotNetCliMcp.sln' not found at repo root. Continuing anyway."
}

# 1) Check prerequisites: dotnet 9+
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Err "'dotnet' CLI not found in PATH. Please install .NET SDK 9 or newer and retry."
  exit 1
}
$dotnetVersionRaw = (& dotnet --version).Trim()
$dotnetMajor = 0
try {
  $dotnetMajor = [int]($dotnetVersionRaw.Split('.')[0])
} catch {
  Write-Warn "Could not parse .NET SDK version from '$dotnetVersionRaw'."
}
if ($dotnetMajor -lt 9) {
  Write-Err ".NET SDK 9 or newer is required. Detected: $dotnetVersionRaw"
  exit 1
}
Write-Info ".NET SDK detected: $dotnetVersionRaw"

Push-Location $RepoRoot
try {
  # 2) Restore local dotnet tools
  if (-not $NoTools) {
    if (Test-Path (Join-Path $RepoRoot '.config/dotnet-tools.json')) {
      Write-Info 'Restoring local dotnet tools...'
      dotnet tool restore | Out-Host
    } else {
      Write-Info 'No .config/dotnet-tools.json found. Skipping tool restore.'
    }
  } else {
    Write-Info 'Skipping tool restore (NoTools)'
  }

  # 3) Restore solution
  Write-Info 'Running dotnet restore...'
  dotnet restore $SolutionPath | Out-Host

  # 4) Format (optional, default: run formatter to fix issues)
  if (-not $NoFormat) {
    Write-Info 'Formatting code (dotnet format)...'
    $formatOk = $true
    try {
      # Prefer the standard shim; fallback to explicit tool-run if needed
      dotnet format | Out-Host
    } catch {
      Write-Warn 'dotnet format failed, retrying via dotnet tool run dotnet-format...'
      try { dotnet tool run dotnet-format | Out-Host } catch { $formatOk = $false }
    }
    if (-not $formatOk) { Write-Warn 'Code formatting step did not complete successfully.' }
  } else {
    Write-Info 'Skipping code format (NoFormat)'
  }

  # 5) Build (optional, default: build)
  if (-not $NoBuild) {
    Write-Info 'Building solution (Debug)...'
    dotnet build $SolutionPath -c Debug --nologo --verbosity minimal | Out-Host
  } else {
    Write-Info 'Skipping build (NoBuild)'
  }

  # 6) Test (optional, default: test)
  if (-not $NoTest) {
    Write-Info 'Running tests (Debug)...'
    dotnet test $SolutionPath -c Debug --nologo --verbosity minimal | Out-Host
  } else {
    Write-Info 'Skipping tests (NoTest)'
  }

  # 7) Install Git hooks (optional)
  if ($InstallGitHooks) {
    Write-Info 'Installing local git hooks (pre-commit)...'

    if (-not (Test-Path (Join-Path $RepoRoot '.git'))) {
      Write-Warn 'Not a git repository (no .git directory found). Skipping hook installation.'
    } else {
      $hooksDir = Join-Path $RepoRoot '.githooks'
      if (-not (Test-Path $hooksDir)) { New-Item -ItemType Directory -Path $hooksDir | Out-Null }

      $preCommitPath = Join-Path $hooksDir 'pre-commit'
      $preCommitContent = @(
        '#!/usr/bin/env bash',
        'set -euo pipefail',
        '',
        'echo "[githook] Verifying formatting..."',
        'if ! dotnet format --verify-no-changes >/dev/null 2>&1; then',
        '  echo "Formatting issues found. Run: dotnet format"',
        '  exit 1',
        'fi',
        '',
        'echo "[githook] Running tests..."',
        'dotnet test -c Debug --nologo --verbosity quiet'
      ) -join "`n"

      Set-Content -Path $preCommitPath -NoNewline -Value $preCommitContent -Encoding UTF8

      # Make executable on Unix-like systems
      if (-not $IsWindows) {
        & chmod +x $preCommitPath
      }

      # Point repo to use .githooks directory
      git config core.hooksPath .githooks
      Write-Info 'Git hooks installed. Pre-commit will enforce formatting and tests.'
    }
  }

  Write-Info 'Collaborator setup completed successfully.'
}
finally {
  Pop-Location
}
