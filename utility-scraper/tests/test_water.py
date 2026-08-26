from decimal import Decimal
from pathlib import Path

import pytest

from app.errors import InvalidAccountError, ProviderParseError
from app.providers.water import WaterScraper


FIXTURES = Path(__file__).parent / "fixtures"


def test_parses_paid_and_unpaid_water_history():
    html = """
    <table id="ContentPlaceHolder1_GridView1"><tr><th>name</th><th>total</th></tr><tr><td>subscriber</td><td>7.500</td></tr></table>
    <table id="ContentPlaceHolder1_GridView2">
      <tr><th>subscription</th><th>details</th><th>date</th><th>bill</th><th>due</th><th>status</th></tr>
      <tr><td>96309</td><td>دورة 2026-07</td><td>15/07/2026</td><td>10.250</td><td>7.500</td><td>غير مسددة</td></tr>
    </table>
    <table id="ContentPlaceHolder1_GridView4">
      <tr><th>date</th><th>details</th><th>subscription</th><th>bill</th><th>status</th></tr>
      <tr><td>15/06/2026</td><td>دورة 2026-06</td><td>96309</td><td>8.125</td><td>مسددة</td></tr>
    </table>
    """

    bills = WaterScraper.parse(html, "96309")

    assert len(bills) == 2
    unpaid, paid = bills
    assert unpaid.amount == Decimal("10.250")
    assert unpaid.amount_due == Decimal("7.500")
    assert unpaid.remaining_amount == Decimal("7.500")
    assert unpaid.status == "UNPAID"
    assert paid.amount == Decimal("8.125")
    assert paid.paid_amount is None
    assert paid.remaining_amount is None
    assert paid.status == "PAID"

    repeated = WaterScraper.parse(html, "96309")
    assert [bill.external_id for bill in repeated] == [
        bill.external_id for bill in bills
    ]
    result = WaterScraper.parse_result(html, "96309")
    assert result.total_outstanding_balance == Decimal("7.500")


def test_valid_empty_tables_return_no_bills():
    html = """
    <table id="ContentPlaceHolder1_GridView1"><tr><th>name</th></tr><tr><td>subscriber</td></tr></table>
    <table id="ContentPlaceHolder1_GridView2"><tr><th>header</th></tr></table>
    """
    assert WaterScraper.parse(html, "96309") == []
    assert WaterScraper.parse_result(html, "96309").total_outstanding_balance is None


def test_rejects_invalid_water_account_before_network_call():
    scraper = WaterScraper("https://provider.invalid", 1, 1, 1, 1)
    with pytest.raises(InvalidAccountError):
        scraper.fetch("96-309")


def test_missing_result_tables_is_a_controlled_parse_failure():
    with pytest.raises(ProviderParseError):
        WaterScraper.parse("<html><body>changed</body></html>", "96309")


def test_full_water_history_fixture_extracts_every_paid_and_unpaid_bill():
    html = (FIXTURES / "water_history.html").read_text(encoding="utf-8")

    bills = WaterScraper.parse(html, "10001")

    assert len(bills) == 8
    assert sum(bill.status == "PAID" for bill in bills) == 5
    assert sum(bill.status == "UNPAID" for bill in bills) == 3
    assert len({bill.external_id for bill in bills}) == 8
    assert [bill.external_id for bill in bills] == [
        bill.external_id for bill in WaterScraper.parse(html, "10001")
    ]
    result = WaterScraper.parse_result(html, "10001")
    assert result.total_outstanding_balance == Decimal("24.000")
