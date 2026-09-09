param(
    [int]$IntervalSeconds = 5,
    [string]$OutputFile
)

while ($true) {
    $timestamp = (Get-Date).ToUniversalTime().ToString("o")
    Write-Host $timestamp

    $rows = docker stats --no-stream `
        --format "{{.Name}}|{{.CPUPerc}}|{{.MemUsage}}|{{.PIDs}}" `
        aqarios-api-1 `
        aqarios-postgres-1 `
        aqarios-frontend-1

    $rows | ForEach-Object {
        $parts = $_ -split '\|', 4
        $sample = [pscustomobject]@{
            TimestampUtc = $timestamp
            Container = $parts[0]
            CpuPercent = $parts[1]
            MemoryUsage = $parts[2]
            Pids = $parts[3]
        }
        $sample | Format-Table -AutoSize
        if ($OutputFile) {
            $sample | Export-Csv -Path $OutputFile -Append -NoTypeInformation
        }
    }

    Start-Sleep -Seconds $IntervalSeconds
}
