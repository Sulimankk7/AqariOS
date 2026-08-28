# Plans, Subscriptions, and Plan Change Requests - Backend Implementation

Date: 2026-08-27

## Source Document

The backend implementation was based on the exact Business Rules document located at:

`docs/AqariOS_Plans_Subscriptions_Business_Rules.md`

The originally referenced path was not used. The file was located by filename and read directly before implementation.

## Phase 0 Audit Summary

Reusable backend pieces found:

- Existing subscription plan and company subscription domain models.
- Existing subscription enums for status and billing cycle.
- Existing price/currency snapshot fields on company subscriptions.
- Existing partial unique index concept for one current subscription per company.
- Existing PostgreSQL `xmin` concurrency support patterns.
- Existing tenant context, RLS session context, MediatR request pipeline, validation, global exception handling, audit, and permission catalog patterns.

Conflicts with the Business Rules:

- Tenant-facing direct subscribe, direct plan change, and direct cancellation flows existed.
- Subscription plans could be modified even after being used by subscriptions.
- Active plan listing was previously anonymous.
- The legacy subscription service could silently create a company during subscribe.
- Platform Admin subscription and plan change request APIs were missing.
- Company subscription RLS did not include a Platform Admin policy.

Business-rule ambiguity documented:

- `SortOrder` is display ordering only.
- No upgrade/downgrade ranking rule exists.
- No proration, paid-date shift, scheduled effective date, or billing provider behavior is defined.
- Approval therefore only applies the requested plan, billing cycle, and price/currency snapshot atomically.

## Backend Changes Implemented

Domain:

- Added `PlanChangeRequest`.
- Added `PlanChangeRequestStatus`.
- Added domain state transitions for create, cancel, approve, and reject.
- Added `SubscriptionPlan.Activate()` and `SubscriptionPlan.Deactivate()`.
- Added `CompanySubscription.ApplyApprovedPlanChange()`.

Application:

- Added subscription administration DTOs.
- Added plan change request DTOs.
- Added CQRS use cases for:
  - active plan listing for Company Admins,
  - Platform Admin plan listing/detail/create/activate/deactivate,
  - Company Admin current subscription read,
  - Platform Admin subscription listing/detail/create,
  - Company Admin plan change request create/list/cancel,
  - Platform Admin plan change request list/detail/approve/reject.
- Added company and subscription permissions.
- Marked legacy direct subscription service mutation methods as obsolete and made them throw business-rule exceptions.

API:

- Replaced the tenant subscription controller with Business Rules aligned endpoints.
- Kept legacy subscribe/change/cancel routes as obsolete authenticated endpoints returning `410 Gone`, with no mutations.
- Added Platform Admin controllers for:
  - plans,
  - subscriptions,
  - plan change requests.
- Added authorization policies for all new subscription permissions.
- Added global exception mappings for duplicate/immutable subscription business-rule violations.

Infrastructure and Persistence:

- Added `PlanChangeRequestConfiguration`.
- Added `PlanChangeRequests` DbSet wiring.
- Added Npgsql enum mappings for subscription and plan change request enums.
- Added manual migration `20260827170000_PlansSubscriptionsBusinessRules`.
- Updated the EF model snapshot manually.
- Added database constraints and triggers for:
  - one pending plan change request per company,
  - immutable plan change request intent,
  - platform-only approve/reject transitions,
  - company-only pending cancellation transition,
  - immutable commercial fields for plans already used by subscriptions,
  - restricted delete behavior for referenced plans/subscriptions/users.
- Added RLS policies for plan change requests and Platform Admin access to company subscriptions.

Tests Added or Updated:

- Domain unit tests for plan change request transitions.
- Validator tests for subscription business-rule requests.
- API authorization tests for Company Admin subscription endpoints.
- API authorization tests for Platform Admin subscription administration endpoints.
- Integration test source coverage for:
  - used plan immutability,
  - one pending plan change request per company,
  - plan change request RLS visibility and insert scope,
  - referenced plan delete restriction,
  - plan deactivation preserving existing subscription snapshots.
- Existing PostgreSQL test fixtures were updated with the new enum mapping.

## Explicit Non-Changes

The following were intentionally not changed:

- Frontend source.
- Docker configuration.
- Database state.
- Generated EF migration execution state.
- Billing provider integration.
- Mock endpoints.
- Business rules not present in the source document.

## Verification

Performed:

- Static repository inspection.
- `git diff --check -- docs src tests`

Result:

- Scoped backend/docs/tests diff check passed.
- A full `git diff --check` still reports pre-existing frontend trailing whitespace in `frontend/src/features/auth/constants/translations.ts`, which was not modified as part of this task.

Not performed, per instruction:

- `dotnet build`
- `dotnet test`
- `dotnet ef`
- migrations
- database updates
- Docker commands

Manual build, test, migration review, and database verification are still required.
