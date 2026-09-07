import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';
import '../../core/network/api_problem.dart';
import '../auth/domain/user_profile.dart';
import 'settings.dart';

class CompanyProfileScreen extends StatefulWidget {
  const CompanyProfileScreen({
    required this.repository,
    required this.user,
    super.key,
  });
  final SettingsRepository repository;
  final UserProfile user;

  @override
  State<CompanyProfileScreen> createState() => _CompanyProfileScreenState();
}

class _CompanyProfileScreenState extends State<CompanyProfileScreen> {
  CompanyProfile? company;
  Object? error;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final value = await widget.repository.company();
      if (value.id != widget.user.activeCompanyId) {
        throw const ApiProblem(
          message: 'لا يمكن الوصول إلى بيانات هذه الشركة.',
        );
      }
      if (mounted) setState(() => company = value);
    } catch (value) {
      if (mounted) setState(() => error = value);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: 'بيانات الشركة',
      onBack: () => Navigator.pop(context),
    ),
    body: loading
        ? const AqariLoadingState(label: 'جارٍ تحميل بيانات الشركة')
        : error != null
        ? AqariErrorState(
            title: 'تعذر تحميل بيانات الشركة',
            message: error.toString(),
            retryLabel: 'إعادة المحاولة',
            onRetry: load,
          )
        : RefreshIndicator(
            onRefresh: load,
            child: ListView(
              padding: const EdgeInsets.all(AqariSpacing.page),
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                const AqariSectionHeader(title: 'هوية الشركة'),
                AqariSectionSurface(
                  child: Column(
                    children: [
                      _fact(context, 'الاسم القانوني', company!.legalName),
                      _fact(context, 'اسم العرض', company!.displayName),
                      _fact(context, 'الهاتف الرئيسي', company!.phone),
                      _fact(
                        context,
                        'البريد الإلكتروني',
                        company!.email ?? '—',
                        ltr: true,
                      ),
                      const AqariDivider(),
                      _fact(
                        context,
                        'رقم السجل التجاري',
                        company!.registration ?? '—',
                      ),
                      _fact(
                        context,
                        'الرقم الضريبي',
                        company!.taxNumber ?? '—',
                      ),
                      _fact(
                        context,
                        'نوع الشركة',
                        const [
                              'مالك فردي',
                              'شركة إدارة',
                              'شركة استثمار',
                            ].elementAtOrNull(company!.companyType) ??
                            '—',
                      ),
                      _fact(
                        context,
                        'رمز الدولة',
                        company!.countryCode,
                        ltr: true,
                      ),
                      _fact(
                        context,
                        'الحالة',
                        company!.active ? 'نشطة' : 'غير نشطة',
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
    bottomNavigationBar:
        !loading && error == null && widget.user.hasPermission('company.manage')
        ? AqariActionBar(
            child: AqariButton(
              label: 'تعديل بيانات الشركة',
              expanded: true,
              onPressed: () async {
                final changed = await Navigator.push<bool>(
                  context,
                  MaterialPageRoute(
                    builder: (_) => CompanyProfileEditScreen(
                      repository: widget.repository,
                      company: company!,
                    ),
                  ),
                );
                if (changed == true) await load();
              },
            ),
          )
        : null,
  );
}

Widget _fact(
  BuildContext context,
  String label,
  String value, {
  bool ltr = false,
}) => AqariDetailRow(label: label, value: value, ltr: ltr);

class CompanyProfileEditScreen extends StatefulWidget {
  const CompanyProfileEditScreen({
    required this.repository,
    required this.company,
    super.key,
  });
  final SettingsRepository repository;
  final CompanyProfile company;

  @override
  State<CompanyProfileEditScreen> createState() =>
      _CompanyProfileEditScreenState();
}

class _CompanyProfileEditScreenState extends State<CompanyProfileEditScreen> {
  late final legal = TextEditingController(text: widget.company.legalName);
  late final display = TextEditingController(text: widget.company.displayName);
  late final phone = TextEditingController(text: widget.company.phone);
  late final email = TextEditingController(text: widget.company.email ?? '');
  String? legalError, displayError, phoneError, emailError;
  Object? error;
  bool saving = false;

  @override
  void dispose() {
    legal.dispose();
    display.dispose();
    phone.dispose();
    email.dispose();
    super.dispose();
  }

  Future<void> save() async {
    final emailValue = email.text.trim();
    setState(() {
      legalError = legal.text.trim().isEmpty ? 'هذا الحقل مطلوب.' : null;
      displayError = display.text.trim().isEmpty ? 'هذا الحقل مطلوب.' : null;
      phoneError = phone.text.trim().isEmpty ? 'هذا الحقل مطلوب.' : null;
      emailError =
          emailValue.isNotEmpty &&
              !RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(emailValue)
          ? 'أدخل بريداً إلكترونياً صالحاً.'
          : null;
      error = null;
    });
    if ([
      legalError,
      displayError,
      phoneError,
      emailError,
    ].any((value) => value != null)) {
      return;
    }
    setState(() => saving = true);
    try {
      await widget.repository.updateCompany(
        widget.company.id,
        legalName: legal.text,
        displayName: display.text,
        phone: phone.text,
        email: email.text,
      );
      if (mounted) {
        AqariSnackbar.show(context, 'تم حفظ بيانات الشركة.');
        Navigator.pop(context, true);
      }
    } catch (value) {
      if (mounted) setState(() => error = value);
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: 'تعديل بيانات الشركة',
      onBack: () => Navigator.pop(context),
    ),
    body: ListView(
      padding: const EdgeInsets.all(AqariSpacing.page),
      children: [
        const AqariSectionHeader(title: 'البيانات الأساسية'),
        AqariTextField(
          controller: legal,
          label: 'الاسم القانوني',
          errorText: legalError,
        ),
        const SizedBox(height: AqariSpacing.x3),
        AqariTextField(
          controller: display,
          label: 'اسم العرض',
          errorText: displayError,
        ),
        const SizedBox(height: AqariSpacing.x3),
        AqariTextField(
          controller: phone,
          label: 'الهاتف الرئيسي',
          keyboardType: TextInputType.phone,
          errorText: phoneError,
        ),
        const SizedBox(height: AqariSpacing.x3),
        AqariTextField(
          controller: email,
          label: 'البريد الإلكتروني (اختياري)',
          keyboardType: TextInputType.emailAddress,
          errorText: emailError,
        ),
        if (error != null)
          Padding(
            padding: const EdgeInsets.only(top: AqariSpacing.x3),
            child: Text(
              error.toString(),
              style: TextStyle(color: context.aqariColors.error),
            ),
          ),
      ],
    ),
    bottomNavigationBar: AqariActionBar(
      child: AqariButton(
        label: 'حفظ',
        expanded: true,
        loading: saving,
        onPressed: saving ? null : save,
      ),
    ),
  );
}
