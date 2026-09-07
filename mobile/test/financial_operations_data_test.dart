import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:aqarios_mobile/core/network/api_problem.dart';
import 'package:aqarios_mobile/features/financial_operations/application/expenses_controller.dart';
import 'package:aqarios_mobile/features/financial_operations/domain/expense_models.dart';
import 'package:aqarios_mobile/features/payments/domain/payment_models.dart';
import 'financial_operations_fixtures.dart';
import 'payments_fixtures.dart';

void main() {
  test(
    'expense DTO serialization preserves categories methods and receipts',
    () {
      final expense = ExpenseDetail({
        ...expenseJson(category: 'UtilityCommonArea', method: 'BankTransfer'),
        'receipts': [receiptJson()],
      });
      expect(expense.category, 3);
      expect(expense.method, 1);
      expect(expense.amount, 125.750);
      expect(expense.invoiceNumber, 'INV-2026-0001-ABC');
      expect(expense.receipts.single.fileId, 'file1');
      expect(paymentEnum('new-category', expenseCategories), -1);
    },
  );

  test('expense list request uses exact Web filters without search', () async {
    final h = FinancialOperationsHarness();
    addTearDown(h.client.close);
    await h.expenses.list(
      const ExpenseFilters(
        buildingId: 'b1',
        category: 4,
        from: '2026-01-01',
        to: '2026-12-31',
        lastId: 'e2',
        lastExpenseDate: '2026-08-01',
      ),
    );
    expect(h.requests.single.url.queryParameters, {
      'pageSize': '50',
      'buildingId': 'b1',
      'category': '4',
      'dateFrom': '2026-01-01',
      'dateTo': '2026-12-31',
      'lastSeenId': 'e2',
      'lastSeenExpenseDate': '2026-08-01',
    });
    expect(
      h.requests.single.url.queryParameters.containsKey('searchTerm'),
      false,
    );
  });

  test(
    'expense pagination infers full page and resets when filters change',
    () async {
      final h = FinancialOperationsHarness(
        handler: (r) async =>
            jsonResponse(List.generate(50, (i) => expenseJson(id: 'e$i'))),
      );
      addTearDown(h.client.close);
      final controller = ExpensesController(h.expenses);
      addTearDown(controller.dispose);
      await controller.load();
      expect(controller.hasNext, true);
      await controller.next();
      expect(controller.page, 2);
      expect(h.requests.last.url.queryParameters['lastSeenId'], 'e49');
      expect(
        h.requests.last.url.queryParameters['lastSeenExpenseDate'],
        '2026-09-01',
      );
      await controller.previous();
      expect(controller.page, 1);
      await controller.apply(const ExpenseFilters(category: 10));
      expect(controller.hasPrevious, false);
      expect(h.requests.last.url.queryParameters['category'], '10');
      expect(
        h.requests.last.url.queryParameters.containsKey('lastSeenId'),
        false,
      );
    },
  );

  test(
    'stale expense responses and changed sessions cannot overwrite data',
    () async {
      final delayed = Completer<http.Response>();
      final h = FinancialOperationsHarness(
        handler: (r) async => r.url.queryParameters['category'] == '1'
            ? delayed.future
            : jsonResponse([expenseJson(id: 'new')]),
      );
      addTearDown(h.client.close);
      final controller = ExpensesController(h.expenses);
      addTearDown(controller.dispose);
      final first = controller.apply(const ExpenseFilters(category: 1));
      await controller.apply(const ExpenseFilters(category: 2));
      delayed.complete(jsonResponse([expenseJson(id: 'old')]));
      await first;
      expect(controller.items.single.id, 'new');

      final pending = Completer<http.Response>();
      final h2 = FinancialOperationsHarness(handler: (_) => pending.future);
      addTearDown(h2.client.close);
      final result = h2.expenses.detail('e1');
      h2.expenses.clear();
      pending.complete(jsonResponse(expenseJson()));
      await expectLater(
        result,
        throwsA(
          isA<ApiProblem>().having((p) => p.code, 'scope', 'SESSION_CHANGED'),
        ),
      );
    },
  );

  test(
    'expense detail and signed receipt URL preserve read-only contracts',
    () async {
      final h = FinancialOperationsHarness();
      addTearDown(h.client.close);
      final detail = await h.expenses.detail('e1');
      expect(detail.receipts.single.receiptNumber, 'EXP-0001');
      final uri = await h.expenses.receiptUrl('file1');
      expect(uri.host, 'storage.example.test');
      expect(h.requests.map((r) => r.method), isNot(contains('POST')));
      expect(h.requests.map((r) => r.method), isNot(contains('PUT')));
      expect(h.requests.map((r) => r.method), isNot(contains('DELETE')));
      expect(h.requests.last.url.queryParameters, {'inline': 'false'});
    },
  );
}
