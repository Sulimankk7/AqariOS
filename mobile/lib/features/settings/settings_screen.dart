import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';
import '../auth/application/session_controller.dart';
import 'company_profile_screen.dart';
import 'operational_settings_screen.dart';
import 'settings.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({
    required this.repository,
    required this.session,
    required this.themePreference,
    required this.locale,
    required this.onThemeChanged,
    required this.onLocaleChanged,
    super.key,
  });

  final SettingsRepository repository;
  final SessionController session;
  final AqariThemePreference themePreference;
  final Locale locale;
  final ValueChanged<AqariThemePreference> onThemeChanged;
  final ValueChanged<Locale> onLocaleChanged;

  void _push(BuildContext context, Widget child) =>
      Navigator.push<void>(context, MaterialPageRoute(builder: (_) => child));

  @override
  Widget build(BuildContext context) {
    final user = session.user!;
    return Scaffold(
      appBar: AqariAppBar(
        title: context.isArabic ? 'الإعدادات' : 'Settings',
        onBack: () => Navigator.pop(context),
      ),
      body: ListView(
        padding: const EdgeInsets.all(AqariSpacing.page),
        children: [
          AqariSectionHeader(title: context.isArabic ? 'الشركة' : 'Company'),
          AqariListRow(
            title: context.isArabic ? 'بيانات الشركة' : 'Company profile',
            supportingText: context.isArabic
                ? 'عرض وتعديل بيانات الشركة المدعومة'
                : 'View and edit supported company information',
            leading: const Icon(Icons.business_outlined),
            showChevron: true,
            onPressed: () => _push(
              context,
              CompanyProfileScreen(repository: repository, user: user),
            ),
          ),
          AqariListRow(
            title: context.isArabic
                ? 'إعدادات التشغيل'
                : 'Operational settings',
            supportingText: context.isArabic
                ? 'إعدادات الإيجار والمالية'
                : 'Leasing and finance settings',
            leading: const Icon(Icons.tune_rounded),
            showChevron: true,
            onPressed: () => _push(
              context,
              OperationalSettingsScreen(repository: repository, user: user),
            ),
          ),
          const SizedBox(height: AqariSpacing.section),
          AqariSectionHeader(
            title: context.isArabic
                ? 'المظهر واللغة'
                : 'Appearance and language',
          ),
          AqariSelect<AqariThemePreference>(
            label: context.isArabic ? 'مظهر التطبيق' : 'App appearance',
            valueLabel: _themeLabel(themePreference, context.isArabic),
            items: AqariThemePreference.values,
            itemLabel: (value) => _themeLabel(value, context.isArabic),
            onChanged: onThemeChanged,
          ),
          const SizedBox(height: AqariSpacing.x3),
          AqariSelect<Locale>(
            label: context.isArabic ? 'لغة التطبيق' : 'App language',
            valueLabel: locale.languageCode == 'ar' ? 'العربية' : 'English',
            items: const [Locale('ar'), Locale('en')],
            itemLabel: (value) =>
                value.languageCode == 'ar' ? 'العربية' : 'English',
            onChanged: onLocaleChanged,
          ),
          const SizedBox(height: AqariSpacing.section),
          AqariSectionHeader(
            title: context.isArabic ? 'الحساب والأمان' : 'Account and security',
          ),
          AqariListRow(
            title: user.fullName,
            supportingText: user.email ?? user.phone,
            leading: AqariAvatar(
              label: user.fullName.isEmpty ? '?' : user.fullName,
            ),
          ),
          AqariButton(
            label: context.isArabic ? 'تسجيل الخروج' : 'Sign out',
            variant: AqariButtonVariant.outlined,
            onPressed: session.logout,
          ),
          const SizedBox(height: AqariSpacing.x2),
          AqariButton(
            label: context.isArabic
                ? 'تسجيل الخروج من جميع الأجهزة'
                : 'Sign out on all devices',
            variant: AqariButtonVariant.destructive,
            onPressed: () => _logoutAll(context),
          ),
        ],
      ),
    );
  }

  static String _themeLabel(AqariThemePreference value, bool arabic) =>
      switch (value) {
        AqariThemePreference.light => arabic ? 'فاتح' : 'Light',
        AqariThemePreference.dark => arabic ? 'داكن' : 'Dark',
        AqariThemePreference.system => arabic ? 'النظام' : 'System',
      };

  Future<void> _logoutAll(BuildContext context) async {
    final confirmed = await AqariDialog.show<bool>(
      context: context,
      title: 'تسجيل الخروج من جميع الأجهزة',
      content: const Text('سيتم إلغاء جلسات تسجيل الدخول على جميع الأجهزة.'),
      secondaryLabel: 'إلغاء',
      primaryLabel: 'تسجيل الخروج',
      onPrimary: () => Navigator.pop(context, true),
    );
    if (confirmed != true) return;
    try {
      await session.logoutAll();
    } catch (error) {
      if (context.mounted) {
        AqariSnackbar.show(
          context,
          error.toString(),
          kind: AqariStatusKind.error,
        );
      }
    }
  }
}
