# AqariOS Subscription Creation — Company Selection and Verification

## Outcome

The approved Create Subscription flow is now functional without exposing company or plan identifiers. Platform administrators select an active company by display name, select an active plan, review its compact preview, choose monthly or yearly billing, choose a start date, optionally enable the plan-defined trial, and submit the existing subscription creation contract.

## Backend audit and implementation

No existing HTTP endpoint exposed the company selection data. The application layer already contained `GetPlatformCompaniesQuery`, its handler, and `PlatformCompanyListItemDto`. The handler returns only active, non-deleted companies ordered by display name, with only the company ID and display name.

The smallest missing capability was added:

- `GET /api/v1/platform/companies`
- restricted to the `SYSTEM_ADMIN` role
- restricted by the existing `platform.subscriptions.manage` policy
- delegates to the existing `GetPlatformCompaniesQuery`
- returns the existing `PlatformCompanyListItemDto`

No new permission, repository, business rule, persistence behavior, migration, or database schema was introduced.

## Frontend behavior

- The company selector loads real companies from the platform endpoint.
- Dropdown options display company names only; company IDs remain internal values.
- Empty and failed requests show localized user-facing messages.
- The plan selector continues to load active plans only.
- The compact plan preview remains and shows localized names, prices, limits, and plan-defined trial information.
- Billing cycle is a two-option Monthly/Yearly control.
- Start date defaults to the user's local current date when the dialog opens.
- End date is calculated automatically using calendar-month/calendar-year arithmetic and is not editable.
- Trial is disabled by default and can be enabled only for a plan that supports it. Its end date is calculated from the plan's configured duration.
- Submission uses the existing request contract: `companyId`, `planId`, `billingCycle`, `startDate`, calculated `endDate`, and calculated optional `trialEndDate`.
- Existing machine-readable subscription conflict mapping remains unchanged.

## Security verification

Controller authorization tests verify that the company endpoint:

- requires the `SYSTEM_ADMIN` platform role;
- requires `platform.subscriptions.manage`; and
- uses a platform-only permission.

The client does not treat the dropdown as a security boundary. The server continues to authenticate and authorize the request, while the existing subscription creation handler validates the submitted identifiers and business rules.

## Files changed for this fix

- `src/PropertyOS.Api/PlatformAdministration/CompaniesController.cs`
- `tests/PropertyOS.Tests.Unit/Api/PlatformAdministration/SubscriptionAdministrationAuthorizationTests.cs`
- `frontend/src/features/subscriptions/types/subscriptions.types.ts`
- `frontend/src/features/subscriptions/api/subscriptions.api.ts`
- `frontend/src/features/subscriptions/hooks/useSubscriptions.ts`
- `frontend/src/features/subscriptions/pages/PlatformSubscriptionsPage.tsx`
- `frontend/src/shared/i18n/translations/en.ts`
- `frontend/src/shared/i18n/translations/ar.ts`

## Verification results

### Frontend

`npm run build`

- Result: PASS
- Vite transformed 2,546 modules and produced the production bundle.
- Vite emitted its existing bundle-size advisory for a JavaScript chunk larger than 500 kB; this is not a compilation error.

### Backend

`dotnet build PropertyOS.sln --no-restore -v:minimal`

- Result: PASS
- Warnings: 0
- Errors: 0

`dotnet test tests/PropertyOS.Tests.Unit/PropertyOS.Tests.Unit.csproj --no-build --no-restore -v:minimal`

- Result: PASS
- Total: 1,129
- Passed: 1,129
- Failed: 0
- Skipped: 0

The first backend build attempt was blocked by an already-running local `PropertyOS.Api` development process holding output DLLs. Only that exact development process was stopped; the rerun passed with zero warnings and zero errors.

## Verification boundary

The data flow and contracts were verified through source inspection, frontend compilation, backend compilation, and unit authorization coverage. A browser smoke test was not performed because permission to access the local browser URL was not granted. No visual or live API result is claimed.

No migrations, database updates, Entity Framework commands, Docker commands, mock companies, or hardcoded company options were used.
