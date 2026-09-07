import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';
import '../../core/network/api_problem.dart';
import '../auth/domain/user_profile.dart';
import 'settings.dart';

class OperationalSettingsScreen extends StatefulWidget {
  const OperationalSettingsScreen({
    required this.repository,
    required this.user,
    super.key,
  });
  final SettingsRepository repository;
  final UserProfile user;

  @override
  State<OperationalSettingsScreen> createState() =>
      _OperationalSettingsScreenState();
}

class _OperationalSettingsScreenState extends State<OperationalSettingsScreen> {
  CompanyProfile? company;
  CompanyOperationalSettings? settings;
  Object? error;
  bool loading = true, saving = false;
  final grace = TextEditingController(),
      fee = TextEditingController(),
      month = TextEditingController();
  int feeType = 0;
  String? graceError, feeError, monthError;

  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  void dispose() {
    grace.dispose();
    fee.dispose();
    month.dispose();
    super.dispose();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final profile = await widget.repository.company();
      if (profile.id != widget.user.activeCompanyId) {
        throw const ApiProblem(
          message: 'لا يمكن الوصول إلى إعدادات هذه الشركة.',
        );
      }
      final value = await widget.repository.operations(profile.id);
      if (!mounted) return;
      company = profile;
      settings = value;
      feeType = value.lateFeeType;
      grace.text = '${value.graceDays}';
      month.text = '${value.fiscalMonth}';
      fee.text = value.lateFeeValue?.toString() ?? '';
    } catch (value) {
      if (mounted) error = value;
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> save() async {
    final graceValue = int.tryParse(grace.text.trim());
    final monthValue = int.tryParse(month.text.trim());
    final feeValue = double.tryParse(fee.text.trim());
    setState(() {
      graceError = graceValue == null || graceValue < 0 || graceValue > 32767
          ? 'أدخل عدداً صحيحاً يساوي صفراً أو أكبر.'
          : null;
      monthError = monthValue == null || monthValue < 1 || monthValue > 12
          ? 'أدخل شهراً من 1 إلى 12.'
          : null;
      feeError = feeType != 0 && (feeValue == null || feeValue <= 0)
          ? 'أدخل قيمة أكبر من صفر.'
          : null;
      error = null;
    });
    if ([graceError, monthError, feeError].any((value) => value != null)) {
      return;
    }
    setState(() => saving = true);
    try {
      await widget.repository.updateOperations(
        company!.id,
        graceDays: graceValue!,
        lateFeeType: feeType,
        lateFeeValue: feeType == 0 ? null : feeValue,
        fiscalMonth: monthValue!,
      );
      if (mounted) {
        AqariSnackbar.show(context, 'تم حفظ إعدادات التشغيل.');
        await load();
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
      title: 'إعدادات التشغيل',
      onBack: () => Navigator.pop(context),
    ),
    body: loading
        ? const AqariLoadingState(label: 'جارٍ تحميل إعدادات التشغيل')
        : error != null && settings == null
        ? AqariErrorState(
            title: 'تعذر تحميل إعدادات التشغيل',
            message: error.toString(),
            retryLabel: 'إعادة المحاولة',
            onRetry: load,
          )
        : ListView(
            padding: const EdgeInsets.all(AqariSpacing.page),
            children: [
              const AqariSectionHeader(title: 'الإيجار والمالية'),
              AqariTextField(
                controller: grace,
                label: 'فترة السماح للإيجار — عدد الأيام',
                keyboardType: TextInputType.number,
                errorText: graceError,
                enabled: widget.user.hasPermission('company.manage'),
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariSelect<int>(
                label: 'نوع غرامة التأخير',
                valueLabel: const ['بدون', 'مبلغ ثابت', 'نسبة مئوية'][feeType],
                items: const [0, 1, 2],
                itemLabel: (value) =>
                    const ['بدون', 'مبلغ ثابت', 'نسبة مئوية'][value],
                enabled: widget.user.hasPermission('company.manage'),
                onChanged: (value) => setState(() {
                  feeType = value;
                  if (value == 0) {
                    fee.clear();
                    feeError = null;
                  }
                }),
              ),
              if (feeType != 0) ...[
                const SizedBox(height: AqariSpacing.x3),
                AqariTextField(
                  controller: fee,
                  label: 'قيمة غرامة التأخير',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  errorText: feeError,
                  enabled: widget.user.hasPermission('company.manage'),
                ),
              ],
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: month,
                label: 'بداية السنة المالية — الشهر',
                keyboardType: TextInputType.number,
                errorText: monthError,
                enabled: widget.user.hasPermission('company.manage'),
              ),
              const SizedBox(height: AqariSpacing.section),
              const AqariSectionHeader(title: 'إعدادات الشركة الافتراضية'),
              AqariSectionSurface(
                child: Column(
                  children: [
                    _readonly(context, 'العملة الافتراضية', settings!.currency),
                    _readonly(context, 'لغة الشركة', settings!.language),
                    _readonly(context, 'المنطقة الزمنية', settings!.timezone),
                  ],
                ),
              ),
              if (error != null)
                Text(
                  error.toString(),
                  style: TextStyle(color: context.aqariColors.error),
                ),
            ],
          ),
    bottomNavigationBar:
        !loading &&
            settings != null &&
            widget.user.hasPermission('company.manage')
        ? AqariActionBar(
            child: AqariButton(
              label: 'حفظ',
              expanded: true,
              loading: saving,
              onPressed: saving ? null : save,
            ),
          )
        : null,
  );
}

Widget _readonly(BuildContext context, String label, String value) => Padding(
  padding: const EdgeInsets.symmetric(vertical: AqariSpacing.x2),
  child: Row(
    children: [
      Expanded(child: Text(label)),
      Flexible(child: AqariLtrContent(child: Text(value))),
    ],
  ),
);
