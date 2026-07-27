# Phase 12 — Security Hardening

Consolidates the mission's security work and the final adversarial audit (three-agent review that actively attempted to refute each control). Verification numbers at the end reflect the post-fix-wave suite run.

## Authentication
- JWT: issuer/audience/lifetime/signing-key validation on; 1-minute clock skew; **no fallback secrets anywhere** — startup fails fast on missing/short `Jwt:Secret` and non-Development rejects the committed placeholder (Program.cs + JwtTokenGenerator).
- SignalR hub auth via `access_token` query string, scoped to `/hubs` paths only.
- Refresh tokens: rotation + theft-detection pre-existing; **fix applied this phase** — the rotation path now row-locks the token (`FOR UPDATE`) so concurrent refreshes of the same token serialize (double-rotation race removed) while reuse-detection semantics are preserved.
- Credential-guessing: dedicated login (5/min default) and OTP-issuance (3/min) fixed-window limiters layered over the global limiter; `Retry-After` on all 429s; partition keys immune to forged `X-Forwarded-For` (proxy trust is an explicit CIDR list that REPLACES the loopback defaults; empty config trusts no proxy). All verified by the formerly-skipped, now-rewritten Module 3 tests (36/36).

## Authorization / RBAC
- Permission-based policies for every module; superuser claim honored uniformly. **Fixes applied:** `company.manage` policy now guards company profile/settings mutations AND subscription subscribe/change-plan/cancel (previously any authenticated member could cancel the company's subscription or change grace-period policy); maintenance attachment endpoint no longer accepts client-supplied uploader identity; `documents.view_confidential` enforced in query handlers.
- Companies by-ID reads/mutations now tenant-masked (`request.Id != CompanyId` → 404 unless platform admin).

## Multi-Tenancy / RLS — the load-bearing findings
1. **Read-path reality:** only `ICommand`s get transactions, so `TenantSessionInterceptor` never sets RLS context for queries. RLS therefore CANNOT be the only read-path defense. **Fix applied (P0):** every read repository behind a GET now takes an explicit `companyId` predicate from `ITenantContext` (Financials, Leasing contracts, Maintenance — joining Documents/Notifications/Tenants which already did this). Defense-in-depth: repository predicate first, RLS backstop for command paths.
2. **Deployment requirement (cannot be fixed in code):** the local/dev connection uses the `postgres` superuser — RLS is inert on such connections. **Production MUST run under a dedicated non-superuser, non-owner role without BYPASSRLS** (the integration fixture's `propertyos_app`/`app_user` split is the model). Recorded as a hard deployment gate in `14-production-readiness.md`.
3. RLS coverage: policies exist for all tenant-scoped tables including (this mission) the previously-unprotected notification tables with per-row recipient ownership. **Fix applied:** the four Module 5 tenant-table policies used non-null-safe `current_setting` (500 instead of clean denial when context unset) — migrated to the `NULLIF(..., '')::uuid` form used everywhere else.
4. Webhook tenant context: the anonymous eFAWATEERcom webhook's command chain runs without claims; under enforced RLS the `FOR UPDATE` transaction lookup would return nothing. Interim posture: webhook is HMAC-gated + fail-closed (503 without secret) and the gateway is a Null placeholder; the platform-scope company-resolution pattern (as used by jobs) is the designated fix when a real gateway integration lands. Tracked as a blocker item alongside the gateway itself.

## Input / File Security
- Path traversal closed with canonicalization + containment (tested against `../`, rooted, UNC, mixed separators, encoded forms at the binding layer); **fix applied:** `ConfirmFileUpload` now rejects `..`/backslash keys BEFORE the tenant-prefix check (audit found the prefix check alone was orderable around).
- Signed capability URLs: HMAC-SHA256(purpose+key+absolute expiry), fixed-time compare, fail-closed without `FileStorage:UrlSigningSecret` — audit's refutation attempts (token malleability, purpose swap, expiry tamper) failed.
- Upload size: declared-vs-on-disk verification + bounded body reads (411/413).
- Mass-assignment: full request-model scan — server-authoritative fields absent (CompanyId never bindable; actor fields removed where found).
- eFAWATEERcom webhook: raw-body HMAC, constant-time compare, fail-closed — audit-verified sound.

## Configuration & Secrets
- No hardcoded fallback secrets remain (JWT fail-fast; file-signing and webhook secrets fail closed). `appsettings.Local.json` in the working tree contains real credentials — **rotate at deployment; never commit** (standing warning).
- `TestController` (anonymous DB write) is Development-only (404 otherwise).
- Hangfire dashboard deliberately unmapped (needs its own auth filter before exposure).

## Accepted risks / open items (explicit)
| Item | Rationale / trigger |
|---|---|
| Superuser dev connection | Dev convenience; production gate documented (see above) — RLS suites prove enforcement under the proper role. |
| `notifications.view_all` RLS-level bypass | Doc itself defers it to a dedicated design pass; REST-level `ViewAll` policy enforced meanwhile. |
| Channel-provider I/O inside the dispatch transaction | Current providers are local/instant (SignalR, Null); revisit with claim-then-send when real SMS/email gateways (network I/O) land. |
| Magic-byte sniffing limited to pdf/png/jpg | webp/docx/xlsx are ZIP containers; extension+MIME+size+authz still apply; AV scanning is an infra decision. |
| No AV/malware scanning | Infrastructure/product decision; storage is tenant-contained and served nosniff/attachment. |

## Phase Result
COMPLETE — all P0/P1 audit findings fixed and verified; residual items are documented deployment gates or explicitly deferred-by-doc designs. (Final suite numbers recorded in `14-production-readiness.md`.)
