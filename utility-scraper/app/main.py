from __future__ import annotations

import asyncio
import logging

from fastapi import Depends, FastAPI, Request
from fastapi.responses import JSONResponse

from app.auth import HmacAuthenticator
from app.config import Settings
from app.errors import ScraperError
from app.limits import ProviderGate
from app.models import ScrapeRequest, ScrapeResponse
from app.providers.electricity import ElectricityScraper
from app.providers.water import WaterScraper

logger = logging.getLogger("utility_scraper")


def create_app(settings: Settings | None = None) -> FastAPI:
    config = settings or Settings.from_env()
    authenticator = HmacAuthenticator(
        config.shared_secret, config.auth_clock_skew_seconds
    )
    electricity_gate = ProviderGate(
        config.electricity_max_concurrency,
        config.electricity_requests_per_minute,
    )
    water_gate = ProviderGate(
        config.water_max_concurrency,
        config.water_requests_per_minute,
    )
    electricity = ElectricityScraper(
        config.electricity_url,
        config.connect_timeout_seconds,
        config.electricity_read_timeout_seconds,
        config.electricity_max_attempts,
        config.retry_backoff_seconds,
    )
    water = WaterScraper(
        config.water_url,
        config.connect_timeout_seconds,
        config.water_read_timeout_seconds,
        config.water_max_attempts,
        config.retry_backoff_seconds,
    )

    application = FastAPI(
        title="AqariOS Utility Scraper",
        version="1.0.0",
        docs_url=None,
        redoc_url=None,
        openapi_url=None,
    )

    @application.middleware("http")
    async def limit_request_size(request: Request, call_next):
        content_length = request.headers.get("content-length")
        if content_length:
            try:
                if int(content_length) > config.max_request_bytes:
                    return JSONResponse(
                        status_code=413,
                        content={"detail": "Request body is too large."},
                    )
            except ValueError:
                return JSONResponse(status_code=400, content={"detail": "Invalid request."})
        return await call_next(request)

    async def authenticate(request: Request) -> None:
        await authenticator.authenticate(request)

    async def execute(scraper, gate: ProviderGate, account_number: str):
        async with gate.enter():
            try:
                result = await asyncio.to_thread(scraper.fetch, account_number)
                return ScrapeResponse(
                    success=True,
                    bills=result.bills,
                    total_outstanding_balance=result.total_outstanding_balance,
                )
            except ScraperError as exc:
                logger.warning("Provider inquiry failed with code %s.", exc.code)
                return JSONResponse(
                    status_code=exc.http_status,
                    content=ScrapeResponse(
                        success=False,
                        error_code=exc.code,
                        error_message=exc.safe_message,
                    ).model_dump(mode="json"),
                )
            except Exception:
                logger.exception("Unexpected provider integration failure.")
                return JSONResponse(
                    status_code=500,
                    content=ScrapeResponse(
                        success=False,
                        error_code="UNEXPECTED_ERROR",
                        error_message="The provider integration failed unexpectedly.",
                    ).model_dump(mode="json"),
                )

    @application.get("/health")
    async def health() -> dict[str, str]:
        return {"status": "healthy"}

    @application.post(
        "/internal/v1/electricity/bills", response_model=ScrapeResponse
    )
    async def electricity_bills(
        request: ScrapeRequest, _: None = Depends(authenticate)
    ):
        return await execute(electricity, electricity_gate, request.account_number)

    @application.post("/internal/v1/water/bills", response_model=ScrapeResponse)
    async def water_bills(
        request: ScrapeRequest, _: None = Depends(authenticate)
    ):
        return await execute(water, water_gate, request.account_number)

    return application


app = create_app()
