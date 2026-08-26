from __future__ import annotations

from datetime import date
from decimal import Decimal
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class ScrapeRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    account_number: str = Field(min_length=1, max_length=32)


class NormalizedBill(BaseModel):
    model_config = ConfigDict(extra="forbid")

    external_id: str = Field(min_length=1, max_length=128)
    bill_date: date
    due_date: date | None = None
    payment_date: date | None = None
    amount: Decimal = Field(gt=0)
    amount_due: Decimal | None = Field(default=None, ge=0)
    paid_amount: Decimal | None = Field(default=None, ge=0)
    remaining_amount: Decimal | None = Field(default=None, ge=0)
    status: Literal["PAID", "UNPAID", "UNKNOWN"]
    consumption: Decimal | None = Field(default=None, ge=0)
    reference: str | None = Field(default=None, max_length=256)
    currency: Literal["JOD"] = "JOD"


class NormalizedScrapeResult(BaseModel):
    model_config = ConfigDict(extra="forbid")

    bills: list[NormalizedBill] = Field(default_factory=list)
    total_outstanding_balance: Decimal | None = Field(default=None, ge=0)


class ScrapeResponse(BaseModel):
    success: bool
    bills: list[NormalizedBill] = Field(default_factory=list)
    total_outstanding_balance: Decimal | None = Field(default=None, ge=0)
    error_code: str | None = None
    error_message: str | None = None
