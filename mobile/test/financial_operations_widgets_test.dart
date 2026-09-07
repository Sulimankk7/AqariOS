import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/financial_operations/presentation/expense_detail_screen.dart';
import 'package:aqarios_mobile/features/financial_operations/presentation/expenses_screen.dart';
import 'package:aqarios_mobile/features/financial_operations/presentation/financial_operations_screen.dart';
import 'financial_operations_fixtures.dart';
import 'leasing_widgets_test.dart' show leasingApp;
import 'payments_fixtures.dart' show jsonResponse;

void main() {
  Future<void> pump(
    WidgetTester tester,
    Widget child, {
    bool ar = false,
    bool dark = false,
    double scale = 1,
  }) async {
    tester.view.physicalSize = const Size(411, 914);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      leasingApp(child, arabic: ar, dark: dark, scale: scale),
    );
    await tester.pumpAndSettle();
  }

  testWidgets(
    'financial operations shell reuses payment records and opens expenses',
    (tester) async {
      final h = FinancialOperationsHarness();
      addTearDown(h.client.close);
      await pump(tester, FinancialOperationsScreen(scope: h.scope));
      expect(find.text('Financial records'), findsOneWidget);
      expect(find.text('Scheduled obligations'), findsOneWidget);
      await tester.tap(find.text('Expenses'));
      await tester.pumpAndSettle();
      expect(find.byType(ExpensesScreen), findsOneWidget);
      expect(find.text('Elevator monthly maintenance'), findsOneWidget);
    },
  );

  testWidgets(
    'expense filters send building category dates and reset clears them',
    (tester) async {
      final h = FinancialOperationsHarness();
      addTearDown(h.client.close);
      await pump(tester, ExpensesScreen(scope: h.scope));
      await tester.tap(find.text('Filters'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('All buildings'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cedar House'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('All categories'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Maintenance'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Apply'));
      await tester.pumpAndSettle();
      expect(h.requests.last.url.queryParameters['buildingId'], 'b1');
      expect(h.requests.last.url.queryParameters['category'], '4');
      expect(
        h.requests.last.url.queryParameters.containsKey('searchTerm'),
        false,
      );
      await tester.tap(find.text('Reset'));
      await tester.pumpAndSettle();
      expect(h.requests.last.url.queryParameters, {'pageSize': '50'});
    },
  );

  testWidgets('expense loading error retry and empty states are distinct', (
    tester,
  ) async {
    final pending = Completer<http.Response>();
    var attempt = 0;
    final h = FinancialOperationsHarness(
      handler: (_) {
        attempt++;
        return attempt == 1 ? pending.future : Future.value(jsonResponse([]));
      },
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(leasingApp(ExpensesScreen(scope: h.scope)));
    await tester.pump();
    expect(find.byType(AqariSkeleton), findsWidgets);
    pending.complete(jsonResponse({'detail': 'Server unavailable'}, 503));
    await tester.pumpAndSettle();
    expect(find.text('Could not load expenses'), findsOneWidget);
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.text('No expenses'), findsOneWidget);
    expect(attempt, 2);
  });

  testWidgets('expense full page enables next and details open receipt URL', (
    tester,
  ) async {
    final h = FinancialOperationsHarness(
      handler: (r) async {
        if (r.url.path == '/api/v1/expenses') {
          if (r.url.queryParameters.containsKey('lastSeenId')) {
            return jsonResponse([expenseJson(id: 'next')]);
          }
          return jsonResponse(List.generate(50, (i) => expenseJson(id: 'e$i')));
        }
        if (r.url.path.startsWith('/api/v1/expenses/')) {
          return jsonResponse({
            ...expenseJson(id: r.url.pathSegments.last),
            'receipts': [receiptJson()],
          });
        }
        return FinancialOperationsHarness.defaultResponse(r);
      },
    );
    addTearDown(h.client.close);
    await pump(tester, ExpensesScreen(scope: h.scope));
    await tester.scrollUntilVisible(
      find.text('Next'),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(h.requests.last.url.queryParameters['lastSeenId'], 'e49');
    await tester.tap(find.text('Elevator monthly maintenance').first);
    await tester.pumpAndSettle();
    expect(find.byType(ExpenseDetailScreen), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('EXP-0001'),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('EXP-0001'));
    await tester.pumpAndSettle();
    expect(h.files.opened, 1);
    expect(h.requests.last.url.queryParameters['inline'], 'false');
  });

  testWidgets('Arabic dark long expense content stays renderable', (
    tester,
  ) async {
    final h = FinancialOperationsHarness(
      handler: (r) async {
        if (r.url.path == '/api/v1/expenses') {
          return jsonResponse([
            expenseJson(
              description: 'مصروف تشغيل طويل جداً للمبنى السكني رقم ABC-123'
                  .padRight(180, 'ن'),
              method: 'Cheque',
            ),
          ]);
        }
        return jsonResponse({
          ...expenseJson(
            description: 'مصروف تشغيل طويل جداً'.padRight(240, 'ص'),
            method: 'Cheque',
          ),
          'notes': 'ملاحظات طويلة '.padRight(600, 'م'),
          'receipts': [receiptJson()],
        });
      },
    );
    addTearDown(h.client.close);
    await pump(
      tester,
      ExpensesScreen(scope: h.scope),
      ar: true,
      dark: true,
      scale: 1.4,
    );
    expect(
      Directionality.of(tester.element(find.byType(ExpensesScreen))),
      TextDirection.rtl,
    );
    expect(tester.takeException(), isNull);
    await tester.tap(find.textContaining('مصروف تشغيل').first);
    await tester.pumpAndSettle();
    expect(find.text('تفاصيل المصروف'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
