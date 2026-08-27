# AqariOS Docker setup

This setup runs the React/Vite frontend, ASP.NET Core .NET 9 API, a one-shot Entity Framework Core migration container, and PostgreSQL 17. The frontend Nginx server also proxies `/api` and `/hubs` to the API, so browser traffic remains same-origin.

## Requirements

- Docker Desktop
- WSL2 enabled
- Docker Desktop configured for Linux containers

## First start

From the repository root, create the local environment file:

```powershell
Copy-Item .env.example .env
```

Open `.env` and set these required values:

- `POSTGRES_PASSWORD`: a strong local PostgreSQL password.
- `JWT_SECRET`: a unique secret of at least 32 bytes. Production startup rejects the committed development placeholder.
- `FILE_STORAGE_URL_SIGNING_SECRET`: another unique secret of at least 32 bytes for signed file URLs.

The notification, monitoring, and payment-provider variables are optional and blank by default. The Compose setup explicitly overrides the matching committed application settings so those integrations are not used accidentally. Keep `VITE_API_BASE_URL=/` to route browser API and SignalR requests through Nginx.

Build and start the complete stack:

```powershell
docker compose up -d --build
```

The `migrate` service waits for PostgreSQL, applies all existing EF Core migrations, exits successfully, and only then allows the API to start. The application does not apply migrations itself at startup.

On a fresh PostgreSQL data volume, the official PostgreSQL entrypoint first runs `docker/postgres/01-create-roles.sql`. It idempotently provisions the three roles required by the existing migrations:

- `propertyos_owner`: non-login schema-object owner used by migrations for table and function ownership.
- `propertyos_app`: restricted login role targeted by application RLS policies and grants. Docker does not assign it a password.
- `propertyos_auth`: restricted login role targeted by authentication-table RLS policies and grants. Docker does not assign it a password.

All three roles are non-superusers, cannot create databases or roles, cannot replicate, and cannot bypass row-level security.

PostgreSQL only executes files in `/docker-entrypoint-initdb.d` when initializing an empty data directory. If the current local development volume was created before this role initializer was added, recreate it once. Warning: this permanently deletes the current Docker PostgreSQL data and API file-storage volumes. Never run this against production data:

```powershell
docker compose down -v
docker compose up -d --build
```

Open the frontend at `http://localhost:5173`. The API is also available directly at `http://localhost:5235`. OpenAPI and Scalar are mapped by the application only when `ASPNETCORE_ENVIRONMENT=Development`; they are intentionally unavailable with the default `Production` environment.

## Operations

Check containers and health:

```powershell
docker compose ps
```

View all logs:

```powershell
docker compose logs -f
```

View API logs only:

```powershell
docker compose logs -f api
```

View migration logs:

```powershell
docker compose logs migrate
```

Verify the provisioned PostgreSQL roles using the values loaded from `.env`:

```powershell
docker compose exec postgres psql -U ${env:POSTGRES_USER} -d ${env:POSTGRES_DB} -c "\du"
```

Stop the stack:

```powershell
docker compose down
```

Warning: the following command permanently deletes the PostgreSQL database and locally stored API files in the Docker volumes:

```powershell
docker compose down -v
```

Monitor container CPU and memory usage (including during later k6 tests):

```powershell
docker stats
```

## Architecture and networking

```text
Browser
  |
  v
frontend (Nginx, host 5173 -> container 8080)
  |-- static React/Vite files
  |-- /api  --------------------+
  |-- /hubs (WebSocket) --------+--> api (container 8080; optional host 5235)
                                         |
                                         v
                                  postgres (internal 5432)
```

PostgreSQL is not published to the host. The API and migration container connect with `Host=postgres`, the Compose service name. The `backend` network is internal; only the API bridges it to the `web` network.

## Volumes

- `postgres_data`: persistent PostgreSQL data.
- `api_storage`: persistent files for the application's physical file-storage provider.

Compose prefixes these volume names with the project name (`aqarios`) when creating them.

## Optional utility scraper

The repository contains a separate `utility-scraper` service, but the committed API configuration disables electricity and water synchronization and leaves its base URL empty. It is therefore not required for the default application stack and is not started by this Compose file. Enabling it would require a deliberate service configuration and a shared secret; no Redis service is used anywhere in the current application.

## Validation commands

After filling `.env`, validate and rebuild with:

```powershell
docker compose config
docker compose build
docker compose up -d
docker compose ps
docker compose logs migrate
docker compose logs api
```

If the API fails before listening, first confirm that `migrate` exited with code 0 and PostgreSQL is healthy. The application has registered database health checks but does not map a public health-check endpoint; the API container health check therefore verifies that its HTTP listener responds, regardless of response status.
