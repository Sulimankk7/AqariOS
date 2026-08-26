from __future__ import annotations

import hashlib
import hmac
import time
from collections import OrderedDict

from fastapi import HTTPException, Request, status


TIMESTAMP_HEADER = "X-AqariOS-Timestamp"
NONCE_HEADER = "X-AqariOS-Nonce"
SIGNATURE_HEADER = "X-AqariOS-Signature"


def canonical_message(
    method: str, path: str, timestamp: str, nonce: str, body: bytes
) -> bytes:
    body_hash = hashlib.sha256(body).hexdigest()
    return f"{timestamp}\n{nonce}\n{method.upper()}\n{path}\n{body_hash}".encode()


class HmacAuthenticator:
    def __init__(self, shared_secret: str, clock_skew_seconds: int) -> None:
        self._secret = shared_secret.encode("utf-8")
        self._clock_skew_seconds = clock_skew_seconds
        self._seen_nonces: OrderedDict[str, int] = OrderedDict()

    async def authenticate(self, request: Request) -> None:
        timestamp = request.headers.get(TIMESTAMP_HEADER, "")
        nonce = request.headers.get(NONCE_HEADER, "")
        supplied = request.headers.get(SIGNATURE_HEADER, "")

        try:
            timestamp_value = int(timestamp)
        except ValueError as exc:
            raise self._unauthorized() from exc

        now = int(time.time())
        if abs(now - timestamp_value) > self._clock_skew_seconds:
            raise self._unauthorized()

        if not nonce or len(nonce) > 64 or not supplied:
            raise self._unauthorized()

        self._purge_expired_nonces(now)
        if nonce in self._seen_nonces:
            raise self._unauthorized()

        body = await request.body()
        expected = hmac.new(
            self._secret,
            canonical_message(request.method, request.url.path, timestamp, nonce, body),
            hashlib.sha256,
        ).hexdigest()

        if not hmac.compare_digest(expected, supplied.lower()):
            raise self._unauthorized()

        self._seen_nonces[nonce] = now
        while len(self._seen_nonces) > 10_000:
            self._seen_nonces.popitem(last=False)

    def _purge_expired_nonces(self, now: int) -> None:
        oldest_allowed = now - self._clock_skew_seconds
        while self._seen_nonces:
            _, seen_at = next(iter(self._seen_nonces.items()))
            if seen_at >= oldest_allowed:
                break
            self._seen_nonces.popitem(last=False)

    @staticmethod
    def _unauthorized() -> HTTPException:
        return HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Service authentication failed.",
        )
