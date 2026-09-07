import 'dart:convert';

import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/tenant_portal/data/tenant_portal_repository.dart';
import 'package:aqarios_mobile/features/tenant_portal/presentation/tenant_dashboard_screen.dart';
import 'package:aqarios_mobile/features/tenant_portal/presentation/tenant_lease_screen.dart';
import 'package:aqarios_mobile/features/tenant_portal/presentation/tenant_profile_screen.dart';
import 'package:aqarios_mobile/features/tenant_portal/utilities/tenant_utilities_screen.dart';
import 'package:aqarios_mobile/features/tenant_portal/utilities/tenant_utility_controller.dart';
import 'package:aqarios_mobile/features/tenant_portal/utilities/tenant_utility_repository.dart';
import 'package:aqarios_mobile/features/auth/domain/user_profile.dart';
import 'package:aqarios_mobile/features/notifications/application/notifications_controller.dart';
import 'package:aqarios_mobile/features/notifications/data/notifications_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  testWidgets(
    'dashboard composes profile, utility and shared notification data',
    (tester) async {
      final api = _api((request) async {
        if (request.url.path.endsWith('/tenant-portal/me')) {
          return _json(_profile());
        }
        if (request.url.path.endsWith('/dashboard-summary')) {
          return _json({
            'electricityLinked': true,
            'waterLinked': false,
            'electricityAccountNumber': '0020013902',
            'electricitySyncStatus': 'Synced',
            'latestElectricityBill': {
              'id': 'bill-1',
              'billDate': '2026-08-01',
              'amount': 12.5,
              'currency': 'JOD',
              'isPaid': false,
              'paymentStatus': 'Unpaid',
              'discoveredAt': '2026-08-01T00:00:00Z',
            },
          });
        }
        if (request.url.path.endsWith('/notifications/me')) {
          return _json([
            {
              'id': 'n1',
              'subject': 'Rent reminder',
              'body': 'Body',
              'notificationType': 2,
              'priority': 1,
              'status': 1,
              'createdAt': '2026-09-01T10:00:00Z',
              'readAt': null,
            },
          ]);
        }
        return _json({});
      });
      final utilities = TenantUtilityController(TenantUtilityRepository(api));
      final notifications = NotificationsController(
        NotificationsRepository(api),
        unreadCount: 2,
      );
      addTearDown(utilities.dispose);
      addTearDown(notifications.dispose);
      addTearDown(api.close);
      await tester.pumpWidget(
        _app(
          TenantDashboardScreen(
            user: const UserProfile(
              id: 'u1',
              fullName: 'Fallback',
              preferredLanguage: 'en',
              activeCompanyId: 'c1',
              companyRoles: [
                UserCompanyRole(companyId: 'c1', roleCode: 'TENANT'),
              ],
              permissions: {},
              systemRoles: {},
            ),
            repository: TenantPortalRepository(api),
            utilities: utilities,
            notifications: notifications,
            onOpenProfile: () {},
            onOpenPayments: () {},
            onOpenUtilities: () {},
          ),
          const Locale('en'),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Welcome back, سارة أحمد'), findsOneWidget);
      expect(find.text('1'), findsNWidgets(2));
      await tester.scrollUntilVisible(
        find.text('Recent notifications'),
        400,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text('Rent reminder'), findsOneWidget);
      expect(find.text('2'), findsOneWidget);
    },
  );

  testWidgets(
    'profile is read-only and renders family, emergency and vehicle data in Arabic RTL',
    (tester) async {
      final api = _api((_) async => _json(_profile()));
      await tester.pumpWidget(
        _app(
          TenantProfileScreen(repository: TenantPortalRepository(api)),
          const Locale('ar'),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('المعلومات الشخصية'), findsOneWidget);
      expect(find.text('أفراد العائلة (1)'), findsOneWidget);
      await tester.scrollUntilVisible(
        find.text('جهات اتصال الطوارئ (1)'),
        300,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text('جهات اتصال الطوارئ (1)'), findsOneWidget);
      await tester.scrollUntilVisible(
        find.text('المركبات (1)'),
        300,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text('المركبات (1)'), findsOneWidget);
      expect(find.byType(TextField), findsNothing);
      expect(
        Directionality.of(tester.element(find.text('المعلومات الشخصية'))),
        TextDirection.rtl,
      );
      api.close();
    },
  );

  testWidgets('lease renders audited read-only fields in English dark mode', (
    tester,
  ) async {
    final api = _api((_) async => _json(_lease()));
    await tester.pumpWidget(
      _app(
        TenantLeaseScreen(repository: TenantPortalRepository(api)),
        const Locale('en'),
        dark: true,
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Contract details'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('Financial terms'),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Financial terms'), findsOneWidget);
    expect(find.text('CTR-2026-01'), findsWidgets);
    expect(find.text('Apartment'), findsOneWidget);
    expect(find.byType(TextField), findsNothing);
    expect(
      Directionality.of(tester.element(find.text('Contract details'))),
      TextDirection.ltr,
    );
    api.close();
  });

  testWidgets('lease 404 renders the explicit no-active-lease state', (
    tester,
  ) async {
    final api = _api(
      (_) async => http.Response(jsonEncode({'title': 'Not found'}), 404),
    );
    await tester.pumpWidget(
      _app(
        TenantLeaseScreen(repository: TenantPortalRepository(api)),
        const Locale('en'),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('No active lease'), findsOneWidget);
    api.close();
  });

  testWidgets(
    'utilities exposes link sheet with exact electricity validation',
    (tester) async {
      final api = _api((request) async {
        if (request.url.path.endsWith('/accounts')) {
          return _json([]);
        }
        if (request.url.path.endsWith('/bills')) {
          return _json({'items': [], 'nextCursor': null, 'hasMore': false});
        }
        if (request.url.path.endsWith('/dashboard-summary')) {
          return _json({'electricityLinked': false, 'waterLinked': false});
        }
        return _json({});
      });
      final controller = TenantUtilityController(TenantUtilityRepository(api));
      await tester.pumpWidget(
        _app(TenantUtilitiesScreen(controller: controller), const Locale('en')),
      );
      await tester.pumpAndSettle();
      expect(find.text('No electricity account is linked.'), findsOneWidget);

      await tester.tap(find.widgetWithText(AqariButton, 'Link account').first);
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).first, '123');
      await tester.tap(find.widgetWithText(AqariButton, 'Link account').last);
      await tester.pump();
      expect(
        find.text('Electricity account must contain exactly 10 digits.'),
        findsOneWidget,
      );
      controller.dispose();
      api.close();
    },
  );
}

Widget _app(Widget home, Locale locale, {bool dark = false}) => MaterialApp(
  locale: locale,
  supportedLocales: const [Locale('ar'), Locale('en')],
  localizationsDelegates: const [
    GlobalMaterialLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
  ],
  theme: AqariTheme.light(locale),
  darkTheme: AqariTheme.dark(locale),
  themeMode: dark ? ThemeMode.dark : ThemeMode.light,
  home: Scaffold(body: home),
);

ApiClient _api(Future<http.Response> Function(http.Request) handler) =>
    ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: _Store(),
      client: MockClient(handler),
    );
http.Response _json(Object body, {int status = 200}) => http.Response(
  jsonEncode(body),
  status,
  headers: {'content-type': 'application/json'},
);

Map<String, dynamic> _profile() => {
  'id': 'tenant-1',
  'companyId': 'company-1',
  'name': 'سارة أحمد',
  'nationalId': '1234567890',
  'phone': '0791111111',
  'email': 'sara@example.com',
  'occupation': 'Engineer',
  'employer': 'Aqari',
  'createdAt': '2026-01-01T00:00:00Z',
  'updatedAt': '2026-01-01T00:00:00Z',
  'familyMembers': [
    {
      'id': 'f1',
      'name': 'أحمد',
      'relationshipType': 'Spouse',
      'ageBracket': 'Adult',
      'createdAt': '2026-01-01T00:00:00Z',
    },
  ],
  'emergencyContacts': [
    {
      'id': 'e1',
      'name': 'ليلى',
      'relationshipType': 'Sister',
      'phone': '0790000000',
      'createdAt': '2026-01-01T00:00:00Z',
    },
  ],
  'vehicles': [
    {
      'id': 'v1',
      'plateNumber': '12-34567',
      'makeModel': 'Toyota Corolla',
      'color': 'White',
      'createdAt': '2026-01-01T00:00:00Z',
    },
  ],
};

Map<String, dynamic> _lease() => {
  'id': 'lease-1',
  'contractNumber': 'CTR-2026-01',
  'startDate': '2026-01-01',
  'endDate': '2026-12-31',
  'signedDate': '2025-12-20',
  'monthlyRentAmount': 350,
  'currency': 'JOD',
  'securityDepositAmount': 350,
  'paymentFrequency': 'Monthly',
  'paymentDueDay': 1,
  'status': 'Active',
  'legalRegime': 'Jordan',
  'tenantType': 'Individual',
  'apartmentId': 'unit-1',
  'apartmentUnitNumber': 'Apartment',
  'apartmentBedrooms': 2,
  'apartmentBathrooms': 2,
  'apartmentAreaSqm': 110,
  'buildingId': 'building-1',
  'buildingName': 'Olive Residence',
};

class _Store extends SecureSessionStore {
  @override
  Future<String?> readAccessToken() async => 'token';
  @override
  Future<String?> readRefreshToken() async => null;
}
