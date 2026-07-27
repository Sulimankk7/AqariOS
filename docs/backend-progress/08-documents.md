# Phase 08 — Documents (Module 10) + cross-cutting Files

## Initial State
Full Domain/Application/Infrastructure; no API; two-phase upload design whose binary endpoints did not exist; severe file-handling security gaps.

## Problems Found / Implementation Completed
1. Systemic: 9 command records `IRequest`→`ICommand`/`ICommand<T>` (persistence restored); exception model migrated (incl. two QUERY handlers that threw raw `KeyNotFoundException`; category duplicates → Conflict; `DOCUMENT_CATEGORY_IN_USE`; `FILE_VALIDATION_FAILED` wrapping of validator ArgumentExceptions); 2 missing delete validators.
2. **Path traversal closed**: `PhysicalFileStorageProvider.GetFullPath` canonicalizes and rejects rooted/escaping keys (unit-tested with `../`, `C:\Windows\win.ini`, `/etc/passwd`, UNC, mixed separators).
3. **Cross-tenant file registration closed**: `ConfirmFileUpload` enforces the `{companyId}/` key prefix and binds the confirmation to the issued `FileId` (also persisted as the row Id via a new optional factory parameter, keeping key↔row identity coherent).
4. **Real signed capability URLs**: `IFileUrlSigner`/`HmacFileUrlSigner` (HMAC-SHA256 over purpose+key+absolute-expiry, fixed-time compare, fail-closed without `FileStorage:UrlSigningSecret` ≥32 bytes — NEW required deployment config). Replaces the previous random, unverifiable tokens.
5. **Binary endpoints exist**: `FilesController` — authenticated `upload-request`/`confirm`, token-authenticated `PUT /files/upload` (411/413, bounded read backstop against under-declared Content-Length) and `GET /files/download` (nosniff, attachment disposition, octet-stream).
6. **Size verification**: declared size must equal on-disk size at confirm (`FILE_SIZE_MISMATCH`); provider gained `GetSizeAsync`.
7. Full Documents API: categories + building documents controllers (12 endpoints), `DocumentsPermissions`, policies wired. Deviations recorded: DTO-returning commands → 200/201 with DTO bodies; replace → 201 to the new document.

## Database Impact
None (RLS + GRANTs pre-existed for the three tables).

## Security Review
See items 2–6. `documents.view_confidential` enforcement lives in the query handlers (repository checks); the registered-but-unwired `ConfidentialDocumentAuthorizationHandler` remains a Phase-12 note.

## Tests / Verification
`FileStorageSecurityTests` (10 tests: containment matrix, signer round-trip/tamper/expiry/fail-closed, save/read/size round-trip); document command tests extended. Suites green (703/703, 118/118).

## Remaining Issues
Magic-byte coverage limited to pdf/png/jpg (webp/docx/xlsx unchecked — ZIP-container sniffing tracked); no AV scanning (infrastructure decision); `SaveAsync`/`DeleteAsync` now have callers via the upload endpoint but document deletion does not yet delete blobs (soft-delete keeps them — acceptable, documented).

## Phase Result
COMPLETE.
