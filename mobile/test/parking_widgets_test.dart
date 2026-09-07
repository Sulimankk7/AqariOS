import 'dart:convert';

import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/features/leasing/data/lease_files.dart';
import 'package:aqarios_mobile/features/leasing/data/leasing_repository.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_detail_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_scope.dart';
import 'package:aqarios_mobile/features/parking/parking_screen.dart';
import 'package:aqarios_mobile/features/properties/data/properties_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'leasing_fixtures.dart';
import 'properties_fixtures.dart';

const leaseId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

Widget app(Widget child, {bool dark = false}) => MaterialApp(
  locale: const Locale('ar'),
  supportedLocales: const [Locale('ar'), Locale('en')],
  localizationsDelegates: const [
    GlobalMaterialLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
  ],
  theme: dark
      ? AqariTheme.dark(const Locale('ar'))
      : AqariTheme.light(const Locale('ar')),
  home: child,
);

Map<String, dynamic> spotJson() => {
  'id': 'spot-1',
  'buildingId': 'b1',
  'spotCode': 'P-01',
  'parkingType': 0,
  'locationDescription': 'قرب المدخل',
};

Map<String, dynamic> assignmentJson() => {
  'assignmentId': 'assignment-1',
  'parkingSpotId': 'spot-1',
  'parkingSpotCode': 'P-01',
  'parkingType': 'Standard',
  'location': 'قرب المدخل',
  'leaseContractId': leaseId,
  'tenantId': 'tenant-1',
  'tenantName': 'اسم مستأجر طويل للاختبار',
  'startDate': '2026-01-01',
  'endDate': null,
  'status': 'Active',
};

class ParkingHarness {
  ParkingHarness(Future<http.Response> Function(http.Request) handler) {
    client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: TestSessionStore(),
      client: MockClient((request) async {
        requests.add(request);
        return handler(request);
      }),
    );
    properties = PropertiesRepository(client);
    scope = LeaseScope(
      repository: LeasingRepository(client),
      properties: properties,
      user: propertyUser({'properties.read'}),
      files: LeaseFiles(client),
    );
  }

  late final ApiClient client;
  late final PropertiesRepository properties;
  late final LeaseScope scope;
  final requests = <http.Request>[];

  Widget screen() => ParkingScreen(properties: properties, leaseScope: scope);
}

Future<void> selectBuilding(WidgetTester tester) async {
  await tester.tap(find.byWidgetPredicate((widget) => widget is AqariSelect));
  await tester.pumpAndSettle();
  await tester.tap(find.text('Cedar House').last);
  await tester.pumpAndSettle();
}

http.Response standardResponse(http.Request request) {
  final path = request.url.path;
  final Object result = switch (path) {
    '/api/v1/buildings' => [buildingJson()],
    '/api/v1/buildings/b1/parking-spots' => [spotJson()],
    '/api/v1/parking-spots/spot-1/assignment' => assignmentJson(),
    '/api/v1/leasing/contracts/$leaseId' => leaseJson(
      id: leaseId,
      number: 'ISE-2026-0012-LONG',
      status: 2,
    ),
    '/api/v1/leasing/contracts/$leaseId/parking' => [],
    _ => <String, dynamic>{'detail': 'Unknown route'},
  };
  return http.Response(
    jsonEncode(result),
    path == '/unknown' ? 404 : 200,
    headers: {'content-type': 'application/json'},
  );
}

void main() {
  testWidgets('archive confirms, cancel is inert, and success refreshes list', (
    tester,
  ) async {
    var deletes = 0;
    var listReads = 0;
    final harness = ParkingHarness((request) async {
      if (request.url.path.endsWith('/parking-spots')) listReads++;
      if (request.method == 'DELETE') {
        deletes++;
        return http.Response('', 204);
      }
      return standardResponse(request);
    });
    addTearDown(harness.client.close);
    await tester.pumpWidget(app(harness.screen()));
    await tester.pumpAndSettle();
    await selectBuilding(tester);
    await tester.tap(find.text('P-01'));
    await tester.pumpAndSettle();
    await tester.tap(find.byType(AqariPopupMenu<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('أرشفة الموقف'));
    await tester.pumpAndSettle();
    expect(find.text('أرشفة الموقف؟'), findsOneWidget);
    await tester.tap(find.text('إلغاء'));
    await tester.pumpAndSettle();
    expect(deletes, 0);
    await tester.tap(find.byType(AqariPopupMenu<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('أرشفة الموقف'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('أرشفة'));
    await tester.pumpAndSettle();
    expect(deletes, 1);
    expect(listReads, 2);
  });

  testWidgets('archive keeps backend errors visible in the confirmation', (
    tester,
  ) async {
    final harness = ParkingHarness(
      (request) async => request.method == 'DELETE'
          ? http.Response(
              '{"detail":"لا يمكن أرشفة موقف مخصص حالياً."}',
              409,
              headers: {'content-type': 'application/json'},
            )
          : standardResponse(request),
    );
    addTearDown(harness.client.close);
    await tester.pumpWidget(app(harness.screen(), dark: true));
    await tester.pumpAndSettle();
    await selectBuilding(tester);
    await tester.tap(find.text('P-01'));
    await tester.pumpAndSettle();
    await tester.tap(find.byType(AqariPopupMenu<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('أرشفة الموقف'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('أرشفة'));
    await tester.pumpAndSettle();
    expect(find.text('لا يمكن أرشفة موقف مخصص حالياً.'), findsOneWidget);
    expect(find.text('أرشفة الموقف؟'), findsOneWidget);
  });

  testWidgets('contract number replaces UUID and opens existing lease detail', (
    tester,
  ) async {
    final harness = ParkingHarness(
      (request) async => standardResponse(request),
    );
    addTearDown(harness.client.close);
    await tester.pumpWidget(app(harness.screen()));
    await tester.pumpAndSettle();
    await selectBuilding(tester);
    await tester.tap(find.text('P-01'));
    await tester.pumpAndSettle();
    expect(find.text('ISE-2026-0012-LONG'), findsOneWidget);
    expect(find.text(leaseId), findsNothing);
    await tester.tap(find.text('ISE-2026-0012-LONG'));
    await tester.pumpAndSettle();
    expect(find.byType(LeaseDetailScreen), findsOneWidget);
    expect(find.text(leaseId), findsNothing);
  });

  testWidgets(
    'assigned detail remains RTL with long tenant and contract text',
    (tester) async {
      final harness = ParkingHarness(
        (request) async => standardResponse(request),
      );
      addTearDown(harness.client.close);
      await tester.pumpWidget(app(harness.screen(), dark: true));
      await tester.pumpAndSettle();
      await selectBuilding(tester);
      await tester.tap(find.text('P-01'));
      await tester.pumpAndSettle();
      expect(
        Directionality.of(tester.element(find.text('العقد'))),
        TextDirection.rtl,
      );
      expect(find.textContaining('اسم مستأجر طويل'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
