import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../auth/application/session_controller.dart';
import '../../auth/domain/user_profile.dart';
import '../../dashboard/application/dashboard_controller.dart';
import '../../dashboard/presentation/company_dashboard_screen.dart';
import '../../properties/data/properties_repository.dart';
import '../../properties/presentation/properties_landing.dart';
import '../../leasing/data/leasing_repository.dart';
import '../../leasing/data/lease_files.dart';
import '../../leasing/presentation/lease_scope.dart';
import '../../leasing/presentation/leasing_landing.dart';
import '../../payments/data/payments_repository.dart';
import '../../payments/data/payment_files.dart';
import '../../payments/presentation/payment_scope.dart';
import '../../payments/presentation/payments_screen.dart';
import '../../financial_operations/data/expenses_repository.dart';
import '../../financial_operations/presentation/financial_operations_scope.dart';
import '../../financial_operations/presentation/financial_operations_screen.dart';
import '../../notifications/application/notifications_controller.dart';
import '../../notifications/presentation/notifications_screen.dart';
import '../../tenant_portal/data/tenant_portal_repository.dart';
import '../../tenant_portal/presentation/tenant_dashboard_screen.dart';
import '../../tenant_portal/presentation/tenant_lease_screen.dart';
import '../../tenant_portal/presentation/tenant_profile_screen.dart';
import '../../tenant_portal/utilities/tenant_utilities_screen.dart';
import '../../tenant_portal/utilities/tenant_utility_controller.dart';
import '../../settings/settings.dart';
import '../../settings/settings_screen.dart';

const ownerNavigationLabels = [
  'الرئيسية',
  'العقارات',
  'التأجير',
  'المالية',
  'المزيد',
];

const tenantNavigationLabels = [
  'الرئيسية',
  'عقد الإيجار',
  'الدفعات',
  'الخدمات',
  'الملف الشخصي',
];

class AppShell extends StatefulWidget {
  const AppShell({
    required this.session,
    required this.dashboardController,
    required this.propertiesRepository,
    required this.leasingRepository,
    required this.leaseFiles,
    required this.paymentsRepository,
    required this.paymentFiles,
    required this.expensesRepository,
    required this.notificationsController,
    required this.tenantPortalRepository,
    required this.tenantUtilityController,
    required this.themePreference,
    required this.locale,
    required this.onThemeChanged,
    required this.onLocaleChanged,
    super.key,
  });
  final SessionController session;
  final DashboardController dashboardController;
  final PropertiesRepository propertiesRepository;
  final LeasingRepository leasingRepository;
  final LeaseFiles leaseFiles;
  final PaymentsRepository paymentsRepository;
  final PaymentFiles paymentFiles;
  final ExpensesRepository expensesRepository;
  final NotificationsController notificationsController;
  final TenantPortalRepository tenantPortalRepository;
  final TenantUtilityController tenantUtilityController;
  final AqariThemePreference themePreference;
  final Locale locale;
  final ValueChanged<AqariThemePreference> onThemeChanged;
  final ValueChanged<Locale> onLocaleChanged;

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  int _selected = 0;

  @override
  Widget build(BuildContext context) {
    final user = widget.session.user!;
    final destinations = _destinations(user);
    if (_selected >= destinations.length) _selected = 0;
    final current = destinations[_selected];
    return Scaffold(
      appBar: AqariAppBar(
        title: current.label,
        actions: [
          NotificationsEntry(
            key: ValueKey('notifications:${user.id}:${user.activeCompanyId}'),
            controller: widget.notificationsController,
          ),
          if (user.role == AqariRole.tenant) _tenantAccountMenu(),
        ],
      ),
      body: current.content,
      bottomNavigationBar: AqariBottomNavigation(
        items: destinations
            .map(
              (item) => AqariNavigationDestination(
                label: item.label,
                icon: item.icon,
              ),
            )
            .toList(growable: false),
        selectedIndex: _selected,
        onSelected: (value) => setState(() => _selected = value),
      ),
    );
  }

  List<_ShellDestination> _destinations(
    UserProfile user,
  ) => switch (user.role) {
    AqariRole.companyAdmin => [
      _ShellDestination(
        context.isArabic ? ownerNavigationLabels[0] : 'Home',
        Icons.dashboard_outlined,
        CompanyDashboardScreen(controller: widget.dashboardController),
      ),
      _ShellDestination(
        context.isArabic ? ownerNavigationLabels[1] : 'Properties',
        Icons.apartment_outlined,
        PropertiesLanding(
          repository: widget.propertiesRepository,
          user: user,
          leaseScope: LeaseScope(
            repository: widget.leasingRepository,
            properties: widget.propertiesRepository,
            user: user,
            files: widget.leaseFiles,
          ),
        ),
      ),
      _ShellDestination(
        context.isArabic ? ownerNavigationLabels[2] : 'Leasing',
        Icons.description_outlined,
        LeasingLanding(
          key: ValueKey('leasing:${user.id}:${user.activeCompanyId}'),
          scope: LeaseScope(
            repository: widget.leasingRepository,
            properties: widget.propertiesRepository,
            user: user,
            files: widget.leaseFiles,
          ),
        ),
      ),
      _ShellDestination(
        context.isArabic ? ownerNavigationLabels[3] : 'Finance',
        Icons.account_balance_wallet_outlined,
        FinancialOperationsScreen(
          key: ValueKey('financials:${user.id}:${user.activeCompanyId}'),
          scope: _financialScope(user),
        ),
      ),
      _ShellDestination(
        context.isArabic ? ownerNavigationLabels[4] : 'More',
        Icons.more_horiz_rounded,
        _more(user),
      ),
    ],
    AqariRole.tenant => [
      _ShellDestination(
        context.isArabic ? tenantNavigationLabels[0] : 'Home',
        Icons.dashboard_outlined,
        TenantDashboardScreen(
          key: ValueKey('tenant-dashboard:${user.id}:${user.activeCompanyId}'),
          user: user,
          repository: widget.tenantPortalRepository,
          utilities: widget.tenantUtilityController,
          notifications: widget.notificationsController,
          onOpenProfile: () => setState(() => _selected = 4),
          onOpenPayments: () => setState(() => _selected = 2),
          onOpenUtilities: () => setState(() => _selected = 3),
        ),
      ),
      _ShellDestination(
        context.isArabic ? tenantNavigationLabels[1] : 'Lease',
        Icons.description_outlined,
        TenantLeaseScreen(
          key: ValueKey('tenant-lease:${user.id}:${user.activeCompanyId}'),
          repository: widget.tenantPortalRepository,
          onOpenPayments: () => setState(() => _selected = 2),
        ),
      ),
      _ShellDestination(
        context.isArabic ? tenantNavigationLabels[2] : 'Payments',
        Icons.payments_outlined,
        PaymentsScreen(
          key: ValueKey('tenant-payments:${user.id}:${user.activeCompanyId}'),
          scope: _paymentScope(user),
          tenant: true,
        ),
      ),
      _ShellDestination(
        context.isArabic ? tenantNavigationLabels[3] : 'Utilities',
        Icons.receipt_long_outlined,
        TenantUtilitiesScreen(
          key: ValueKey('tenant-utilities:${user.id}:${user.activeCompanyId}'),
          controller: widget.tenantUtilityController,
        ),
      ),
      _ShellDestination(
        context.isArabic ? tenantNavigationLabels[4] : 'Profile',
        Icons.person_outline,
        TenantProfileScreen(
          key: ValueKey('tenant-profile:${user.id}:${user.activeCompanyId}'),
          repository: widget.tenantPortalRepository,
        ),
      ),
    ],
    AqariRole.systemAdmin => [
      const _ShellDestination(
        'المنصة',
        Icons.dashboard_outlined,
        _UnavailableFeature(title: 'إدارة المنصة'),
      ),
      const _ShellDestination(
        'التسجيلات',
        Icons.business_outlined,
        _UnavailableFeature(title: 'طلبات التسجيل'),
      ),
      const _ShellDestination(
        'الخطط',
        Icons.layers_outlined,
        _UnavailableFeature(title: 'خطط الاشتراك'),
      ),
      _ShellDestination('المزيد', Icons.more_horiz_rounded, _more(user)),
    ],
    AqariRole.unsupported => const [],
  };

  Widget _tenantAccountMenu() => AqariPopupMenu<_TenantAccountAction>(
    label: context.isArabic ? 'الحساب والمظهر' : 'Account and appearance',
    items: [
      AqariPopupMenuAction(
        value: _TenantAccountAction.language,
        label: context.isArabic ? 'English' : 'العربية',
        icon: Icons.language_rounded,
      ),
      AqariPopupMenuAction(
        value: _TenantAccountAction.appearance,
        label: context.isArabic ? 'تغيير المظهر' : 'Change appearance',
        icon: Icons.contrast_rounded,
      ),
      AqariPopupMenuAction(
        value: _TenantAccountAction.logout,
        label: context.isArabic ? 'تسجيل الخروج' : 'Sign out',
        icon: Icons.logout_rounded,
      ),
    ],
    onSelected: (value) {
      switch (value) {
        case _TenantAccountAction.language:
          widget.onLocaleChanged(Locale(context.isArabic ? 'en' : 'ar'));
        case _TenantAccountAction.appearance:
          widget.onThemeChanged(switch (widget.themePreference) {
            AqariThemePreference.system => AqariThemePreference.light,
            AqariThemePreference.light => AqariThemePreference.dark,
            AqariThemePreference.dark => AqariThemePreference.system,
          });
        case _TenantAccountAction.logout:
          widget.session.logout();
      }
    },
  );

  Widget _more(UserProfile user) => _MoreScreen(
    session: widget.session,
    user: user,
    settings: SettingsRepository(widget.propertiesRepository.client),
    themePreference: widget.themePreference,
    locale: widget.locale,
    onThemeChanged: widget.onThemeChanged,
    onLocaleChanged: widget.onLocaleChanged,
  );

  PaymentScope _paymentScope(UserProfile user) => PaymentScope(
    repository: widget.paymentsRepository,
    files: widget.paymentFiles,
    leasing: LeaseScope(
      repository: widget.leasingRepository,
      properties: widget.propertiesRepository,
      user: user,
      files: widget.leaseFiles,
    ),
    onChanged: () {
      if (user.role == AqariRole.companyAdmin) {
        widget.dashboardController.load(force: true);
      }
    },
  );

  FinancialOperationsScope _financialScope(UserProfile user) {
    final leasing = LeaseScope(
      repository: widget.leasingRepository,
      properties: widget.propertiesRepository,
      user: user,
      files: widget.leaseFiles,
    );
    return FinancialOperationsScope(
      payments: PaymentScope(
        repository: widget.paymentsRepository,
        files: widget.paymentFiles,
        leasing: leasing,
        onChanged: () {
          if (user.role == AqariRole.companyAdmin) {
            widget.dashboardController.load(force: true);
          }
        },
      ),
      expenses: widget.expensesRepository,
      leasing: leasing,
      files: widget.paymentFiles,
    );
  }
}

enum _TenantAccountAction { language, appearance, logout }

class _ShellDestination {
  const _ShellDestination(this.label, this.icon, this.content);
  final String label;
  final IconData icon;
  final Widget content;
}

class _UnavailableFeature extends StatelessWidget {
  const _UnavailableFeature({required this.title});
  final String title;
  @override
  Widget build(BuildContext context) => Center(
    child: AqariEmptyState(
      title: '$title قيد التنفيذ',
      message:
          'هذه الوجهة مرتبطة بعقد حقيقي وستُنفّذ في المرحلة المخصصة لها دون بيانات تجريبية.',
    ),
  );
}

class _MoreScreen extends StatelessWidget {
  const _MoreScreen({
    required this.session,
    required this.user,
    required this.settings,
    required this.themePreference,
    required this.locale,
    required this.onThemeChanged,
    required this.onLocaleChanged,
  });
  final SessionController session;
  final UserProfile user;
  final SettingsRepository settings;
  final AqariThemePreference themePreference;
  final Locale locale;
  final ValueChanged<AqariThemePreference> onThemeChanged;
  final ValueChanged<Locale> onLocaleChanged;
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(AqariSpacing.page),
    children: [
      AqariSectionHeader(title: context.isArabic ? 'الحساب' : 'Account'),
      AqariCard(
        child: Row(
          children: [
            AqariAvatar(label: user.fullName.isEmpty ? 'م' : user.fullName),
            const SizedBox(width: AqariSpacing.x3),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    user.fullName,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  if (user.email != null)
                    Text(
                      user.email!,
                      textDirection: TextDirection.ltr,
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: context.aqariColors.textSecondary,
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: AqariSpacing.section),
      AqariListRow(
        title: context.isArabic ? 'الإعدادات' : 'Settings',
        supportingText: context.isArabic
            ? 'الشركة والتشغيل والمظهر والأمان'
            : 'Company, operations, appearance and security',
        leading: const Icon(Icons.settings_outlined),
        showChevron: true,
        onPressed: () => Navigator.push<void>(
          context,
          MaterialPageRoute(
            builder: (_) => SettingsScreen(
              repository: settings,
              session: session,
              themePreference: themePreference,
              locale: locale,
              onThemeChanged: onThemeChanged,
              onLocaleChanged: onLocaleChanged,
            ),
          ),
        ),
      ),
      const SizedBox(height: AqariSpacing.x3),
      AqariButton(
        label: context.isArabic ? 'تسجيل الخروج' : 'Sign out',
        variant: AqariButtonVariant.destructive,
        icon: Icons.logout_rounded,
        expanded: true,
        loading: session.submitting,
        onPressed: session.logout,
      ),
    ],
  );
}
