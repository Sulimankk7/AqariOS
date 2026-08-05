# Azure Blob Storage Infrastructure Architecture & Deferred Deletion Policy

## 1. Overview & Context

This document defines the architectural design, implementation principles, security posture, and lifecycle policies for object storage in PropertyOS, specifically detailing the integration of **Azure Blob Storage** alongside the existing **Physical File Storage** provider.

### Motivation
PropertyOS requires production-grade, highly scalable, and durable object storage to handle large volumes of documents across thousands of tenants. While local physical file storage was sufficient for early development and single-server environments, cloud object storage (Azure Blob Storage) is required for multi-region deployment, automated high-availability, zero-maintenance capacity expansion, and direct client-to-storage presigned uploads.

### Additive Architecture Principle
The introduction of Azure Blob Storage is an **additive infrastructure extension**, not a refactor or redesign. The core system architecture—including API contracts, Domain entities, Command/Query handlers, Application interfaces, and database schemas—remains strictly untouched. 

---

## 2. Provider Architecture (`IStorageProvider`)

PropertyOS abstracts file storage behind the `IStorageProvider` contract located in `PropertyOS.Application.Files.Services`.

```
IStorageProvider  (Application Layer — Storage Agnostic)
    ├── PhysicalFileStorageProvider  (Infrastructure — Local Disk Storage)
    └── AzureBlobStorageProvider     (Infrastructure — Azure Blob Storage)
```

### Application Layer Storage-Agnostic Isolation
The Application layer interacts exclusively with `IStorageProvider`. It contains zero dependencies on `Azure.Storage.Blobs` SDK types, Azure configuration keys, or provider-specific logic. 

- **Application & Domain**: Clean from cloud vendor lock-in.
- **Infrastructure Layer**: Encapsulates all Azure SDK calls (`BlobServiceClient`, `BlobContainerClient`, `BlobSasBuilder`).

### Configuration-Driven Selection
Provider selection is controlled via configuration in `appsettings.json` (or environment variables) under the `FileStorage` section:

```json
{
  "FileStorage": {
    "Provider": "AzureBlob",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ContainerName": "documents",
    "PreSignedUrlExpirationMinutes": 15,
    "MaxSizeBytes": 52428800,
    "BlobRetentionDays": 30
  }
}
```

The Dependency Injection container in `PropertyOS.Infrastructure.DependencyInjection` uses a factory delegate to resolve `IStorageProvider` dynamically based on `FileStorageOptions.Provider`:

- `Provider = "AzureBlob"` → Resolves `AzureBlobStorageProvider`
- `Provider = "Physical"` (or default fallback) → Resolves `PhysicalFileStorageProvider`

---

## 3. Azure Client Lifetime & Initialization

### Singleton Lifetime Rationale
`BlobServiceClient` and `BlobContainerClient` are registered in the DI container as **Singletons**.
- **Performance**: `BlobServiceClient` is thread-safe and internally maintains an HTTP connection pool. Recreating it per request incurs substantial allocation and TCP connection overhead, risking socket exhaustion under load.
- **Thread Safety**: All Azure SDK client instance methods are safe for concurrent execution across request threads.

### Fail-Fast Startup Behavior
During container client initialization, `AzureBlobStorageProvider` verifies that the target container exists (`container.Exists()`). If the container does not exist or configuration is missing:
- An explicit `InvalidOperationException` is thrown at startup with actionable error messages.
- The provider **never auto-creates containers** at runtime, respecting infrastructure-as-code and pre-provisioned security boundary requirements.

---

## 4. Upload & Download Workflows

PropertyOS uses a **3-Phase Upload Workflow** (`Upload-Request` → `Binary Upload` → `Confirm`).

### 4.1 Upload Flow Comparison

```
[ Physical Provider ]
Client ──(1) POST upload-request──> API ──> Returns HMAC Upload URL (/api/v1/files/upload?key=...&sig=...)
Client ──(2) PUT binary payload───> API (FilesController) ──> PhysicalFileStorageProvider.SaveAsync
Client ──(3) POST confirm─────────> API ──> Validates file & creates FileStorage DB record

[ Azure Blob Provider ]
Client ──(1) POST upload-request──> API ──> Returns Azure SAS Write URL (blob.core.windows.net/.../key?sv=...)
Client ──(2) PUT binary payload───> Azure Blob Storage Directly (Bypasses API)
Client ──(3) POST confirm─────────> API ──> Validates blob in Azure & creates FileStorage DB record
```

#### Opaque Presigned URLs
The frontend treats the returned `uploadUrl` as an opaque URL string. It performs a direct HTTP `PUT` using standard browser `XMLHttpRequest` without injecting API-specific headers (e.g., `Authorization: Bearer`), avoiding CORS or authentication conflicts when uploading directly to Azure Storage.

### 4.2 Download Flow
1. Client requests document download URL via API (`GetDocumentDownloadUrl`).
2. API verifies tenant scope and checks that the document is active (`DeletedAt == null`).
3. Provider generates a short-lived download URL:
   - **Physical**: Presigned API URL with HMAC signature (`/api/v1/files/download?key=...&sig=...`).
   - **Azure**: Pre-signed Azure SAS Read URL with `Content-Disposition: attachment; filename="..."` header.
4. Client fetches file directly from storage.

---

## 5. Security Posture & Presigned SAS URLs

Azure presigned URLs are constructed using `BlobSasBuilder` with strict security constraints:

1. **HTTPS Only**: `Protocol = SasProtocol.Https` is strictly enforced on all SAS tokens.
2. **Time-Limited**: Tokens expire after `PreSignedUrlExpirationMinutes` (default: 15 minutes).
3. **Minimum Scope**:
   - Upload SAS: `BlobSasPermissions.Write | BlobSasPermissions.Create` only.
   - Download SAS: `BlobSasPermissions.Read` only.
   - Scoped strictly to the specific blob key (never container-wide or account-wide).
4. **Credential Isolation**: Presigned SAS URLs and storage connection strings are never logged to application output or telemetry.

---

## 6. Streaming Behavior & Memory Efficiency

To support high-throughput operations and large files without memory pressure:

1. **Upload Streaming**: Binary payloads uploaded to Azure are streamed directly from input streams to the blob endpoint without buffering into intermediate byte arrays or `MemoryStream` instances.
2. **Download Streaming**: Reading a blob via `GetReadStreamAsync` returns a live network stream from Azure's `DownloadStreamingAsync`.
3. **8-Byte Magic Byte Confirmation**: During file confirmation (`ConfirmFileUpload`), `FileValidationService` reads only the first **8 bytes** of the header stream to verify file signature magic bytes. The stream is immediately disposed thereafter, avoiding full blob downloads during confirmation.

---

## 7. StorageKey & Backward Compatibility

- **StorageKey Format**: Unchanged `{companyId}/{module}/{entityId}/{fileId}-{filename}`.
- **FileId**: Unchanged version-7 UUID (`Guid.CreateVersion7()`).
- **Database Schema**: Zero schema migrations or column changes. Existing `file_storage` rows in PostgreSQL remain 100% valid regardless of provider.

---

## 8. Deferred Blob Deletion Policy

### Architectural Policy Statement
PropertyOS strictly enforces a **Soft Delete** philosophy across all domain entities (`ISoftDeletable`). The object storage subsystem follows this identical philosophy:

> **When a document or file record is deleted in PropertyOS, the underlying physical storage blob MUST NOT be deleted synchronously within the HTTP request cycle.**

### Rationale & Rules
1. **Synchronous Request Isolation**: Deleting a document soft-deletes the `BuildingDocument` and `FileStorage` records in PostgreSQL (`DeletedAt` is set). The document immediately becomes invisible in the UI and inaccessible for download URL generation.
2. **Data Loss Protection**: Immediate hard deletion of blobs risks unrecoverable data loss during accidental user deletions or transaction rollbacks.
3. **Auditability**: Deferred deletion ensures historical audit compliance and allows administrative restoration within the retention window.

---

## 9. Mandatory Component: Storage Retention Job (TODO)

> [!IMPORTANT]  
> **TODO [Production Requirement]**: A background **Storage Retention Job** must be implemented prior to production release as a mandatory infrastructure background service.

### Retention Job Specifications
1. **Execution Model**: Infrastructure background worker (e.g., Quartz.NET, Hangfire, or HostedService) running on a daily schedule.
2. **Retention Period**: Configured via `FileStorage:BlobRetentionDays` (default: `30` days).
3. **Target Selection**:
   - Queries `file_storage` records where `DeletedAt IS NOT NULL` AND `DeletedAt < (UtcNow - BlobRetentionDays)`.
   - Verifies that the `StorageKey` is not referenced by any active (`DeletedAt IS NULL`) records.
4. **Provider-Agnostic Execution**: Invokes `IStorageProvider.DeleteAsync(storageKey)` so that retention processing works uniformly across Azure Blob Storage, Physical Storage, or future providers (S3/MinIO).
5. **Resilience & Idempotency**:
   - Processes deletions in batches.
   - Logs every successful and failed blob deletion explicitly with structured context.
   - Failure to delete a single blob must **not** abort the entire job batch.
   - Safe to re-run multiple times (idempotent).

---

## 10. Future Authentication Hardening: Managed Identity

The initial Azure Blob Storage implementation uses connection string authentication with a shared account key (`StorageSharedKeyCredential`). 

### Future Migration Path
To comply with enterprise zero-trust standards, the system can transition to **Azure Managed Identity** (`DefaultAzureCredential`) without changing the core storage architecture:

1. **DI Update Only**: Modify `DependencyInjection.cs` to instantiate `BlobServiceClient` using `Uri` and `DefaultAzureCredential`.
2. **User Delegation SAS**: SAS URL generation will use `BlobContainerClient.GetUserDelegationKeyAsync` to sign SAS tokens dynamically.
3. **Zero Layer Impact**: Application code, `IStorageProvider`, handlers, and controllers remain 100% unchanged.

---

## 11. Summary of Provider Capabilities

| Feature | PhysicalFileStorageProvider | AzureBlobStorageProvider |
|---|---|---|
| Provider Key | `"Physical"` | `"AzureBlob"` |
| Transport / Target | Local Disk / Network Share | Azure Blob Container |
| Presigned Upload | HMAC API Proxy URL | Azure SAS Write URL (Direct) |
| Presigned Download | HMAC API Proxy URL | Azure SAS Read URL (Direct) |
| Magic-Byte Read | Direct FileStream (8 bytes) | Azure Network Stream (8 bytes) |
| Blob Deletion Policy | Deferred (Retention Job) | Deferred (Retention Job) |
