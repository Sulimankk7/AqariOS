# AqariOS Utility Scraper

Internal-only Python service for the IDECO electricity and YW water WebForms integrations. It has no database access and accepts only an account number. AqariOS remains responsible for authentication/tenant context, scheduling, locking, persistence, RLS, and deduplication.

The service must be deployed on a private network. Every internal bill request is authenticated with a timestamped, nonce-bearing HMAC-SHA256 signature. Use the same 32-byte-or-longer secret in `AQARIOS_SCRAPER_SHARED_SECRET` and `.NET` configuration `UtilityBills:ScraperService:SharedSecret`; inject it from a secret store rather than committing it.

Run locally:

```sh
python -m venv .venv
.venv/bin/pip install -r requirements-dev.txt
AQARIOS_SCRAPER_SHARED_SECRET="replace-with-a-random-secret-at-least-32-bytes" .venv/bin/uvicorn app.main:app --host 127.0.0.1 --port 8080
```

The ASP.NET Core API is intentionally fail-closed by default. After the private
scraper service is reachable, configure the API through its deployment secret
store/environment (do not commit the values):

```text
UtilityBills__ScraperService__BaseUrl=http://utility-scraper:8080
UtilityBills__ScraperService__SharedSecret=<same 32-byte-or-longer secret>
UtilityBills__Electricity__Enabled=true
UtilityBills__Water__Enabled=true
```

If the base URL/shared secret is absent or a provider remains disabled, AqariOS
records a provider-not-configured sync outcome and leaves historical bootstrap
incomplete; no provider request is made and an empty bill history is not
manufactured.

Provider URLs, timeouts, attempts, and concurrency are service configuration only. They are never accepted in API requests. Access logging is disabled in the container command so account numbers cannot be written through request-body logging; application logs contain only controlled error codes.
