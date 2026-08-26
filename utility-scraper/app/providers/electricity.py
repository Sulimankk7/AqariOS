from __future__ import annotations

import re
from datetime import date
from decimal import Decimal
from urllib.parse import urljoin

import requests
from bs4 import BeautifulSoup, Tag

from app.errors import InvalidAccountError, ProviderParseError
from app.models import NormalizedBill, NormalizedScrapeResult
from app.providers.common import (
    clean_text,
    execute_with_retries,
    parse_decimal,
    parse_optional_date,
    stable_external_id,
)

UNPAID_TABLE_ID = "ctl00_ContentPlaceHolder1_gvUnpayedInvoices"
PAID_TABLE_ID = "ctl00_ContentPlaceHolder1_gvInvoices"
NO_INVOICES_ROW_ID = "ctl00_ContentPlaceHolder1_trNoInvoices"
NO_INVOICES_LABEL_ID = "ctl00_ContentPlaceHolder1_lblNoInvoices"
BILLS_BEING_CALCULATED_TEXT = "الفواتير قيد الاحتساب"

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (X11; Linux x86_64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/151.0.0.0 Safari/537.36"
    ),
    "Accept-Language": "ar,en-US;q=0.9,en;q=0.8",
}


class ElectricityScraper:
    def __init__(
        self,
        url: str,
        connect_timeout: float,
        read_timeout: float,
        max_attempts: int,
        retry_backoff_seconds: float,
    ) -> None:
        self._url = url
        self._timeout = (connect_timeout, read_timeout)
        self._max_attempts = max_attempts
        self._retry_backoff_seconds = retry_backoff_seconds

    def fetch(self, account_number: str) -> NormalizedScrapeResult:
        if not re.fullmatch(r"\d{10}", account_number):
            raise InvalidAccountError(
                "The electricity account number must contain exactly 10 digits."
            )

        return execute_with_retries(
            lambda: self._fetch_once(account_number),
            self._max_attempts,
            self._retry_backoff_seconds,
        )

    def _fetch_once(self, account_number: str) -> NormalizedScrapeResult:
        with requests.Session() as session:
            initial = session.get(self._url, headers=HEADERS, timeout=self._timeout)
            initial.raise_for_status()
            action, payload = self.build_submission(
                initial.text, account_number, self._url
            )
            response = session.post(
                action,
                data=payload,
                headers={**HEADERS, "Referer": initial.url},
                timeout=self._timeout,
            )
            response.raise_for_status()
            return self.parse_result(response.text, account_number)

    @staticmethod
    def build_submission(
        initial_html: str, account_number: str, initial_url: str
    ) -> tuple[str, dict[str, str]]:
        """Build the real IDECO WebForms post while preserving hidden state."""
        soup = BeautifulSoup(initial_html, "html.parser")
        customer = ElectricityScraper._find_customer_input(soup)
        if customer is None or not customer.get("name"):
            raise ProviderParseError()
        form = customer.find_parent("form") or soup.find("form")
        if form is None:
            raise ProviderParseError()

        payload = {
            str(tag.get("name")): str(tag.get("value", ""))
            for tag in soup.select("input[type='hidden']")
            if tag.get("name")
        }
        payload[str(customer["name"])] = account_number
        submit = ElectricityScraper._find_submit(form)
        if submit is not None and submit.get("name"):
            payload[str(submit["name"])] = str(submit.get("value", ""))

        action = urljoin(initial_url, str(form.get("action") or initial_url))
        return action, payload

    @staticmethod
    def parse(html: str, account_number: str) -> list[NormalizedBill]:
        return ElectricityScraper.parse_result(html, account_number).bills

    @staticmethod
    def parse_result(html: str, account_number: str) -> NormalizedScrapeResult:
        soup = BeautifulSoup(html, "html.parser")
        unpaid_table = soup.find("table", id=UNPAID_TABLE_ID)
        paid_table = soup.find("table", id=PAID_TABLE_ID)
        if unpaid_table is None and paid_table is None:
            if ElectricityScraper._bills_are_being_calculated(soup):
                return NormalizedScrapeResult(
                    bills=[], total_outstanding_balance=None
                )
            raise ProviderParseError()

        try:
            bills: list[NormalizedBill] = []
            if isinstance(unpaid_table, Tag):
                bills.extend(
                    ElectricityScraper._parse_table(
                        unpaid_table, account_number, is_paid=False
                    )
                )
            if isinstance(paid_table, Tag):
                bills.extend(
                    ElectricityScraper._parse_table(
                        paid_table, account_number, is_paid=True
                    )
                )
            unpaid_remaining = [
                bill.remaining_amount for bill in bills if bill.status == "UNPAID"
            ]
            total_outstanding_balance = (
                sum(unpaid_remaining, start=Decimal("0"))
                if all(value is not None for value in unpaid_remaining)
                else None
            )
            return NormalizedScrapeResult(
                bills=bills,
                total_outstanding_balance=total_outstanding_balance,
            )
        except (ValueError, IndexError) as exc:
            raise ProviderParseError() from exc

    @staticmethod
    def _bills_are_being_calculated(soup: BeautifulSoup) -> bool:
        elements = (
            soup.find(id=NO_INVOICES_LABEL_ID),
            soup.find(id=NO_INVOICES_ROW_ID),
        )
        return any(
            isinstance(element, Tag)
            and clean_text(element.get_text(" ", strip=True))
            == BILLS_BEING_CALCULATED_TEXT
            for element in elements
        )

    @staticmethod
    def _parse_table(
        table: Tag, account_number: str, *, is_paid: bool
    ) -> list[NormalizedBill]:
        bills: list[NormalizedBill] = []
        minimum_columns = 8 if is_paid else 6
        for row in table.find_all("tr"):
            cells = row.find_all(["th", "td"])
            if not row.find_all("td"):
                continue
            values = [clean_text(cell.get_text(" ", strip=True)) for cell in cells]
            if len(values) < minimum_columns:
                raise ValueError("unexpected IDECO invoice column count")

            month_match = re.fullmatch(r"\s*(\d{4})\s*/\s*(\d{2})\s*", values[1])
            if month_match is None:
                raise ValueError("invalid IDECO billing period")
            bill_date = date(int(month_match.group(1)), int(month_match.group(2)), 1)

            detail_key = ElectricityScraper._detail_key(row) or values[1]
            consumption = parse_decimal(values[2], required=False)
            if is_paid:
                amount_due = parse_decimal(values[3], required=False)
                amount = parse_decimal(values[4])
                paid_amount = parse_decimal(values[5], required=False)
                remaining_amount = parse_decimal(values[6], required=False)
                payment_date_value = clean_text(values[7])
                payment_date = (
                    None
                    if payment_date_value == "مسددة"
                    else parse_optional_date(payment_date_value)
                )
                status = (
                    "UNPAID"
                    if remaining_amount is not None and remaining_amount > 0
                    else "PAID"
                )
            else:
                amount = parse_decimal(values[3])
                amount_due = parse_decimal(values[4], required=False)
                paid_amount = parse_decimal("0")
                remaining_amount = amount_due
                payment_date = parse_optional_date(values[5])
                status = "UNPAID"

            if amount is None or amount <= 0:
                raise ValueError("invoice amount must be positive")

            bills.append(
                NormalizedBill(
                    external_id=stable_external_id(
                        "ideco", account_number, detail_key, values[1]
                    ),
                    bill_date=bill_date,
                    payment_date=payment_date,
                    amount=amount,
                    amount_due=amount_due,
                    paid_amount=paid_amount,
                    remaining_amount=remaining_amount,
                    status=status,
                    consumption=consumption,
                    reference=detail_key[:256],
                )
            )
        return bills

    @staticmethod
    def _detail_key(row: Tag) -> str:
        for link in row.find_all("a"):
            onclick = str(link.get("onclick", ""))
            if "SubInvoiceDetails.aspx" not in onclick:
                continue
            match = re.search(
                r"IssuYM.*?(\d{4}).*?(\d{2}).*?CityId.*?(\d+).*?CusmId.*?(\d+)",
                onclick,
                flags=re.IGNORECASE,
            )
            if match:
                year, month, city, customer = match.groups()
                return f"IssuYM={year}{month}&CityId={city}&CusmId={customer}"
        return ""

    @staticmethod
    def _find_customer_input(soup: BeautifulSoup) -> Tag | None:
        for tag in soup.find_all("input"):
            if "txtCustomerNo" in str(tag.get("name", "")) or "txtCustomerNo" in str(tag.get("id", "")):
                return tag
        return None

    @staticmethod
    def _find_submit(form: Tag) -> Tag | None:
        for tag in form.find_all(["input", "button"]):
            blob = " ".join(
                str(value)
                for value in (
                    tag.get("name", ""),
                    tag.get("id", ""),
                    tag.get("value", ""),
                    tag.get_text(" ", strip=True),
                )
            ).lower()
            if any(value in blob for value in ("search", "query", "submit", "استعلام", "بحث", "عرض")):
                return tag
        return form.find("input", {"type": "submit"})
