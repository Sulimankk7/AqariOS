import 'dart:convert';

import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/features/auth/application/session_controller.dart';
import 'package:aqarios_mobile/features/auth/data/auth_repository.dart';
import 'package:aqarios_mobile/features/settings/company_profile_screen.dart';
import 'package:aqarios_mobile/features/settings/operational_settings_screen.dart';
import 'package:aqarios_mobile/features/settings/settings.dart';
import 'package:aqarios_mobile/features/settings/settings_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'properties_fixtures.dart';

Map<String, dynamic> companyJson() => {
  'id': 'test-company',
  'legalName': 'شركة عقارية ذات اسم قانوني طويل',
  'displayName': 'العقارية',
  'commercialRegistrationNo': 'CR-10',
  'taxNumber': 'TAX-20',
  'companyType': 1,
  'primaryPhone': '0790000000',
  'primaryEmail': 'a.very.long.company.email.address@example.test',
  'countryCode': 'JO',
  'isActive': true,
};

Map<String, dynamic> operationsJson() => {
  'defaultCurrency': 'JOD',
  'rentGracePeriodDays': 5,
  'lateFeeType': 1,
  'lateFeeValue': 12.5,
  'fiscalYearStartMonth': 1,
  'defaultLanguage': 'ar',
  'timezone': 'Asia/Amman',
};

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

Future<void> reveal(WidgetTester tester, Finder finder) async {
  await tester.scrollUntilVisible(
    finder,
    300,
    scrollable: find.byType(Scrollable).first,
  );
  await tester.pumpAndSettle();
}

class SettingsHarness {
  SettingsHarness(Future<http.Response> Function(http.Request) handler) {
    client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: store,
      client: MockClient((request) async {
        requests.add(request);
        return handler(request);
      }),
    );
    repository = SettingsRepository(client);
  }
  final store = TestSessionStore();
  final requests = <http.Request>[];
  late final ApiClient client;
  late final SettingsRepository repository;
}

http.Response response(http.Request request) {
  final path = request.url.path;
  if (request.method == 'PUT' || path.endsWith('/logout-all')) {
    return http.Response('', 204);
  }
  final data = path.endsWith('/settings') ? operationsJson() : companyJson();
  return http.Response(
    jsonEncode(data),
    200,
    headers: {'content-type': 'application/json'},
  );
}

void main() {
  testWidgets(
    'company profile loads, validates, updates, and hides immutable editing',
    (tester) async {
      final h = SettingsHarness((request) async => response(request));
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          CompanyProfileScreen(
            repository: h.repository,
            user: propertyUser({'company.manage'}),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('شركة عقارية ذات اسم قانوني طويل'), findsOneWidget);
      expect(find.text('CR-10'), findsOneWidget);
      await reveal(tester, find.text('تعديل بيانات الشركة'));
      await tester.tap(find.text('تعديل بيانات الشركة'));
      await tester.pumpAndSettle();
      expect(find.text('رقم السجل التجاري'), findsNothing);
      final fields = find.byType(TextField);
      await tester.enterText(fields.at(0), '');
      await tester.enterText(fields.at(3), 'invalid-email');
      await tester.tap(find.text('حفظ'));
      await tester.pumpAndSettle();
      expect(find.text('هذا الحقل مطلوب.'), findsOneWidget);
      expect(find.text('أدخل بريداً إلكترونياً صالحاً.'), findsOneWidget);
      expect(h.requests.where((request) => request.method == 'PUT'), isEmpty);
      await tester.enterText(fields.at(0), 'شركة محدثة');
      await tester.enterText(fields.at(3), 'valid@example.test');
      await tester.tap(find.text('حفظ'));
      await tester.pumpAndSettle();
      final update = h.requests.singleWhere(
        (request) => request.method == 'PUT',
      );
      expect(jsonDecode(update.body), {
        'legalName': 'شركة محدثة',
        'displayName': 'العقارية',
        'primaryPhone': '0790000000',
        'primaryEmail': 'valid@example.test',
      });
    },
  );

  testWidgets('company profile keeps backend update error visible', (
    tester,
  ) async {
    final h = SettingsHarness(
      (request) async => request.method == 'PUT'
          ? http.Response.bytes(
              utf8.encode('{"detail":"تعذر حفظ بيانات الشركة."}'),
              409,
              headers: {'content-type': 'application/json; charset=utf-8'},
            )
          : response(request),
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        CompanyProfileScreen(
          repository: h.repository,
          user: propertyUser({'company.manage'}),
        ),
      ),
    );
    await tester.pumpAndSettle();
    await reveal(tester, find.text('تعديل بيانات الشركة'));
    await tester.tap(find.text('تعديل بيانات الشركة'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('حفظ'));
    await tester.pumpAndSettle();
    expect(find.text('تعذر حفظ بيانات الشركة.'), findsOneWidget);
  });

  testWidgets(
    'operations shows defaults and validates every writable combination',
    (tester) async {
      final h = SettingsHarness((request) async => response(request));
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          OperationalSettingsScreen(
            repository: h.repository,
            user: propertyUser({'company.manage'}),
          ),
          dark: true,
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('JOD'), findsOneWidget);
      expect(find.text('Asia/Amman'), findsOneWidget);
      final fields = find.byType(TextField);
      await tester.enterText(fields.at(0), '-1');
      await tester.enterText(fields.last, '13');
      await tester.tap(find.text('حفظ'));
      await tester.pumpAndSettle();
      expect(find.textContaining('يساوي صفراً أو أكبر'), findsOneWidget);
      expect(find.text('أدخل شهراً من 1 إلى 12.'), findsOneWidget);
      await tester.enterText(fields.at(0), '0');
      await tester.enterText(fields.last, '12');
      await tester.tap(
        find.byWidgetPredicate((widget) => widget is AqariSelect),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('بدون').last);
      await tester.pumpAndSettle();
      await tester.tap(find.text('حفظ'));
      await tester.pumpAndSettle();
      final update = h.requests.lastWhere((request) => request.method == 'PUT');
      expect((jsonDecode(update.body) as Map)['lateFeeValue'], isNull);
    },
  );

  testWidgets('operations requires a positive fixed or percentage late fee', (
    tester,
  ) async {
    final h = SettingsHarness((request) async => response(request));
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        OperationalSettingsScreen(
          repository: h.repository,
          user: propertyUser({'company.manage'}),
        ),
      ),
    );
    await tester.pumpAndSettle();
    final fields = find.byType(TextField);
    await tester.enterText(fields.at(1), '0');
    await tester.tap(find.text('حفظ'));
    await tester.pumpAndSettle();
    expect(find.text('أدخل قيمة أكبر من صفر.'), findsOneWidget);
    expect(h.requests.where((request) => request.method == 'PUT'), isEmpty);
  });

  testWidgets(
    'appearance, language, account and supported security are exposed',
    (tester) async {
      final h = SettingsHarness((request) async {
        if (request.url.path.endsWith('/auth/login')) {
          return http.Response.bytes(
            utf8.encode(
              jsonEncode({
                'accessToken': 'token',
                'isPersistentSession': true,
                'user': {
                  'id': 'user-1',
                  'fullName': 'مستخدم الاختبار',
                  'email': 'long.user.email.address@example.test',
                  'preferredLanguage': 'ar',
                  'activeCompanyId': 'company-1',
                  'companyRoles': [
                    {'companyId': 'company-1', 'roleCode': 'COMPANY_ADMIN'},
                  ],
                  'permissions': ['company.manage'],
                  'systemRoles': [],
                },
              }),
            ),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'},
          );
        }
        return response(request);
      });
      addTearDown(h.client.close);
      final session = SessionController(AuthRepository(h.client, h.store));
      addTearDown(session.dispose);
      await session.login(
        identifier: 'user',
        password: 'password',
        rememberMe: true,
      );
      AqariThemePreference? theme;
      Locale? locale;
      await tester.pumpWidget(
        app(
          SettingsScreen(
            repository: h.repository,
            session: session,
            themePreference: AqariThemePreference.system,
            locale: const Locale('ar'),
            onThemeChanged: (value) => theme = value,
            onLocaleChanged: (value) => locale = value,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('تسجيل الخروج'), findsOneWidget);
      await reveal(tester, find.text('تسجيل الخروج من جميع الأجهزة'));
      expect(find.text('تسجيل الخروج من جميع الأجهزة'), findsOneWidget);
      expect(find.textContaining('MFA'), findsNothing);
      expect(find.textContaining('تغيير كلمة المرور'), findsNothing);
      await reveal(
        tester,
        find.byWidgetPredicate((widget) => widget is AqariSelect).first,
      );
      await tester.tap(
        find.byWidgetPredicate((widget) => widget is AqariSelect).first,
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('داكن'));
      await tester.pump(const Duration(milliseconds: 500));
      expect(theme, AqariThemePreference.dark);
      await tester.tap(
        find.byWidgetPredicate((widget) => widget is AqariSelect).last,
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('English'));
      await tester.pump(const Duration(milliseconds: 500));
      expect(locale, const Locale('en'));
    },
  );
}
