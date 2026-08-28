param(
    [ValidateSet("ratelimit", "smoke", "load", "stress", "spike", "breakpoint")]
    [string]$Profile = "load",

    [string]$BaseUrl = "http://localhost:5235"
)

$ErrorActionPreference = "Stop"

$K6Dir = $PSScriptRoot
$OutputDir = Join-Path (Split-Path $K6Dir -Parent) "output"

Write-Host ""
Write-Host "AqariOS k6 Test Runner" -ForegroundColor Cyan
Write-Host "Profile: $Profile"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

if ($Profile -eq "ratelimit") {
    Write-Host "Running rate-limit verification..." -ForegroundColor Yellow
    & k6 run `
        -e "BASE_URL=$BaseUrl" `
        (Join-Path $K6Dir "rate-limit-check.js")

    exit $LASTEXITCODE
}

$Seed = Get-ChildItem `
    -Path $OutputDir `
    -Filter "aqarios_seed_*.json" `
    -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $Seed) {
    throw "No aqarios_seed_*.json found in $OutputDir. Run seed_aqarios.py first."
}

# open() paths in k6 are resolved relative to the JS script.
$SeedForK6 = "../output/$($Seed.Name)"

Write-Host "Using seed: $($Seed.FullName)" -ForegroundColor Green
Write-Host ""

& k6 run `
    -e "PROFILE=$Profile" `
    -e "BASE_URL=$BaseUrl" `
    -e "SEED_FILE=$SeedForK6" `
    (Join-Path $K6Dir "aqarios-test.js")

exit $LASTEXITCODE
