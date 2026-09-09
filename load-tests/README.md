# AqariOS repeatable test environment

## Safety

Use only localhost or an authorized isolated environment. The runners reject external targets unless explicitly overridden. Stress, spike, and soak are never run by setup or smoke commands. Generated seed files contain passwords and JWTs and are ignored by Git.

## Start and seed

Normal local stack (production-equivalent limiter):

```powershell
docker compose up -d --build
docker compose ps
python .\seed_aqarios.py
```

Public registration creates a pending landlord application. Seed creation therefore requires an existing test `SYSTEM_ADMIN`; set `AQARIOS_PLATFORM_ADMIN_EMAIL` and `AQARIOS_PLATFORM_ADMIN_PASSWORD`. The seeder submits registration, approves it through `/api/v1/platform/landlord-registrations/{id}/approve`, and only then logs in as the owner. It will not bypass approval or create a platform administrator directly.

If Python is not installed on Windows, forward those two environment variables into a disposable container:

```powershell
docker run --rm -v "${PWD}:/work" -w /work `
  -e AQARIOS_BASE_URL=http://host.docker.internal:5235 `
  -e AQARIOS_PLATFORM_ADMIN_EMAIL -e AQARIOS_PLATFORM_ADMIN_PASSWORD `
  python:3.13-slim sh -c "pip install --quiet requests && python seed_aqarios.py"
```

Capacity stack (high global limit, named authentication/security limits unchanged):

```powershell
docker compose -f docker-compose.yml -f docker-compose.load.yml up -d --build
```

Small defaults are configurable with `AQARIOS_OWNERS`, `AQARIOS_BUILDINGS_PER_OWNER`, `AQARIOS_FLOORS_PER_BUILDING`, `AQARIOS_APARTMENTS_PER_FLOOR`, `AQARIOS_TENANTS_PER_OWNER`, `AQARIOS_LEASES_PER_OWNER`, `AQARIOS_TENANT_ACCOUNTS_PER_OWNER`, `AQARIOS_MAINTENANCE_PER_OWNER`, `AQARIOS_EXPENSES_PER_OWNER`, and `AQARIOS_MARKETPLACE_PER_OWNER`. Set `AQARIOS_BASE_URL` for another authorized environment. To recover a partial run, preserve the same size variables and set:

```powershell
$env:AQARIOS_RESUME_FILE = "load-tests/output/aqarios_seed_<run>.json"
python .\seed_aqarios.py
```

Progress is atomically persisted after every created entity. Resume logs existing accounts in again, skips tracked entities, and continues the relationship graph. Cleanup is intentionally not automatic; only IDs in the seed manifest are eligible for a future explicit cleanup tool.

## Run profiles

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile ratelimit
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile smoke
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile load
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile mixed
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile stress
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile spike
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\load-tests\k6\run-k6.ps1 -Profile soak
```

Linux/macOS equivalents are `./load-tests/k6/run-k6.sh <profile>`. Mixed writes are capped with `MIXED_ITERATIONS` (default 20) and `MIXED_VUS` (default 5). Soak uses `SOAK_DURATION` (default `30m`) and `SOAK_VUS` (default 20). Think times use `THINK_MIN`/`THINK_MAX` seconds.

Each run writes a compact summary and raw samples under `load-tests/reports/`. The summary includes request count/rate, average/p50/p95/p99 latency, failures, unexpected responses, 429 count, checks, and max VUs. Raw samples carry `endpoint`, `status`, `operation`, and `profile` tags for endpoint/status error breakdown and slow-endpoint analysis.

Run `load-tests/k6/monitor-docker.ps1` in a second terminal for container CPU, memory, and PID snapshots. Do not commit its output or generated reports.
