from __future__ import annotations

import hashlib
import re
import time
from datetime import date, datetime
from decimal import Decimal, InvalidOperation
from typing import Callable, TypeVar

import requests

from app.errors import ProviderTimeoutError, ProviderUnavailableError, ScraperError

T = TypeVar("T")

ARABIC_NUMBER_TRANSLATION = str.maketrans("٠١٢٣٤٥٦٧٨٩٫٬", "0123456789.,")


def clean_text(value: object) -> str:
    return re.sub(r"\s+", " ", str(value or "")).strip()


def parse_decimal(value: object, *, required: bool = True) -> Decimal | None:
    raw = clean_text(value).translate(ARABIC_NUMBER_TRANSLATION)
    raw = re.sub(r"[^\d,.\-]", "", raw)
    if not raw:
        if required:
            raise ValueError("missing numeric value")
        return None

    if "," in raw and "." in raw:
        raw = raw.replace(",", "") if raw.rfind(".") > raw.rfind(",") else raw.replace(".", "").replace(",", ".")
    elif "," in raw:
        raw = raw.replace(",", ".") if len(raw.rsplit(",", 1)[-1]) <= 3 else raw.replace(",", "")

    try:
        return Decimal(raw)
    except InvalidOperation as exc:
        raise ValueError("invalid numeric value") from exc


def parse_optional_date(value: object) -> date | None:
    raw = clean_text(value).translate(ARABIC_NUMBER_TRANSLATION)
    if not raw or raw in {"غير مسددة", "-", "--"}:
        return None

    for date_format in ("%d/%m/%Y", "%Y-%m-%d"):
        try:
            return datetime.strptime(raw, date_format).date()
        except ValueError:
            continue
    raise ValueError("invalid date value")


def stable_external_id(provider: str, *parts: object) -> str:
    material = "\x1f".join(clean_text(part) for part in parts).encode("utf-8")
    return f"{provider}:{hashlib.sha256(material).hexdigest()}"


def execute_with_retries(
    action: Callable[[], T], max_attempts: int, backoff_seconds: float
) -> T:
    last_error: Exception | None = None
    for attempt in range(1, max_attempts + 1):
        try:
            return action()
        except requests.Timeout as exc:
            last_error = exc
        except requests.RequestException as exc:
            last_error = exc
        except ScraperError:
            raise

        if attempt < max_attempts:
            time.sleep(backoff_seconds * attempt)

    if isinstance(last_error, requests.Timeout):
        raise ProviderTimeoutError() from last_error
    raise ProviderUnavailableError() from last_error
