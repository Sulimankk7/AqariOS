import hashlib
import hmac
import json
import time
import uuid
from decimal import Decimal
from pathlib import Path

from fastapi.testclient import TestClient

from app.auth import canonical_message
from app.config import Settings
from app.main import create_app
from app.models import NormalizedBill, NormalizedScrapeResult
from app.providers.electricity import ElectricityScraper

SECRET = "python-unit-test-secret-with-at-least-32-bytes"
FIXTURES = Path(__file__).parent / "fixtures"


def signed_headers(path: str, body: bytes, nonce: str | None = None):
    timestamp = str(int(time.time()))
    nonce = nonce or uuid.uuid4().hex
    signature = hmac.new(
        SECRET.encode(),
        canonical_message("POST", path, timestamp, nonce, body),
        hashlib.sha256,
    ).hexdigest()
    return {
        "Content-Type": "application/json",
        "X-AqariOS-Timestamp": timestamp,
        "X-AqariOS-Nonce": nonce,
        "X-AqariOS-Signature": signature,
    }


def test_internal_endpoint_requires_valid_hmac_and_returns_normalized_data(monkeypatch):
    monkeypatch.setattr(
        ElectricityScraper,
        "fetch",
        lambda _self, _account: NormalizedScrapeResult(
            bills=[NormalizedBill(
                external_id="ideco:test",
                bill_date="2026-07-01",
                amount=Decimal("12.750"),
                amount_due=Decimal("5.000"),
                paid_amount=Decimal("7.750"),
                remaining_amount=Decimal("5.000"),
                status="UNPAID",
                consumption=Decimal("100"),
                reference="provider-reference",
            )],
            total_outstanding_balance=Decimal("5.000"),
        ),
    )
    client = TestClient(create_app(Settings(shared_secret=SECRET)))
    path = "/internal/v1/electricity/bills"
    body = json.dumps({"account_number": "0260004283"}, separators=(",", ":")).encode()

    unauthorized = client.post(path, content=body, headers={"Content-Type": "application/json"})
    assert unauthorized.status_code == 401

    response = client.post(path, content=body, headers=signed_headers(path, body))
    assert response.status_code == 200
    payload = response.json()
    assert payload["success"] is True
    assert payload["bills"][0]["external_id"] == "ideco:test"
    assert Decimal(payload["bills"][0]["amount"]) == Decimal("12.750")
    assert Decimal(payload["total_outstanding_balance"]) == Decimal("5.000")


def test_replayed_nonce_and_extra_request_fields_fail_closed(monkeypatch):
    monkeypatch.setattr(
        ElectricityScraper,
        "fetch",
        lambda _self, _account: NormalizedScrapeResult(),
    )
    client = TestClient(create_app(Settings(shared_secret=SECRET)))
    path = "/internal/v1/electricity/bills"
    body = b'{"account_number":"0260004283"}'
    headers = signed_headers(path, body, nonce="replay-test")

    assert client.post(path, content=body, headers=headers).status_code == 200
    assert client.post(path, content=body, headers=headers).status_code == 401

    body_with_identity = b'{"account_number":"0260004283","tenant_id":"forbidden"}'
    response = client.post(
        path,
        content=body_with_identity,
        headers=signed_headers(path, body_with_identity),
    )
    assert response.status_code == 422


def test_internal_contract_carries_complete_ideco_history(monkeypatch):
    fixture_html = (FIXTURES / "ideco_history.html").read_text(encoding="utf-8")
    monkeypatch.setattr(
        ElectricityScraper,
        "fetch",
        lambda _self, account: ElectricityScraper.parse_result(fixture_html, account),
    )
    client = TestClient(create_app(Settings(shared_secret=SECRET)))
    path = "/internal/v1/electricity/bills"
    body = json.dumps({"account_number": "0260004283"}, separators=(",", ":")).encode()

    response = client.post(path, content=body, headers=signed_headers(path, body))

    assert response.status_code == 200
    payload = response.json()
    assert payload["success"] is True
    assert len(payload["bills"]) == 8
    assert sum(bill["status"] == "PAID" for bill in payload["bills"]) == 5
    assert sum(bill["status"] == "UNPAID" for bill in payload["bills"]) == 3
    assert Decimal(payload["total_outstanding_balance"]) == Decimal("78.750")
