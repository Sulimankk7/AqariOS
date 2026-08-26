from __future__ import annotations

import os
from dataclasses import dataclass

from dotenv import load_dotenv


load_dotenv()


def _positive_int(name: str, default: int) -> int:
    value = int(os.getenv(name, str(default)))
    if value < 1:
        raise ValueError(f"{name} must be greater than zero")
    return value


def _positive_float(name: str, default: float) -> float:
    value = float(os.getenv(name, str(default)))
    if value <= 0:
        raise ValueError(f"{name} must be greater than zero")
    return value


@dataclass(frozen=True)
class Settings:
    shared_secret: str
    electricity_url: str = (
        "https://www.ideco.com.jo/portal/WebForms/SubscriberReceivableLinks.aspx"
    )
    water_url: str = "https://bills.yw.com.jo:8086/BillsInquiry.aspx"
    connect_timeout_seconds: float = 10.0
    electricity_read_timeout_seconds: float = 30.0
    water_read_timeout_seconds: float = 90.0
    electricity_max_attempts: int = 2
    water_max_attempts: int = 3
    retry_backoff_seconds: float = 1.0
    electricity_max_concurrency: int = 2
    water_max_concurrency: int = 1
    electricity_requests_per_minute: int = 10
    water_requests_per_minute: int = 5
    auth_clock_skew_seconds: int = 300
    max_request_bytes: int = 1024

    @classmethod
    def from_env(cls) -> "Settings":
        secret = os.getenv("AQARIOS_SCRAPER_SHARED_SECRET", "")
        if len(secret.encode("utf-8")) < 32:
            raise RuntimeError(
                "AQARIOS_SCRAPER_SHARED_SECRET must contain at least 32 bytes"
            )

        return cls(
            shared_secret=secret,
            electricity_url=os.getenv(
                "ELECTRICITY_PROVIDER_URL", cls.electricity_url
            ),
            water_url=os.getenv("WATER_PROVIDER_URL", cls.water_url),
            connect_timeout_seconds=_positive_float("CONNECT_TIMEOUT_SECONDS", 10),
            electricity_read_timeout_seconds=_positive_float(
                "ELECTRICITY_READ_TIMEOUT_SECONDS", 30
            ),
            water_read_timeout_seconds=_positive_float(
                "WATER_READ_TIMEOUT_SECONDS", 90
            ),
            electricity_max_attempts=_positive_int(
                "ELECTRICITY_MAX_ATTEMPTS", 2
            ),
            water_max_attempts=_positive_int("WATER_MAX_ATTEMPTS", 3),
            retry_backoff_seconds=_positive_float("RETRY_BACKOFF_SECONDS", 1),
            electricity_max_concurrency=_positive_int(
                "ELECTRICITY_MAX_CONCURRENCY", 2
            ),
            water_max_concurrency=_positive_int("WATER_MAX_CONCURRENCY", 1),
            electricity_requests_per_minute=_positive_int(
                "ELECTRICITY_REQUESTS_PER_MINUTE", 10
            ),
            water_requests_per_minute=_positive_int(
                "WATER_REQUESTS_PER_MINUTE", 5
            ),
            auth_clock_skew_seconds=_positive_int("AUTH_CLOCK_SKEW_SECONDS", 300),
            max_request_bytes=_positive_int("MAX_REQUEST_BYTES", 1024),
        )
