# AqariOS — Full API Audit

> Generated from the current source tree on 2026-09-08. Backend routes come from controller attributes; Web/Mobile usage comes from production client source under `frontend/src` and `mobile/lib`. Tests and generated build files are excluded.

## Executive summary

- Backend: **214 endpoints** in **43 controllers**.
- Web: **157 HTTP call sites**, matching **0 backend endpoints**.
- Mobile: **45 HTTP call sites**, matching **0 backend endpoints**.
- Client calls without an exact static route+method match: **202** (dynamic route construction and query strings can require manual confirmation).
- Standard authenticated calls use `Authorization: Bearer <JWT>`. Authentication refresh can also use the secure `refreshToken` cookie. Company scope is resolved server-side from JWT; `companyId` is not a normal tenant request-body field.
- Standard errors use ASP.NET `ProblemDetails` / `ValidationProblemDetails`; common statuses are 400, 401, 403, 404, 409, 422, 429, and 500. Individual declared statuses are listed per endpoint.

## How to read body and response types

Each endpoint shows route/query/body inputs and declared response variants. Complex type fields are listed in the schema catalogue after the endpoint inventory. JSON property names follow ASP.NET camelCase serialization (for example `LeaseContractId` → `leaseContractId`). Enum values are serialized according to the server JSON enum configuration; verify numeric-vs-string behavior before integrating a new client.

## Backend endpoints, file by file

### LeaseContractsController

Source: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs`

#### GET `/
    [Authorize(Policy = LeasingPermissions.Create)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: GetNextNumber`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:51`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Activate`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:231`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `TerminateLeaseContractRequest` (see schema catalogue)
- Responses: `200: Terminate`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:254`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Approve)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `RenewLeaseContractRequest` (see schema catalogue)
- Responses: `200: Renew`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:291`

#### DELETE `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `documentId: Guid`
- Body: none
- Responses: `200: DeleteDocument`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:210`

#### PUT `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `documentId: Guid`
- Body: `ReplaceContractDocumentRequest` (see schema catalogue)
- Responses: `200: ReplaceDocument`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:192`

#### PUT `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateDraftLeaseContractRequest` (see schema catalogue)
- Responses: `200: UpdateDraft`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:100`

#### GET `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ContractDocumentDownloadUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `documentId: Guid`, `inline: bool (optional)`, `redirect: bool (optional)`
- Body: none
- Responses: `200: DownloadDocument`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:166`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateLeaseContractRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:62`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `AttachContractDocumentRequest` (see schema catalogue)
- Responses: `200: AttachDocument`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:139`

#### GET `/
    [ProducesResponseType(typeof(LeaseContractDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:328`

#### GET `/
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `daysAhead: int (optional)`
- Body: none
- Responses: `200: GetExpiring`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:370`

#### GET `/
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `searchTerm: string (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: Search`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:350`

#### GET `/
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `apartmentId: Guid`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetHistoryByApartment`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/LeaseContractsController.cs:389`

### TenantEmergencyContactsController

Source: `src/PropertyOS.Api/Leasing/TenantEmergencyContactsController.cs`

#### DELETE `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `contactId: Guid`
- Body: none
- Responses: `200: DeleteEmergencyContact`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantEmergencyContactsController.cs:129`

#### PUT `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `contactId: Guid`
- Body: `UpdateTenantEmergencyContactRequest` (see schema catalogue)
- Responses: `200: UpdateEmergencyContact`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantEmergencyContactsController.cs:101`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: `CreateTenantEmergencyContactRequest` (see schema catalogue)
- Responses: `200: CreateEmergencyContact`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantEmergencyContactsController.cs:75`

#### GET `/
    [ProducesResponseType(typeof(List<TenantEmergencyContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: none
- Responses: `200: GetEmergencyContacts`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantEmergencyContactsController.cs:40`

#### GET `/
    [ProducesResponseType(typeof(TenantEmergencyContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `contactId: Guid`
- Body: none
- Responses: `200: GetEmergencyContactById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantEmergencyContactsController.cs:56`

### TenantFamilyMembersController

Source: `src/PropertyOS.Api/Leasing/TenantFamilyMembersController.cs`

#### DELETE `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `familyMemberId: Guid`
- Body: none
- Responses: `200: DeleteFamilyMember`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantFamilyMembersController.cs:129`

#### PUT `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `familyMemberId: Guid`
- Body: `UpdateTenantFamilyMemberRequest` (see schema catalogue)
- Responses: `200: UpdateFamilyMember`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantFamilyMembersController.cs:101`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: `CreateTenantFamilyMemberRequest` (see schema catalogue)
- Responses: `200: CreateFamilyMember`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantFamilyMembersController.cs:75`

#### GET `/
    [ProducesResponseType(typeof(List<TenantFamilyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: none
- Responses: `200: GetFamilyMembers`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantFamilyMembersController.cs:40`

#### GET `/
    [ProducesResponseType(typeof(TenantFamilyMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `familyMemberId: Guid`
- Body: none
- Responses: `200: GetFamilyMemberById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantFamilyMembersController.cs:56`

### TenantVehiclesController

Source: `src/PropertyOS.Api/Leasing/TenantVehiclesController.cs`

#### DELETE `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `vehicleId: Guid`
- Body: none
- Responses: `200: DeleteVehicle`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantVehiclesController.cs:131`

#### PUT `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `vehicleId: Guid`
- Body: `UpdateTenantVehicleRequest` (see schema catalogue)
- Responses: `200: UpdateVehicle`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantVehiclesController.cs:102`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: `CreateTenantVehicleRequest` (see schema catalogue)
- Responses: `200: CreateVehicle`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantVehiclesController.cs:75`

#### GET `/
    [ProducesResponseType(typeof(List<TenantVehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: none
- Responses: `200: GetVehicles`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantVehiclesController.cs:40`

#### GET `/
    [ProducesResponseType(typeof(TenantVehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `vehicleId: Guid`
- Body: none
- Responses: `200: GetVehicleById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantVehiclesController.cs:56`

### ApartmentsController

Source: `src/PropertyOS.Api/Controllers/ApartmentsController.cs`

#### GET `/
    [Authorize(Policy = PropertyPermissions.Create)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `floorId: Guid`
- Body: none
- Responses: `200: GetNextNumber`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ApartmentsController.cs:46`

#### POST `/
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `floorId: Guid`
- Body: `CreateApartmentRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ApartmentsController.cs:65`

#### DELETE `/
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Archive`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ApartmentsController.cs:206`

#### GET `/
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<ApartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ApartmentDto>>> List(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? floorId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/apartments UserId={UserId} CompanyId={CompanyId}", 
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new ListApartmentsQuery(buildingId, floorId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    / <summary>
    / Retrieves an apartment by its unique identifier.
    / </summary>
    / <param name="id">Apartment unique identifier.</param>
    / <param name="cancellationToken">Cancellation token passed from request.</param>
    / <returns>Apartment details.</returns>
    / <response code="200">Returns apartment details.</response>
    / <response code="401">If unauthenticated.</response>
    / <response code="403">If user lacks properties.read permission.</response>
    / <response code="404">If apartment is not found or belongs to another tenant.</response>
    [HttpGet("api/v1/apartments/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(ApartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ApartmentsController.cs:110`

#### PUT `/
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateApartmentRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ApartmentsController.cs:170`

### FloorsController

Source: `src/PropertyOS.Api/Controllers/FloorsController.cs`

#### GET `/
    [Authorize(Policy = PropertyPermissions.Create)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid`
- Body: none
- Responses: `200: GetNextNumber`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/FloorsController.cs:46`

#### POST `/
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid`
- Body: `CreateFloorRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/FloorsController.cs:65`

#### DELETE `/
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Archive`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/FloorsController.cs:200`

#### GET `/
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<FloorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FloorDto>>> ListByBuilding(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/buildings/{BuildingId}/floors UserId={UserId} CompanyId={CompanyId}", 
            buildingId, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new ListFloorsQuery(buildingId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    / <summary>
    / Retrieves a floor by its unique identifier.
    / </summary>
    / <param name="id">Floor unique identifier.</param>
    / <param name="cancellationToken">Cancellation token passed from request.</param>
    / <returns>Floor details.</returns>
    / <response code="200">Returns floor details.</response>
    / <response code="401">If unauthenticated.</response>
    / <response code="403">If user lacks properties.read permission.</response>
    / <response code="404">If floor is not found or belongs to another tenant.</response>
    [HttpGet("api/v1/floors/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(FloorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/FloorsController.cs:104`

#### PUT `/
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateFloorRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/FloorsController.cs:164`

### ParkingSpotsController

Source: `src/PropertyOS.Api/Controllers/ParkingSpotsController.cs`

#### POST `/
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid`
- Body: `CreateParkingSpotRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingSpotsController.cs:55`

#### DELETE `/
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Archive`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingSpotsController.cs:177`

#### GET `/
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<ParkingSpotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ParkingSpotDto>>> ListByBuilding(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var query = new ListParkingSpotsQuery(buildingId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    / <summary>
    / Retrieves a parking spot by its unique identifier.
    / </summary>
    / <param name="id">Parking spot unique identifier.</param>
    / <param name="cancellationToken">Cancellation token passed from request.</param>
    / <returns>Parking spot details.</returns>
    / <response code="200">Returns parking spot details.</response>
    / <response code="401">If unauthenticated.</response>
    / <response code="403">If user lacks properties.read permission.</response>
    / <response code="404">If spot is not found or belongs to another tenant.</response>
    [HttpGet("api/v1/parking-spots/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(ParkingSpotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingSpotsController.cs:91`

#### PUT `/
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateParkingSpotRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingSpotsController.cs:143`

### ParkingAssignmentsController

Source: `src/PropertyOS.Api/Controllers/ParkingAssignmentsController.cs`

#### GET `/
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<ParkingAssignmentDto>), StatusCodes.Status200OK)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `undefined: undefined`
- Body: none
- Responses: `200: Ok`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingAssignmentsController.cs:40`

#### POST `/
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `assignmentId: Guid`
- Body: none
- Responses: `200: End`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingAssignmentsController.cs:47`

#### POST `/
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    ` — undefined

- Auth: `PropertyPermissions.Read`
- Route/query: `parkingSpotId: Guid`
- Body: none
- Responses: `200: ParkingAssignmentDto`, `204: empty`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/ParkingAssignmentsController.cs:21`

### FilesController

Source: `src/PropertyOS.Api/Files/FilesController.cs`

#### GET `/
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `key: string`, `expires: long`, `sig: string`, `inline: bool (optional)`
- Body: none
- Responses: `200: Download`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Files/FilesController.cs:138`

#### PUT `/
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status411LengthRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `key: string`, `expires: long`, `sig: string`
- Body: none
- Responses: `200: Upload`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Files/FilesController.cs:99`

#### GET `/
    [Authorize]
    [ProducesResponseType(typeof(FileDownloadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `id: Guid`, `inline: bool (optional)`
- Body: none
- Responses: `200: GetDownloadUrl`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Files/FilesController.cs:175`

#### POST `/
    [Authorize]
    [ProducesResponseType(typeof(FileStorageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `ConfirmFileUploadRequest` (see schema catalogue)
- Responses: `200: ConfirmUpload`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Files/FilesController.cs:76`

#### POST `/
    [Authorize]
    [ProducesResponseType(typeof(UploadFileRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `UploadFileRequestRequest` (see schema catalogue)
- Responses: `200: RequestUpload`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Files/FilesController.cs:52`

### EfawateercomController

Source: `src/PropertyOS.Api/Financials/EfawateercomController.cs`

#### POST `/
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: Webhook`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:259`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateEfawateercomTransactionRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:67`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: MarkSent`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:97`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `CancelEfawateercomTransactionRequest?` (see schema catalogue)
- Responses: `200: Cancel`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:120`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `ExpireEfawateercomTransactionRequest?` (see schema catalogue)
- Responses: `200: Expire`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:148`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `externalId: string`
- Body: none
- Responses: `200: Poll`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:176`

#### GET `/
    [ProducesResponseType(typeof(EfawateercomTransactionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:235`

#### GET `/
    [ProducesResponseType(typeof(List<EfawateercomTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `status: EfawateercomStatus? (optional)`, `paymentReference: string? (optional)`, `rentPaymentId: Guid? (optional)`, `lastSeenId: Guid? (optional)`, `lastSeenRequestTime: DateTimeOffset? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/EfawateercomController.cs:203`

### DocumentCategoriesController

Source: `src/PropertyOS.Api/Documents/DocumentCategoriesController.cs`

#### DELETE `/
    [Authorize(Policy = DocumentsPermissions.ManageCategories)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Delete`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/DocumentCategoriesController.cs:122`

#### PUT `/
    [Authorize(Policy = DocumentsPermissions.ManageCategories)]
    [ProducesResponseType(typeof(DocumentCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateDocumentCategoryRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/DocumentCategoriesController.cs:92`

#### POST `/
    [Authorize(Policy = DocumentsPermissions.ManageCategories)]
    [ProducesResponseType(typeof(DocumentCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateDocumentCategoryRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/DocumentCategoriesController.cs:62`

#### GET `/
    [ProducesResponseType(typeof(List<DocumentCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/DocumentCategoriesController.cs:45`

### BuildingDocumentsController

Source: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs`

#### DELETE `/
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Delete`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:208`

#### PUT `/
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateBuildingDocumentRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:141`

#### POST `/
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid`
- Body: `CreateBuildingDocumentRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:87`

#### POST `/
    [Authorize(Policy = DocumentsPermissions.Upload)]
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `ReplaceBuildingDocumentRequest` (see schema catalogue)
- Responses: `200: Replace`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:175`

#### GET `/
    [ProducesResponseType(typeof(BuildingDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:121`

#### GET `/
    [ProducesResponseType(typeof(DocumentDownloadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetDownloadUrl`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:249`

#### GET `/
    [ProducesResponseType(typeof(List<ExpiringDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `withinDays: int (optional)`
- Body: none
- Responses: `200: GetExpiring`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:230`

#### GET `/
    [ProducesResponseType(typeof(PagedBuildingDocumentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid`, `categoryId: Guid? (optional)`, `searchTerm: string? (optional)`, `pageNumber: int (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetForBuilding`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Documents/BuildingDocumentsController.cs:56`

### ChequesController

Source: `src/PropertyOS.Api/Financials/ChequesController.cs`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.ChequesRead)]
    [ProducesResponseType(typeof(List<ChequeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `daysAhead: int (optional)`
- Body: none
- Responses: `200: GetUpcoming`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ChequesController.cs:104`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.ChequesRead)]
    [ProducesResponseType(typeof(List<ChequeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `status: ChequeStatus? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetCheques`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ChequesController.cs:83`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `RecordChequeStatusChangeRequest` (see schema catalogue)
- Responses: `200: RecordStatusChange`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ChequesController.cs:47`

### ExpensesController

Source: `src/PropertyOS.Api/Financials/ExpensesController.cs`

#### DELETE `/
    [Authorize(Policy = FinancialsPermissions.ExpensesApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Delete`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:184`

#### PUT `/
    [Authorize(Policy = FinancialsPermissions.ExpensesCreate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateExpenseRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:148`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.ExpensesCreate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateExpenseRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:50`

#### DELETE `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `receiptId: Guid`
- Body: none
- Responses: `200: RemoveReceipt`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:239`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `AttachExpenseReceiptRequest` (see schema catalogue)
- Responses: `200: AttachReceipt`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:207`

#### GET `/
    [ProducesResponseType(typeof(ExpenseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:126`

#### GET `/
    [ProducesResponseType(typeof(List<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid? (optional)`, `category: ExpenseCategory? (optional)`, `dateFrom: DateOnly? (optional)`, `dateTo: DateOnly? (optional)`, `lastSeenId: Guid? (optional)`, `lastSeenExpenseDate: DateOnly? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ExpensesController.cs:92`

### RentPaymentsController

Source: `src/PropertyOS.Api/Financials/RentPaymentsController.cs`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `leaseId: Guid`
- Body: none
- Responses: `200: GenerateInstallments`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:382`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `CancelRentPaymentRequest?` (see schema catalogue)
- Responses: `200: Cancel`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:247`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `RecordManualRentPaymentRequest` (see schema catalogue)
- Responses: `200: RecordManual`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:204`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(typeof(RemindRentPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: RemindTenant`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:404`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `leaseId: Guid`
- Body: none
- Responses: `200: GetForLease`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:80`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: none
- Responses: `200: GetForTenant`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:99`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `searchTerm: string (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: Search`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:119`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `pageSize: int (optional)`
- Body: none
- Responses: `200: GetOutstanding`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:140`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid? (optional)`, `status: DueDateStatus? (optional)`, `dateFrom: DateOnly? (optional)`, `dateTo: DateOnly? (optional)`, `searchTerm: string? (optional)`, `lastSeenId: Guid? (optional)`, `lastSeenDueDate: DateOnly? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:167`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(RentPaymentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:58`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: IssueReceipt`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:275`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsRead)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetSettlementStatement`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:320`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsRead)]
    [ProducesResponseType(typeof(List<RentPaymentReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `leaseContractId: Guid? (optional)`, `tenantId: Guid? (optional)`, `dateFrom: DateOnly? (optional)`, `dateTo: DateOnly? (optional)`, `lastSeenId: Guid? (optional)`, `lastSeenIssueDate: DateOnly? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetReceipts`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:347`

#### GET `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsRead)]
    [ProducesResponseType(typeof(RentPaymentReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetReceipt`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/RentPaymentsController.cs:298`

### PaymentAllocationsController

Source: `src/PropertyOS.Api/Financials/PaymentAllocationsController.cs`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `RecordPaymentAllocationRequest` (see schema catalogue)
- Responses: `200: Record`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/PaymentAllocationsController.cs:42`

#### POST `/
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `ReversePaymentAllocationRequest` (see schema catalogue)
- Responses: `200: Reverse`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/PaymentAllocationsController.cs:74`

### ReceiptSequenceController

Source: `src/PropertyOS.Api/Financials/ReceiptSequenceController.cs`

#### PUT `/
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `UpdateCompanyReceiptSequenceRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/ReceiptSequenceController.cs:41`

### TenantsController

Source: `src/PropertyOS.Api/Leasing/TenantsController.cs`

#### DELETE `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: none
- Responses: `200: Delete`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:138`

#### PUT `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: `UpdateTenantRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:103`

#### POST `/
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateTenantRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:50`

#### POST `/
    [Authorize(Policy = PropertyOS.Application.Leasing.Security.LeasingPermissions.Create)]
    [ProducesResponseType(typeof(ProvisionTenantAccountResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: `ProvisionTenantAccountRequest?` (see schema catalogue)
- Responses: `200: ProvisionAccount`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:201`

#### GET `/
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetLeaseHistoryByTenant`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:179`

#### GET `/
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `searchTerm: string (optional)`
- Body: none
- Responses: `200: Search`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:160`

#### GET `/
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `tenantId: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantsController.cs:81`

### MaintenanceRequestsController

Source: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs`

#### DELETE `/
    [Authorize(Policy = MaintenancePermissions.Comment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `commentId: Guid`
- Body: none
- Responses: `200: RemoveComment`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:334`

#### PUT `/
    [Authorize(Policy = MaintenancePermissions.Comment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `commentId: Guid`
- Body: `EditMaintenanceCommentRequest` (see schema catalogue)
- Responses: `200: EditComment`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:303`

#### POST `/
    [Authorize(Policy = MaintenancePermissions.Comment)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `AddMaintenanceCommentRequest` (see schema catalogue)
- Responses: `200: AddComment`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:273`

#### DELETE `/
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Delete`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:232`

#### DELETE `/
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `attachmentId: Guid`
- Body: none
- Responses: `200: RemoveAttachment`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:411`

#### PUT `/
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateMaintenanceRequestRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:167`

#### POST `/
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateMaintenanceRequestRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:57`

#### POST `/
    [Authorize(Policy = MaintenancePermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `AddMaintenanceAttachmentRequest` (see schema catalogue)
- Responses: `200: AddAttachment`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:380`

#### PATCH `/
    [Authorize(Policy = MaintenancePermissions.UpdateStatus)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateMaintenanceRequestStatusRequest` (see schema catalogue)
- Responses: `200: UpdateStatus`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:203`

#### GET `/
    [ProducesResponseType(typeof(List<MaintenanceAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetAttachments`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:361`

#### GET `/
    [ProducesResponseType(typeof(List<MaintenanceCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetComments`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:254`

#### GET `/
    [ProducesResponseType(typeof(List<MaintenanceRequestSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `buildingId: Guid? (optional)`, `apartmentId: Guid? (optional)`, `tenantId: Guid? (optional)`, `status: MaintenanceStatus? (optional)`, `priority: MaintenancePriority? (optional)`, `category: MaintenanceCategory? (optional)`, `dateFrom: DateOnly? (optional)`, `dateTo: DateOnly? (optional)`, `searchText: string? (optional)`, `lastSeenId: Guid? (optional)`, `lastSeenRequestDate: DateOnly? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:101`

#### GET `/
    [ProducesResponseType(typeof(List<MaintenanceStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetStatusHistory`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:438`

#### GET `/
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Maintenance/MaintenanceRequestsController.cs:145`

### NotificationTemplatesController

Source: `src/PropertyOS.Api/Notifications/NotificationTemplatesController.cs`

#### DELETE `/
    [Authorize(Policy = NotificationsPermissions.ManageTemplates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Delete`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationTemplatesController.cs:148`

#### PUT `/
    [Authorize(Policy = NotificationsPermissions.ManageTemplates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateNotificationTemplateRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationTemplatesController.cs:115`

#### POST `/
    [Authorize(Policy = NotificationsPermissions.ManageTemplates)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateNotificationTemplateRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationTemplatesController.cs:84`

#### GET `/
    [ProducesResponseType(typeof(List<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationTemplatesController.cs:46`

#### GET `/
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationTemplatesController.cs:63`

### NotificationsController

Source: `src/PropertyOS.Api/Notifications/NotificationsController.cs`

#### PATCH `/
    [Authorize(Policy = NotificationsPermissions.Send)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `notificationId: Guid`, `deliveryId: Guid`
- Body: `UpdateNotificationDeliveryRequest` (see schema catalogue)
- Responses: `200: UpdateDelivery`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:192`

#### POST `/
    [Authorize(Policy = NotificationsPermissions.Send)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateNotificationRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:156`

#### GET `/
    [Authorize(Policy = NotificationsPermissions.ViewAll)]
    [ProducesResponseType(typeof(List<NotificationDeliveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `lastSeenSentAt: DateTimeOffset? (optional)`, `lastSeenId: Guid? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetFailedDeliveries`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:225`

#### GET `/
    [Authorize(Policy = NotificationsPermissions.ViewAll)]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `lastSeenCreatedAt: DateTimeOffset? (optional)`, `lastSeenId: Guid? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetForCompany`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:130`

#### PATCH `/
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: MarkAsRead`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:92`

#### GET `/
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: GetMyUnreadCount`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:75`

#### GET `/
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `lastSeenCreatedAt: DateTimeOffset? (optional)`, `lastSeenId: Guid? (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetMine`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:52`

#### PATCH `/
    [ProducesResponseType(typeof(MarkAllNotificationsAsReadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: MarkAllAsRead`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Notifications/NotificationsController.cs:111`

### AuthController

Source: `src/PropertyOS.Api/Controllers/AuthController.cs`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthOtpVerifyLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `OtpVerifyDto` (see schema catalogue)
- Responses: `200: VerifyOtp`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:302`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthPasswordResetRequestLimit")]
    [ProducesResponseType(typeof(PasswordResetRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `PasswordResetRequestDto` (see schema catalogue)
- Responses: `200: RequestPasswordReset`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:341`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthPasswordResetVerifyLimit")]
    [ProducesResponseType(typeof(PasswordResetCompleteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `PasswordResetCompleteDto` (see schema catalogue)
- Responses: `200: CompletePasswordReset`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:375`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthPasswordResetVerifyLimit")]
    [ProducesResponseType(typeof(PasswordResetOtpVerifyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `PasswordResetOtpVerifyDto` (see schema catalogue)
- Responses: `200: VerifyPasswordResetOtp`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:360`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthLoginLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `LoginRequestDto` (see schema catalogue)
- Responses: `200: Login`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:103`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthLoginLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `ActivateTenantAccountRequest` (see schema catalogue)
- Responses: `200: ActivateTenant`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:459`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [EnableRateLimiting("AuthOtpRequestLimit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `OtpRequestDto` (see schema catalogue)
- Responses: `200: RequestOtp`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:248`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `RefreshTokenRequestDto?` (see schema catalogue)
- Responses: `200: Refresh`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:136`

#### POST `/api/v1/auth/
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `RegisterRequestDto` (see schema catalogue)
- Responses: `200: Register`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:62`

#### GET `/api/v1/auth/
    [AllowAnonymous]
    [ProducesResponseType(typeof(TenantActivationStatusDto), StatusCodes.Status200OK)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `token: string? (optional)`
- Body: none
- Responses: `200: GetTenantActivationStatus`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:441`

#### POST `/api/v1/auth/
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `RefreshTokenRequestDto?` (see schema catalogue)
- Responses: `200: Logout`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:185`

#### POST `/api/v1/auth/
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: LogoutAll`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:215`

#### GET `/api/v1/auth/
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: GetMyProfile`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/AuthController.cs:405`

### BuildingsController

Source: `src/PropertyOS.Api/Controllers/BuildingsController.cs`

#### GET `/api/v1/buildings/
    [Authorize(Policy = PropertyPermissions.Create)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: GetNextCode`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/BuildingsController.cs:47`

#### POST `/api/v1/buildings/
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateBuildingRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/BuildingsController.cs:64`

#### DELETE `/api/v1/buildings/
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: Archive`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/BuildingsController.cs:212`

#### GET `/api/v1/buildings/
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(BuildingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/BuildingsController.cs:109`

#### GET `/api/v1/buildings/
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<BuildingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<BuildingDto>>> List(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/buildings UserId={UserId} CompanyId={CompanyId}", 
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new ListBuildingsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    / <summary>
    / Updates an existing building's details.
    / </summary>
    / <param name="id">Building unique identifier.</param>
    / <param name="request">Building update payload.</param>
    / <param name="cancellationToken">Cancellation token passed from request.</param>
    / <returns>No content on success.</returns>
    / <response code="204">Building updated successfully.</response>
    / <response code="400">If request payload validation fails.</response>
    / <response code="401">If unauthenticated.</response>
    / <response code="403">If user lacks properties.update permission.</response>
    / <response code="404">If building is not found or belongs to another tenant.</response>
    / <response code="409">If updated internal code conflicts with another building.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateBuildingRequest` (see schema catalogue)
- Responses: `200: Update`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/BuildingsController.cs:136`

### CompaniesController

Source: `src/PropertyOS.Api/Companies/CompaniesController.cs`

#### PUT `/api/v1/companies/
    [Authorize(Policy = CompaniesPermissions.Manage)]
    [EndpointSummary("Update company profile")]
    [EndpointDescription("Updates basic profile information of a company (Legal Name, Display Name, Primary Phone, Primary Email).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `companyId: Guid`
- Body: `UpdateCompanyRequest` (see schema catalogue)
- Responses: `200: UpdateCompany`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Companies/CompaniesController.cs:72`

#### PUT `/api/v1/companies/
    [Authorize(Policy = CompaniesPermissions.Manage)]
    [EndpointSummary("Update company settings")]
    [EndpointDescription("Updates operational settings for a company including late fee policies, grace periods, and fiscal year start month.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `companyId: Guid`
- Body: `UpdateCompanySettingsRequest` (see schema catalogue)
- Responses: `200: UpdateCompanySettings`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Companies/CompaniesController.cs:118`

#### GET `/api/v1/companies/
    [EndpointSummary("Get company by ID")]
    [EndpointDescription("Retrieves details of a specific company by its unique identifier.")]
    [ProducesResponseType(typeof(CompanyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `companyId: Guid`
- Body: none
- Responses: `200: GetCompanyById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Companies/CompaniesController.cs:54`

#### GET `/api/v1/companies/
    [EndpointSummary("Get company settings")]
    [EndpointDescription("Retrieves operational settings (grace period, late fee policies, fiscal year) for a specific company.")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `companyId: Guid`
- Body: none
- Responses: `200: GetCompanySettings`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Companies/CompaniesController.cs:100`

#### GET `/api/v1/companies/
    [EndpointSummary("Get current authenticated company details")]
    [EndpointDescription("Retrieves the full company profile and settings for the currently authenticated tenant context.")]
    [ProducesResponseType(typeof(CompanyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: GetMyCompany`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Companies/CompaniesController.cs:37`

### ContactRequestsController

Source: `src/PropertyOS.Api/ContactRequests/ContactRequestsController.cs`

#### POST `/api/v1/contact-requests/
    [AllowAnonymous]
    [EnableRateLimiting("ContactRequestLimit")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType(typeof(ContactRequestCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `CreateContactRequestRequest` (see schema catalogue)
- Responses: `200: Create`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/ContactRequests/ContactRequestsController.cs:22`

#### PATCH `/api/v1/platform/contact-requests/
    [Authorize(Policy = PlatformPermissions.ContactRequestsManage)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `id: Guid`
- Body: `UpdateContactRequestStatusRequest` (see schema catalogue)
- Responses: `200: UpdateStatus`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/ContactRequestsController.cs:35`

#### GET `/api/v1/platform/contact-requests/
    [Authorize(Policy = PlatformPermissions.ContactRequestsRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`, `status: string? (optional)`
- Body: none
- Responses: `200: Get`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/ContactRequestsController.cs:25`

#### GET `/api/v1/platform/contact-requests/
    [Authorize(Policy = PlatformPermissions.ContactRequestsRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/ContactRequestsController.cs:30`

### DashboardController

Source: `src/PropertyOS.Api/Controllers/DashboardController.cs`

#### GET `/api/v1/dashboard/
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: GetSummary`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/DashboardController.cs:41`

### DevelopmentEmailController

Source: `src/PropertyOS.Api/Controllers/DevelopmentEmailController.cs`

#### POST `/api/v1/development/email/
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `DevelopmentTestEmailRequest` (see schema catalogue)
- Responses: `200: SendTestEmail`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/DevelopmentEmailController.cs:36`

### DevelopmentSeedController

Source: `src/PropertyOS.Api/Controllers/DevelopmentSeedController.cs`

#### POST `/api/v1/development/seed/
    [Authorize(Policy = PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: SeedOwnerPaymentVerification`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/DevelopmentSeedController.cs:36`

### DevelopmentSmsController

Source: `src/PropertyOS.Api/Controllers/DevelopmentSmsController.cs`

#### POST `/api/v1/development/sms/
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `DevelopmentTestSmsRequest` (see schema catalogue)
- Responses: `200: SendTestSms`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/DevelopmentSmsController.cs:36`

### LocationsController

Source: `src/PropertyOS.Api/Controllers/LocationsController.cs`

#### GET `/api/v1/locations/
    ` — undefined

- Auth: `authenticated`
- Route/query: `latitude: decimal`, `longitude: decimal`, `language: string`, `service: IReverseGeocodingService`
- Body: none
- Responses: `200: ReverseGeocode`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/LocationsController.cs:14`

### MarketplaceController

Source: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs`

#### DELETE `/api/v1/marketplace/
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `imageId: Guid`
- Body: none
- Responses: `200: SetListingCoverImage`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:98`

#### POST `/api/v1/marketplace/
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateMarketplaceListingCommand` (see schema catalogue)
- Responses: `200: CreateListing`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:46`

#### POST `/api/v1/marketplace/
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: DeleteListing`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:70`

#### POST `/api/v1/marketplace/
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `AddListingImageCommand` (see schema catalogue)
- Responses: `200: AddListingImage`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:88`

#### POST `/api/v1/marketplace/
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `List<Guid>` (see schema catalogue)
- Responses: `200: ReorderListingImages`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:112`

#### PUT `/api/v1/marketplace/
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: PublishListing`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:53`

#### GET `/api/v1/marketplace/
    [AllowAnonymous]
    public async Task<ActionResult<List<PublicListingSummaryDto>>> GetPublicListings([FromQuery] GetPublicListingsQuery query)
    {
        var listings = await _mediator.Send(query);
        return Ok(listings);
    }

    [HttpGet("listings/{id:guid}")]
    [AllowAnonymous]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetListingDetails`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:123`

#### GET `/api/v1/marketplace/
    public async Task<ActionResult<List<CompanyListingSummaryDto>>> GetCompanyListings([FromQuery] GetCompanyListingsQuery query)
    {
        var listings = await _mediator.Send(query);
        return Ok(listings);
    }

    / ─────────────────────────────────────────────────────────────────────────
    / Viewing Requests (Public submissions + Staff worklist)
    / ─────────────────────────────────────────────────────────────────────────

    [HttpPost("viewing-requests")]
    [AllowAnonymous]
    [EnableRateLimiting("ViewingRequestLimit")]
    [RequestSizeLimit(1024)] / Strict request size limit to prevent DOS
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: `CreateViewingRequestCommand` (see schema catalogue)
- Responses: `200: CreateViewingRequest`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:146`

#### GET `/api/v1/marketplace/
    public async Task<ActionResult<List<ViewingRequestSummaryDto>>> GetViewingRequests([FromQuery] GetViewingRequestsQuery query)
    {
        var requests = await _mediator.Send(query);
        return Ok(requests);
    }

    [HttpPost("viewing-requests/{id:guid}/status")]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `UpdateViewingRequestStatusCommand` (see schema catalogue)
- Responses: `200: UpdateViewingRequestStatus`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Marketplace/MarketplaceController.cs:167`

### OwnerPaymentsController

Source: `src/PropertyOS.Api/Financials/OwnerPaymentsController.cs`

#### POST `/api/v1/owner/payments/
    [Authorize(Policy = PropertyOS.Application.Common.Security.PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`, `submissionId: Guid`
- Body: `RejectPaymentSubmissionRequest` (see schema catalogue)
- Responses: `200: RejectSubmission`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/OwnerPaymentsController.cs:50`

#### GET `/api/v1/owner/payments/
    [Authorize(Policy = PropertyOS.Application.Common.Security.PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(typeof(PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>), StatusCodes.Status200OK)]
    ` — undefined

- Auth: `PropertyOS.Application.Common.Security.PlatformPermissions.PaymentsApprove`
- Route/query: `id: Guid`, `submissionId: Guid`
- Body: none
- Responses: `204: empty`, `400: unspecified`, `404: unspecified`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/OwnerPaymentsController.cs:24`

### LandlordRegistrationsController

Source: `src/PropertyOS.Api/PlatformAdministration/LandlordRegistrationsController.cs`

#### POST `/api/v1/platform/landlord-registrations/
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsApprove)]
    [ProducesResponseType(typeof(LandlordRegistrationReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `registrationId: Guid`
- Body: none
- Responses: `200: Approve`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/LandlordRegistrationsController.cs:56`

#### GET `/api/v1/platform/landlord-registrations/
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsRead)]
    [ProducesResponseType(typeof(LandlordRegistrationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `registrationId: Guid`
- Body: none
- Responses: `200: GetById`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/LandlordRegistrationsController.cs:43`

#### GET `/api/v1/platform/landlord-registrations/
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsRead)]
    [ProducesResponseType(typeof(LandlordRegistrationPageDto), StatusCodes.Status200OK)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetPending`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/LandlordRegistrationsController.cs:30`

#### POST `/api/v1/platform/landlord-registrations/
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsReject)]
    [ProducesResponseType(typeof(LandlordRegistrationReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `registrationId: Guid`
- Body: `RejectLandlordRegistrationRequest` (see schema catalogue)
- Responses: `200: Reject`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/LandlordRegistrationsController.cs:71`

### PlanChangeRequestsController

Source: `src/PropertyOS.Api/PlatformAdministration/PlanChangeRequestsController.cs`

#### GET `/api/v1/platform/plan-change-requests/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`, `status: PlanChangeRequestStatus? (optional)`, `companyId: Guid? (optional)`, `search: string? (optional)`
- Body: none
- Responses: `200: GetRequests`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlanChangeRequestsController.cs:25`

#### GET `/api/v1/platform/plan-change-requests/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `requestId: Guid`
- Body: none
- Responses: `200: GetRequest`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlanChangeRequestsController.cs:35`

#### POST `/api/v1/platform/plan-change-requests/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsReview)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `requestId: Guid`
- Body: `ApprovePlanChangeRequestRequest?` (see schema catalogue)
- Responses: `200: Approve`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlanChangeRequestsController.cs:41`

#### POST `/api/v1/platform/plan-change-requests/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsReview)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `requestId: Guid`
- Body: `RejectPlanChangeRequestRequest` (see schema catalogue)
- Responses: `200: Reject`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlanChangeRequestsController.cs:51`

### PlansController

Source: `src/PropertyOS.Api/PlatformAdministration/PlansController.cs`

#### POST `/api/v1/platform/plans/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansCreate)]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `CreatePlanRequest` (see schema catalogue)
- Responses: `200: CreatePlan`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlansController.cs:38`

#### POST `/api/v1/platform/plans/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansLifecycle)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `planId: Guid`
- Body: none
- Responses: `200: Activate`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlansController.cs:56`

#### POST `/api/v1/platform/plans/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansLifecycle)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `planId: Guid`
- Body: none
- Responses: `200: Deactivate`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlansController.cs:61`

#### GET `/api/v1/platform/plans/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`, `isActive: bool? (optional)`, `search: string? (optional)`
- Body: none
- Responses: `200: GetPlans`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlansController.cs:25`

#### GET `/api/v1/platform/plans/
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `planId: Guid`
- Body: none
- Responses: `200: GetPlan`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/PlansController.cs:33`

### SubscriptionsController

Source: `src/PropertyOS.Api/PlatformAdministration/SubscriptionsController.cs`

#### POST `/api/v1/platform/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsManage)]
    [ProducesResponseType(typeof(SubscriptionAdministrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `CreateCompanySubscriptionRequest` (see schema catalogue)
- Responses: `200: CreateSubscription`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/SubscriptionsController.cs:42`

#### GET `/api/v1/platform/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`, `search: string? (optional)`, `status: SubscriptionStatusEnum? (optional)`, `billingCycle: BillingCycleEnum? (optional)`, `planId: Guid? (optional)`, `sortBy: string (optional)`, `descending: bool (optional)`
- Body: none
- Responses: `200: GetSubscriptions`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/SubscriptionsController.cs:25`

#### GET `/api/v1/platform/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsRead)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `subscriptionId: Guid`
- Body: none
- Responses: `200: GetSubscription`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/PlatformAdministration/SubscriptionsController.cs:36`

### SubscriptionController

Source: `src/PropertyOS.Api/Controllers/SubscriptionController.cs`

#### GET `/api/v1/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.OwnSubscriptionView)]
    [ProducesResponseType(typeof(PaygUsageSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: GetCurrentUsage`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/SubscriptionController.cs:45`

#### POST `/api/v1/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsCancel)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `requestId: Guid`
- Body: none
- Responses: `200: CancelPlanChangeRequest`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/SubscriptionController.cs:76`

#### POST `/api/v1/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsCreate)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `CreatePlanChangeRequestRequest` (see schema catalogue)
- Responses: `200: CreatePlanChangeRequest`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/SubscriptionController.cs:61`

#### GET `/api/v1/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsRead)]
    [ProducesResponseType(typeof(PlanChangeRequestPageDto), StatusCodes.Status200OK)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetMyPlanChangeRequests`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/SubscriptionController.cs:52`

#### GET `/api/v1/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.OwnSubscriptionView)]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: GetMySubscription`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/SubscriptionController.cs:38`

#### GET `/api/v1/subscriptions/
    [Authorize(Policy = SubscriptionsPermissions.PlansView)]
    [ProducesResponseType(typeof(PlanPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `page: int (optional)`, `pageSize: int (optional)`
- Body: none
- Responses: `200: GetActivePlans`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/SubscriptionController.cs:28`

### TenantPortalController

Source: `src/PropertyOS.Api/Leasing/TenantPortalController.cs`

#### GET `/api/v1/tenant-portal/
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `PlatformPermissions.TenantPortalAccess`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: GetMySettlementStatement`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantPortalController.cs:102`

#### GET `/api/v1/tenant-portal/
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    ` — undefined

- Auth: `PlatformPermissions.TenantPortalAccess`
- Route/query: none
- Body: none
- Responses: `200: GetMyPayments`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantPortalController.cs:84`

#### GET `/api/v1/tenant-portal/
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `PlatformPermissions.TenantPortalAccess`
- Route/query: none
- Body: none
- Responses: `200: GetMyProfile`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantPortalController.cs:43`

#### GET `/api/v1/tenant-portal/
    [ProducesResponseType(typeof(TenantLeaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `PlatformPermissions.TenantPortalAccess`
- Route/query: none
- Body: none
- Responses: `200: GetMyActiveLease`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Leasing/TenantPortalController.cs:63`

### TenantPaymentsController

Source: `src/PropertyOS.Api/Financials/TenantPaymentsController.cs`

#### POST `/api/v1/tenant/payments/
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `id: Guid`
- Body: `SubmitPaymentVerificationRequest` (see schema catalogue)
- Responses: `200: SubmitVerification`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Financials/TenantPaymentsController.cs:24`

### TestController

Source: `src/PropertyOS.Api/Controllers/TestController.cs`

#### POST `/api/v1/test/
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: Mutate`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/TestController.cs:37`

### UserController

Source: `src/PropertyOS.Api/Controllers/UserController.cs`

#### GET `/api/v1/users/
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    ` — undefined

- Auth: `authenticated`
- Route/query: none
- Body: none
- Responses: `200: GetCurrentUser`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/Controllers/UserController.cs:48`

### UtilityAccountsController

Source: `src/PropertyOS.Api/UtilityBills/UtilityAccountsController.cs`

#### GET `/api/v1/utility-bills/accounts/
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    ` — undefined

- Auth: `authenticated`
- Route/query: `cursor: string? (optional)`, `pageSize: int (optional)`, `utilityType: UtilityType? (optional)`, `syncStatus: UtilitySyncStatus? (optional)`, `isActive: bool? (optional)`, `leaseContractId: Guid? (optional)`, `includeUnlinked: bool (optional)`
- Body: none
- Responses: `200: List`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/UtilityAccountsController.cs:37`

#### GET `/api/v1/utility-bills/accounts/
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    ` — undefined

- Auth: `UtilityBillsPermissions.Manage`
- Route/query: `id: Guid`
- Body: none
- Responses: `202: unspecified`, `404: unspecified`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/UtilityAccountsController.cs:133`

#### DELETE `/api/v1/utility-bills/accounts/
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `UtilityBillsPermissions.Manage`
- Route/query: `id: Guid`
- Body: none
- Responses: `200: unspecified`, `404: unspecified`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/UtilityAccountsController.cs:106`

#### POST `/api/v1/utility-bills/accounts/
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `UtilityBillsPermissions.Manage`
- Route/query: `id: Guid`
- Body: `ReplaceUtilityAccountRequest` (see schema catalogue)
- Responses: `202: unspecified`, `404: unspecified`, `409: unspecified`, `422: unspecified`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/UtilityAccountsController.cs:63`

### TenantPortalUtilityBillsController

Source: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs`

#### GET `/api/v1/utility-bills/my/
    [ProducesResponseType(StatusCodes.Status200OK)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: GetMyUtilityAccounts`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs:44`

#### GET `/api/v1/utility-bills/my/
    [ProducesResponseType(StatusCodes.Status200OK)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `utilityType: UtilityType? (optional)`, `pageSize: int (optional)`, `cursor: string? (optional)`
- Body: none
- Responses: `200: GetMyUtilityBills`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs:130`

#### GET `/api/v1/utility-bills/my/
    [ProducesResponseType(StatusCodes.Status200OK)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: none
- Responses: `200: GetUtilityDashboardSummary`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs:149`

#### DELETE `/api/v1/utility-bills/my/
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `id: Guid`
- Body: none
- Responses: `202: unspecified`, `404: unspecified`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs:101`

#### POST `/api/v1/utility-bills/my/
    [ProducesResponseType(typeof(UtilityAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: none
- Body: `TenantLinkUtilityAccountRequest` (see schema catalogue)
- Responses: `200: LinkMyUtilityAccount`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs:57`

#### POST `/api/v1/utility-bills/my/
    [ProducesResponseType(typeof(UtilityAccountDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    ` — undefined

- Auth: `anonymous`
- Route/query: `id: Guid`
- Body: `ReplaceUtilityAccountRequest` (see schema catalogue)
- Responses: `200: ReplaceMyUtilityAccount`
- Used by: no exact production client call found
- Location: `src/PropertyOS.Api/UtilityBills/TenantPortalUtilityBillsController.cs:82`

## Request and response schema catalogue

### ActivateTenantAccountRequest

- Fields: `ActivationToken: string`, `Password: string`
- Source: `src/PropertyOS.Api/Models/Identity/ActivateTenantAccountRequest.cs:6`

### AddListingImageCommand

- Fields: `ListingId: Guid`, `FileId: Guid`, `IsCover: bool`
- Source: `src/PropertyOS.Application/Marketplace/Commands/AddListingImage/AddListingImageCommand.cs:6`

### AddMaintenanceAttachmentRequest

- Fields: `FileId: Guid?`, `Description: string?`
- Source: `src/PropertyOS.Api/Models/Maintenance/AddMaintenanceAttachmentRequest.cs:10`

### AddMaintenanceCommentRequest

- Fields: `CommentText: string`
- Source: `src/PropertyOS.Api/Models/Maintenance/AddMaintenanceCommentRequest.cs:6`

### ApprovePlanChangeRequestRequest

- Fields: `DecisionNote: string?`
- Source: `src/PropertyOS.Api/PlatformAdministration/Requests/SubscriptionAdministrationRequests.cs:37`

### AttachContractDocumentRequest

- Fields: `FileId: Guid`, `DocumentType: ContractDocumentType`, `Description: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/AttachContractDocumentRequest.cs:9`

### AttachExpenseReceiptRequest

- Fields: `FileId: Guid`, `Amount: decimal`, `IssuedAt: DateOnly`, `Description: string?`
- Source: `src/PropertyOS.Api/Models/Financials/AttachExpenseReceiptRequest.cs:8`

### CancelEfawateercomTransactionRequest

- Fields: `Reason: string?`
- Source: `src/PropertyOS.Api/Models/Financials/CancelEfawateercomTransactionRequest.cs:6`

### CancelRentPaymentRequest

- Fields: `Reason: string?`
- Source: `src/PropertyOS.Api/Models/Financials/CancelRentPaymentRequest.cs:6`

### ConfirmFileUploadRequest

- Fields: `FileId: Guid`, `StorageKey: string`, `OriginalFilename: string`, `MimeType: string`, `SizeBytes: long`
- Source: `src/PropertyOS.Api/Models/Files/ConfirmFileUploadRequest.cs:6`

### CreateApartmentRequest

- Fields: `UnitNumber: string`, `AreaSqm: decimal`, `OwnershipStatus: OwnershipStatus`, `ExternalOwnerName: string?`, `ExternalOwnerPhone: string?`, `Bedrooms: short`, `Bathrooms: short`, `BaseRentAmount: decimal?`, `BaseRentCurrency: string`
- Source: `src/PropertyOS.Api/Models/Properties/CreateApartmentRequest.cs:6`

### CreateBuildingDocumentRequest

- Fields: `CategoryId: Guid`, `FileId: Guid`, `DocumentName: string`, `Description: string?`, `IssueDate: DateOnly?`, `ExpiryDate: DateOnly?`, `IsConfidential: bool`
- Source: `src/PropertyOS.Api/Models/Documents/CreateBuildingDocumentRequest.cs:8`

### CreateBuildingRequest

- Fields: `Name: string`, `TotalFloors: short`, `BuildingType: BuildingType`, `InternalCode: string?`, `ConstructionYear: short?`, `GpsLatitude: decimal?`, `GpsLongitude: decimal?`, `AddressGovernorate: Governorate`, `AddressCity: string`, `AddressNeighborhood: string`, `AddressStreet: string?`, `AddressPostalCode: string?`
- Source: `src/PropertyOS.Api/Models/Properties/CreateBuildingRequest.cs:6`

### CreateCompanySubscriptionRequest

- Fields: `CompanyId: Guid`, `PlanId: Guid`, `BillingCycle: BillingCycleEnum`, `StartDate: DateOnly`, `EndDate: DateOnly`, `TrialEndDate: DateOnly?`
- Source: `src/PropertyOS.Api/PlatformAdministration/Requests/SubscriptionAdministrationRequests.cs:27`

### CreateContactRequestRequest

- Fields: `Name: string`, `CompanyName: string`, `PhoneNumber: string`, `NumberOfBuildings: int`, `Notes: string?`
- Source: `src/PropertyOS.Api/ContactRequests/Requests/CreateContactRequestRequest.cs:2`

### CreateDocumentCategoryRequest

- Fields: `Name: string`, `Description: string?`
- Source: `src/PropertyOS.Api/Models/Documents/CreateDocumentCategoryRequest.cs:6`

### CreateEfawateercomTransactionRequest

- Fields: `RentPaymentId: Guid`, `ExternalTransactionId: string`, `Amount: decimal`, `PaymentReference: string?`
- Source: `src/PropertyOS.Api/Models/Financials/CreateEfawateercomTransactionRequest.cs:8`

### CreateExpenseRequest

- Fields: `BuildingId: Guid?`, `Category: ExpenseCategory`, `Amount: decimal`, `ExpenseDate: DateOnly`, `PaymentMethod: ExpensePaymentMethod`, `Description: string`, `VendorName: string?`, `InvoiceNumber: string?`, `Notes: string?`, `Receipts: List<CreateExpenseReceiptRequest>?`
- Source: `src/PropertyOS.Api/Models/Financials/CreateExpenseRequest.cs:20`

### CreateFloorRequest

- Fields: `FloorNumber: short`, `FloorLabel: string`, `FloorType: FloorType`
- Source: `src/PropertyOS.Api/Models/Properties/CreateFloorRequest.cs:6`

### CreateLeaseContractRequest

- Fields: `ApartmentId: Guid`, `TenantId: Guid`, `ContractNumber: string`, `StartDate: DateTime`, `EndDate: DateTime`, `MonthlyRentAmount: decimal`, `SecurityDepositAmount: decimal`, `PaymentFrequency: PaymentFrequency`, `PaymentDueDay: short`, `LegalRegime: LegalRegime`, `TenantType: TenantType`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/CreateLeaseContractRequest.cs:9`

### CreateMaintenanceRequestRequest

- Fields: `BuildingId: Guid`, `Title: string`, `Description: string`, `Category: MaintenanceCategory`, `Priority: MaintenancePriority`, `RequestDate: DateOnly`, `ApartmentId: Guid?`, `TenantId: Guid?`
- Source: `src/PropertyOS.Api/Models/Maintenance/CreateMaintenanceRequestRequest.cs:9`

### CreateMarketplaceListingCommand

- Fields: `ApartmentId: Guid`, `Title: string`, `Description: string`, `MonthlyRent: decimal`, `SecurityDeposit: decimal?`, `Currency: CurrencyCode`, `ContactPhone: string`, `ContactWhatsapp: string?`, `ExpirationDate: DateOnly?`, `IsFeatured: bool`
- Source: `src/PropertyOS.Application/Marketplace/Commands/CreateMarketplaceListing/CreateMarketplaceListingCommand.cs:7`

### CreateNotificationRequest

- Fields: `RecipientUserId: Guid`, `Subject: string`, `Body: string`, `NotificationType: NotificationType`, `Priority: NotificationPriority`, `Channels: List<DeliveryChannel>`, `TemplateId: Guid?`
- Source: `src/PropertyOS.Api/Models/Notifications/CreateNotificationRequest.cs:10`

### CreateNotificationTemplateRequest

- Fields: `TemplateName: string`, `Subject: string`, `Body: string`, `NotificationType: NotificationType`, `IsActive: bool`
- Source: `src/PropertyOS.Api/Models/Notifications/CreateNotificationTemplateRequest.cs:8`

### CreateParkingSpotRequest

- Fields: `SpotCode: string`, `ParkingType: ParkingType`, `DefaultApartmentId: Guid?`, `LocationDescription: string?`
- Source: `src/PropertyOS.Api/Models/Properties/CreateParkingSpotRequest.cs:7`

### CreatePlanChangeRequestRequest

- Fields: `RequestedPlanId: Guid`, `RequestedBillingCycle: BillingCycleEnum`
- Source: `src/PropertyOS.Api/Subscriptions/Requests/CreatePlanChangeRequestRequest.cs:5`

### CreatePlanRequest

- Fields: `Code: string`, `NameEn: string`, `NameAr: string`, `DescriptionEn: string?`, `DescriptionAr: string?`, `MonthlyPrice: decimal`, `YearlyPrice: decimal`, `PricingModel: SubscriptionPricingModel`, `PaygMonthlyUnitPrice: decimal?`, `PaygYearlyMonthlyEquivalentUnitPrice: decimal?`, `Currency: string`, `MaxBuildings: int?`, `MaxUsers: int?`, `MaxStorageMb: int?`, `FeatureFlags: string`, `SupportsTrial: bool`, `TrialDurationDays: short?`, `SortOrder: short`
- Source: `src/PropertyOS.Api/PlatformAdministration/Requests/SubscriptionAdministrationRequests.cs:5`

### CreateTenantEmergencyContactRequest

- Fields: `Name: string`, `RelationshipType: string`, `Phone: string`
- Source: `src/PropertyOS.Api/Models/Leasing/CreateTenantEmergencyContactRequest.cs:3`

### CreateTenantFamilyMemberRequest

- Fields: `Name: string`, `RelationshipType: string`, `AgeBracket: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/CreateTenantFamilyMemberRequest.cs:3`

### CreateTenantRequest

- Fields: `Name: string`, `NationalId: string`, `Phone: string`, `Email: string`, `Occupation: string?`, `Employer: string?`, `PhoneCountryCode: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/CreateTenantRequest.cs:6`

### CreateTenantVehicleRequest

- Fields: `PlateNumber: string`, `MakeModel: string`, `Color: string`
- Source: `src/PropertyOS.Api/Models/Leasing/CreateTenantVehicleRequest.cs:3`

### CreateViewingRequestCommand

- Fields: `ListingId: Guid`, `ApplicantName: string`, `PhoneNumber: string`, `Email: string?`, `PreferredViewingDate: DateOnly?`, `Notes: string?`
- Source: `src/PropertyOS.Application/Marketplace/Commands/CreateViewingRequest/CreateViewingRequestCommand.cs:6`

### DevelopmentTestEmailRequest

- Fields: `To: string`
- Source: `src/PropertyOS.Api/Controllers/DevelopmentEmailController.cs:71`

### DevelopmentTestSmsRequest

- Fields: `To: string`, `Message: string`
- Source: `src/PropertyOS.Api/Controllers/DevelopmentSmsController.cs:69`

### EditMaintenanceCommentRequest

- Fields: `NewText: string`
- Source: `src/PropertyOS.Api/Models/Maintenance/EditMaintenanceCommentRequest.cs:6`

### ExpireEfawateercomTransactionRequest

- Fields: `ResponseCode: string?`, `ResponseMessage: string?`
- Source: `src/PropertyOS.Api/Models/Financials/ExpireEfawateercomTransactionRequest.cs:6`

### LoginRequestDto

- Fields: `EmailOrPhone: string`, `Password: string`, `RememberMe: bool?`
- Source: `src/PropertyOS.Application/DTOs/Identity/LoginRequestDto.cs:7`

### OtpRequestDto

- Fields: `Phone: string`, `Purpose: OtpPurpose`
- Source: `src/PropertyOS.Application/DTOs/Identity/OtpRequestDto.cs:8`

### OtpVerifyDto

- Fields: `Phone: string`, `Code: string`, `Purpose: OtpPurpose`
- Source: `src/PropertyOS.Application/DTOs/Identity/OtpVerifyDto.cs:8`

### ParkingAssignmentDto

- Fields: `AssignmentId: Guid`, `ParkingSpotId: Guid`, `ParkingSpotCode: string`, `ParkingType: string`, `Location: string`, `LeaseContractId: Guid`, `TenantId: Guid`, `TenantName: string`, `StartDate: DateOnly`, `EndDate: DateOnly?`, `Status: string`
- Source: `src/PropertyOS.Application/Properties/ParkingAssignments/Queries/Common/ParkingAssignmentDto.cs:5`

### PasswordResetCompleteDto

- Fields: `ResetCredential: string`, `NewPassword: string`
- Source: `src/PropertyOS.Application/DTOs/Identity/PasswordResetDtos.cs:35`

### PasswordResetOtpVerifyDto

- Fields: `Phone: string`, `Code: string`
- Source: `src/PropertyOS.Application/DTOs/Identity/PasswordResetDtos.cs:23`

### PasswordResetRequestDto

- Fields: `DeliveryMethod: PasswordResetDeliveryMethod`, `Identifier: string`
- Source: `src/PropertyOS.Application/DTOs/Identity/PasswordResetDtos.cs:12`

### RecordChequeStatusChangeRequest

- Fields: `NewStatus: ChequeStatus`, `ActionDate: DateOnly`, `BounceReason: string?`, `BounceFeeCharged: decimal?`, `CancellationReason: string?`, `ReplacementChequeId: Guid?`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Financials/RecordChequeStatusChangeRequest.cs:9`

### RecordManualRentPaymentRequest

- Fields: `LeaseContractId: Guid`, `Amount: decimal`, `PaymentMethod: PaymentMethod`, `PaymentReferenceNumber: string?`, `Notes: string?`, `Cheque: ManualChequeDetailsRequest?`, `Allocations: List<AllocationDetailRequest>?`
- Source: `src/PropertyOS.Api/Models/Financials/RecordManualRentPaymentRequest.cs:23`

### RecordPaymentAllocationRequest

- Fields: `ReceivingPaymentId: Guid`, `Allocations: List<AllocationDetailRequest>`, `AllocationDate: DateOnly`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Financials/RecordPaymentAllocationRequest.cs:9`

### RefreshTokenRequestDto

- Fields: `RefreshToken: string?`
- Source: `src/PropertyOS.Application/DTOs/Identity/RefreshTokenRequestDto.cs:6`

### RejectLandlordRegistrationRequest

- Fields: `Reason: string`
- Source: `src/PropertyOS.Api/PlatformAdministration/Requests/RejectLandlordRegistrationRequest.cs:3`

### RejectPaymentSubmissionRequest

- Fields: `Reason: string`
- Source: `src/PropertyOS.Api/Financials/OwnerPaymentsController.cs:66`

### RejectPlanChangeRequestRequest

- Fields: `RejectionReason: string`, `DecisionNote: string?`
- Source: `src/PropertyOS.Api/PlatformAdministration/Requests/SubscriptionAdministrationRequests.cs:42`

### RenewLeaseContractRequest

- Fields: `ContractNumber: string`, `StartDate: DateTime`, `EndDate: DateTime`, `MonthlyRentAmount: decimal`, `SecurityDepositAmount: decimal`, `PaymentFrequency: PaymentFrequency`, `PaymentDueDay: short`, `LegalRegime: LegalRegime`, `TenantType: TenantType`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/RenewLeaseContractRequest.cs:9`

### ReplaceBuildingDocumentRequest

- Fields: `NewFileId: Guid`, `NewDocumentName: string?`, `NewDescription: string?`, `NewIssueDate: DateOnly?`, `NewExpiryDate: DateOnly?`, `NewIsConfidential: bool?`
- Source: `src/PropertyOS.Api/Models/Documents/ReplaceBuildingDocumentRequest.cs:9`

### ReplaceContractDocumentRequest

- Fields: `NewFileId: Guid`, `Description: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/ReplaceContractDocumentRequest.cs:5`

### ReplaceUtilityAccountRequest

- Fields: `AccountNumber: string`, `MeterNumber: string?`
- Source: `src/PropertyOS.Api/UtilityBills/ReplaceUtilityAccountRequest.cs:6`

### ReversePaymentAllocationRequest

- Fields: `ReversalReason: string`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Financials/ReversePaymentAllocationRequest.cs:6`

### SubmitPaymentVerificationRequest

- Fields: `Amount: decimal`, `PaymentMethod: PaymentMethod`, `ReferenceNumber: string?`, `ProofFileId: Guid?`, `ChequeDetails: ChequeSubmissionInput?`
- Source: `src/PropertyOS.Api/Financials/TenantPaymentsController.cs:45`

### TenantLinkUtilityAccountRequest

- Fields: `UtilityType: UtilityType`, `AccountNumber: string`, `MeterNumber: string?`
- Source: `src/PropertyOS.Api/UtilityBills/TenantLinkUtilityAccountRequest.cs:11`

### TerminateLeaseContractRequest

- Fields: `TerminationType: TerminationType`, `TerminationDate: DateTime`, `OutstandingBalance: decimal`, `DepositReturnedAmount: decimal`, `DepositDeductionAmount: decimal`, `DepositDeductionReason: string?`, `FinalUtilitySettlementCompleted: bool`, `Reason: string?`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/TerminateLeaseContractRequest.cs:9`

### UpdateApartmentRequest

- Fields: `BaseRentAmount: decimal?`, `BaseRentCurrency: string`
- Source: `src/PropertyOS.Api/Models/Properties/UpdateApartmentRequest.cs:5`

### UpdateBuildingDocumentRequest

- Fields: `DocumentName: string`, `Description: string?`, `IssueDate: DateOnly?`, `ExpiryDate: DateOnly?`, `IsConfidential: bool`
- Source: `src/PropertyOS.Api/Models/Documents/UpdateBuildingDocumentRequest.cs:8`

### UpdateBuildingRequest

- Fields: `Name: string`, `BuildingType: BuildingType`, `InternalCode: string?`, `ConstructionYear: short?`, `GpsLatitude: decimal?`, `GpsLongitude: decimal?`, `AddressGovernorate: Governorate`, `AddressCity: string`, `AddressNeighborhood: string`, `AddressStreet: string?`, `AddressPostalCode: string?`
- Source: `src/PropertyOS.Api/Models/Properties/UpdateBuildingRequest.cs:6`

### UpdateCompanyReceiptSequenceRequest

- Fields: `Prefix: string`, `PaddingLength: short`, `ResetPolicy: ReceiptResetPolicy`
- Source: `src/PropertyOS.Api/Models/Financials/UpdateCompanyReceiptSequenceRequest.cs:8`

### UpdateCompanyRequest

- Fields: `LegalName: string`, `DisplayName: string`, `PrimaryPhone: string`, `PrimaryEmail: string?`
- Source: `src/PropertyOS.Api/Companies/Requests/UpdateCompanyRequest.cs:6`

### UpdateCompanySettingsRequest

- Fields: `RentGracePeriodDays: short`, `LateFeeType: LateFeeType`, `LateFeeValue: decimal?`, `FiscalYearStartMonth: short`
- Source: `src/PropertyOS.Api/Companies/Requests/UpdateCompanySettingsRequest.cs:8`

### UpdateContactRequestStatusRequest

- Fields: `Status: string`
- Source: `src/PropertyOS.Api/PlatformAdministration/Requests/UpdateContactRequestStatusRequest.cs:2`

### UpdateDocumentCategoryRequest

- Fields: `Name: string`, `Description: string?`
- Source: `src/PropertyOS.Api/Models/Documents/UpdateDocumentCategoryRequest.cs:6`

### UpdateDraftLeaseContractRequest

- Fields: `ApartmentId: Guid`, `TenantId: Guid`, `StartDate: DateTime`, `EndDate: DateTime`, `MonthlyRentAmount: decimal`, `SecurityDepositAmount: decimal`, `PaymentFrequency: PaymentFrequency`, `PaymentDueDay: short`, `LegalRegime: LegalRegime`, `TenantType: TenantType`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/UpdateDraftLeaseContractRequest.cs:9`

### UpdateExpenseRequest

- Fields: `BuildingId: Guid?`, `Category: ExpenseCategory`, `Amount: decimal`, `ExpenseDate: DateOnly`, `PaymentMethod: ExpensePaymentMethod`, `Description: string`, `VendorName: string?`, `InvoiceNumber: string?`, `Notes: string?`
- Source: `src/PropertyOS.Api/Models/Financials/UpdateExpenseRequest.cs:9`

### UpdateFloorRequest

- Fields: `FloorLabel: string`, `FloorType: FloorType`
- Source: `src/PropertyOS.Api/Models/Properties/UpdateFloorRequest.cs:6`

### UpdateMaintenanceRequestRequest

- Fields: `BuildingId: Guid`, `Title: string`, `Description: string`, `Category: MaintenanceCategory`, `Priority: MaintenancePriority`, `ApartmentId: Guid?`, `TenantId: Guid?`, `InternalNotes: string?`
- Source: `src/PropertyOS.Api/Models/Maintenance/UpdateMaintenanceRequestRequest.cs:9`

### UpdateMaintenanceRequestStatusRequest

- Fields: `NewStatus: MaintenanceStatus`, `Reason: string?`
- Source: `src/PropertyOS.Api/Models/Maintenance/UpdateMaintenanceRequestStatusRequest.cs:8`

### UpdateNotificationDeliveryRequest

- Fields: `NewStatus: DeliveryStatus`, `FailureReason: string?`
- Source: `src/PropertyOS.Api/Models/Notifications/UpdateNotificationDeliveryRequest.cs:8`

### UpdateNotificationTemplateRequest

- Fields: `TemplateName: string`, `Subject: string`, `Body: string`, `NotificationType: NotificationType`, `IsActive: bool`
- Source: `src/PropertyOS.Api/Models/Notifications/UpdateNotificationTemplateRequest.cs:8`

### UpdateParkingSpotRequest

- Fields: `SpotCode: string`, `ParkingType: ParkingType`, `DefaultApartmentId: Guid?`, `LocationDescription: string?`
- Source: `src/PropertyOS.Api/Models/Properties/UpdateParkingSpotRequest.cs:7`

### UpdateTenantEmergencyContactRequest

- Fields: `Name: string`, `RelationshipType: string`, `Phone: string`
- Source: `src/PropertyOS.Api/Models/Leasing/UpdateTenantEmergencyContactRequest.cs:3`

### UpdateTenantFamilyMemberRequest

- Fields: `Name: string`, `RelationshipType: string`, `AgeBracket: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/UpdateTenantFamilyMemberRequest.cs:3`

### UpdateTenantRequest

- Fields: `Name: string`, `NationalId: string`, `Phone: string`, `Email: string?`, `Occupation: string?`, `Employer: string?`, `PhoneCountryCode: string?`
- Source: `src/PropertyOS.Api/Models/Leasing/UpdateTenantRequest.cs:6`

### UpdateTenantVehicleRequest

- Fields: `PlateNumber: string`, `MakeModel: string`, `Color: string`
- Source: `src/PropertyOS.Api/Models/Leasing/UpdateTenantVehicleRequest.cs:3`

### UpdateViewingRequestStatusCommand

- Fields: `Id: Guid`, `Status: ViewingRequestStatus`, `StaffNotes: string?`
- Source: `src/PropertyOS.Application/Marketplace/Commands/UpdateViewingRequestStatus/UpdateViewingRequestStatusCommand.cs:7`

### UploadFileRequestRequest

- Fields: `ModuleName: string`, `EntityId: Guid`, `Filename: string`, `MimeType: string`, `SizeBytes: long`
- Source: `src/PropertyOS.Api/Models/Files/UploadFileRequestRequest.cs:6`

## Web API call sites, file by file

### `frontend/src/features/apartments/api/apartments.api.ts`

- GET `/api/v1/apartments/${id}` → `ApartmentDto`; backend match: **not statically matched** (line 33)
- PUT `/api/v1/apartments/${id}` → `void`; backend match: **not statically matched** (line 57)
- DELETE `/api/v1/apartments/${id}` → `void`; backend match: **not statically matched** (line 65)
- POST `/api/v1/floors/${floorId}/apartments` → `void`; backend match: **not statically matched** (line 45)
- GET `/api/v1/floors/${floorId}/apartments/next-number` → `inferred`; backend match: **not statically matched** (line 13)

### `frontend/src/features/auth/api/auth.api.ts`

- POST `/api/v1/auth/login` → `LoginResponseDto`; backend match: **not statically matched** (line 37)
- POST `/api/v1/auth/logout` → `void`; backend match: **not statically matched** (line 86)
- GET `/api/v1/auth/me` → `UserProfileDto`; backend match: **not statically matched** (line 94)
- POST `/api/v1/auth/otp/request` → `{ message: string }`; backend match: **not statically matched** (line 53)
- POST `/api/v1/auth/otp/verify` → `LoginResponseDto`; backend match: **not statically matched** (line 61)
- POST `/api/v1/auth/password-reset/complete` → `PasswordResetCompleteResponseDto`; backend match: **not statically matched** (line 73)
- POST `/api/v1/auth/password-reset/request` → `PasswordResetRequestResponseDto`; backend match: **not statically matched** (line 65)
- POST `/api/v1/auth/password-reset/verify-otp` → `PasswordResetOtpVerifyResponseDto`; backend match: **not statically matched** (line 69)
- POST `/api/v1/auth/refresh` → `LoginResponseDto`; backend match: **not statically matched** (line 81)
- POST `/api/v1/auth/register` → `RegisterResponseDto`; backend match: **not statically matched** (line 45)
- POST `/api/v1/auth/tenant-activate` → `LoginResponseDto`; backend match: **not statically matched** (line 110)
- GET `/api/v1/auth/tenant-activation-status?token=${encodeURIComponent(token)}` → `TenantActivationStatusDto`; backend match: **not statically matched** (line 102)

### `frontend/src/features/settings/settings.api.ts`

- POST `/api/v1/auth/logout-all` → `void`; backend match: **not statically matched** (line 44)
- PUT `/api/v1/companies/${id}` → `void`; backend match: **not statically matched** (line 39)
- GET `/api/v1/companies/${id}/settings` → `CompanySettings`; backend match: **not statically matched** (line 41)
- PUT `/api/v1/companies/${id}/settings` → `void`; backend match: **not statically matched** (line 43)
- GET `/api/v1/companies/me` → `Company`; backend match: **not statically matched** (line 37)

### `frontend/src/features/documents/documents.api.ts`

- PUT `/api/v1/building-documents/${id}` → `BuildingDocument`; backend match: **not statically matched** (line 69)
- DELETE `/api/v1/building-documents/${id}` → `void`; backend match: **not statically matched** (line 74)
- GET `/api/v1/building-documents/${id}` → `BuildingDocument`; backend match: **not statically matched** (line 94)
- GET `/api/v1/building-documents/${id}/download-url` → `{ downloadUrl: string }`; backend match: **not statically matched** (line 96)
- POST `/api/v1/building-documents/${id}/replace` → `BuildingDocument`; backend match: **not statically matched** (line 71)
- POST `/api/v1/buildings/${buildingId}/documents` → `BuildingDocument`; backend match: **not statically matched** (line 64)
- GET `/api/v1/buildings/${buildingId}/documents?${params}` → `DocumentPage`; backend match: **not statically matched** (line 89)
- GET `/api/v1/document-categories` → `Category[]`; backend match: **not statically matched** (line 75)
- POST `/api/v1/document-categories` → `Category`; backend match: **not statically matched** (line 77)
- PUT `/api/v1/document-categories/${id}` → `Category`; backend match: **not statically matched** (line 79)
- DELETE `/api/v1/document-categories/${id}` → `void`; backend match: **not statically matched** (line 81)

### `frontend/src/features/floors/api/floors.api.ts`

- GET `/api/v1/buildings/${buildingId}/floors` → `FloorDto[]`; backend match: **not statically matched** (line 14)
- POST `/api/v1/buildings/${buildingId}/floors` → `void`; backend match: **not statically matched** (line 35)
- GET `/api/v1/buildings/${buildingId}/floors/next-number` → `inferred`; backend match: **not statically matched** (line 7)
- GET `/api/v1/floors/${id}` → `FloorDto`; backend match: **not statically matched** (line 22)
- PUT `/api/v1/floors/${id}` → `void`; backend match: **not statically matched** (line 47)
- DELETE `/api/v1/floors/${id}` → `void`; backend match: **not statically matched** (line 55)

### `frontend/src/features/parking/parking.api.ts`

- GET `/api/v1/buildings/${buildingId}/parking-spots` → `ParkingSpot[]`; backend match: **not statically matched** (line 31)
- POST `/api/v1/buildings/${buildingId}/parking-spots` → `void`; backend match: **not statically matched** (line 33)
- GET `/api/v1/leasing/contracts/${leaseContractId}/parking` → `ParkingAssignment[]`; backend match: **not statically matched** (line 41)
- POST `/api/v1/parking-assignments/${assignmentId}/end` → `void`; backend match: **not statically matched** (line 45)
- PUT `/api/v1/parking-spots/${id}` → `void`; backend match: **not statically matched** (line 35)
- DELETE `/api/v1/parking-spots/${id}` → `void`; backend match: **not statically matched** (line 36)
- GET `/api/v1/parking-spots/${parkingSpotId}/assignment` → `ParkingAssignment | undefined`; backend match: **not statically matched** (line 38)
- POST `/api/v1/parking-spots/${parkingSpotId}/assignment` → `{ assignmentId: string }`; backend match: **not statically matched** (line 43)

### `frontend/src/features/landing/api/contactRequests.api.ts`

- POST `/api/v1/contact-requests` → `ContactRequestCreated`; backend match: **not statically matched** (line 19)

### `frontend/src/features/dashboard/api/dashboard.api.ts`

- GET `/api/v1/dashboard/summary` → `DashboardSummaryDto`; backend match: **not statically matched** (line 10)

### `frontend/src/features/financials/api/financials.api.ts`

- GET `/api/v1/expenses/${expenseId}` → `ExpenseDetailDto`; backend match: **not statically matched** (line 101)
- GET `/api/v1/expenses${qs ? ` → `ExpenseDto[]`; backend match: **not statically matched** (line 97)
- GET `/api/v1/rent-payments/${paymentId}` → `RentPaymentDetailDto`; backend match: **not statically matched** (line 48)
- GET `/api/v1/rent-payments/${paymentId}/receipt` → `RentPaymentReceiptDto`; backend match: **not statically matched** (line 53)
- POST `/api/v1/rent-payments/${paymentId}/remind` → `RemindRentPaymentResponseDto`; backend match: **not statically matched** (line 63)
- GET `/api/v1/rent-payments${qs ? ` → `RentPaymentDto[]`; backend match: **not statically matched** (line 44)

### `frontend/src/shared/services/files.api.ts`

- GET `/api/v1/files/${fileId}/download-url?inline=${inline}` → `FileDownloadUrlResponse`; backend match: **not statically matched** (line 67)
- POST `/api/v1/files/confirm` → `FileStorageDto`; backend match: **not statically matched** (line 123)
- POST `/api/v1/files/upload-request` → `UploadFileRequestResponse`; backend match: **not statically matched** (line 62)

### `frontend/src/features/buildings/api/locations.api.ts`

- GET `/api/v1/locations/reverse-geocode?${query}` → `ReverseGeocodingResult`; backend match: **not statically matched** (line 13)

### `frontend/src/features/notifications/api/notifications.api.ts`

- PATCH `/api/v1/notifications/${id}/read` → `void`; backend match: **not statically matched** (line 32)
- PATCH `/api/v1/notifications/me/read-all` → `MarkAllNotificationsAsReadResult`; backend match: **not statically matched** (line 40)
- GET `/api/v1/notifications/me/unread-count` → `number`; backend match: **not statically matched** (line 24)
- GET `/api/v1/notifications/me${query.size ? ` → `NotificationDto[]`; backend match: **not statically matched** (line 16)

### `frontend/src/features/payments/api/payments.api.ts`

- POST `/api/v1/owner/payments/${paymentId}/submissions/${submissionId}/approve` → `inferred`; backend match: **not statically matched** (line 31)
- POST `/api/v1/owner/payments/${paymentId}/submissions/${submissionId}/reject` → `inferred`; backend match: **not statically matched** (line 35)
- GET `/api/v1/owner/payments/pending-verifications?${params.toString()}` → `inferred`; backend match: **not statically matched** (line 12)
- GET `/api/v1/rent-payments/${paymentId}` → `inferred`; backend match: **not statically matched** (line 16)
- GET `/api/v1/rent-payments/${paymentId}/receipt` → `RentPaymentReceiptDto`; backend match: **not statically matched** (line 21)

### `frontend/src/features/tenantPortal/api/tenantPortal.api.ts`

- GET `/api/v1/tenant-portal/lease` → `TenantLeaseDto`; backend match: **not statically matched** (line 35)
- GET `/api/v1/tenant-portal/me` → `TenantDetailDto`; backend match: **not statically matched** (line 25)
- GET `/api/v1/tenant-portal/payments` → `TenantPaymentDto[]`; backend match: **not statically matched** (line 83)
- POST `/api/v1/tenant/payments/${cleanId}/submit-verification` → `string`; backend match: **not statically matched** (line 74)

### `frontend/src/features/utilityBills/api/utilityBills.api.ts`

- GET `${ACCOUNTS}/${id}` → `ManagementUtilityAccountDto`; backend match: **not statically matched** (line 20)
- DELETE `${ACCOUNTS}/${id}` → `void`; backend match: **not statically matched** (line 26)
- POST `${ACCOUNTS}/${id}/replace` → `string`; backend match: **not statically matched** (line 29)
- POST `${ACCOUNTS}/${id}/sync` → `void`; backend match: **not statically matched** (line 32)

### `frontend/src/features/leasing/api/leasing.api.ts`

- DELETE `${BASE_PATH}/${contractId}/documents/${documentId}` → `void`; backend match: **not statically matched** (line 72)
- GET `${BASE_PATH}/${contractId}/documents/${documentId}/download?inline=${inline}&redirect=false` → `ContractDocumentDownloadUrlDto`; backend match: **not statically matched** (line 64)
- PUT `${BASE_PATH}/${contractId}/documents/${documentId}/replace` → `void`; backend match: **not statically matched** (line 68)
- GET `${BASE_PATH}/${id}` → `LeaseContractDetailDto`; backend match: **not statically matched** (line 36)
- PUT `${BASE_PATH}/${id}` → `void`; backend match: **not statically matched** (line 44)
- POST `${BASE_PATH}/${id}/activate` → `void`; backend match: **not statically matched** (line 48)
- POST `${BASE_PATH}/${id}/documents` → `string`; backend match: **not statically matched** (line 60)
- POST `${BASE_PATH}/${id}/renew` → `string`; backend match: **not statically matched** (line 56)
- POST `${BASE_PATH}/${id}/terminate` → `void`; backend match: **not statically matched** (line 52)
- GET `${BASE_PATH}/expiring?daysAhead=${daysAhead}` → `LeaseContractDto[]`; backend match: **not statically matched** (line 28)
- GET `${BASE_PATH}/history/apartment/${apartmentId}?pageSize=${pageSize}` → `LeaseContractDto[]`; backend match: **not statically matched** (line 32)
- GET `${BASE_PATH}/next-number` → `inferred`; backend match: **not statically matched** (line 18)
- GET `${BASE_PATH}/search${query}` → `LeaseContractDto[]`; backend match: **not statically matched** (line 24)
- GET `${TENANTS_PATH}${query}` → `TenantLookupDto[]`; backend match: **not statically matched** (line 77)

### `frontend/src/features/buildings/api/buildings.api.ts`

- GET `${BASE_PATH}/${id}` → `BuildingDto`; backend match: **not statically matched** (line 19)
- PUT `${BASE_PATH}/${id}` → `void`; backend match: **not statically matched** (line 33)
- DELETE `${BASE_PATH}/${id}` → `void`; backend match: **not statically matched** (line 37)
- GET `${BASE_PATH}/next-code` → `inferred`; backend match: **not statically matched** (line 13)

### `frontend/src/features/maintenance/api/maintenance.api.ts`

- GET `${BASE_PATH}/${id}` → `MaintenanceRequestDetailDto`; backend match: **not statically matched** (line 25)
- PUT `${BASE_PATH}/${id}` → `void`; backend match: **not statically matched** (line 33)
- DELETE `${BASE_PATH}/${id}` → `void`; backend match: **not statically matched** (line 41)
- GET `${BASE_PATH}/${id}/attachments` → `MaintenanceAttachmentDto[]`; backend match: **not statically matched** (line 61)
- POST `${BASE_PATH}/${id}/attachments` → `string`; backend match: **not statically matched** (line 65)
- DELETE `${BASE_PATH}/${id}/attachments/${attachmentId}` → `void`; backend match: **not statically matched** (line 69)
- GET `${BASE_PATH}/${id}/comments` → `MaintenanceCommentDto[]`; backend match: **not statically matched** (line 45)
- POST `${BASE_PATH}/${id}/comments` → `string`; backend match: **not statically matched** (line 49)
- PUT `${BASE_PATH}/${id}/comments/${commentId}` → `void`; backend match: **not statically matched** (line 53)
- DELETE `${BASE_PATH}/${id}/comments/${commentId}` → `void`; backend match: **not statically matched** (line 57)
- PATCH `${BASE_PATH}/${id}/status` → `void`; backend match: **not statically matched** (line 37)
- GET `${BASE_PATH}/${id}/status-history` → `MaintenanceStatusHistoryDto[]`; backend match: **not statically matched** (line 73)

### `frontend/src/features/tenants/api/tenants.api.ts`

- GET `${BASE_PATH}/${tenantId}` → `TenantDetailDto`; backend match: **not statically matched** (line 30)
- PUT `${BASE_PATH}/${tenantId}` → `void`; backend match: **not statically matched** (line 38)
- DELETE `${BASE_PATH}/${tenantId}` → `void`; backend match: **not statically matched** (line 42)
- POST `${BASE_PATH}/${tenantId}/account` → `ProvisionTenantAccountResponseDto`; backend match: **not statically matched** (line 125)
- GET `${BASE_PATH}/${tenantId}/emergency-contacts` → `TenantEmergencyContactDto[]`; backend match: **not statically matched** (line 74)
- POST `${BASE_PATH}/${tenantId}/emergency-contacts` → `string`; backend match: **not statically matched** (line 82)
- GET `${BASE_PATH}/${tenantId}/emergency-contacts/${contactId}` → `TenantEmergencyContactDto`; backend match: **not statically matched** (line 78)
- PUT `${BASE_PATH}/${tenantId}/emergency-contacts/${contactId}` → `void`; backend match: **not statically matched** (line 90)
- DELETE `${BASE_PATH}/${tenantId}/emergency-contacts/${contactId}` → `void`; backend match: **not statically matched** (line 94)
- GET `${BASE_PATH}/${tenantId}/family-members` → `TenantFamilyMemberDto[]`; backend match: **not statically matched** (line 50)
- POST `${BASE_PATH}/${tenantId}/family-members` → `string`; backend match: **not statically matched** (line 58)
- GET `${BASE_PATH}/${tenantId}/family-members/${familyMemberId}` → `TenantFamilyMemberDto`; backend match: **not statically matched** (line 54)
- PUT `${BASE_PATH}/${tenantId}/family-members/${familyMemberId}` → `void`; backend match: **not statically matched** (line 66)
- DELETE `${BASE_PATH}/${tenantId}/family-members/${familyMemberId}` → `void`; backend match: **not statically matched** (line 70)
- GET `${BASE_PATH}/${tenantId}/leases?pageSize=${pageSize}` → `LeaseContractDto[]`; backend match: **not statically matched** (line 46)
- GET `${BASE_PATH}/${tenantId}/vehicles` → `TenantVehicleDto[]`; backend match: **not statically matched** (line 98)
- POST `${BASE_PATH}/${tenantId}/vehicles` → `string`; backend match: **not statically matched** (line 106)
- GET `${BASE_PATH}/${tenantId}/vehicles/${vehicleId}` → `TenantVehicleDto`; backend match: **not statically matched** (line 102)
- PUT `${BASE_PATH}/${tenantId}/vehicles/${vehicleId}` → `void`; backend match: **not statically matched** (line 114)
- DELETE `${BASE_PATH}/${tenantId}/vehicles/${vehicleId}` → `void`; backend match: **not statically matched** (line 118)
- GET `${BASE_PATH}${query}` → `TenantDto[]`; backend match: **not statically matched** (line 26)

### `frontend/src/features/platformAdmin/api/contactRequests.api.ts`

- GET `${base}?${query}` → `ContactRequestPage`; backend match: **not statically matched** (line 5)
- GET `${base}/${encodeURIComponent(id)}` → `ContactRequestDetail`; backend match: **not statically matched** (line 6)
- PATCH `${base}/${encodeURIComponent(id)}/status` → `ContactRequestDetail`; backend match: **not statically matched** (line 7)

### `frontend/src/features/platformAdmin/api/platformAdmin.api.ts`

- GET `${base}/${encodeURIComponent(registrationId)}` → `LandlordRegistrationDetailDto`; backend match: **not statically matched** (line 16)
- POST `${base}/${encodeURIComponent(registrationId)}/approve` → `LandlordRegistrationReviewResultDto`; backend match: **not statically matched** (line 19)
- POST `${base}/${encodeURIComponent(registrationId)}/reject` → `LandlordRegistrationReviewResultDto`; backend match: **not statically matched** (line 22)
- GET `${base}/pending?page=${page}&pageSize=${pageSize}` → `LandlordRegistrationPageDto`; backend match: **not statically matched** (line 13)

### `frontend/src/features/subscriptions/api/subscriptions.api.ts`

- GET `${companyBase}/me` → `UserSubscriptionDto`; backend match: **not statically matched** (line 38)
- POST `${companyBase}/plan-change-requests` → `PlanChangeRequestWireDto`; backend match: **not statically matched** (line 41)
- POST `${companyBase}/plan-change-requests/${encodeURIComponent(id)}/cancel` → `PlanChangeRequestWireDto`; backend match: **not statically matched** (line 42)
- GET `${companyBase}/plan-change-requests${query({ page, pageSize })}` → `PlanChangeRequestPageWireDto`; backend match: **not statically matched** (line 40)
- GET `${companyBase}/plans${query({ page, pageSize })}` → `PlanPageWireDto`; backend match: **not statically matched** (line 37)
- GET `${companyBase}/usage/current` → `PaygUsageWireDto`; backend match: **not statically matched** (line 39)
- GET `${platformBase}/companies` → `PlatformCompanyListItemDto[]`; backend match: **not statically matched** (line 55)
- GET `${platformBase}/plan-change-requests/${encodeURIComponent(id)}` → `PlanChangeRequestWireDto`; backend match: **not statically matched** (line 58)
- POST `${platformBase}/plan-change-requests/${encodeURIComponent(id)}/approve` → `PlanChangeRequestDto`; backend match: **not statically matched** (line 59)
- POST `${platformBase}/plan-change-requests/${encodeURIComponent(id)}/reject` → `PlanChangeRequestDto`; backend match: **not statically matched** (line 60)
- GET `${platformBase}/plan-change-requests${query(filters)}` → `PlanChangeRequestPageWireDto`; backend match: **not statically matched** (line 57)
- POST `${platformBase}/plans` → `SubscriptionPlanWireDto`; backend match: **not statically matched** (line 46)
- GET `${platformBase}/plans/${encodeURIComponent(id)}` → `SubscriptionPlanWireDto`; backend match: **not statically matched** (line 45)
- GET `${platformBase}/plans${query(filters)}` → `PlanPageWireDto`; backend match: **not statically matched** (line 44)
- POST `${platformBase}/subscriptions` → `SubscriptionAdministrationWireDto`; backend match: **not statically matched** (line 53)
- GET `${platformBase}/subscriptions/${encodeURIComponent(id)}` → `SubscriptionAdministrationWireDto`; backend match: **not statically matched** (line 50)
- GET `${platformBase}/subscriptions${query(filters)}` → `SubscriptionPageWireDto`; backend match: **not statically matched** (line 49)

### `frontend/src/features/tenantPortal/api/utilityBills.api.ts`

- GET `${MY_UTILITY_BILLS}/accounts` → `TenantUtilityAccountDto[]`; backend match: **not statically matched** (line 25)
- POST `${MY_UTILITY_BILLS}/accounts` → `TenantUtilityAccountDto`; backend match: **not statically matched** (line 28)
- DELETE `${MY_UTILITY_BILLS}/accounts/${id}` → `void`; backend match: **not statically matched** (line 34)
- POST `${MY_UTILITY_BILLS}/accounts/${id}/replace` → `TenantUtilityAccountDto`; backend match: **not statically matched** (line 31)
- POST `${MY_UTILITY_BILLS}/accounts/${id}/sync` → `void`; backend match: **not statically matched** (line 37)
- GET `${MY_UTILITY_BILLS}/dashboard-summary` → `TenantUtilityDashboardSummaryDto`; backend match: **not statically matched** (line 45)

## Mobile API call sites, file by file

### `mobile/lib/features/properties/data/properties_repository.dart`

- DELETE `/api/v1/${kind.name}/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 118)
- GET `/api/v1/apartments`; backend match: **not statically matched** (line 76)
- GET `/api/v1/apartments/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 103)
- GET `/api/v1/buildings`; backend match: **not statically matched** (line 58)
- GET `/api/v1/buildings/${Uri.encodeComponent(buildingId)}/floors`; backend match: **not statically matched** (line 65)
- GET `/api/v1/buildings/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 89)
- GET `/api/v1/floors/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 96)

### `mobile/lib/features/auth/data/auth_repository.dart`

- POST `/api/v1/auth/login`; backend match: **not statically matched** (line 27)
- POST `/api/v1/auth/logout`; backend match: **not statically matched** (line 147)
- POST `/api/v1/auth/logout-all`; backend match: **not statically matched** (line 158)
- GET `/api/v1/auth/me`; backend match: **not statically matched** (line 136)
- POST `/api/v1/auth/otp/verify`; backend match: **not statically matched** (line 77)
- POST `/api/v1/auth/password-reset/verify-otp`; backend match: **not statically matched** (line 93)
- POST `/api/v1/auth/tenant-activate`; backend match: **not statically matched** (line 123)
- GET `/api/v1/auth/tenant-activation-status`; backend match: **not statically matched** (line 115)

### `mobile/lib/features/settings/settings.dart`

- PUT `/api/v1/companies/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 50)
- GET `/api/v1/companies/${Uri.encodeComponent(id)}/settings`; backend match: **not statically matched** (line 62)
- PUT `/api/v1/companies/${Uri.encodeComponent(id)}/settings`; backend match: **not statically matched** (line 73)
- GET `/api/v1/companies/me`; backend match: **not statically matched** (line 42)

### `mobile/lib/features/dashboard/data/dashboard_repository.dart`

- GET `/api/v1/dashboard/summary`; backend match: **not statically matched** (line 18)

### `mobile/lib/features/leasing/data/leasing_repository.dart`

- GET `/api/v1/leasing/tenants`; backend match: **not statically matched** (line 65)
- GET `/api/v1/leasing/tenants`; backend match: **not statically matched** (line 70)
- GET `/api/v1/leasing/tenants/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 77)
- PUT `/api/v1/leasing/tenants/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 84)
- DELETE `/api/v1/leasing/tenants/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 89)
- POST `/api/v1/leasing/tenants/${Uri.encodeComponent(id)}/account`; backend match: **not statically matched** (line 99)
- GET `/api/v1/leasing/tenants/${Uri.encodeComponent(id)}/leases`; backend match: **not statically matched** (line 91)
- PUT `/api/v1/leasing/tenants/${Uri.encodeComponent(tenantId)}/$collection/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 116)
- DELETE `/api/v1/leasing/tenants/${Uri.encodeComponent(tenantId)}/$collection/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 124)
- GET `$base/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 58)
- GET `$base/${Uri.encodeComponent(id)}/parking`; backend match: **not statically matched** (line 128)
- PUT `$base/$id`; backend match: **not statically matched** (line 134)
- POST `$base/$id/activate`; backend match: **not statically matched** (line 135)
- DELETE `$base/$id/documents/$docId`; backend match: **not statically matched** (line 145)
- GET `$base/$id/documents/$docId/download`; backend match: **not statically matched** (line 148)
- PUT `$base/$id/documents/$docId/replace`; backend match: **not statically matched** (line 143)
- POST `$base/$id/terminate`; backend match: **not statically matched** (line 139)
- GET `$base/expiring`; backend match: **not statically matched** (line 45)
- GET `$base/history/apartment/${Uri.encodeComponent(apartmentId)}`; backend match: **not statically matched** (line 51)
- GET `$base/next-number`; backend match: **not statically matched** (line 62)
- GET `$base/search`; backend match: **not statically matched** (line 39)
- GET `$utilities/${Uri.encodeComponent(id)}`; backend match: **not statically matched** (line 196)
- GET `$utilities/${Uri.encodeComponent(id)}/bills`; backend match: **not statically matched** (line 205)
- DELETE `$utilities/$id`; backend match: **not statically matched** (line 220)
- POST `$utilities/$id/sync`; backend match: **not statically matched** (line 219)

## Non-REST and special integrations

- SignalR notifications hub: `/hubs/notifications`; bearer token may be supplied as the `access_token` query parameter for hub connections only.
- File upload/download uses signed capability URLs. The binary upload body is raw bytes (`application/octet-stream`), and download returns a binary stream with attachment headers.
- eFAWATEERcom callback is an inbound webhook protected by configured HMAC signing, not by a user JWT.
- Development email/SMS/seed/test controllers are environment-gated and must not be treated as production APIs.
- Health checks, OpenAPI JSON, Scalar UI, and Hangfire dashboard are operational surfaces configured in `Program.cs`, not business-client endpoints.

## Audit limitations

- This is a static source audit. Runtime-only conventions, middleware-added responses, and dynamically concatenated client URLs may require a running integration environment to confirm.
- `ProducesResponseType` is not present on every action. Where absent, the report retains the action return type or marks the response as unspecified.
- The JSON companion is the complete machine-readable inventory and includes all extracted parameters, source locations, client matches, and parsed schemas.
