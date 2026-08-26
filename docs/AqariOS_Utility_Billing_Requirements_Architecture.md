# AqariOS — Utility Billing Integration
## Requirements & Architecture Specification

**Status:** Draft / Architecture Specification  
**Scope:** Tenant Electricity & Water Billing  
**Primary goal:** Integrate external utility bill inquiry into AqariOS without storing data in Excel, without excessive server load, without excessive load on external provider portals, and with strict duplicate/concurrency protection.

---

# 1. Feature Overview

AqariOS will support optional electricity and water account information associated with a tenant's lease.

The feature has three main responsibilities:

1. Store utility account identifiers in the AqariOS database.
2. Periodically query the official electricity/water inquiry sources through provider-specific integrations.
3. Store discovered bills and notify the tenant when a new payable bill is detected.

The feature must be designed as an independent bounded capability so that utility-provider integration does not contaminate leasing, financial, payment, or accounting logic.

---

# 2. Core Product Requirements

## 2.1 Optional Utility Linkage

When creating a lease contract, the landlord/property manager may optionally provide:

### Electricity
- Electricity account/subscription number.
- Electricity bill/account identifier required by the external inquiry portal.

### Water
- Water account/subscription number.
- Water bill/account identifier required by the external inquiry portal.

Both utility types are optional.

A lease may have:

- No utility accounts.
- Electricity only.
- Water only.
- Both electricity and water.

The absence of utility information must never prevent normal lease creation.

---

# 3. Tenant Experience

## 3.1 Dashboard

For a newly created tenant account with no utility accounts linked, the dashboard should show a compact state such as:

> الكهرباء والمياه غير مربوطة

or equivalent localized wording.

This must be informational and non-blocking.

Once utility accounts are linked, the dashboard may show a compact utility summary:

- Electricity: linked / latest bill status.
- Water: linked / latest bill status.
- Outstanding amount, if available.

---

# 4. Bills Page

Add a tenant-facing sidebar item:

> فواتيري

The page is responsible for displaying utility billing information.

Suggested structure:

### Electricity
- Account number.
- Latest bill.
- Bill date.
- Amount.
- Status.
- Historical bills.

### Water
- Account number.
- Latest bill.
- Bill date.
- Amount.
- Status.
- Historical bills.

The page must read from AqariOS's database.

It must NOT trigger live scraping every time the tenant opens the page.

---

# 5. Source-of-Truth Model

The external provider is the source of truth for newly discovered utility bills.

AqariOS becomes the source of truth for:

- Linked utility account configuration.
- Previously discovered bills.
- Last successful synchronization.
- Synchronization state.
- Next scheduled check.
- Billing interval statistics.
- Notification idempotency state.

External provider data should be persisted after successful discovery.

---

# 6. Database Model

## 6.1 UtilityAccount

Recommended conceptual entity:

```text
UtilityAccount
---------------
Id
CompanyId
LeaseContractId
UtilityType
AccountNumber
SubscriptionNumber / ProviderIdentifier
IsActive
LastSuccessfulSyncAt
LastAttemptedSyncAt
NextCheckAt
AverageBillingIntervalDays
EstimatedNextBillDate
BillingIntervalSampleCount
LastKnownBillDate
CreatedAt
UpdatedAt
```

### UtilityType

```text
Electricity
Water
```

### Constraints

A lease must not have more than one active account for the same utility type.

Recommended database uniqueness:

```text
UNIQUE (LeaseContractId, UtilityType)
```

Tenant/company isolation must remain enforced according to the existing AqariOS multi-tenant/RLS architecture.

---

# 7. UtilityBill

Recommended conceptual entity:

```text
UtilityBill
-----------
Id
UtilityAccountId
ProviderExternalId
BillDate
Amount
Currency
Status
IsPaid
DiscoveredAt
DueDate
RawReference / ProviderReference
NotificationSentAt
CreatedAt
UpdatedAt
```

The exact provider fields should remain provider-neutral wherever possible.

Provider-specific information should be stored separately or in a controlled metadata structure rather than leaking provider-specific concepts throughout the domain.

---

# 8. Duplicate Protection

The system must be idempotent.

Recommended database uniqueness:

```text
UNIQUE (UtilityAccountId, ProviderExternalId)
```

If the provider does not expose a stable external bill ID, the integration must define a deterministic provider-specific identity key using stable bill attributes.

The database constraint is mandatory protection.

Application-level checks alone are insufficient.

---

# 9. Electricity Synchronization Strategy

Based on the observed billing pattern, electricity bills are expected around the beginning of the month.

The system should only perform electricity synchronization during:

```text
Day 1
Day 2
Day 3
```

The synchronization window should be limited to the morning.

Do NOT execute all electricity accounts simultaneously at midnight or at a single exact timestamp.

---

# 10. Electricity Load Distribution

Electricity accounts must be distributed across the available morning synchronization window.

Conceptually:

```text
07:00 → Batch A
08:00 → Batch B
09:00 → Batch C
```

The exact batch sizes should be configurable.

For example:

```text
BatchSize = configurable
```

The scheduler processes only a bounded number of accounts per execution.

If there are more accounts than the current batch capacity, remaining accounts are scheduled for the next available execution slot.

The goal is:

```text
Controlled throughput
+
No traffic spike
+
No full-table scraping
```

---

# 11. Water Synchronization Strategy

Water billing does not have a fixed billing date.

Historical data indicates a roughly recurring monthly pattern but with variable dates.

Therefore AqariOS must NOT assume:

```text
Every 1st of the month
```

and must NOT continuously scrape every water account every day.

Instead, the system uses historical billing intervals.

---

# 12. Initial Water Historical Processing

When a water account is linked for the first time:

1. Query the provider once.
2. Retrieve available historical bill records if the provider exposes them.
3. Store previously unknown bills.
4. Sort historical bill dates chronologically.
5. Calculate billing intervals between consecutive bills.
6. Calculate an initial billing interval estimate.
7. Store the calculated interval.
8. Calculate `EstimatedNextBillDate`.
9. Schedule the next synchronization.

This historical processing happens only when necessary.

It must NOT be repeated on every synchronization.

---

# 13. Water Billing Interval Model

Example:

```text
Bill 1 → 2026-01-21
Bill 2 → 2026-02-18
Bill 3 → 2026-03-15
```

Intervals:

```text
28 days
25 days
```

The system can maintain an incremental estimate.

Recommended stored values:

```text
AverageBillingIntervalDays
BillingIntervalSampleCount
EstimatedNextBillDate
```

The exact statistical method can be implemented as a simple incremental average initially.

Example:

```text
NewAverage =
(
    OldAverage × OldSampleCount + NewInterval
)
/
(
    OldSampleCount + 1
)
```

This avoids reprocessing the complete billing history.

---

# 14. Incremental Learning Rule

After a new water bill is discovered:

```text
Previous known bill date
        ↓
New bill date
        ↓
Calculate only the new interval
        ↓
Update average
        ↓
Update estimated next bill date
```

Do NOT:

```text
Load all historical bills
↓
Recalculate all intervals
↓
Recalculate all users
```

This is explicitly prohibited by the performance requirements.

---

# 15. Water Next-Check Scheduling

The system should derive the next check from the account's stored billing behavior.

Conceptually:

```text
EstimatedNextBillDate
        ↓
Safety window
        ↓
NextCheckAt
```

The safety window should be configurable.

Example:

```text
Estimated bill date - N days
```

The system then checks only accounts whose:

```text
NextCheckAt <= current time
```

This means the daily scheduler does not scrape all water accounts.

---

# 16. Important Scheduling Rule

A daily scheduler may run, but it must NOT mean:

> scrape every user every day.

Instead:

```text
Scheduler
    ↓
SELECT only due accounts
    ↓
LIMIT batch size
    ↓
Claim accounts
    ↓
Process batch
    ↓
Schedule next check
```

This keeps the cost proportional to accounts that actually require checking.

---

# 17. No Full-Table Daily Processing

The implementation must avoid:

```text
foreach user
    calculate billing prediction
    query provider
```

It must also avoid:

```text
daily full history analysis
```

and:

```text
daily recalculation of all averages
```

The database should be indexed around scheduling fields such as:

```text
NextCheckAt
IsActive
UtilityType
```

so due-account retrieval remains bounded and efficient.

---

# 18. Concurrency Protection

Utility synchronization must be safe when:

- Hangfire retries a job.
- Multiple workers run simultaneously.
- A previous worker is slow.
- A deployment overlaps with a scheduled job.
- A provider request takes longer than expected.

Recommended claiming mechanism:

```sql
FOR UPDATE SKIP LOCKED
```

The worker should atomically claim a small batch before processing.

This prevents two workers from processing the same utility account simultaneously.

---

# 19. Idempotent Bill Insertion

Bill discovery must follow:

```text
Provider response
        ↓
Normalize bill identity
        ↓
Database uniqueness check/constraint
        ↓
Insert if new
        ↓
Ignore if already known
```

The database constraint remains the final protection against duplicates.

---

# 20. Notification Requirements

When a genuinely new outstanding bill is discovered:

```text
New bill
   ↓
Persist bill
   ↓
Check notification state
   ↓
Send notification
```

The same bill must never generate multiple tenant notifications because of:

- Scheduler retries.
- Application restarts.
- Worker duplication.
- Temporary provider errors.

Recommended notification idempotency is tied to the specific:

```text
UtilityBill
```

rather than only the user.

---

# 21. Notification Examples

Electricity:

> لديك فاتورة كهرباء جديدة مستحقة بقيمة 5.50 د.أ.

Water:

> لديك فاتورة مياه جديدة مستحقة بقيمة 6.10 د.أ.

The exact wording should support Arabic and English localization.

---

# 22. Provider Integration Architecture

Do not put web-scraping code directly into:

- Controllers.
- Domain entities.
- Lease handlers.
- Notification handlers.
- Database repositories.

Use provider abstractions.

Recommended conceptual interface:

```csharp
IUtilityBillingProvider
```

with provider-specific implementations such as:

```text
ElectricityBillingProvider
WaterBillingProvider
```

or more explicit provider-specific interfaces if required by the external systems.

---

# 23. Scraper Safety

The provider integration must:

- Use bounded requests.
- Apply reasonable timeouts.
- Avoid uncontrolled retries.
- Respect provider rate limits.
- Avoid concurrent requests for the same account.
- Fail closed when the provider is unavailable.
- Never block the main web request pipeline.
- Never run provider scraping during tenant page rendering.

---

# 24. Retry Policy

External provider failures must not cause aggressive retry loops.

Recommended behavior:

```text
Attempt
 ↓
Failure
 ↓
Record failure
 ↓
Schedule controlled retry
```

Retries should have:

- Maximum retry count.
- Backoff.
- Provider-specific timeout.
- No immediate infinite retry.

A provider outage must not create a request storm.

---

# 25. Main Web Application Isolation

Utility synchronization must run as background work.

Tenant requests such as:

```text
GET /tenant-portal/bills
```

must only query stored database data.

They must never wait for:

```text
Electricity portal
Water portal
Web scraping
```

This guarantees that an external provider outage does not make the AqariOS tenant portal slow.

---

# 26. Lease Creation Integration

Utility information should be optional when creating a lease.

Conceptually:

```text
Create Lease
    ├── Tenant
    ├── Apartment
    ├── Lease Terms
    ├── Electricity Account (optional)
    └── Water Account (optional)
```

The utility fields must not alter the core financial contract semantics.

Utility linkage should be treated as an optional associated capability.

---

# 27. Transaction Boundary

Lease creation and utility-account creation must be handled safely.

If implemented inside the same application command:

```text
BEGIN TRANSACTION
    Create Lease
    Create optional utility accounts
COMMIT
```

If the lease creation succeeds but utility linkage fails, the application must have an explicit policy.

Preferred behavior:

- Validate utility identifiers before persistence where possible.
- Do not create partially linked utility accounts.
- Do not leave orphan utility accounts.
- Preserve lease integrity.

---

# 28. Security

Utility account numbers are tenant-associated data.

Requirements:

- Tenant authorization must be enforced server-side.
- Tenant A must never access Tenant B's utility accounts.
- Company/organization boundaries must be enforced.
- Utility bill queries must use the authenticated tenant context.
- No account number should be trusted from frontend input for authorization.
- RLS must remain consistent with AqariOS's existing architecture.

---

# 29. API Surface

Suggested API categories:

### Tenant

```text
GET /api/v1/tenant-portal/bills
GET /api/v1/tenant-portal/bills/electricity
GET /api/v1/tenant-portal/bills/water
```

### Utility Management

```text
POST /api/v1/utility-accounts
PUT /api/v1/utility-accounts/{id}
DELETE /api/v1/utility-accounts/{id}
```

Exact routes should follow existing AqariOS API conventions.

---

# 30. Dashboard API

The tenant dashboard should receive a lightweight utility summary.

Example:

```json
{
  "electricity": {
    "linked": true,
    "hasOutstandingBill": true,
    "amount": 5.50
  },
  "water": {
    "linked": false,
    "hasOutstandingBill": false,
    "amount": null
  }
}
```

The dashboard must not call external providers.

---

# 31. Database Indexing

Recommended indexes:

```text
UtilityAccount:
    (NextCheckAt)
    (CompanyId, IsActive)
    (LeaseContractId, UtilityType)

UtilityBill:
    (UtilityAccountId, BillDate DESC)
    (UtilityAccountId, Status)
```

The exact indexes should be reconciled with the final PostgreSQL execution plans.

---

# 32. Observability

Each synchronization attempt should record enough information to diagnose failures without logging sensitive data unnecessarily.

Recommended fields:

```text
UtilityAccountId
UtilityType
Provider
StartedAt
CompletedAt
Success
BillsDiscovered
ErrorCategory
NextCheckAt
```

Do not log:

- Passwords.
- Authentication tokens.
- Session cookies.
- Sensitive provider credentials.

Account identifiers should be masked where practical in logs.

---

# 33. Performance Requirements

The feature must satisfy:

### Web requests

Utility provider latency must have:

```text
0 ms impact
```

on normal tenant page rendering.

### Scheduler

Processing must be:

```text
bounded
batched
claim-based
```

### Database

No daily full-table historical processing.

### Provider

No uncontrolled parallel scraping.

### Notifications

No duplicate notification generation.

---

# 34. Failure Scenarios

## Provider unavailable

Result:

```text
No bill mutation
Record failed attempt
Schedule controlled retry
```

Existing data remains available.

## Provider returns duplicate bill

Result:

```text
Database uniqueness constraint rejects duplicate
No duplicate notification
```

## Worker crashes after bill insert

On retry:

```text
Bill already exists
Notification idempotency prevents duplicate notification
```

## Two workers claim same account

`SKIP LOCKED` ensures only one worker receives the claimed row.

## Tenant opens Bills page during provider outage

The page still loads normally from stored AqariOS data.

---

# 35. Historical Data Processing Safety

Historical processing is allowed only when needed, primarily during initial water-account onboarding.

It must be:

- One-time.
- Bounded.
- Idempotent.
- Provider-specific.
- Persisted incrementally.
- Never automatically repeated every day.

If historical data is unavailable, the system must fall back safely to a default scheduling strategy rather than continuously scraping.

---

# 36. Recommended Processing Lifecycle

## New Water Account

```text
Create UtilityAccount
        ↓
Initial provider inquiry
        ↓
Import historical bills
        ↓
Calculate interval statistics
        ↓
Calculate EstimatedNextBillDate
        ↓
Set NextCheckAt
        ↓
Normal scheduled operation
```

## Normal Water Operation

```text
Scheduler
   ↓
Find due accounts only
   ↓
Claim bounded batch
   ↓
Provider inquiry
   ↓
New bill?
   ├── No → schedule next check
   └── Yes
         ↓
      Persist bill
         ↓
      Update interval incrementally
         ↓
      Notify once
         ↓
      Schedule next check
```

## Electricity Operation

```text
Month begins
   ↓
Day 1–3
   ↓
Morning batches
   ↓
Provider inquiry
   ↓
New bill?
   ├── No → no notification
   └── Yes → persist + notify once
```

---

# 37. Recommended Initial Water Scheduling Policy

The exact values should remain configurable.

Recommended concept:

```text
EstimatedNextBillDate
        -
SafetyLeadDays
        =
NextCheckAt
```

Then use bounded follow-up checks around the expected billing window rather than checking every account every day.

After a new bill is found, the account returns to its calculated schedule.

This is preferable to permanent daily scraping.

---

# 38. Anti-Load Rules

The implementation must explicitly prohibit:

- One giant midnight job processing all accounts.
- Unlimited parallel scraping.
- Scraping from frontend requests.
- Reprocessing all historical bills every day.
- Recalculating every user's average every day.
- Retrying provider failures immediately in a tight loop.
- Sending notifications directly from every scraper attempt.
- Running duplicate workers against the same account.

---

# 39. Testing Requirements

## Domain Tests

- Billing interval calculation.
- Incremental average update.
- Estimated next bill date.
- Boundary cases.
- Missing historical data.

## Repository Tests

- Due-account selection.
- Tenant isolation.
- Duplicate bill prevention.
- Correct ordering.
- Active/inactive account filtering.

## Concurrency Tests

- Two workers cannot claim the same account.
- Duplicate insertion is rejected safely.
- Retry does not duplicate bills.
- Retry does not duplicate notifications.

## Application Tests

- New water account initializes historical processing.
- New electricity account is scheduled correctly.
- New bill produces one notification.
- Existing bill produces no notification.
- Provider failure does not corrupt state.

## API Tests

- Tenant cannot access another tenant's utility data.
- Unlinked accounts return the correct empty state.
- Bills endpoint never invokes provider scraping.

---

# 40. Implementation Constraints

Before implementation:

1. Inspect the existing AqariOS architecture.
2. Reuse existing:
   - Multi-tenancy.
   - RLS.
   - Hangfire/background-job patterns.
   - Notification infrastructure.
   - Repository conventions.
   - Transaction patterns.
   - Localization.
3. Do not duplicate existing infrastructure.
4. Do not modify unrelated financial or leasing logic.
5. Do not introduce Excel as a persistence layer.
6. Do not introduce external provider calls into controllers.
7. Do not execute provider calls synchronously during tenant page requests.

---

# 41. Migration Requirements

The feature will require database migrations for the new utility entities and constraints.

No migration should be generated or applied until the physical schema has been reviewed.

Required physical protections include:

```text
Unique lease + utility type
Unique utility account + provider bill identity
Scheduling indexes
Tenant/company ownership
```

---

# 42. Definition of Done

The feature is complete only when:

- [ ] Electricity account can optionally be linked to a lease.
- [ ] Water account can optionally be linked to a lease.
- [ ] Utility data is stored in PostgreSQL, not Excel.
- [ ] Tenant can view utility status.
- [ ] Tenant has a dedicated "My Bills" area.
- [ ] Electricity synchronization runs only during the configured beginning-of-month window.
- [ ] Electricity work is distributed into bounded morning batches.
- [ ] Water uses historical billing intervals.
- [ ] Water interval statistics are updated incrementally.
- [ ] Water does not perform unnecessary daily scraping.
- [ ] Scheduler queries only due accounts.
- [ ] Batch processing is bounded.
- [ ] `SKIP LOCKED`/equivalent claiming prevents duplicate workers.
- [ ] Bill insertion is idempotent.
- [ ] Notifications are idempotent.
- [ ] Provider failures cannot slow normal tenant requests.
- [ ] Tenant/company isolation is enforced.
- [ ] No Excel persistence remains.
- [ ] Unit tests cover interval calculation and idempotency.
- [ ] Integration tests cover tenant isolation and persistence.
- [ ] Concurrency behavior is tested.
- [ ] Existing AqariOS functionality remains unaffected.

---

# 43. Final Architecture Principle

The central design principle is:

> **Predict once, schedule intelligently, query only when necessary, persist the result, and never repeatedly reprocess history.**

AqariOS should not attempt to continuously "guess" when every user's bill will arrive.

Instead:

```text
Historical data
      ↓
One-time baseline
      ↓
Stored billing interval
      ↓
Estimated next billing window
      ↓
Targeted synchronization
      ↓
New bill
      ↓
Incremental update
      ↓
Next targeted synchronization
```

This provides a system that is:

- Lightweight.
- Idempotent.
- Concurrency-safe.
- Provider-isolated.
- Tenant-safe.
- Resistant to provider outages.
- Suitable for scaling to a large number of leases.
