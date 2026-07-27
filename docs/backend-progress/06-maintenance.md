# Phase 06 — Maintenance (Module 8)

## Initial State
Full Domain/Application/Infrastructure; no API. Two systemic defects shared with Modules 10/11: all 9 command records were plain `IRequest` (TransactionBehavior never opened a transaction nor called SaveChanges — **no maintenance write ever persisted**), and handlers threw raw BCL exceptions (→ 500s). Domain events raised with no dispatcher.

## Requirements Reviewed
`docs/PropertyOS_Module8_Maintenance.md`; transition graph in `MaintenanceRequest.AllowedTransitions`.

## Problems Found / Implementation Completed
1. All 9 commands converted to `ICommand`/`ICommand<Guid>` (persistence restored).
2. Exception model migrated (NotFound; BusinessRule codes `MAINTENANCE_INVALID_TRANSITION`, `MAINTENANCE_COMMENT_*`, `MAINTENANCE_ATTACHMENT_*`, `MAINTENANCE_REQUEST_DELETED/ALREADY_DELETED`).
3. IDs: `MaintenanceRequest`/`Attachment`/`Comment` factories now client-generate UUIDv7 (their IDs are returned by create endpoints); attachments/comments created on tracked parents are explicitly Added via new `AddAttachmentAsync`/`AddCommentAsync` repository methods (navigation-discovery rule); status history stays DB-generated (explicit adds, never returned pre-save).
4. 7 missing validators added (comment text ≤4000, description ≤255 matching column, enum checks).
5. Full API: `MaintenanceRequestsController` (12 endpoints incl. comments/attachments/status-history subresources) + 6 request models + `MaintenancePermissions` (create/update_status/comment) with policies in Program.cs. Client-supplied `UploadedBy` removed from the attachment request (actor-attribution spoofing).
6. Integration tests fixed for client-keyed children (explicit adds at 3 sites).

## Database Impact / Multi-Tenancy / Transactions
None (RLS pre-existed for all 4 tables). Writes now genuinely transactional via TransactionBehavior.

## Tests / Verification
Handler tests extended (invalid transition w/ code, add-comment/attachment success + guards, double-delete); domain tests assert UUIDv7 IDs. Suites: unit 703/703, integration 118/118.

## Remaining Issues
Domain event `MaintenanceRequestStatusChangedEvent` still has no dispatcher (candidate bridge: create a Notification on status change — needs product wording/template decision, tracked). No SLA/escalation job is specified in the doc — none built.

## Phase Result
COMPLETE.
