import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/payments/domain/payment_models.dart';
import 'package:aqarios_mobile/features/payments/presentation/payments_screen.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_detail_screen.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_submission_screen.dart';
import 'package:aqarios_mobile/features/payments/presentation/verification_screen.dart';
import 'payments_fixtures.dart';
import 'leasing_widgets_test.dart' show leasingApp;

void main() {
  Future<void> pump(
    WidgetTester tester,
    Widget child, {
    bool ar = false,
    bool dark = false,
  }) async {
    tester.view.physicalSize = const Size(411, 914);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(leasingApp(child, arabic: ar, dark: dark));
    await tester.pumpAndSettle();
  }

  testWidgets(
    'server search debounces, reset clears actual request and sections show',
    (t) async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await pump(t, Scaffold(body: PaymentsScreen(scope: h.scope)));
      expect(find.text('Scheduled obligations'), findsOneWidget);
      await t.enterText(find.byType(TextField), 'Ali');
      await t.pump(const Duration(milliseconds: 349));
      expect(h.requests.length, 1);
      await t.pump(const Duration(milliseconds: 5));
      await t.pumpAndSettle();
      expect(h.requests.last.url.queryParameters['searchTerm'], 'Ali');
      await t.tap(find.text('Reset'));
      await t.pumpAndSettle();
      expect(
        h.requests.last.url.queryParameters.containsKey('searchTerm'),
        false,
      );
      expect(find.text('Ali'), findsNothing);
    },
  );
  testWidgets(
    'filter sheet sends building/status and date reset without local filtering',
    (t) async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await pump(t, Scaffold(body: PaymentsScreen(scope: h.scope)));
      await t.tap(find.text('Filters'));
      await t.pumpAndSettle();
      await t.tap(find.text('All buildings'));
      await t.pumpAndSettle();
      await t.tap(find.text('Cedar House'));
      await t.pumpAndSettle();
      await t.tap(find.text('Apply'));
      await t.pumpAndSettle();
      expect(h.requests.last.url.queryParameters['buildingId'], 'b1');
      await t.tap(find.text('Pending verification').first);
      await t.pumpAndSettle();
      expect(h.requests.last.url.queryParameters['status'], '1');
    },
  );
  testWidgets('loading to error/retry to empty distinguishes failed request', (
    t,
  ) async {
    final pending = Completer<http.Response>();
    var attempt = 0;
    final h = PaymentHarness(
      handler: (_) async {
        attempt++;
        return attempt == 1 ? pending.future : jsonResponse([]);
      },
    );
    addTearDown(h.client.close);
    await t.pumpWidget(
      leasingApp(Scaffold(body: PaymentsScreen(scope: h.scope))),
    );
    await t.pump();
    expect(find.byType(AqariSkeleton), findsWidgets);
    pending.complete(jsonResponse({}, 500));
    await t.pumpAndSettle();
    expect(find.text('Request failed'), findsOneWidget);
    expect(find.text('No payments'), findsNothing);
    await t.tap(find.text('Retry'));
    await t.pumpAndSettle();
    expect(find.text('No payments'), findsOneWidget);
  });
  testWidgets('no read/approve permissions makes no protected API request', (
    t,
  ) async {
    final h = PaymentHarness(permissions: {});
    addTearDown(h.client.close);
    await pump(t, Scaffold(body: PaymentsScreen(scope: h.scope)));
    expect(h.requests, isEmpty);
    expect(find.text('No payment access'), findsOneWidget);
  });
  testWidgets(
    'queue load-more uses opaque cursor then opens review and handles conflict',
    (t) async {
      var pages = 0;
      final h = PaymentHarness(
        handler: (r) async {
          if (r.url.path.endsWith('/pending-verifications')) {
            pages++;
            return jsonResponse({
              'items': [verificationJson()],
              'nextCursor': pages == 1 ? 'opaque-cursor' : null,
              'hasMore': pages == 1,
            });
          }
          if (r.url.path.endsWith('/approve')) {
            return jsonResponse({'code': 'SUBMISSION_NOT_PENDING'}, 409);
          }
          return PaymentHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await pump(t, Scaffold(body: VerificationScreen(scope: h.scope)));
      await t.tap(find.text('Load more'));
      await t.pumpAndSettle();
      expect(h.requests.last.url.queryParameters['cursor'], 'opaque-cursor');
      expect(find.text('Test tenant'), findsOneWidget);
      await t.tap(find.text('Test tenant'));
      await t.pumpAndSettle();
      await t.scrollUntilVisible(
        find.text('Approve payment'),
        350,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('Approve payment'));
      await t.pumpAndSettle();
      await t.tap(find.widgetWithText(AqariButton, 'Approve payment').last);
      await t.pumpAndSettle();
      expect(find.textContaining('already been processed'), findsOneWidget);
      expect(find.byType(AlertDialog), findsOneWidget);
    },
  );
  testWidgets(
    'reject requires reason and sends exact body; proof opens separately',
    (t) async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await pump(
        t,
        PaymentDetailScreen(
          scope: h.scope,
          id: 'p1',
          verification: VerificationItem(verificationJson()),
        ),
      );
      await t.scrollUntilVisible(
        find.text('View payment proof'),
        300,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('View payment proof'));
      await t.pumpAndSettle();
      expect(h.files.opened, 1);
      expect(h.requests.last.url.queryParameters['inline'], 'true');
      await t.scrollUntilVisible(
        find.text('Reject payment'),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('Reject payment'));
      await t.pumpAndSettle();
      final button = find.widgetWithText(AqariButton, 'Reject payment').last;
      expect(t.widget<AqariButton>(button).onPressed, isNull);
      await t.enterText(find.byType(TextField), ' Wrong proof ');
      await t.pumpAndSettle();
      await t.tap(button);
      await t.pumpAndSettle();
      expect(jsonDecode(h.requests.last.body), {'reason': 'Wrong proof'});
    },
  );
  testWidgets(
    'tenant cash validates amounts, posts once and shows real success',
    (t) async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await pump(
        t,
        PaymentSubmissionScreen(
          scope: h.scope,
          payment: RentPayment(paymentJson()),
        ),
      );
      await t.enterText(find.byType(TextField).first, '251');
      await t.tap(find.text('Submit for review'));
      await t.pumpAndSettle();
      expect(
        find.text('Amount exceeds the remaining balance.'),
        findsOneWidget,
      );
      expect(h.requests, isEmpty);
      await t.enterText(find.byType(TextField).first, '25.125');
      await t.tap(find.text('Submit for review'));
      await t.pumpAndSettle();
      expect(find.text('Payment submitted for review'), findsOneWidget);
      expect(jsonDecode(h.requests.single.body)['amount'], 25.125);
      expect(jsonDecode(h.requests.single.body)['proofFileId'], isNull);
    },
  );
  testWidgets(
    'tenant list item opens from cached projection without a duplicate fetch',
    (t) async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await pump(
        t,
        Scaffold(body: PaymentsScreen(scope: h.scope, tenant: true)),
      );
      expect(h.requests.length, 1);

      await t.tap(find.text('LEASE-2026-001').first);
      await t.pumpAndSettle();

      expect(find.text('Payment details'), findsOneWidget);
      expect(h.requests.length, 1);
    },
  );
  testWidgets(
    'CliQ confirmed proof survives submission failure and retry does not reupload',
    (t) async {
      var submissions = 0;
      final h = PaymentHarness(
        handler: (r) async {
          if (r.url.path.endsWith('/submit-verification')) {
            submissions++;
            return submissions == 1
                ? jsonResponse({'code': 'AMOUNT_EXCEEDS_OUTSTANDING'}, 422)
                : jsonResponse('submitted');
          }
          return PaymentHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await pump(
        t,
        PaymentSubmissionScreen(
          scope: h.scope,
          payment: RentPayment(paymentJson()),
        ),
      );
      await t.tap(find.text('Cash'));
      await t.pumpAndSettle();
      await t.tap(find.text('CliQ'));
      await t.pumpAndSettle();
      await t.enterText(find.byType(TextField).last, 'REF-001');
      await t.scrollUntilVisible(
        find.text('Add attachment'),
        300,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('Add attachment'));
      await t.pumpAndSettle();
      await t.tap(find.text('Files'));
      await t.pumpAndSettle();
      expect(h.files.uploads, 1);
      await t.scrollUntilVisible(
        find.text('Submit for review'),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('Submit for review'));
      await t.pumpAndSettle();
      expect(h.files.uploads, 1);
      await t.scrollUntilVisible(
        find.text('Submit for review'),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('Submit for review'));
      await t.pumpAndSettle();
      expect(submissions, 2);
      expect(h.files.uploads, 1);
      expect(jsonDecode(h.requests.last.body)['proofFileId'], 'confirmed-file');
    },
  );
  testWidgets(
    'receipt 404 renders not-issued and role permissions hide owner mutations',
    (t) async {
      final h = PaymentHarness(permissions: {'payments.read', 'receipts.read'});
      addTearDown(h.client.close);
      await pump(t, PaymentDetailScreen(scope: h.scope, id: 'p1'));
      expect(find.text('Approve payment'), findsNothing);
      expect(find.text('Remind tenant'), findsNothing);
      await t.scrollUntilVisible(
        find.text('View receipt'),
        500,
        scrollable: find.byType(Scrollable).first,
      );
      await t.tap(find.text('View receipt'));
      await t.pumpAndSettle();
      expect(find.text('Receipt not issued'), findsOneWidget);
      expect(find.text('Request failed'), findsNothing);
    },
  );
  testWidgets(
    'Arabic dark mode long text and amounts at 1.5x do not overflow',
    (t) async {
      final h = PaymentHarness(
        handler: (r) async => r.url.path.endsWith('/rent-payments')
            ? jsonResponse([
                {...paymentJson(), 'amountDue': 123456789.125},
              ])
            : PaymentHarness.defaultResponse(r),
      );
      addTearDown(h.client.close);
      t.view.physicalSize = const Size(411, 914);
      t.view.devicePixelRatio = 1;
      addTearDown(t.view.resetPhysicalSize);
      addTearDown(t.view.resetDevicePixelRatio);
      await t.pumpWidget(
        leasingApp(
          MediaQuery(
            data: const MediaQueryData(
              size: Size(411, 914),
              textScaler: TextScaler.linear(1.5),
            ),
            child: Scaffold(body: PaymentsScreen(scope: h.scope)),
          ),
          arabic: true,
          dark: true,
        ),
      );
      await t.pumpAndSettle();
      expect(t.takeException(), isNull);
      expect(find.textContaining('123,456,789.13'), findsOneWidget);
      expect(
        Directionality.of(t.element(find.byType(PaymentsScreen))),
        TextDirection.rtl,
      );
    },
  );
}
