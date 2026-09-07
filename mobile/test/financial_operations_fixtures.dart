import 'dart:convert';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/features/financial_operations/data/expenses_repository.dart';
import 'package:aqarios_mobile/features/financial_operations/presentation/financial_operations_scope.dart';
import 'package:aqarios_mobile/features/leasing/data/lease_files.dart';
import 'package:aqarios_mobile/features/leasing/data/leasing_repository.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_scope.dart';
import 'package:aqarios_mobile/features/payments/data/payments_repository.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_scope.dart';
import 'package:aqarios_mobile/features/properties/data/properties_repository.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'payments_fixtures.dart';
import 'properties_fixtures.dart';

Map<String, dynamic> expenseJson({
  String id = 'e1',
  Object category = 4,
  Object method = 0,
  String description = 'Elevator monthly maintenance',
}) => {
  'id': id,
  'companyId': 'test-company',
  'buildingId': 'b1',
  'category': category,
  'amount': 125.750,
  'currency': 'JOD',
  'expenseDate': '2026-09-01',
  'paymentMethod': method,
  'vendorName': 'Long vendor name for service provider',
  'invoiceNumber': 'INV-2026-0001-ABC',
  'description': description,
  'notes': 'Long operational note for this expense.',
  'createdAt': '2026-09-01T08:00:00Z',
};

Map<String, dynamic> receiptJson() => {
  'id': 'r1',
  'companyId': 'test-company',
  'expenseId': 'e1',
  'fileId': 'file1',
  'receiptNumber': 'EXP-0001',
  'amount': 125.75,
  'issuedAt': '2026-09-01',
  'description': 'Receipt copy',
  'createdAt': '2026-09-01T08:30:00Z',
};

class FinancialOperationsHarness {
  FinancialOperationsHarness({
    Future<http.Response> Function(http.Request)? handler,
  }) {
    client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: TestSessionStore(),
      client: MockClient((r) async {
        requests.add(r);
        return (handler ?? defaultResponse)(r);
      }),
    );
    expenses = ExpensesRepository(client);
    final leaseFiles = LeaseFiles(client);
    files = FakePaymentFiles(leaseFiles);
    final leasing = LeaseScope(
      repository: LeasingRepository(client),
      properties: PropertiesRepository(client),
      user: propertyUser({
        'payments.read',
        'payments.approve',
        'receipts.read',
        'properties.read',
        'expenses.create',
      }),
      files: leaseFiles,
    );
    scope = FinancialOperationsScope(
      payments: PaymentScope(
        repository: PaymentsRepository(client),
        leasing: leasing,
        files: files,
      ),
      expenses: expenses,
      leasing: leasing,
      files: files,
    );
  }

  final requests = <http.Request>[];
  late final ApiClient client;
  late final ExpensesRepository expenses;
  late final FakePaymentFiles files;
  late final FinancialOperationsScope scope;

  static Future<http.Response> defaultResponse(http.Request r) async {
    if (r.url.path == '/api/v1/expenses') {
      return jsonResponse([expenseJson()]);
    }
    if (r.url.path == '/api/v1/expenses/e1') {
      return jsonResponse({
        ...expenseJson(),
        'receipts': [receiptJson()],
      });
    }
    if (r.url.path == '/api/v1/buildings') {
      return jsonResponse([buildingJson()]);
    }
    if (r.url.path.endsWith('/download-url')) {
      return jsonResponse({
        'fileId': 'file1',
        'downloadUrl': 'https://storage.example.test/receipt?sig=test',
      });
    }
    if (r.url.path == '/api/v1/rent-payments') {
      return jsonResponse([paymentJson()]);
    }
    return http.Response(
      jsonEncode({'detail': 'Unknown test route'}),
      404,
      headers: {'content-type': 'application/json; charset=utf-8'},
    );
  }
}
