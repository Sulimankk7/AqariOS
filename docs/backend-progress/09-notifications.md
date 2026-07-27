# Phase 09 — Notifications (Module 11)

## Initial State
Full Domain/Application/Infrastructure; no API, **no RLS at all** (the Module 11 migration contained zero policies AND zero GRANTs), no SignalR hub, no channel providers, no dispatch — created notifications stayed Pending forever. No tests of any kind. Plus the systemic `IRequest` persistence defect.

## Requirements Reviewed
`docs/PropertyOS_Module11_Notifications_FINAL.md` §11.7 (per-row recipient ownership — "the schema's one genuinely per-row restriction"; PDPL content-confidentiality framing), §11.2/11.3 state machines.

## Implementation Completed
1. Systemic: 6 commands → `ICommand`/`ICommand<Guid>`; exception model (recipient mismatch masked as 404; `NOTIFICATION_DELIVERY_INVALID_TRANSITION`, `NOTIFICATION_READ_INVALID_STATE`, template duplicate → Conflict); missing validators; UUIDv7 for `Notification`/`NotificationTemplate` (deliveries stay DB-generated — created only on new roots pre-Add, safe by the navigation rule).
2. **RLS migration `20260726180807_Module11_NotificationsRls`**: GRANTs + company policies (templates, deliveries) + notifications split policies — INSERT company-checked only (staff address other recipients); SELECT/UPDATE/DELETE require company AND (recipient ownership OR unset-user system context for jobs). The doc's permission-gated `notifications.view_all` RLS bypass is deferred per the doc's own "dedicated design pass" note; the REST layer enforces a `ViewAll` policy instead. Verified by 4 new RLS integration tests (per-row isolation between two users of the SAME company, system context, cross-tenant, templates).
3. **Dispatch pipeline**: `INotificationChannelProvider` registry (InApp via `IInAppNotificationPusher` → Api-side `SignalRInAppNotificationPusher` over `IHubContext<NotificationsHub>`; honest Null providers for Email/SMS/WhatsApp returning NotConfigured until real gateways exist); `DispatchNotificationCommand` with a domain-derived dispatchable-state rule (Cancelled/deleted never; Pending always; Sent/Failed only with retryable deliveries; `MarkAsDelivered` reserved for channel callbacks), attempt-count recorded before send, retry cap `NotificationDispatchPolicy.MaxDeliveryAttempts = 3` (app policy — domain defines no constant), parent transition (≥1 sent → Sent sticky; all terminal-failed or zero channels → Failed); `DispatchNotificationsJob` (multi-tenant sweep, every 5 min Asia/Amman, attempted-ID exclusion so batches drain).
4. **Hub + auth**: `NotificationsHub` mapped at `/hubs/notifications`; JWT `access_token` query-string support scoped to hub paths.
5. **API**: notifications (me/unread/read/company/create/delivery-update/failed) + templates CRUD, `NotificationsPermissions` (send/manage_templates/view_all) policies wired.
6. **First test suites for the module**: 18 command-handler tests, 2 domain state-machine test files, dispatch handler tests (mixed channels, retry cap, not-configured convergence, no-op states), job tests, 4 RLS tests.

## Database Impact
One migration (GRANTs + policies) — generated, NOT applied locally; Testcontainers verified.

## Transaction / Multi-Tenancy Review
Dispatch runs per-notification commands under TransactionBehavior with company-scoped RLS context; recipient predicate active for user requests, system context for jobs.

## Verification Results
Suites: unit 703/703, integration 118/118, 0 warnings.

## Remaining Issues
Real Email/SMS/WhatsApp providers (external credentials); `notifications.view_all` RLS-level bypass (REST-level exists); event bridge from other modules (needs the missing platform domain-event dispatcher — cross-cutting note); delivery `Delivered` confirmations depend on future channel callbacks.

## Phase Result
COMPLETE (within approved scope; external-gateway items tracked as blockers above).
