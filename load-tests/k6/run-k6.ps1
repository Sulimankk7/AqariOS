param(
    [ValidateSet("ratelimit", "smoke", "load", "mixed", "stress", "spike", "soak")]
    [string]$Profile = "smoke",

    [string]$BaseUrl = "http://localhost:5235",

    [string]$SeedFile,

    [switch]$AllowExternalTarget
)

$ErrorActionPreference = "Stop"

$K6Dir = $PSScriptRoot
$OutputDir = Join-Path (Split-Path $K6Dir -Parent) "output"
$ReportsDir = Join-Path (Split-Path $K6Dir -Parent) "reports"
New-Item -ItemType Directory -Force -Path $ReportsDir | Out-Null

$target = [Uri]$BaseUrl
$isLocal = $target.IsLoopback -or $target.Host -in @("host.docker.internal", "api")
if (-not $isLocal -and -not $AllowExternalTarget) {
    throw "Refusing non-local target '$BaseUrl'. Use -AllowExternalTarget only for an authorized dedicated test environment."
}

if ($Profile -in @("stress", "spike", "soak") -and -not $isLocal -and -not $AllowExternalTarget) {
    throw "High-load profiles require an explicitly authorized test target."
}

Write-Host ""
Write-Host "AqariOS k6 Test Runner" -ForegroundColor Cyan
Write-Host "Profile: $Profile"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

if ($Profile -eq "ratelimit") {
    Write-Host "Running rate-limit verification..." -ForegroundColor Yellow
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $summary = Join-Path $ReportsDir "ratelimit-$timestamp-summary.json"
    $samples = Join-Path $ReportsDir "ratelimit-$timestamp-samples.json"
    & k6 run `
        -e "BASE_URL=$BaseUrl" `
        --summary-export $summary `
        --out "json=$samples" `
        (Join-Path $K6Dir "rate-limit-check.js")

    exit $LASTEXITCODE
}

if ($SeedFile) {
    $Seed = Get-Item -LiteralPath $SeedFile
} else {
    $Seed = Get-ChildItem `
        -Path $OutputDir `
        -Filter "aqarios_seed_*.json" `
        -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

if (-not $Seed) {
    throw "No aqarios_seed_*.json found in $OutputDir. Run seed_aqarios.py first."
}

# k6 open() accepts an absolute path on Windows and avoids ambiguity when a
# caller supplies a resume file from another directory.
$SeedForK6 = $Seed.FullName
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$summary = Join-Path $ReportsDir "$Profile-$timestamp-summary.json"
$samples = Join-Path $ReportsDir "$Profile-$timestamp-samples.json"

Write-Host "Using seed: $($Seed.FullName)" -ForegroundColor Green
Write-Host ""

& k6 run `
    -e "PROFILE=$Profile" `
    -e "BASE_URL=$BaseUrl" `
    -e "SEED_FILE=$SeedForK6" `
    -e "SUMMARY_FILE=$summary" `
    --out "json=$samples" `
    (Join-Path $K6Dir "aqarios-test.js")

exit $LASTEXITCODE
