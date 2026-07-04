# Apply EF Core migrations to the database
param(
    [string]$Environment = "Development"
)

$ErrorActionPreference = "Stop"

Write-Host "Applying EF Core migrations for Environment: $Environment..." -ForegroundColor Cyan

# Ensure EF tool is installed
Write-Host "Verifying dotnet-ef tool..."
& dotnet tool restore

Write-Host "Running migrations..."
& dotnet ef database update `
    --project src/PropertyOS.Infrastructure `
    --startup-project src/PropertyOS.Api `
    --context PropertyOsDbContext

Write-Host "Migrations applied successfully!" -ForegroundColor Green
