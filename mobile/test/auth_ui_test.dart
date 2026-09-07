import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/auth/application/session_controller.dart';
import 'package:aqarios_mobile/features/auth/data/auth_repository.dart';
import 'package:aqarios_mobile/features/auth/domain/user_profile.dart';
import 'package:aqarios_mobile/features/auth/presentation/auth_components.dart';
import 'package:aqarios_mobile/features/auth/presentation/auth_flows.dart';
import 'package:aqarios_mobile/features/auth/presentation/login_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:http/testing.dart';

void main() {
  late ApiClient client;
  late SessionController session;
  setUp(() {
    client = ApiClient(
      baseUri: Uri.parse('https://example.test'),
      sessionStore: _Store(),
      client: MockClient((_) async => throw StateError('unexpected request')),
    );
    session = SessionController(AuthRepository(client, _Store()));
  });
  tearDown(() {
    session.dispose();
    client.close();
  });

  testWidgets('password login has parity controls and password visibility', (
    tester,
  ) async {
    await pump(tester, LoginScreen(session: session));
    expect(find.text('Password'), findsWidgets);
    expect(find.text('Phone SMS'), findsOneWidget);
    expect(find.text('Forgot password?'), findsOneWidget);
    expect(find.text('Create account'), findsOneWidget);
    expect(find.bySemanticsLabel('Show password'), findsOneWidget);
    await tester.tap(find.bySemanticsLabel('Show password'));
    await tester.pump();
    expect(find.bySemanticsLabel('Hide password'), findsOneWidget);
  });

  testWidgets(
    'registration exposes every Web contract field in one scrollable column',
    (tester) async {
      await pump(
        tester,
        AuthFlowPage(session: session, flow: AuthFlow.register),
      );
      for (final label in [
        'Full name',
        'Company name',
        'Company type',
        'Display name (optional)',
        'Email',
        'Phone number',
        'Password',
        'Confirm password',
        'Preferred language',
      ]) {
        expect(
          find.text(label, skipOffstage: false),
          findsOneWidget,
          reason: label,
        );
      }
      expect(find.byType(Scrollable), findsWidgets);
    },
  );

  testWidgets('SMS and recovery flows use explicit segmented methods', (
    tester,
  ) async {
    await pump(tester, AuthFlowPage(session: session, flow: AuthFlow.otp));
    expect(find.text('Phone SMS'), findsOneWidget);
    expect(find.text('Send code'), findsOneWidget);
    await pump(
      tester,
      AuthFlowPage(session: session, flow: AuthFlow.forgotPassword),
    );
    expect(find.text('Email'), findsOneWidget);
    expect(find.text('SMS'), findsOneWidget);
  });

  testWidgets('OTP is six accessible cells with countdown and change phone', (
    tester,
  ) async {
    await pump(tester, OtpVerifyPage(session: session, phone: '+962790000000'));
    expect(find.byType(AuthOtpInput), findsOneWidget);
    expect(find.byType(TextField), findsNWidgets(6));
    expect(find.text('Change number'), findsOneWidget);
    expect(find.textContaining('Resend in'), findsOneWidget);
  });

  testWidgets('reset, activation and success states render at Pixel 8 size', (
    tester,
  ) async {
    await pump(
      tester,
      PasswordEntryPage(session: session, credential: 'secret'),
    );
    expect(find.text('Reset password'), findsOneWidget);
    await pump(
      tester,
      PasswordEntryPage(
        session: session,
        credential: 'secret',
        activation: true,
        tenantName: 'Tenant',
      ),
    );
    expect(find.text('Activate tenant account'), findsOneWidget);
    expect(find.textContaining('Tenant'), findsOneWidget);
    final profile = UserProfile.fromJson({
      'id': 'u1',
      'fullName': 'Tenant',
      'preferredLanguage': 'en',
      'activeCompanyId': 'c1',
      'companyRoles': [
        {'companyId': 'c1', 'roleCode': 'TENANT'},
      ],
      'permissions': <String>[],
      'systemRoles': <String>[],
    });
    await pump(
      tester,
      ActivationSuccessPage(session: session, profile: profile),
    );
    expect(find.text('Your account is active'), findsOneWidget);
  });

  testWidgets('login remains overflow-free in Arabic RTL dark mode', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 2.75;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      MaterialApp(
        locale: const Locale('ar'),
        supportedLocales: const [Locale('ar'), Locale('en')],
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        theme: AqariTheme.dark(const Locale('ar')),
        home: Directionality(
          textDirection: TextDirection.rtl,
          child: LoginScreen(session: session),
        ),
      ),
    );
    await tester.pump();
    expect(find.text('مرحباً بك مجدداً'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

Future<void> pump(WidgetTester tester, Widget child) async {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.75;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    MaterialApp(
      locale: const Locale('en'),
      theme: AqariTheme.light(const Locale('en')),
      darkTheme: AqariTheme.dark(const Locale('en')),
      home: child,
    ),
  );
  await tester.pump();
  expect(tester.takeException(), isNull);
}

class _Store extends SecureSessionStore {
  @override
  Future<String?> readAccessToken() async => null;
  @override
  Future<String?> readRefreshToken() async => null;
  @override
  Future<void> writeAccessToken(String token) async {}
  @override
  Future<void> writePersistence(bool persistent) async {}
  @override
  Future<void> clear() async {}
}
