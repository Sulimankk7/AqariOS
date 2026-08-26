from decimal import Decimal
from pathlib import Path

import pytest

from app.errors import InvalidAccountError, ProviderParseError
from app.providers.electricity import ElectricityScraper


FIXTURES = Path(__file__).parent / "fixtures"


def test_parses_paid_and_unpaid_ideco_tables_with_stable_ids():
    html = """
    <table id="ctl00_ContentPlaceHolder1_gvUnpayedInvoices">
      <tr><th>details</th><th>month</th><th>consumption</th><th>invoice</th><th>required</th><th>date</th></tr>
      <tr>
        <td><a onclick='window.open("SubInvoiceDetails.aspx?IssuYM=" + "2026" + "07" + "&CityId="+ "26" + "&CusmId="+ "4283")'>التفاصيل</a></td>
        <td>2026 / 07</td><td>٣١٨</td><td>54.750</td><td>20.000</td><td></td>
      </tr>
    </table>
    <table id="ctl00_ContentPlaceHolder1_gvInvoices">
      <tr><th>details</th><th>month</th><th>consumption</th><th>required</th><th>invoice</th><th>paid</th><th>remaining</th><th>date</th></tr>
      <tr>
        <td><a onclick='window.open("SubInvoiceDetails.aspx?IssuYM=" + "2026" + "06" + "&CityId="+ "26" + "&CusmId="+ "4283")'>التفاصيل</a></td>
        <td>2026 / 06</td><td>٢٥٠</td><td>0</td><td>40.125</td><td>40.125</td><td>0</td><td>15/06/2026</td>
      </tr>
    </table>
    """

    bills = ElectricityScraper.parse(html, "0260004283")

    assert len(bills) == 2
    unpaid, paid = bills
    assert unpaid.bill_date.isoformat() == "2026-07-01"
    assert unpaid.amount == Decimal("54.750")
    assert unpaid.amount_due == Decimal("20.000")
    assert unpaid.consumption == Decimal("318")
    assert unpaid.status == "UNPAID"
    assert paid.amount == Decimal("40.125")
    assert paid.paid_amount == Decimal("40.125")
    assert paid.remaining_amount == Decimal("0")
    assert paid.status == "PAID"
    assert paid.payment_date.isoformat() == "2026-06-15"
    assert unpaid.external_id != paid.external_id

    repeated = ElectricityScraper.parse(html, "0260004283")
    assert [bill.external_id for bill in repeated] == [
        bill.external_id for bill in bills
    ]


def test_rejects_invalid_electricity_account_before_network_call():
    scraper = ElectricityScraper("https://provider.invalid", 1, 1, 1, 1)

    with pytest.raises(InvalidAccountError):
        scraper.fetch("123")


def test_missing_provider_tables_is_a_controlled_parse_failure():
    with pytest.raises(ProviderParseError):
        ElectricityScraper.parse("<html><body>changed</body></html>", "0260004283")


def test_real_initial_fixture_preserves_webforms_hidden_fields_and_controls():
    html = (FIXTURES / "ideco_initial.html").read_text(encoding="utf-8")

    action, payload = ElectricityScraper.build_submission(
        html,
        "0260004283",
        "https://www.ideco.com.jo/portal/WebForms/SubscriberReceivableLinks.aspx",
    )

    assert action == (
        "https://www.ideco.com.jo/portal/WebForms/SubscriberReceivableLinks.aspx"
    )
    assert payload["__VIEWSTATE"] == "SANITIZED_VIEWSTATE"
    assert payload["__VIEWSTATEGENERATOR"] == "SANITIZED_GENERATOR"
    assert payload["__EVENTVALIDATION"] == "SANITIZED_EVENTVALIDATION"
    assert payload["ctl00$ContentPlaceHolder1$txtCustomerNo"] == "0260004283"
    assert payload["ctl00$ContentPlaceHolder1$btnSearch"] == "استعلام"


def test_full_history_fixture_extracts_every_paid_and_unpaid_bill():
    html = (FIXTURES / "ideco_history.html").read_text(encoding="utf-8")

    bills = ElectricityScraper.parse(html, "0260004283")

    assert len(bills) == 8
    assert sum(bill.status == "PAID" for bill in bills) == 5
    assert sum(bill.status == "UNPAID" for bill in bills) == 3
    assert {bill.bill_date.isoformat() for bill in bills} == {
        "2026-01-01",
        "2026-02-01",
        "2026-03-01",
        "2026-04-01",
        "2026-05-01",
        "2026-06-01",
        "2026-07-01",
        "2026-08-01",
    }
    assert len({bill.external_id for bill in bills}) == 8
    assert [bill.external_id for bill in bills] == [
        bill.external_id
        for bill in ElectricityScraper.parse(html, "0260004283")
    ]

    result = ElectricityScraper.parse_result(html, "0260004283")
    assert result.total_outstanding_balance == Decimal("78.750")


def test_valid_empty_ideco_tables_are_a_genuine_successful_empty_result():
    html = (FIXTURES / "ideco_empty.html").read_text(encoding="utf-8")

    assert ElectricityScraper.parse(html, "0260004283") == []
    assert (
        ElectricityScraper.parse_result(html, "0260004283")
        .total_outstanding_balance
        == Decimal("0")
    )


def test_bills_being_calculated_is_a_successful_empty_result():
    html = """
    <table>
      <tr id="ctl00_ContentPlaceHolder1_trNoInvoices">
        <td class="LongText" colspan="4">
          <span id="ctl00_ContentPlaceHolder1_lblNoInvoices">
            الفواتير   قيد
            الاحتساب
          </span>
        </td>
      </tr>
    </table>
    """

    result = ElectricityScraper.parse_result(html, "0260004283")

    assert result.bills == []
    assert result.total_outstanding_balance is None


def test_bills_being_calculated_text_outside_known_state_is_a_parse_failure():
    html = "<html><body><p>الفواتير قيد الاحتساب</p></body></html>"

    with pytest.raises(ProviderParseError):
        ElectricityScraper.parse_result(html, "0260004283")


def test_different_no_invoices_message_remains_a_parse_failure():
    html = """
    <table>
      <tr id="ctl00_ContentPlaceHolder1_trNoInvoices">
        <td>
          <span id="ctl00_ContentPlaceHolder1_lblNoInvoices">
            تعذر تنفيذ الاستعلام
          </span>
        </td>
      </tr>
    </table>
    """

    with pytest.raises(ProviderParseError):
        ElectricityScraper.parse_result(html, "0260004283")


def test_missing_unpaid_remaining_balance_keeps_account_total_nullable():
    html = """
    <table id="ctl00_ContentPlaceHolder1_gvUnpayedInvoices">
      <tr><th>details</th><th>month</th><th>consumption</th><th>invoice</th><th>required</th><th>date</th></tr>
      <tr><td>details</td><td>2026 / 08</td><td>250</td><td>40.125</td><td></td><td></td></tr>
    </table>
    """

    result = ElectricityScraper.parse_result(html, "0260004283")

    assert result.total_outstanding_balance is None


@pytest.mark.parametrize("fixture_name", ["ideco_error.html", "ideco_malformed.html"])
def test_provider_error_or_malformed_result_is_a_controlled_failure(fixture_name):
    html = (FIXTURES / fixture_name).read_text(encoding="utf-8")

    with pytest.raises(ProviderParseError):
        ElectricityScraper.parse(html, "0260004283")


def test_positive_remaining_amount_is_never_silently_marked_paid():
    html = """
    <table id="ctl00_ContentPlaceHolder1_gvInvoices">
      <tr><th>details</th><th>month</th><th>consumption</th><th>required</th><th>invoice</th><th>paid</th><th>remaining</th><th>date</th></tr>
      <tr><td>details</td><td>2026 / 06</td><td>250</td><td>5</td><td>40</td><td>35</td><td>5</td><td></td></tr>
    </table>
    """

    bill = ElectricityScraper.parse(html, "0260004283")[0]

    assert bill.remaining_amount == Decimal("5")
    assert bill.payment_date is None
    assert bill.status == "UNPAID"


def test_paid_ideco_status_marker_means_payment_date_is_unavailable():
    html = """
    <table id="ctl00_ContentPlaceHolder1_gvInvoices">
      <tr><th>details</th><th>month</th><th>consumption</th><th>required</th><th>invoice</th><th>paid</th><th>remaining</th><th>date</th></tr>
      <tr><td>details</td><td>2026 / 08</td><td>250</td><td>0</td><td>40.125</td><td>40.125</td><td>0</td><td>مسددة</td></tr>
    </table>
    """

    bill = ElectricityScraper.parse(html, "0260004283")[0]

    assert bill.payment_date is None
    assert bill.amount == Decimal("40.125")
    assert bill.amount_due == Decimal("0")
    assert bill.paid_amount == Decimal("40.125")
    assert bill.remaining_amount == Decimal("0")
    assert bill.status == "PAID"


def test_unrecognized_paid_payment_date_remains_a_parse_failure():
    html = """
    <table id="ctl00_ContentPlaceHolder1_gvInvoices">
      <tr><th>details</th><th>month</th><th>consumption</th><th>required</th><th>invoice</th><th>paid</th><th>remaining</th><th>date</th></tr>
      <tr><td>details</td><td>2026 / 08</td><td>250</td><td>0</td><td>40.125</td><td>40.125</td><td>0</td><td>not-a-date</td></tr>
    </table>
    """

    with pytest.raises(ProviderParseError):
        ElectricityScraper.parse(html, "0260004283")
