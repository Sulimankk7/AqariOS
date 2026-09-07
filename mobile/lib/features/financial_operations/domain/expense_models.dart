import '../../payments/domain/payment_models.dart';

const expenseCategories = [
  'Building',
  'Shared',
  'Emergency',
  'UtilityCommonArea',
  'Maintenance',
  'Cleaning',
  'Security',
  'Elevator',
  'WaterTank',
  'Generator',
  'Administrative',
  'Other',
];

const expensePaymentMethods = ['Cash', 'BankTransfer', 'Cheque', 'Other'];

class Expense extends FinancialData {
  Expense(super.json);
  String get buildingId => text('buildingId');
  String get description => text('description');
  String get vendorName => text('vendorName');
  String get invoiceNumber => text('invoiceNumber');
  String get expenseDate => text('expenseDate');
  num get amount => number('amount');
  int get category => paymentEnum(json['category'], expenseCategories);
  int get method => paymentEnum(json['paymentMethod'], expensePaymentMethods);
}

class ExpenseReceipt extends FinancialData {
  ExpenseReceipt(super.json);
  String get fileId => text('fileId');
  String get receiptNumber => text('receiptNumber');
  num get amount => number('amount');
  String get issuedAt => text('issuedAt');
}

class ExpenseDetail extends Expense {
  ExpenseDetail(super.json);
  List<ExpenseReceipt> get receipts =>
      objects('receipts').map(ExpenseReceipt.new).toList();
}

class ExpenseFilters {
  const ExpenseFilters({
    this.buildingId = '',
    this.category = -1,
    this.from = '',
    this.to = '',
    this.lastId = '',
    this.lastExpenseDate = '',
  });

  final String buildingId, from, to, lastId, lastExpenseDate;
  final int category;

  ExpenseFilters cursor(String id, String date) => ExpenseFilters(
    buildingId: buildingId,
    category: category,
    from: from,
    to: to,
    lastId: id,
    lastExpenseDate: date,
  );

  Map<String, String> get query => {
    'pageSize': '50',
    if (buildingId.isNotEmpty) 'buildingId': buildingId,
    if (category >= 0) 'category': '$category',
    if (from.isNotEmpty) 'dateFrom': from,
    if (to.isNotEmpty) 'dateTo': to,
    if (lastId.isNotEmpty) 'lastSeenId': lastId,
    if (lastExpenseDate.isNotEmpty) 'lastSeenExpenseDate': lastExpenseDate,
  };
}
