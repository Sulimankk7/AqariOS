import 'dart:convert';
import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/leasing/domain/lease_behavior.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_list_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_detail_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_form_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'leasing_fixtures.dart';
import 'leasing_widgets_test.dart' show leasingApp;

void main() {
  testWidgets('Pixel 8 dimensions: Arabic list, dark detail, create form', (
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
    final h = LeasingHarness(
      handler: (r) async {
        if (r.url.path.endsWith('/search')) {
          return http.Response(
            jsonEncode([
              leaseJson(number: 'LEASE-2026-012'),
              leaseJson(
                id: 'lease-2',
                status: 2,
                number: 'عقد المبنى السكني — 2026',
              ),
            ]),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'},
          );
        }
        return LeasingHarness.defaultResponse(r);
      },
    );
    addTearDown(h.client.close);
    final screens = <String, Widget>{
      'leasing_list_ar': Scaffold(
        appBar: const AqariAppBar(title: 'التأجير'),
        body: LeaseListScreen(scope: h.scope),
      ),
      'leasing_detail_dark_ar': LeaseDetailScreen(
        scope: h.scope,
        id: 'lease-1',
      ),
      'leasing_create_ar': LeaseFormScreen(
        scope: h.scope,
        mode: LeaseFormMode.create,
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
      if (const bool.fromEnvironment('CAPTURE_LEASING')) {
        await expectLater(
          find.byKey(ValueKey(entry.key)),
          matchesGoldenFile('goldens/${entry.key}.png'),
        );
      }
    }
  });
}
