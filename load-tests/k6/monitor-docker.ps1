param(
    [int]$IntervalSeconds = 5
)

while ($true) {
    Clear-Host
    Get-Date

    docker stats --no-stream `
        --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.PIDs}}" `
        aqarios-api-1 `
        aqarios-postgres-1 `
        aqarios-frontend-1

    Start-Sleep -Seconds $IntervalSeconds
}
