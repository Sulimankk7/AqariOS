import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../data/tenant_portal_repository.dart';
import '../domain/tenant_portal_models.dart';

class TenantLeaseScreen extends StatefulWidget {
  const TenantLeaseScreen({
    required this.repository,
    this.onOpenPayments,
    super.key,
  });
  final TenantPortalRepository repository;
  final VoidCallback? onOpenPayments;

  @override
  State<TenantLeaseScreen> createState() => _TenantLeaseScreenState();
}

class _TenantLeaseScreenState extends State<TenantLeaseScreen> {
  TenantLease? _lease;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({bool force = false}) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final value = await widget.repository.currentLease(force: force);
      if (mounted) {
        setState(() => _lease = value);
      }
    } catch (error) {
      if (mounted) {
        setState(() => _error = error);
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return AqariLoadingState(
        label: _t(context, 'جارٍ تحميل عقد الإيجار', 'Loading lease'),
      );
    }
    if (_error != null) {
      return Center(
        child: AqariErrorState(
          title: _t(context, 'تعذر تحميل عقد الإيجار', 'Could not load lease'),
          message: _error is ApiProblem
              ? (_error as ApiProblem).message
              : _t(
                  context,
                  'حدث خطأ غير متوقع.',
                  'An unexpected error occurred.',
                ),
          retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
          onRetry: () => _load(force: true),
        ),
      );
    }
    final lease = _lease;
    if (lease == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(AqariSpacing.page),
          child: AqariEmptyState(
            title: _t(context, 'لا يوجد عقد إيجار نشط', 'No active lease'),
            message: _t(
              context,
              'لا يوجد عقد حالي مرتبط بحسابك.',
              'There is no current lease linked to your account.',
            ),
          ),
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: () => _load(force: true),
      child: ListView(
        key: const PageStorageKey('tenant-lease'),
        padding: const EdgeInsets.all(AqariSpacing.page),
        children: [
          AqariCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        lease.contractNumber,
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                    ),
                    _status(context, lease.status),
                  ],
                ),
                const SizedBox(height: AqariSpacing.x2),
                Text(
                  '${lease.buildingName} • ${_t(context, 'الوحدة', 'Unit')} ${lease.apartmentUnitNumber}',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: context.aqariColors.textSecondary,
                  ),
                ),
                if (widget.onOpenPayments != null) ...[
                  const SizedBox(height: AqariSpacing.x4),
                  AqariButton(
                    label: _t(context, 'عرض الدفعات', 'View payments'),
                    icon: Icons.payments_outlined,
                    variant: AqariButtonVariant.tonal,
                    expanded: true,
                    onPressed: widget.onOpenPayments,
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: AqariSpacing.section),
          _section(context, _t(context, 'تفاصيل العقد', 'Contract details'), [
            _row(
              context,
              _t(context, 'رقم العقد', 'Contract number'),
              lease.contractNumber,
              ltr: true,
            ),
            _row(context, _t(context, 'الحالة', 'Status'), lease.status),
            _row(
              context,
              _t(context, 'تاريخ البدء', 'Start date'),
              _date(context, lease.startDate),
            ),
            _row(
              context,
              _t(context, 'تاريخ الانتهاء', 'End date'),
              _date(context, lease.endDate),
            ),
            _row(
              context,
              _t(context, 'تاريخ التوقيع', 'Signed date'),
              lease.signedDate == null
                  ? '—'
                  : _date(context, lease.signedDate!),
            ),
            _row(
              context,
              _t(context, 'نظام الدفعات', 'Payment frequency'),
              lease.paymentFrequency,
            ),
            _row(
              context,
              _t(context, 'يوم الاستحقاق', 'Payment due day'),
              '${lease.paymentDueDay}',
            ),
            _row(
              context,
              _t(context, 'النظام القانوني', 'Legal regime'),
              lease.legalRegime,
            ),
            _row(
              context,
              _t(context, 'نوع المستأجر', 'Tenant type'),
              lease.tenantType,
            ),
          ]),
          const SizedBox(height: AqariSpacing.section),
          _section(
            context,
            _t(context, 'العقار والوحدة', 'Property and unit'),
            [
              _row(
                context,
                _t(context, 'المبنى', 'Building'),
                lease.buildingName,
              ),
              _row(
                context,
                _t(context, 'الوحدة', 'Unit'),
                lease.apartmentUnitNumber,
              ),
              _row(
                context,
                _t(context, 'غرف النوم', 'Bedrooms'),
                '${lease.apartmentBedrooms}',
              ),
              _row(
                context,
                _t(context, 'الحمامات', 'Bathrooms'),
                '${lease.apartmentBathrooms}',
              ),
              _row(
                context,
                _t(context, 'المساحة', 'Area'),
                '${lease.apartmentAreaSqm} ${_t(context, 'م²', 'm²')}',
              ),
            ],
          ),
          const SizedBox(height: AqariSpacing.section),
          _section(context, _t(context, 'القيم المالية', 'Financial terms'), [
            _row(
              context,
              _t(context, 'الإيجار الشهري', 'Monthly rent'),
              _money(context, lease.monthlyRentAmount, lease.currency),
            ),
            _row(
              context,
              _t(context, 'التأمين', 'Security deposit'),
              _money(context, lease.securityDepositAmount, lease.currency),
            ),
            _row(context, _t(context, 'العملة', 'Currency'), lease.currency),
          ]),
          const SizedBox(height: AqariSpacing.x8),
        ],
      ),
    );
  }

  Widget _section(BuildContext context, String title, List<Widget> rows) =>
      Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AqariSectionHeader(title: title),
          AqariCard(child: Column(children: rows)),
        ],
      );
  Widget _row(
    BuildContext context,
    String label,
    String value, {
    bool ltr = false,
  }) => AqariDetailRow(label: label, value: value, ltr: ltr);
  Widget _status(BuildContext context, String value) {
    final lower = value.toLowerCase();
    final variant = lower.contains('active')
        ? AqariStatusVariant.success
        : lower.contains('termin') || lower.contains('cancel')
        ? AqariStatusVariant.error
        : AqariStatusVariant.neutral;
    return AqariStatusBadge(label: value, variant: variant);
  }
}

String _t(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;
String _date(BuildContext context, String value) {
  final date = DateTime.tryParse(value);
  return date == null
      ? '—'
      : DateFormat.yMMMd(context.isArabic ? 'ar' : 'en').format(date);
}

String _money(BuildContext context, num value, String currency) =>
    NumberFormat.currency(
      locale: context.isArabic ? 'ar' : 'en',
      name: currency,
      decimalDigits: 3,
    ).format(value);
