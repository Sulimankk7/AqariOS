import 'dart:async';
import 'dart:convert';

import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/dashboard/application/dashboard_controller.dart';
import 'package:aqarios_mobile/features/dashboard/data/dashboard_repository.dart';
import 'package:aqarios_mobile/features/dashboard/presentation/company_dashboard_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

// Isolated contract fixture, never imported by production code.
const payload = {
  'property': {
    'totalBuildings': 1,
    'totalApartments': 2,
    'occupiedApartments': 1,
    'vacantApartments': 1,
    'occupancyRate': 50,
  },
  'leasing': {
    'activeLeases': 1,
    'expiringIn30Days': 0,
    'newLeasesThisMonth': 0,
  },
  'payments': {
    'collectedThisMonth': 0,
    'outstandingAmount': 300,
    'overduePayments': 2,
  },
  'financials': {'expensesThisMonth': 0},
};

class MemoryStore extends SecureSessionStore {
  @override
  Future<String?> readAccessToken() async => null;
}

void main() {
  testWidgets('initial HTTP failure renders a retry state', (tester) async {
    final api = ApiClient(
      baseUri: Uri.parse('https://example.test'),
      sessionStore: MemoryStore(),
      client: MockClient(
        (_) async =>
            http.Response(jsonEncode({'title': 'Service unavailable'}), 503),
      ),
    );
    final controller = DashboardController(DashboardRepository(api));
    await tester.pumpWidget(
      MaterialApp(
        theme: AqariTheme.light(const Locale('ar')),
        home: Scaffold(body: CompanyDashboardScreen(controller: controller)),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('تعذر تحميل لوحة المعلومات'), findsOneWidget);
    expect(find.text('Service unavailable'), findsOneWidget);
    expect(find.text('إعادة المحاولة'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    controller.dispose();
    api.close();
  });

  for (final dark in [false, true]) {
    testWidgets('dashboard has visible body at 390x844, dark=$dark', (
      tester,
    ) async {
      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      var requests = 0;
      final pending = Completer<http.Response>();
      var nextResponse = http.Response(jsonEncode(payload), 200);
      final api = ApiClient(
        baseUri: Uri.parse('https://example.test'),
        sessionStore: MemoryStore(),
        client: MockClient((_) {
          requests++;
          return requests == 1 ? pending.future : Future.value(nextResponse);
        }),
      );
      final controller = DashboardController(DashboardRepository(api));
      addTearDown(api.close);
      await tester.pumpWidget(
        MaterialApp(
          theme: dark
              ? AqariTheme.dark(const Locale('ar'))
              : AqariTheme.light(const Locale('ar')),
          home: Directionality(
            textDirection: TextDirection.rtl,
            child: Scaffold(
              appBar: const AqariAppBar(title: 'الرئيسية'),
              body: CompanyDashboardScreen(controller: controller),
              bottomNavigationBar: AqariBottomNavigation(
                items: const [
                  AqariNavigationDestination(
                    label: 'الرئيسية',
                    icon: Icons.home,
                  ),
                  AqariNavigationDestination(
                    label: 'المزيد',
                    icon: Icons.more_horiz,
                  ),
                ],
                selectedIndex: 0,
                onSelected: (_) {},
              ),
            ),
          ),
        ),
      );
      await tester.pump();
      expect(find.byType(AqariLoadingState), findsOneWidget);
      expect(
        tester.getSize(find.byType(AqariBottomNavigation)).height,
        lessThan(150),
      );
      pending.complete(http.Response(jsonEncode(payload), 200));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(controller.summary!.outstandingAmount, 300);
      expect(controller.summary!.occupancyRate, 50);
      expect(controller.summary!.totalBuildings, 1);
      expect(controller.summary!.totalApartments, 2);
      expect(controller.summary!.occupiedApartments, 1);
      expect(controller.summary!.vacantApartments, 1);
      expect(controller.summary!.activeLeases, 1);
      expect(controller.summary!.overduePayments, 2);
      expect(find.text('نظرة عامة على المحفظة').hitTestable(), findsOneWidget);
      expect(find.text('50.0%'), findsOneWidget);
      expect(
        tester.getSize(find.byType(RefreshIndicator)).height,
        greaterThan(500),
      );
      await tester.scrollUntilVisible(find.text('300.00 د.أ'), 200);
      expect(find.text('300.00 د.أ').hitTestable(), findsOneWidget);
      await tester.scrollUntilVisible(find.text('2 دفعات متأخرة'), 200);
      expect(find.text('2 دفعات متأخرة').hitTestable(), findsOneWidget);
      await controller.load();
      expect(requests, 1); // Five-minute repository cache is preserved.
      tester
          .state<ScrollableState>(find.byType(Scrollable).first)
          .position
          .jumpTo(0);
      await tester.pumpAndSettle();
      await tester.drag(find.byType(ListView), const Offset(0, 350));
      await tester.pumpAndSettle();
      expect(requests, 2);
      nextResponse = http.Response(
        jsonEncode({'title': 'Service unavailable'}),
        503,
      );
      await controller.load(force: true);
      await tester.pumpAndSettle();
      expect(find.text('تعذر تحديث البيانات'), findsOneWidget);
      expect(controller.summary!.outstandingAmount, 300);
      nextResponse = http.Response(
        jsonEncode({
          for (final entry in payload.entries)
            entry.key: {for (final key in entry.value.keys) key: 0},
        }),
        200,
      );
      await controller.load(force: true);
      await tester.pumpAndSettle();
      expect(find.text('0.0%'), findsOneWidget);
      expect(find.text('تحتاج إلى انتباه'), findsNothing);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox());
      controller.dispose();
    });
  }
}
