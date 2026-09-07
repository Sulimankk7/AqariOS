import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import 'core/config/app_environment.dart';
import 'core/design_system/design_system.dart';
import 'core/network/api_client.dart';
import 'core/storage/secure_session_store.dart';
import 'core/storage/app_preferences.dart';
import 'features/auth/application/session_controller.dart';
import 'features/auth/data/auth_repository.dart';
import 'features/auth/presentation/login_screen.dart';
import 'features/auth/presentation/auth_flows.dart';
import 'features/dashboard/application/dashboard_controller.dart';
import 'features/dashboard/data/dashboard_repository.dart';
import 'features/shell/presentation/app_shell.dart';
import 'features/properties/data/properties_repository.dart';
import 'features/leasing/data/leasing_repository.dart';
import 'features/leasing/data/lease_files.dart';
import 'features/payments/data/payments_repository.dart';
import 'features/payments/data/payment_files.dart';
import 'features/financial_operations/data/expenses_repository.dart';
import 'features/notifications/data/notifications_repository.dart';
import 'features/notifications/application/notifications_controller.dart';
import 'features/tenant_portal/data/tenant_portal_repository.dart';
import 'features/tenant_portal/utilities/tenant_utility_controller.dart';
import 'features/tenant_portal/utilities/tenant_utility_repository.dart';
import 'playground/design_system_playground.dart';

class AqariApp extends StatefulWidget {
  const AqariApp({super.key});

  @override
  State<AqariApp> createState() => _AqariAppState();
}

class _AqariAppState extends State<AqariApp> {
  AqariThemePreference _theme = AqariThemePreference.system;
  Locale _locale = const Locale('ar');
  ApiClient? _client;
  SessionController? _session;
  DashboardController? _dashboard;
  PropertiesRepository? _properties;
  LeasingRepository? _leasing;
  LeaseFiles? _leaseFiles;
  PaymentsRepository? _payments;
  PaymentFiles? _paymentFiles;
  ExpensesRepository? _expenses;
  NotificationsRepository? _notifications;
  NotificationsController? _notificationsController;
  TenantPortalRepository? _tenantPortal;
  TenantUtilityController? _tenantUtilities;
  final _navigator = GlobalKey<NavigatorState>();
  String? _propertyScope;
  final _preferences = AppPreferences();
  bool _hasLocalLanguage = false;

  @override
  void initState() {
    super.initState();
    _loadPreferences();
    final baseUri = AppEnvironment.apiBaseUri;
    if (baseUri != null) {
      final store = SecureSessionStore();
      final client = ApiClient(baseUri: baseUri, sessionStore: store);
      final session = SessionController(AuthRepository(client, store));
      client.onSessionExpired = session.invalidate;
      session.addListener(_onSessionChanged);
      _client = client;
      _session = session;
      _dashboard = DashboardController(DashboardRepository(client));
      _properties = PropertiesRepository(client);
      _leasing = LeasingRepository(client);
      _leaseFiles = LeaseFiles(client);
      _payments = PaymentsRepository(client);
      _paymentFiles = PaymentFiles(_leaseFiles!);
      _expenses = ExpensesRepository(client);
      _notifications = NotificationsRepository(client);
      _notificationsController = NotificationsController(_notifications!);
      _tenantPortal = TenantPortalRepository(client);
      _tenantUtilities = TenantUtilityController(
        TenantUtilityRepository(client),
      );
      session.bootstrap();
    }
  }

  Future<void> _loadPreferences() async {
    try {
      final values = await Future.wait([
        _preferences.readTheme(),
        _preferences.readLanguage(),
      ]);
      if (!mounted) return;
      setState(() {
        _theme = switch (values[0]) {
          'light' => AqariThemePreference.light,
          'dark' => AqariThemePreference.dark,
          _ => AqariThemePreference.system,
        };
        if (values[1] == 'ar' || values[1] == 'en') {
          _hasLocalLanguage = true;
          _locale = Locale(values[1]!);
        }
      });
    } catch (_) {
      // Local preferences are optional; safe defaults remain available.
    }
  }

  void _setTheme(AqariThemePreference value) {
    setState(() => _theme = value);
    _preferences.writeTheme(value.name);
  }

  void _setLocale(Locale value) {
    setState(() {
      _hasLocalLanguage = true;
      _locale = value;
    });
    _preferences.writeLanguage(value.languageCode);
  }

  @override
  void dispose() {
    _session?.removeListener(_onSessionChanged);
    _session?.dispose();
    _dashboard?.dispose();
    _leaseFiles?.clear();
    _notificationsController?.dispose();
    _tenantUtilities?.dispose();
    _client?.close();
    super.dispose();
  }

  void _onSessionChanged() {
    if (!mounted) return;
    final user = _session?.user;
    final scope = user == null ? null : '${user.id}:${user.activeCompanyId}';
    if (_propertyScope != scope) {
      _properties?.clear();
      _leasing?.clear();
      _leaseFiles?.clear();
      _payments?.clear();
      _expenses?.clear();
      _notifications?.clear();
      _notificationsController?.reset();
      _tenantPortal?.clear();
      _tenantUtilities?.reset();
      _propertyScope = scope;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          _navigator.currentState?.popUntil((route) => route.isFirst);
        }
      });
    }
    setState(() {
      if (!_hasLocalLanguage) {
        final language = _session?.user?.preferredLanguage.toLowerCase();
        _locale = language?.startsWith('en') == true
            ? const Locale('en')
            : const Locale('ar');
      }
    });
  }

  ThemeMode get _themeMode => switch (_theme) {
    AqariThemePreference.light => ThemeMode.light,
    AqariThemePreference.dark => ThemeMode.dark,
    AqariThemePreference.system => ThemeMode.system,
  };

  @override
  Widget build(BuildContext context) => MaterialApp(
    debugShowCheckedModeBanner: false,
    navigatorKey: _navigator,
    title: 'AqariOS',
    theme: AqariTheme.light(_locale),
    darkTheme: AqariTheme.dark(_locale),
    themeMode: _themeMode,
    locale: _locale,
    supportedLocales: const [Locale('ar'), Locale('en')],
    localizationsDelegates: const [
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    routes: {
      '/design-system': (_) => DesignSystemPlayground(
        themePreference: _theme,
        locale: _locale,
        onThemeChanged: _setTheme,
        onLocaleChanged: _setLocale,
      ),
    },
    onGenerateRoute: (settings) {
      final uri = Uri.tryParse(settings.name ?? '');
      final token = uri?.queryParameters['token']?.trim();
      if (uri?.path == '/auth/activate' &&
          token != null &&
          token.isNotEmpty &&
          _session != null) {
        return MaterialPageRoute<void>(
          settings: settings,
          builder: (_) => AuthFlowPage(
            session: _session!,
            flow: AuthFlow.activation,
            activationToken: token,
          ),
        );
      }
      if (uri?.path == '/auth/reset-password' &&
          token != null &&
          token.isNotEmpty &&
          _session != null) {
        return MaterialPageRoute<void>(
          settings: settings,
          builder: (_) =>
              PasswordEntryPage(session: _session!, credential: token),
        );
      }
      return null;
    },
    home: _buildHome(),
  );

  Widget _buildHome() {
    final session = _session;
    if (session == null) return const _ConfigurationScreen();
    return switch (session.status) {
      SessionStatus.initializing => const Scaffold(
        body: Center(child: AqariLoadingState(label: 'جارٍ استعادة الجلسة')),
      ),
      SessionStatus.bootstrapFailure => _BootstrapErrorScreen(session: session),
      SessionStatus.unauthenticated => LoginScreen(session: session),
      SessionStatus.authenticated => AppShell(
        session: session,
        dashboardController: _dashboard!,
        propertiesRepository: _properties!,
        leasingRepository: _leasing!,
        leaseFiles: _leaseFiles!,
        paymentsRepository: _payments!,
        paymentFiles: _paymentFiles!,
        expensesRepository: _expenses!,
        notificationsController: _notificationsController!,
        tenantPortalRepository: _tenantPortal!,
        tenantUtilityController: _tenantUtilities!,
        themePreference: _theme,
        locale: _locale,
        onThemeChanged: _setTheme,
        onLocaleChanged: _setLocale,
      ),
      SessionStatus.unsupportedRole => _UnsupportedRoleScreen(session: session),
    };
  }
}

class _BootstrapErrorScreen extends StatelessWidget {
  const _BootstrapErrorScreen({required this.session});
  final SessionController session;
  @override
  Widget build(BuildContext context) => Scaffold(
    body: Center(
      child: AqariErrorState(
        title: 'تعذر استعادة الجلسة',
        message: session.error?.message ?? 'حدث خطأ غير متوقع.',
        retryLabel: 'إعادة المحاولة',
        onRetry: session.bootstrap,
      ),
    ),
  );
}

class _ConfigurationScreen extends StatelessWidget {
  const _ConfigurationScreen();
  @override
  Widget build(BuildContext context) => const Scaffold(
    body: Center(
      child: Padding(
        padding: EdgeInsets.all(AqariSpacing.x6),
        child: AqariErrorState(
          title: 'إعداد الخادم مطلوب',
          message:
              'شغّل التطبيق مع --dart-define=AQARIOS_API_BASE_URL=https://your-api-host. لا يحتوي التطبيق على عنوان إنتاجي أو أسرار مضمّنة.',
        ),
      ),
    ),
  );
}

class _UnsupportedRoleScreen extends StatelessWidget {
  const _UnsupportedRoleScreen({required this.session});
  final SessionController session;
  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(AqariSpacing.x6),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const AqariErrorState(
                title: 'الدور غير مدعوم',
                message: 'لا يرتبط هذا الحساب بدور جوّال مدعوم حالياً.',
              ),
              AqariButton(
                label: 'تسجيل الخروج',
                variant: AqariButtonVariant.outlined,
                onPressed: session.logout,
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
