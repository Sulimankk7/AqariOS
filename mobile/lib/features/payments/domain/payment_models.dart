import 'dart:math' as math;

int paymentEnum(Object? value, List<String> names) {
  final number = int.tryParse('$value');
  if (number != null) return number >= 0 && number < names.length ? number : -1;
  final normalized = '$value'
      .replaceAll(RegExp('[^a-zA-Z0-9]'), '')
      .toLowerCase();
  return names.indexWhere((name) => name.toLowerCase() == normalized);
}

const dueStatuses = [
  'Pending',
  'PendingVerification',
  'Paid',
  'PartiallyPaid',
  'Late',
  'OverdueUnpaid',
  'Cancelled',
];
const submissionStatuses = ['Pending', 'Approved', 'Rejected'];
const paymentMethods = [
  'Cash',
  'BankTransfer',
  'Cheque',
  'Efawateercom',
  'CliQ',
];
const purposes = [
  'ScheduledInstallment',
  'UnallocatedReceipt',
  'AdjustmentCredit',
  'AdjustmentDebit',
];
const chequeStatuses = [
  'Issued',
  'Received',
  'Deposited',
  'Cleared',
  'Bounced',
  'Cancelled',
];

String normalizeAmount(String value) {
  var result = value.trim().replaceAll('٫', '.');
  for (var i = 0; i < 10; i++) {
    result = result
        .replaceAll('٠١٢٣٤٥٦٧٨٩'[i], '$i')
        .replaceAll('۰۱۲۳۴۵۶۷۸۹'[i], '$i');
  }
  return result;
}

/// Exact thousandths for validation/comparison; no floating point balance rules.
int? milliAmount(String value) {
  final text = normalizeAmount(value);
  if (!RegExp(r'^-?\d+(\.\d{1,3})?$').hasMatch(text)) return null;
  final parts = text.replaceFirst('-', '').split('.');
  final whole = int.tryParse(parts[0]);
  if (whole == null) return null;
  final amount =
      whole * 1000 +
      int.parse(parts.length == 1 ? '0' : parts[1].padRight(3, '0'));
  return text.startsWith('-') ? -amount : amount;
}

class FinancialData {
  FinancialData(Map<String, dynamic> json) : json = Map.unmodifiable(json);
  final Map<String, dynamic> json;
  String text(String key) => json[key]?.toString() ?? '';
  num number(String key) =>
      json[key] is num ? json[key] as num : num.tryParse(text(key)) ?? 0;
  bool flag(String key) => json[key] == true;
  List<Map<String, dynamic>> objects(String key) => (json[key] as List? ?? [])
      .map((e) => Map<String, dynamic>.from(e as Map))
      .toList();
  String get id => text('id');
  String get currency => text('currency').isEmpty ? 'JOD' : text('currency');
}

class RentPayment extends FinancialData {
  RentPayment(super.json);

  /// Web preserves list-side enrichment when detail projection omits it.
  RentPayment withListFallback(RentPayment? initial) => initial == null
      ? this
      : RentPayment({
          ...initial.json,
          ...json,
          for (final key in [
            'tenantName',
            'buildingName',
            'contractNumber',
            'apartmentNumber',
          ])
            if (text(key).isEmpty) key: initial.text(key),
          if (json['settlementSummary'] == null)
            'settlementSummary': initial.json['settlementSummary'],
          if (json['receiptFileId'] == null)
            'receiptFileId': initial.json['receiptFileId'],
          if (objects('transactionReceipts').isEmpty)
            'transactionReceipts': initial.json['transactionReceipts'],
        });
  String get leaseId => text('leaseContractId');
  String get tenantId => text('tenantId');
  String get buildingId => text('buildingId');
  String get apartmentId => text('apartmentId');
  String get tenantName => text('tenantName');
  String get contractNumber => text('contractNumber');
  String get buildingName => text('buildingName');
  String get apartmentNumber => text('apartmentNumber');
  String get dueDate => text('dueDate');
  num get due => number('amountDue');
  num get paid => number('amountPaid');
  num get remaining => math.max(0, due - paid);
  int get remainingMilli =>
      math.max(0, (milliAmount('$due') ?? 0) - (milliAmount('$paid') ?? 0));
  int get status => paymentEnum(json['dueDateStatus'], dueStatuses);
  int get purpose => paymentEnum(json['paymentPurpose'], purposes);
  int get method => paymentEnum(json['paymentMethod'], paymentMethods);
  int get latestStatus =>
      paymentEnum(json['latestSubmissionStatus'], submissionStatuses);
  bool get canSubmit => [0, 3, 4, 5].contains(status);
  SettlementSummary? get settlement => json['settlementSummary'] is Map
      ? SettlementSummary(Map<String, dynamic>.from(json['settlementSummary']))
      : null;
  bool get canRemind =>
      purpose == 0 &&
      (settlement?.remaining ?? 0) > 0 &&
      status != 2 &&
      status != 6;
  List<TransactionReceipt> get transactions =>
      objects('transactionReceipts').map(TransactionReceipt.new).toList();
  List<PaymentSubmission> get submissions =>
      objects('submissions').map(PaymentSubmission.new).toList();
  List<PaymentAllocation> get incoming =>
      objects('incomingAllocations').map(PaymentAllocation.new).toList();
  List<PaymentAllocation> get outgoing =>
      objects('outgoingAllocations').map(PaymentAllocation.new).toList();
  Cheque? get cheque => json['chequeDetails'] is Map
      ? Cheque(Map<String, dynamic>.from(json['chequeDetails']))
      : null;
}

class PaymentSubmission extends FinancialData {
  PaymentSubmission(super.json);
  int get status => paymentEnum(json['status'], submissionStatuses);
  int get method => paymentEnum(json['paymentMethod'], paymentMethods);
  num? get amount => json['amount'] == null ? null : number('amount');
  String get proofId => text('proofFileId');
}

class VerificationItem extends FinancialData {
  VerificationItem(super.json);
  String get paymentId => text('rentPaymentId');
  String get submissionId => text('paymentSubmissionId');
  num get submitted => json['submittedAmount'] == null
      ? number('amountDue')
      : number('submittedAmount');
}

class VerificationPage {
  VerificationPage(Map<String, dynamic> json)
    : items = (json['items'] as List)
          .map((e) => VerificationItem(Map<String, dynamic>.from(e as Map)))
          .toList(),
      nextCursor = json['nextCursor'] as String?,
      hasMore = json['hasMore'] == true;
  final List<VerificationItem> items;
  final String? nextCursor;
  final bool hasMore;
}

class PaymentAllocation extends FinancialData {
  PaymentAllocation(super.json);
  int get status =>
      paymentEnum(json['allocationStatus'], ['Active', 'Reversed']);
}

class PaymentReceipt extends FinancialData {
  PaymentReceipt(super.json);
}

class TransactionReceipt extends FinancialData {
  TransactionReceipt(super.json);
}

class SettlementSummary extends FinancialData {
  SettlementSummary(super.json);
  bool get available => flag('isAvailable');
  num get remaining => number('remaining');
}

class Cheque extends FinancialData {
  Cheque(super.json);
  int get status => paymentEnum(json['status'], chequeStatuses);
}

class PaymentFilters {
  const PaymentFilters({
    this.buildingId = '',
    this.status = -1,
    this.from = '',
    this.to = '',
    this.search = '',
    this.lastId = '',
    this.lastDate = '',
  });
  final String buildingId, from, to, search, lastId, lastDate;
  final int status;
  PaymentFilters cursor(String id, String date) => PaymentFilters(
    buildingId: buildingId,
    status: status,
    from: from,
    to: to,
    search: search,
    lastId: id,
    lastDate: date,
  );
  Map<String, String> get query => {
    'pageSize': '50',
    if (buildingId.isNotEmpty) 'buildingId': buildingId,
    if (status >= 0) 'status': '$status',
    if (from.isNotEmpty) 'dateFrom': from,
    if (to.isNotEmpty) 'dateTo': to,
    if (search.trim().isNotEmpty) 'searchTerm': search.trim(),
    if (lastId.isNotEmpty) 'lastSeenId': lastId,
    if (lastDate.isNotEmpty) 'lastSeenDueDate': lastDate,
  };
}

List<RentPayment> sortTenantPayments(List<RentPayment> source) {
  const priority = [5, 4, 0, 1, 3, 2, 6];
  final indexed = source.indexed.toList();
  int rank(int status) =>
      priority.contains(status) ? priority.indexOf(status) : 7;
  indexed.sort((a, b) {
    final status = rank(a.$2.status).compareTo(rank(b.$2.status));
    if (status != 0) return status;
    final date = b.$2.dueDate.compareTo(a.$2.dueDate);
    return date == 0 ? a.$1.compareTo(b.$1) : date;
  });
  return indexed.map((e) => e.$2).toList();
}

class PaymentDraft {
  String amount = '',
      reference = '',
      proofId = '',
      chequeNumber = '',
      bank = '',
      issueDate = '',
      dueDate = '';
  int method = 0;
  Map<String, String> validate(RentPayment payment) {
    final errors = <String, String>{};
    final amountValue = milliAmount(amount);
    if (amountValue == null || amountValue <= 0) {
      errors['amount'] = 'invalidAmount';
    } else if (amountValue > payment.remainingMilli) {
      errors['amount'] = 'exceeds';
    }
    if (![0, 4, 2].contains(method)) errors['method'] = 'invalidMethod';
    if (method == 4) {
      if (reference.trim().isEmpty) errors['reference'] = 'required';
      if (proofId.isEmpty) errors['proof'] = 'proofRequired';
    }
    if (method == 2) {
      if (chequeNumber.trim().isEmpty || chequeNumber.trim().length > 100) {
        errors['chequeNumber'] = 'chequeNumberInvalid';
      }
      if (bank.trim().isEmpty || bank.trim().length > 255) {
        errors['bank'] = 'bankInvalid';
      }
      if (DateTime.tryParse(issueDate) == null) {
        errors['issueDate'] = 'required';
      }
      if (DateTime.tryParse(dueDate) == null) errors['dueDate'] = 'required';
      if (issueDate.isNotEmpty &&
          dueDate.isNotEmpty &&
          dueDate.compareTo(issueDate) < 0) {
        errors['dueDate'] = 'dateOrder';
      }
    }
    return errors;
  }

  Map<String, dynamic> get payload => {
    'amount': num.parse(normalizeAmount(amount)),
    'paymentMethod': method,
    'referenceNumber': method == 2
        ? chequeNumber.trim()
        : reference.trim().isEmpty
        ? null
        : reference.trim(),
    'proofFileId': method == 0 || proofId.isEmpty ? null : proofId,
    if (method == 2)
      'chequeDetails': {
        'chequeNumber': chequeNumber.trim(),
        'bankName': bank.trim(),
        'issueDate': issueDate,
        'dueDate': dueDate,
      },
  };
}
