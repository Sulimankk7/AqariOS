# Phase 10 — Cross-cutting: Files & Reports

## Files
Covered in depth by `08-documents.md` (the Files subsystem ships with Module 10): two-phase signed upload/download, path containment, tenant key containment, size verification, fail-closed HMAC signing, bounded uploads. **Result: COMPLETE** (S3/MinIO provider remains a deployment-time swap behind `IStorageProvider` — `AWSSDK.S3` is referenced but the physical provider is the configured implementation; acceptable per current scope).

## Reports
**Result: BLOCKED — missing approved specification.** Evidence:
- All four `Reports/` project folders exist and are empty; no entities, migrations, or DbSets.
- `docs/PropertyOS_Backend_Architecture.md` names `report_definitions`/`report_snapshots` and the `reports.export` permission, but **no module document specifies these tables**.
- `docs/PropertyOS_Architecture_Review.md` (§78, §319) explicitly flags this: physical design for the two report tables "never specified in any module doc — specify before the Reports feature is built; not a blocker for the 11 modules already delivered."

Per the execution policy (approved docs are the business/schema source of truth; unresolved business decisions are a stop condition for that item), inventing the reporting schema would violate the mission constraints. The `reports.export` permission constant already exists in the catalog for when the design lands. Everything not dependent on Reports has proceeded.

**Required to unblock:** an approved design pass for `report_definitions` / `report_snapshots` (+ the report use-case list). 
