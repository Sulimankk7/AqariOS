#!/usr/bin/env python3
"""
AqariOS API Seeder - Runtime Verified v6

Verified runtime behavior:
- POST /api/v1/auth/register returns top-level:
    userId, companyId, accessToken, refreshToken, tokenType, expiresIn, user
- The token returned by register may NOT contain permission claims.
- Therefore the seeder immediately performs POST /api/v1/auth/login
  and uses the login accessToken for all permission-protected endpoints.
- The runtime role is COMPANY_ADMIN and /api/v1/auth/me returns the effective
  permissions such as properties.read/properties.create/contracts.create/etc.

This script seeds only through the public API. It does not write SQL directly.
"""

import os
import sys
import json
import time
import hashlib
from pathlib import Path
from datetime import date, timedelta, datetime, timezone
from urllib.parse import urljoin

try:
    import requests
except ImportError:
    print("Missing dependency: requests")
    print("Install it with:")
    print("  python -m pip install requests")
    sys.exit(1)


# ============================================================
# Configuration
# ============================================================

BASE_URL = os.getenv("AQARIOS_BASE_URL", "http://localhost:5235").rstrip("/")

# Small verification dataset by default.
# Increase these later after the seeder completes successfully.
OWNERS = int(os.getenv("AQARIOS_OWNERS", "2"))
BUILDINGS_PER_OWNER = int(os.getenv("AQARIOS_BUILDINGS_PER_OWNER", "2"))
FLOORS_PER_BUILDING = int(os.getenv("AQARIOS_FLOORS_PER_BUILDING", "3"))
APARTMENTS_PER_FLOOR = int(os.getenv("AQARIOS_APARTMENTS_PER_FLOOR", "5"))
TENANTS_PER_OWNER = int(os.getenv("AQARIOS_TENANTS_PER_OWNER", "10"))
LEASES_PER_OWNER = int(os.getenv("AQARIOS_LEASES_PER_OWNER", "10"))

# Tenant activation is rate-limited (5 / 60 sec / IP), so keep this low at first.
TENANT_ACCOUNTS_PER_OWNER = int(os.getenv("AQARIOS_TENANT_ACCOUNTS_PER_OWNER", "2"))

MAINTENANCE_PER_OWNER = int(os.getenv("AQARIOS_MAINTENANCE_PER_OWNER", "10"))
EXPENSES_PER_OWNER = int(os.getenv("AQARIOS_EXPENSES_PER_OWNER", "5"))
MARKETPLACE_PER_OWNER = int(os.getenv("AQARIOS_MARKETPLACE_PER_OWNER", "5"))

OWNER_PASSWORD = os.getenv("AQARIOS_OWNER_PASSWORD", "OwnerTest123!")
TENANT_PASSWORD = os.getenv("AQARIOS_TENANT_PASSWORD", "TenantTest123!")

REQUEST_TIMEOUT = float(os.getenv("AQARIOS_TIMEOUT", "30"))

# Global limiter is 100 requests / 60 sec / IP.
# 0.75 sec keeps the normal request rate below that threshold.
REQUEST_DELAY = float(os.getenv("AQARIOS_REQUEST_DELAY", "0.75"))

# Dedicated tenant-activate limiter is 5 / 60 sec / IP.
TENANT_ACTIVATION_DELAY = float(
    os.getenv("AQARIOS_TENANT_ACTIVATION_DELAY", "12.5")
)

# If HTTP 429 is received, wait and retry.
MAX_429_RETRIES = int(os.getenv("AQARIOS_MAX_429_RETRIES", "3"))

# File upload module name. The OpenAPI schema requires moduleName but does not
# publish a fixed enum. We try a small set once and cache the first accepted
# value. You can force one with AQARIOS_FILE_MODULE_NAME.
FILE_MODULE_NAME = os.getenv("AQARIOS_FILE_MODULE_NAME", "").strip()
_RESOLVED_FILE_MODULE_NAME = None

RUN_ID = os.getenv(
    "AQARIOS_RUN_ID",
    datetime.now(timezone.utc).strftime("%Y%m%d%H%M%S"),
)

OUT_DIR = Path(os.getenv("AQARIOS_OUTPUT_DIR", "load-tests/output"))
OUT_DIR.mkdir(parents=True, exist_ok=True)

OUT_FILE = OUT_DIR / f"aqarios_seed_{RUN_ID}.json"

# IMPORTANT:
# The Windows host may already be on the next calendar day (e.g. Jordan UTC+3)
# while the API/container is still on the previous UTC day.
# Using date.today() on the host can therefore make a "today" date look like
# a future date to the API. Use UTC and subtract one day for write fields that
# are validated as "must not be in the future".
UTC_TODAY = datetime.now(timezone.utc).date()
SAFE_NON_FUTURE_DATE = UTC_TODAY - timedelta(days=1)


# ============================================================
# Errors / Helpers
# ============================================================

class SeederError(RuntimeError):
    pass


def _safe_json(response):
    if not response.content:
        return None

    try:
        return response.json()
    except Exception:
        return response.text.strip()


def call(
    session,
    method,
    path,
    *,
    token=None,
    body=None,
    expected=(200,),
):
    """
    Execute an AqariOS API request.

    - Adds Bearer token when provided.
    - Respects global request delay.
    - Retries HTTP 429 using Retry-After when available.
    - Raises SeederError on unexpected status.
    """
    headers = {
        "Accept": "application/json",
    }

    if token:
        headers["Authorization"] = f"Bearer {token}"

    if body is not None:
        headers["Content-Type"] = "application/json"

    url = f"{BASE_URL}{path}"

    for attempt in range(MAX_429_RETRIES + 1):
        try:
            response = session.request(
                method,
                url,
                headers=headers,
                json=body,
                timeout=REQUEST_TIMEOUT,
            )
        except requests.RequestException as exc:
            raise SeederError(f"Request failed: {method} {path}: {exc}") from exc

        time.sleep(REQUEST_DELAY)

        if response.status_code == 429 and attempt < MAX_429_RETRIES:
            retry_after = response.headers.get("Retry-After")

            try:
                wait_seconds = float(retry_after) if retry_after else 15.0
            except ValueError:
                wait_seconds = 15.0

            wait_seconds = max(wait_seconds, 15.0)

            print(
                f"[RATE LIMIT] {method} {path} -> 429. "
                f"Waiting {wait_seconds:.1f}s before retry..."
            )
            time.sleep(wait_seconds)
            continue

        if response.status_code not in expected:
            payload = _safe_json(response)
            if isinstance(payload, (dict, list)):
                payload_text = json.dumps(
                    payload,
                    ensure_ascii=False,
                    indent=2,
                )
            else:
                payload_text = str(payload)

            raise SeederError(
                f"{method} {path} -> HTTP {response.status_code}\n"
                f"{payload_text[:2000]}"
            )

        return _safe_json(response)

    raise SeederError(f"Too many retries: {method} {path}")


def scalar_guid(value):
    """
    Endpoints that return Guid may be parsed by requests as a Python string.
    Normalize it.
    """
    if isinstance(value, str):
        return value.strip().strip('"')
    return value


def save_progress(result):
    OUT_FILE.write_text(
        json.dumps(result, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )


# ============================================================
# Authentication
# ============================================================

def register_and_login_owner(session, owner_index):
    """
    Runtime-verified flow:

        register
          -> returns top-level userId + companyId
          -> register JWT may be missing permission claims

        login
          -> returns JWT with permissions
          -> this token is used by the seeder
    """
    suffix = f"{RUN_ID}-{owner_index:02d}"
    email = f"owner-{suffix}@aqarios-test.local"

    register_body = {
        "fullName": f"AqariOS Load Owner {owner_index:02d}",
        "email": email,
        "phone": None,
        "password": OWNER_PASSWORD,
        "companyName": f"AqariOS Load Company {suffix}",
        "displayName": f"Load Company {owner_index:02d}",
        "companyType": 1,
        "countryCode": "JO",
        "preferredLanguage": "ar",
    }

    reg = call(
        session,
        "POST",
        "/api/v1/auth/register",
        body=register_body,
        expected=(201,),
    )

    if not isinstance(reg, dict):
        raise SeederError(
            f"Unexpected register response type: {type(reg).__name__}"
        )

    user_id = reg.get("userId")
    company_id = reg.get("companyId")

    if not user_id or not company_id:
        raise SeederError(
            "Register succeeded but runtime-verified top-level "
            "`userId` / `companyId` are missing.\n"
            f"Returned keys: {list(reg.keys())}"
        )

    login_body = {
        "emailOrPhone": email,
        "password": OWNER_PASSWORD,
    }

    login = call(
        session,
        "POST",
        "/api/v1/auth/login",
        body=login_body,
        expected=(200,),
    )

    if not isinstance(login, dict):
        raise SeederError(
            f"Unexpected login response type: {type(login).__name__}"
        )

    access_token = login.get("accessToken")
    user = login.get("user") or {}
    permissions = user.get("permissions") or []
    roles = user.get("companyRoles") or []

    if not access_token:
        raise SeederError("Login succeeded but accessToken is missing.")

    required = {
        "properties.read",
        "properties.create",
        "contracts.create",
        "contracts.approve",
        "maintenance.create",
        "expenses.create",
    }

    missing = sorted(required.difference(set(permissions)))
    if missing:
        raise SeederError(
            "Login succeeded but required permissions are missing: "
            + ", ".join(missing)
        )

    role_code = roles[0].get("roleCode") if roles else None

    # Runtime preflight: this must be authorized with the LOGIN token.
    call(
        session,
        "GET",
        "/api/v1/buildings",
        token=access_token,
        expected=(200,),
    )

    return {
        "email": email,
        "password": OWNER_PASSWORD,
        "user_id": user_id,
        "company_id": company_id,
        "access_token": access_token,
        "role_code": role_code,
        "permissions": permissions,
        "buildings": [],
        "tenants": [],
        "tenant_accounts": [],
        "leases": [],
        "maintenance_requests": [],
        "expenses": [],
        "marketplace_listings": [],
    }


# ============================================================
# Buildings / Floors / Apartments
# ============================================================

def create_building(session, token, owner_index, building_index):
    internal_code = (
        f"LT-{RUN_ID[-8:]}-{owner_index:02d}-{building_index:02d}"
    )
    name = f"Load Building {owner_index:02d}-{building_index:02d}"

    body = {
        "name": name,
        "totalFloors": FLOORS_PER_BUILDING,
        "buildingType": building_index % 3,
        "internalCode": internal_code,
        "constructionYear": 2022,
        "gpsLatitude": 31.9539,
        "gpsLongitude": 35.9106,
        "addressGovernorate": 0,
        "addressCity": "Amman",
        "addressNeighborhood": "LoadTest",
        "addressStreet": f"Test Street {building_index}",
        "addressPostalCode": "11118",
    }

    call(
        session,
        "POST",
        "/api/v1/buildings",
        token=token,
        body=body,
        expected=(204,),
    )

    buildings = call(
        session,
        "GET",
        "/api/v1/buildings",
        token=token,
        expected=(200,),
    )

    if not isinstance(buildings, list):
        raise SeederError(
            f"GET /buildings returned {type(buildings).__name__}, expected list."
        )

    match = next(
        (
            b for b in buildings
            if b.get("internalCode") == internal_code
            or b.get("name") == name
        ),
        None,
    )

    if not match:
        raise SeederError(
            f"Created building was not found after POST: {internal_code}"
        )

    return {
        "id": match["id"],
        "name": name,
        "internal_code": internal_code,
        "floors": [],
        "apartments": [],
    }


def create_floor(session, token, building_id, floor_number):
    body = {
        "floorNumber": floor_number,
        "floorLabel": f"Floor {floor_number}",
        "floorType": 1 if floor_number == 0 else 2,
    }

    call(
        session,
        "POST",
        f"/api/v1/buildings/{building_id}/floors",
        token=token,
        body=body,
        expected=(204,),
    )

    floors = call(
        session,
        "GET",
        f"/api/v1/buildings/{building_id}/floors",
        token=token,
        expected=(200,),
    )

    if not isinstance(floors, list):
        raise SeederError(
            f"GET floors returned {type(floors).__name__}, expected list."
        )

    match = next(
        (
            f for f in floors
            if int(f.get("floorNumber", -9999)) == floor_number
        ),
        None,
    )

    if not match:
        raise SeederError(
            f"Created floor {floor_number} was not found for building {building_id}"
        )

    return {
        "id": match["id"],
        "floor_number": floor_number,
        "apartments": [],
    }


def create_apartment(
    session,
    token,
    building_id,
    floor_id,
    owner_index,
    building_index,
    floor_index,
    apartment_index,
):
    unit_number = (
        f"LT-{owner_index:02d}"
        f"{building_index:02d}"
        f"{floor_index:02d}"
        f"{apartment_index:02d}"
    )

    body = {
        "unitNumber": unit_number,
        "areaSqm": 85 + apartment_index * 5,
        "ownershipStatus": 0,
        "bedrooms": 1 + (apartment_index % 4),
        "bathrooms": 1 + (apartment_index % 3),
        "baseRentAmount": 300 + apartment_index * 25,
        "baseRentCurrency": "JOD",
    }

    call(
        session,
        "POST",
        f"/api/v1/floors/{floor_id}/apartments",
        token=token,
        body=body,
        expected=(204,),
    )

    apartments = call(
        session,
        "GET",
        (
            "/api/v1/apartments"
            f"?buildingId={building_id}&floorId={floor_id}"
        ),
        token=token,
        expected=(200,),
    )

    if not isinstance(apartments, list):
        raise SeederError(
            f"GET apartments returned {type(apartments).__name__}, expected list."
        )

    match = next(
        (
            a for a in apartments
            if a.get("unitNumber") == unit_number
        ),
        None,
    )

    if not match:
        raise SeederError(
            f"Created apartment was not found: {unit_number}"
        )

    return {
        "id": match["id"],
        "unit_number": unit_number,
        "building_id": building_id,
        "floor_id": floor_id,
    }


# ============================================================
# Tenants
# ============================================================

def jordan_test_phone(owner_index, tenant_index):
    """
    Generate a deterministic but run-unique Jordanian test mobile number.

    The previous seeder used only owner_index/tenant_index, so rerunning the
    script generated the SAME phone numbers and could hit PHONE_ALREADY_EXISTS
    after a failed/partial seed run.

    Format used here:
        +96279XXXXXXX
    which matches the project's Jordan E.164 validation.
    """
    raw = f"{RUN_ID}:{owner_index}:{tenant_index}".encode("utf-8")
    digest = hashlib.sha256(raw).digest()
    number = int.from_bytes(digest[:8], "big") % 10_000_000
    return f"+96279{number:07d}"


def create_tenant(session, token, owner_index, tenant_index):
    email = (
        f"tenant-{RUN_ID}-{owner_index:02d}-{tenant_index:03d}"
        "@aqarios-test.local"
    )

    body = {
        "name": f"Load Tenant {owner_index:02d}-{tenant_index:03d}",
        "nationalId": (
            f"{RUN_ID[-6:]}{owner_index:02d}{tenant_index:02d}"
        )[-10:],
        "phone": jordan_test_phone(owner_index, tenant_index),
        "email": email,
        "occupation": "Load Test User",
        "employer": "AqariOS Performance Lab",
    }

    tenant_id = call(
        session,
        "POST",
        "/api/v1/leasing/tenants",
        token=token,
        body=body,
        expected=(201,),
    )

    tenant_id = scalar_guid(tenant_id)

    if not tenant_id:
        raise SeederError("Tenant create returned no tenant ID.")

    return {
        "id": tenant_id,
        "email": email,
        "phone": body["phone"],
        "name": body["name"],
    }


def provision_and_activate_tenant(session, owner_token, tenant):
    provision_body = {
        "contactMethod": 1,
        "phone": tenant["phone"],
        "email": tenant["email"],
    }

    try:
        provision = call(
            session,
            "POST",
            f"/api/v1/leasing/tenants/{tenant['id']}/account",
            token=owner_token,
            body=provision_body,
            expected=(201,),
        )
    except SeederError as exc:
        # Runtime hardening for partially-seeded development databases:
        # if an old test user already owns the generated phone number, retry
        # provisioning by Email only. Email is unique per RUN_ID.
        if "PHONE_ALREADY_EXISTS" not in str(exc):
            raise

        print(
            f"[WARN] Phone already exists for {tenant['email']}; "
            "retrying tenant account provisioning with email only..."
        )

        provision_body = {
            "contactMethod": 1,
            "phone": None,
            "email": tenant["email"],
        }

        provision = call(
            session,
            "POST",
            f"/api/v1/leasing/tenants/{tenant['id']}/account",
            token=owner_token,
            body=provision_body,
            expected=(201,),
        )

    if not isinstance(provision, dict):
        raise SeederError(
            "Tenant provision response is not a JSON object."
        )

    activation_token = provision.get("activationToken")

    if not activation_token:
        raise SeederError(
            "Tenant provision response does not contain activationToken."
        )

    activated = call(
        session,
        "POST",
        "/api/v1/auth/tenant-activate",
        body={
            "activationToken": activation_token,
            "password": TENANT_PASSWORD,
        },
        expected=(200,),
    )

    if not isinstance(activated, dict) or not activated.get("accessToken"):
        raise SeederError(
            "Tenant activation succeeded but accessToken is missing."
        )

    # Dedicated 5/min/IP limiter.
    time.sleep(TENANT_ACTIVATION_DELAY)

    return {
        "tenant_id": tenant["id"],
        "user_id": provision.get("userId"),
        "company_id": provision.get("companyId"),
        "email": tenant["email"],
        "password": TENANT_PASSWORD,
        "access_token": activated["accessToken"],
    }


# ============================================================
# File Upload / Signed Contract Documents
# ============================================================

def make_minimal_pdf_bytes(title="AqariOS Load Test Signed Lease"):
    """
    Build a tiny, syntactically valid PDF in memory.
    This keeps the seeder self-contained and also satisfies the API's
    file magic-byte validation for application/pdf.
    """
    safe_title = (
        str(title)
        .replace("\\", "\\\\")
        .replace("(", "\\(")
        .replace(")", "\\)")
    )

    stream = (
        "BT\n"
        "/F1 12 Tf\n"
        "72 720 Td\n"
        f"({safe_title}) Tj\n"
        "0 -20 Td\n"
        "(Synthetic signed contract document for local load testing.) Tj\n"
        "ET\n"
    ).encode("ascii", errors="replace")

    objects = [
        b"<< /Type /Catalog /Pages 2 0 R >>",
        b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        (
            b"<< /Type /Page /Parent 2 0 R "
            b"/MediaBox [0 0 612 792] "
            b"/Resources << /Font << /F1 5 0 R >> >> "
            b"/Contents 4 0 R >>"
        ),
        b"<< /Length " + str(len(stream)).encode("ascii") + b" >>\nstream\n"
        + stream
        + b"endstream",
        b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    ]

    pdf = bytearray(b"%PDF-1.4\n%\xe2\xe3\xcf\xd3\n")
    offsets = [0]

    for index, obj in enumerate(objects, start=1):
        offsets.append(len(pdf))
        pdf.extend(f"{index} 0 obj\n".encode("ascii"))
        pdf.extend(obj)
        pdf.extend(b"\nendobj\n")

    xref_offset = len(pdf)
    pdf.extend(f"xref\n0 {len(objects) + 1}\n".encode("ascii"))
    pdf.extend(b"0000000000 65535 f \n")

    for offset in offsets[1:]:
        pdf.extend(f"{offset:010d} 00000 n \n".encode("ascii"))

    pdf.extend(
        (
            f"trailer\n<< /Size {len(objects) + 1} /Root 1 0 R >>\n"
            f"startxref\n{xref_offset}\n%%EOF\n"
        ).encode("ascii")
    )

    return bytes(pdf)


def resolve_upload_url(upload_url):
    if not upload_url:
        raise SeederError("Upload request succeeded but uploadUrl is missing.")
    return urljoin(BASE_URL + "/", upload_url)


def request_file_upload(
    session,
    token,
    entity_id,
    filename,
    mime_type,
    size_bytes,
):
    """
    Runtime/OpenAPI verified request schema:
      moduleName, entityId, filename, mimeType, sizeBytes

    moduleName is required but is not exposed as an OpenAPI enum. To keep the
    seeder robust across the current implementation, try likely leasing module
    names once and cache the first accepted one.
    """
    global _RESOLVED_FILE_MODULE_NAME

    if _RESOLVED_FILE_MODULE_NAME:
        candidates = [_RESOLVED_FILE_MODULE_NAME]
    elif FILE_MODULE_NAME:
        candidates = [FILE_MODULE_NAME]
    else:
        candidates = [
            "Leasing",
            "LeaseContracts",
            "LeaseContract",
            "leasing",
            "lease-contracts",
        ]

    last_error = None

    for module_name in candidates:
        body = {
            "moduleName": module_name,
            "entityId": entity_id,
            "filename": filename,
            "mimeType": mime_type,
            "sizeBytes": size_bytes,
        }

        try:
            result = call(
                session,
                "POST",
                "/api/v1/files/upload-request",
                token=token,
                body=body,
                expected=(200,),
            )
        except SeederError as exc:
            last_error = exc

            # Only try another moduleName when the request is rejected as a
            # validation/business-rule error. Other errors should stop.
            message = str(exc)
            if "HTTP 400" in message or "HTTP 422" in message:
                continue
            raise

        if not isinstance(result, dict):
            raise SeederError(
                "Upload request returned a non-object response."
            )

        required = ("fileId", "storageKey", "uploadUrl")
        missing = [key for key in required if not result.get(key)]

        if missing:
            raise SeederError(
                "Upload request response is missing: "
                + ", ".join(missing)
            )

        _RESOLVED_FILE_MODULE_NAME = module_name
        return result

    raise SeederError(
        "Could not find an accepted file moduleName for lease documents. "
        "Set AQARIOS_FILE_MODULE_NAME to the value expected by the API.\n"
        f"Last error: {last_error}"
    )


def upload_file_bytes(session, upload_url, payload, mime_type):
    """
    PUT raw bytes to the signed uploadUrl returned by /files/upload-request.
    The upload endpoint is capability-token protected by the URL itself, so
    no Bearer token is sent.
    """
    url = resolve_upload_url(upload_url)

    try:
        response = session.put(
            url,
            data=payload,
            headers={
                "Content-Type": mime_type,
                "Content-Length": str(len(payload)),
            },
            timeout=REQUEST_TIMEOUT,
        )
    except requests.RequestException as exc:
        raise SeederError(
            f"Binary file upload failed: {exc}"
        ) from exc

    time.sleep(REQUEST_DELAY)

    if response.status_code != 204:
        body = _safe_json(response)
        raise SeederError(
            f"PUT signed upload URL -> HTTP {response.status_code}\n"
            f"{str(body)[:2000]}"
        )


def confirm_file_upload(
    session,
    token,
    file_id,
    storage_key,
    filename,
    mime_type,
    size_bytes,
):
    """
    Runtime/OpenAPI verified confirm schema:
      fileId, storageKey, originalFilename, mimeType, sizeBytes

    Returns FileStorageDto, including the persisted `id`.
    """
    body = {
        "fileId": file_id,
        "storageKey": storage_key,
        "originalFilename": filename,
        "mimeType": mime_type,
        "sizeBytes": size_bytes,
    }

    confirmed = call(
        session,
        "POST",
        "/api/v1/files/confirm",
        token=token,
        body=body,
        expected=(200,),
    )

    if not isinstance(confirmed, dict) or not confirmed.get("id"):
        raise SeederError(
            "File confirm succeeded but FileStorageDto.id is missing."
        )

    return confirmed


def attach_signed_contract_document(
    session,
    token,
    contract_id,
    file_id,
):
    """
    Runtime/OpenAPI verified body:
      fileId, documentType, description

    ContractDocumentType.SignedContract is the first C# enum value => 0.
    """
    body = {
        "fileId": file_id,
        "documentType": 0,
        "description": "Synthetic signed lease contract for load testing",
    }

    document_id = call(
        session,
        "POST",
        f"/api/v1/leasing/contracts/{contract_id}/documents",
        token=token,
        body=body,
        expected=(201,),
    )

    document_id = scalar_guid(document_id)

    if not document_id:
        raise SeederError(
            "Attach contract document returned no document ID."
        )

    return document_id


def create_signed_contract_file(
    session,
    token,
    contract_id,
    owner_index,
    lease_index,
):
    """
    Full API-native signed-document workflow:
      upload-request -> PUT binary -> confirm -> attach to lease
    """
    filename = (
        f"signed-lease-{RUN_ID[-8:]}-"
        f"{owner_index:02d}-{lease_index:04d}.pdf"
    )

    mime_type = "application/pdf"
    payload = make_minimal_pdf_bytes(
        f"AqariOS Signed Lease {owner_index:02d}-{lease_index:04d}"
    )

    upload = request_file_upload(
        session,
        token,
        contract_id,
        filename,
        mime_type,
        len(payload),
    )

    upload_file_bytes(
        session,
        upload["uploadUrl"],
        payload,
        mime_type,
    )

    confirmed = confirm_file_upload(
        session,
        token,
        upload["fileId"],
        upload["storageKey"],
        filename,
        mime_type,
        len(payload),
    )

    document_id = attach_signed_contract_document(
        session,
        token,
        contract_id,
        confirmed["id"],
    )

    return {
        "file_id": confirmed["id"],
        "storage_key": confirmed["storageKey"],
        "document_id": document_id,
        "filename": filename,
        "document_type": "SignedContract",
    }


# ============================================================
# Lease Contracts
# ============================================================

def create_and_activate_lease(
    session,
    token,
    owner_index,
    lease_index,
    apartment_id,
    tenant_id,
):
    # Runtime-verified rule:
    # a lease cannot be activated before its start date.
    # Use a UTC-safe past date because the host (Jordan UTC+3) can be on
    # tomorrow while the API/container is still on the previous UTC date.
    start = SAFE_NON_FUTURE_DATE
    end = start + timedelta(days=365)

    contract_number = (
        f"LT-CNT-{RUN_ID[-8:]}-"
        f"{owner_index:02d}-{lease_index:04d}"
    )

    body = {
        "apartmentId": apartment_id,
        "tenantId": tenant_id,
        "contractNumber": contract_number,
        "startDate": f"{start.isoformat()}T00:00:00Z",
        "endDate": f"{end.isoformat()}T00:00:00Z",
        "monthlyRentAmount": 350 + (lease_index % 10) * 25,
        "securityDepositAmount": 350,
        "paymentFrequency": 0,
        "paymentDueDay": 1 + (lease_index % 28),
        "legalRegime": 0,
        "tenantType": 0,
        "notes": "AqariOS automated load-test lease",
    }

    contract_id = call(
        session,
        "POST",
        "/api/v1/leasing/contracts",
        token=token,
        body=body,
        expected=(201,),
    )

    contract_id = scalar_guid(contract_id)

    if not contract_id:
        raise SeederError("Lease creation returned no contract ID.")

    # Runtime-verified activation gate:
    # a SignedContract document MUST exist before activation.
    signed_document = create_signed_contract_file(
        session,
        token,
        contract_id,
        owner_index,
        lease_index,
    )

    call(
        session,
        "POST",
        f"/api/v1/leasing/contracts/{contract_id}/activate",
        token=token,
        expected=(204,),
    )

    return {
        "id": contract_id,
        "contract_number": contract_number,
        "apartment_id": apartment_id,
        "tenant_id": tenant_id,
        "signed_document": signed_document,
    }


# ============================================================
# Maintenance / Expenses / Marketplace
# ============================================================

def create_maintenance(
    session,
    token,
    apartment,
    tenant_id,
    index,
):
    body = {
        "buildingId": apartment["building_id"],
        "apartmentId": apartment["id"],
        "tenantId": tenant_id,
        "title": f"Load Test Maintenance {index:04d}",
        "description": (
            "Synthetic maintenance request for AqariOS performance testing."
        ),
        "category": index % 10,
        "priority": index % 4,
        "requestDate": SAFE_NON_FUTURE_DATE.isoformat(),
    }

    request_id = call(
        session,
        "POST",
        "/api/v1/maintenance-requests",
        token=token,
        body=body,
        expected=(201,),
    )

    return scalar_guid(request_id)


def create_expense(session, token, building_id, index):
    body = {
        "buildingId": building_id,
        "category": index % 12,
        "amount": round(20 + index * 3.75, 2),
        "expenseDate": SAFE_NON_FUTURE_DATE.isoformat(),
        "paymentMethod": index % 4,
        "description": f"Synthetic load-test expense {index:04d}",
        "vendorName": "AqariOS Test Vendor",
        "invoiceNumber": (
            f"LT-INV-{RUN_ID[-8:]}-{index:05d}"
        ),
        "notes": "Generated by AqariOS performance seeder",
    }

    expense_id = call(
        session,
        "POST",
        "/api/v1/expenses",
        token=token,
        body=body,
        expected=(201,),
    )

    return scalar_guid(expense_id)


def create_marketplace_listing(
    session,
    token,
    apartment_id,
    index,
):
    body = {
        "apartmentId": apartment_id,
        "title": f"Load Test Listing {index:04d}",
        "description": (
            "Synthetic marketplace listing for AqariOS performance testing."
        ),
        "monthlyRent": 400 + (index % 10) * 20,
        "securityDeposit": 400,
        "currency": 0,
        "contactPhone": "+962791234567",
        "contactWhatsapp": "+962791234567",
        "expirationDate": (
            UTC_TODAY + timedelta(days=90)
        ).isoformat(),
        "isFeatured": False,
    }

    listing_id = call(
        session,
        "POST",
        "/api/v1/marketplace/listings",
        token=token,
        body=body,
        expected=(201,),
    )

    return scalar_guid(listing_id)


# ============================================================
# Per-owner Seeder
# ============================================================

def seed_owner(session, owner_index):
    print(f"\n=== Owner {owner_index}/{OWNERS} ===")

    owner = register_and_login_owner(session, owner_index)
    token = owner["access_token"]

    print(
        f"[OK] Registered + logged in: {owner['email']} "
        f"(role={owner['role_code']})"
    )

    all_apartments = []

    # Buildings -> Floors -> Apartments
    for building_index in range(1, BUILDINGS_PER_OWNER + 1):
        building = create_building(
            session,
            token,
            owner_index,
            building_index,
        )

        owner["buildings"].append(building)

        for floor_index in range(FLOORS_PER_BUILDING):
            floor = create_floor(
                session,
                token,
                building["id"],
                floor_index,
            )

            building["floors"].append(floor)

            for apartment_index in range(
                1,
                APARTMENTS_PER_FLOOR + 1,
            ):
                apartment = create_apartment(
                    session,
                    token,
                    building["id"],
                    floor["id"],
                    owner_index,
                    building_index,
                    floor_index,
                    apartment_index,
                )

                floor["apartments"].append(apartment)
                building["apartments"].append(apartment)
                all_apartments.append(apartment)

        print(
            f"[OK] {building['name']}: "
            f"{len(building['floors'])} floors / "
            f"{len(building['apartments'])} apartments"
        )

    # Tenants
    for tenant_index in range(1, TENANTS_PER_OWNER + 1):
        tenant = create_tenant(
            session,
            token,
            owner_index,
            tenant_index,
        )

        owner["tenants"].append(tenant)

    print(f"[OK] Created {len(owner['tenants'])} tenants")

    # Only activate a small number of tenant accounts because endpoint is rate-limited.
    account_count = min(
        TENANT_ACCOUNTS_PER_OWNER,
        len(owner["tenants"]),
    )

    for tenant in owner["tenants"][:account_count]:
        account = provision_and_activate_tenant(
            session,
            token,
            tenant,
        )

        owner["tenant_accounts"].append(account)

        print(
            f"[OK] Activated tenant account: {tenant['email']}"
        )

    # Active leases automatically generate rent payment installments.
    lease_count = min(
        LEASES_PER_OWNER,
        len(all_apartments),
        len(owner["tenants"]),
    )

    for lease_index in range(lease_count):
        lease = create_and_activate_lease(
            session,
            token,
            owner_index,
            lease_index + 1,
            all_apartments[lease_index]["id"],
            owner["tenants"][lease_index]["id"],
        )

        owner["leases"].append(lease)

    print(
        f"[OK] Activated {len(owner['leases'])} leases; "
        "rent installments generated"
    )

    # Maintenance
    for index in range(1, MAINTENANCE_PER_OWNER + 1):
        if not all_apartments or not owner["tenants"]:
            break

        apartment = all_apartments[
            (index - 1) % len(all_apartments)
        ]

        tenant = owner["tenants"][
            (index - 1) % len(owner["tenants"])
        ]

        request_id = create_maintenance(
            session,
            token,
            apartment,
            tenant["id"],
            index,
        )

        owner["maintenance_requests"].append(request_id)

    print(
        f"[OK] Created "
        f"{len(owner['maintenance_requests'])} maintenance requests"
    )

    # Expenses
    for index in range(1, EXPENSES_PER_OWNER + 1):
        if not owner["buildings"]:
            break

        building = owner["buildings"][
            (index - 1) % len(owner["buildings"])
        ]

        expense_id = create_expense(
            session,
            token,
            building["id"],
            index,
        )

        owner["expenses"].append(expense_id)

    print(
        f"[OK] Created {len(owner['expenses'])} expenses"
    )

    # Keep marketplace listings on apartments that were not leased.
    free_apartments = all_apartments[lease_count:]

    listing_count = min(
        MARKETPLACE_PER_OWNER,
        len(free_apartments),
    )

    for index in range(listing_count):
        listing_id = create_marketplace_listing(
            session,
            token,
            free_apartments[index]["id"],
            index + 1,
        )

        owner["marketplace_listings"].append(listing_id)

    print(
        f"[OK] Created "
        f"{len(owner['marketplace_listings'])} marketplace listings"
    )

    return owner


# ============================================================
# Main
# ============================================================

def main():
    print("AqariOS API Seeder - Runtime Verified v6")
    print(f"Base URL: {BASE_URL}")
    print(f"Run ID: {RUN_ID}")
    print()
    print("Dataset:")
    print(f"  Owners:                  {OWNERS}")
    print(f"  Buildings / owner:       {BUILDINGS_PER_OWNER}")
    print(f"  Floors / building:       {FLOORS_PER_BUILDING}")
    print(f"  Apartments / floor:      {APARTMENTS_PER_FLOOR}")
    print(f"  Tenants / owner:         {TENANTS_PER_OWNER}")
    print(f"  Leases / owner:          {LEASES_PER_OWNER}")
    print(f"  Tenant accounts / owner: {TENANT_ACCOUNTS_PER_OWNER}")
    print(f"  Maintenance / owner:     {MAINTENANCE_PER_OWNER}")
    print(f"  Expenses / owner:        {EXPENSES_PER_OWNER}")
    print(f"  Marketplace / owner:     {MARKETPLACE_PER_OWNER}")
    print()

    session = requests.Session()

    # Connectivity check.
    try:
        response = session.get(
            f"{BASE_URL}/api/v1/marketplace/listings",
            timeout=REQUEST_TIMEOUT,
        )

        if response.status_code >= 500:
            raise SeederError(
                f"API returned HTTP {response.status_code}: "
                f"{response.text[:500]}"
            )

    except requests.RequestException as exc:
        raise SeederError(
            f"Cannot reach AqariOS API at {BASE_URL}: {exc}"
        ) from exc

    result = {
        "run_id": RUN_ID,
        "base_url": BASE_URL,
        "created_at_utc": datetime.now(
            timezone.utc
        ).isoformat(),
        "config": {
            "owners": OWNERS,
            "buildings_per_owner": BUILDINGS_PER_OWNER,
            "floors_per_building": FLOORS_PER_BUILDING,
            "apartments_per_floor": APARTMENTS_PER_FLOOR,
            "tenants_per_owner": TENANTS_PER_OWNER,
            "leases_per_owner": LEASES_PER_OWNER,
            "tenant_accounts_per_owner": TENANT_ACCOUNTS_PER_OWNER,
            "maintenance_per_owner": MAINTENANCE_PER_OWNER,
            "expenses_per_owner": EXPENSES_PER_OWNER,
            "marketplace_per_owner": MARKETPLACE_PER_OWNER,
        },
        "owners": [],
    }

    try:
        for owner_index in range(1, OWNERS + 1):
            owner = seed_owner(session, owner_index)
            result["owners"].append(owner)

            # Save after every completed company.
            save_progress(result)

    except Exception as exc:
        save_progress(result)
        print()
        print("[FAILED]")
        print(str(exc))
        print()
        print(f"Partial progress saved to: {OUT_FILE}")
        raise

    print()
    print("=== DONE ===")
    print(f"Seeder output: {OUT_FILE}")
    print(
        "IMPORTANT: Keep the output JSON private because "
        "it contains test passwords and access tokens."
    )


if __name__ == "__main__":
    main()
