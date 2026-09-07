import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/payments/domain/payment_models.dart';
import 'package:aqarios_mobile/features/payments/presentation/payments_screen.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_detail_screen.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_submission_screen.dart';
import 'payments_fixtures.dart';
import 'leasing_widgets_test.dart' show leasingApp;

void main() {
  testWidgets('Pixel 8 Arabic light/dark payments and tenant form render', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(411, 914);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    for (final font in ['Tajawal-Regular', 'Tajawal-Medium', 'Tajawal-Bold']) {
      await (FontLoader(
        'Tajawal',
      )..addFont(rootBundle.load('assets/fonts/$font.ttf'))).load();
    }
    await (FontLoader(
      'MaterialIcons',
    )..addFont(rootBundle.load('fonts/MaterialIcons-Regular.otf'))).load();
    final h = PaymentHarness();
    addTearDown(h.client.close);
    final screens = <String, Widget>{
      'payments_ar': Scaffold(
        appBar: const AqariAppBar(title: 'الدفعات'),
        body: PaymentsScreen(scope: h.scope),
      ),
      'payment_detail_dark_ar': PaymentDetailScreen(scope: h.scope, id: 'p1'),
      'payment_submission_ar': PaymentSubmissionScreen(
        scope: h.scope,
        payment: RentPayment(paymentJson()),
      ),
    };
    for (final entry in screens.entries) {
      await tester.pumpWidget(
        leasingApp(
          RepaintBoundary(key: ValueKey(entry.key), child: entry.value),
          arabic: true,
          dark: entry.key.contains('dark'),
        ),
      );
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      if (const bool.fromEnvironment('CAPTURE_PAYMENTS')) {
        await expectLater(
          find.byKey(ValueKey(entry.key)),
          matchesGoldenFile('goldens/${entry.key}.png'),
        );
      }
    }
  });
}
