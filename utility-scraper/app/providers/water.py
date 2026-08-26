from __future__ import annotations

import re
from datetime import datetime
from html import unescape
from urllib.parse import urljoin, urlparse

import requests
from bs4 import BeautifulSoup

from app.errors import InvalidAccountError, ProviderParseError
from app.models import NormalizedBill, NormalizedScrapeResult
from app.providers.common import clean_text, execute_with_retries, parse_decimal, stable_external_id

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (X11; Linux x86_64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/151.0.0.0 Safari/537.36"
    ),
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    "Accept-Language": "ar,en-US;q=0.8,en;q=0.5",
    "Connection": "keep-alive",
}


class WaterScraper:
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
        if not re.fullmatch(r"\d{1,20}", account_number):
            raise InvalidAccountError(
                "The water subscription number must contain digits only."
            )
        return execute_with_retries(
            lambda: self._fetch_once(account_number),
            self._max_attempts,
            self._retry_backoff_seconds,
        )

    def _fetch_once(self, account_number: str) -> NormalizedScrapeResult:
        with requests.Session() as session:
            session.headers.update(HEADERS)
            initial = session.get(
                self._url, timeout=self._timeout, allow_redirects=True
            )
            initial.raise_for_status()
            if "ContentPlaceHolder1" not in initial.text:
                raise ProviderParseError()

            soup = BeautifulSoup(initial.text, "html.parser")
            form = soup.find("form")
            if form is None:
                raise ProviderParseError()
            data = {
                tag.get("name"): tag.get("value", "")
                for tag in soup.select("input[type=hidden]")
                if tag.get("name")
            }
            search_control = self._find_search_control(soup)
            textbox = soup.find("input", {"name": "ctl00$ContentPlaceHolder1$Textbox1"})
            if textbox is None:
                textbox = soup.find("input", id="ContentPlaceHolder1_Textbox1")
            textbox_name = str(textbox.get("name")) if textbox and textbox.get("name") else "ctl00$ContentPlaceHolder1$Textbox1"

            data[textbox_name] = account_number
            data["__EVENTTARGET"] = ""
            data["__EVENTARGUMENT"] = ""
            data["__ASYNCPOST"] = "true"
            data[search_control] = "بحث"
            data["ctl00$ContentPlaceHolder1$ScriptManager1"] = (
                "ctl00$ContentPlaceHolder1$ScriptManager1|" + search_control
            )

            action = urljoin(self._url, str(form.get("action") or self._url))
            origin = f"{urlparse(self._url).scheme}://{urlparse(self._url).netloc}"
            response = session.post(
                action,
                data=data,
                headers={
                    **HEADERS,
                    "Referer": self._url,
                    "Origin": origin,
                    "X-Requested-With": "XMLHttpRequest",
                    "X-MicrosoftAjax": "Delta=true",
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                },
                timeout=self._timeout,
                allow_redirects=True,
            )
            response.raise_for_status()
            extracted = self._extract_delta_html(response.text)
            try:
                return self.parse_result(extracted, account_number)
            except ProviderParseError:
                if extracted != response.text:
                    return self.parse_result(response.text, account_number)
                raise

    @staticmethod
    def parse(html: str, account_number: str) -> list[NormalizedBill]:
        return WaterScraper.parse_result(html, account_number).bills

    @staticmethod
    def parse_result(html: str, account_number: str) -> NormalizedScrapeResult:
        soup = BeautifulSoup(html, "html.parser")
        unpaid_table = soup.find("table", id="ContentPlaceHolder1_GridView2")
        paid_table = soup.find("table", id="ContentPlaceHolder1_GridView4")
        name_table = soup.find("table", id="ContentPlaceHolder1_GridView1")
        if unpaid_table is None and paid_table is None and name_table is None:
            raise ProviderParseError()

        try:
            bills: list[NormalizedBill] = []
            if unpaid_table is not None:
                for row in unpaid_table.find_all("tr")[1:]:
                    values = [clean_text(cell.get_text(" ", strip=True)) for cell in row.find_all("td")]
                    status_text = clean_text(row.get_text(" ", strip=True))
                    if len(values) < 5 or "غير مسددة" not in status_text:
                        continue
                    bills.append(
                        WaterScraper._bill(
                            account_number,
                            bill_date=values[2],
                            details=values[1],
                            bill_value=values[3],
                            amount_due=values[4],
                            is_paid=False,
                        )
                    )

            if paid_table is not None:
                for row in paid_table.find_all("tr")[1:]:
                    values = [clean_text(cell.get_text(" ", strip=True)) for cell in row.find_all("td")]
                    status_text = clean_text(row.get_text(" ", strip=True))
                    if len(values) < 4 or "غير مسددة" in status_text or "مسددة" not in status_text:
                        continue
                    bills.append(
                        WaterScraper._bill(
                            account_number,
                            bill_date=values[0],
                            details=values[1],
                            bill_value=values[3],
                            amount_due=None,
                            is_paid=True,
                        )
                    )
            total_outstanding_balance = None
            if name_table is not None:
                rows = name_table.find_all("tr")
                if len(rows) > 1:
                    values = [
                        clean_text(cell.get_text(" ", strip=True))
                        for cell in rows[1].find_all("td")
                    ]
                    if len(values) > 1:
                        total_outstanding_balance = parse_decimal(
                            values[-1], required=False
                        )

            return NormalizedScrapeResult(
                bills=bills,
                total_outstanding_balance=total_outstanding_balance,
            )
        except (ValueError, IndexError) as exc:
            raise ProviderParseError() from exc

    @staticmethod
    def _bill(
        account_number: str,
        *,
        bill_date: str,
        details: str,
        bill_value: str,
        amount_due: str | None,
        is_paid: bool,
    ) -> NormalizedBill:
        parsed_date = datetime.strptime(bill_date, "%d/%m/%Y").date()
        amount = parse_decimal(bill_value)
        due = parse_decimal(amount_due, required=False) if amount_due is not None else None
        if amount is None or amount <= 0:
            raise ValueError("invoice amount must be positive")
        return NormalizedBill(
            external_id=stable_external_id(
                "water", account_number, parsed_date.isoformat(), details
            ),
            bill_date=parsed_date,
            amount=amount,
            amount_due=due,
            paid_amount=None if is_paid else parse_decimal("0"),
            remaining_amount=None if is_paid else due,
            status="PAID" if is_paid else "UNPAID",
            reference=details[:256] or None,
        )

    @staticmethod
    def _find_search_control(soup: BeautifulSoup) -> str:
        for tag in soup.find_all(["input", "button"]):
            name = str(tag.get("name", ""))
            if "btn_Serch_nat" in name or "بحث" in str(tag.get("value", "")) or "بحث" in tag.get_text(" ", strip=True):
                return name or "ctl00$ContentPlaceHolder1$btn_Serch_nat"
        return "ctl00$ContentPlaceHolder1$btn_Serch_nat"

    @staticmethod
    def _extract_delta_html(text: str) -> str:
        marker = "updatePanel|"
        position = text.find(marker)
        if position < 0:
            return text
        rest = text[position + len(marker) :]
        match = re.match(r"([^|]+)\|(\d+)\|", rest)
        if match:
            length = int(match.group(2))
            html = rest[match.end() : match.end() + length]
            if html:
                return unescape(html)
        return unescape(rest.split("|hiddenField|", 1)[0])
