import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../application/dashboard_controller.dart';
import '../domain/dashboard_summary.dart';

class CompanyDashboardScreen extends StatefulWidget {
  const CompanyDashboardScreen({required this.controller, super.key});
  final DashboardController controller;

  @override
  State<CompanyDashboardScreen> createState() => _CompanyDashboardScreenState();
}

class _CompanyDashboardScreenState extends State<CompanyDashboardScreen> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_refresh);
    widget.controller.load();
  }

  @override
  void dispose() {
    widget.controller.removeListener(_refresh);
    super.dispose();
  }

  void _refresh() {
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final state = widget.controller;
    if (state.summary == null && state.error == null) {
      return const Center(
        child: AqariLoadingState(label: 'جارٍ تحميل لوحة المعلومات'),
      );
    }
    if (state.error != null && state.summary == null) {
      return Center(
        child: AqariErrorState(
          title: 'تعذر تحميل لوحة المعلومات',
          message: state.error!.message,
          retryLabel: 'إعادة المحاولة',
          onRetry: () => state.load(force: true),
        ),
      );
    }
    final summary = state.summary!;
    return RefreshIndicator(
      onRefresh: () => state.load(force: true),
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(AqariSpacing.page),
        children: [
          if (state.error != null)
            AqariErrorState(
              title: 'تعذر تحديث البيانات',
              message: state.error!.message,
              retryLabel: 'إعادة المحاولة',
              onRetry: () => state.load(force: true),
            ),
          Text(
            'نظرة عامة على المحفظة',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: AqariSpacing.x1),
          Text(
            'بيانات محدثة من حساب الشركة النشط',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: context.aqariColors.textSecondary,
            ),
          ),
          const SizedBox(height: AqariSpacing.section),
          _PropertySection(summary: summary),
          const SizedBox(height: AqariSpacing.section),
          _LeasingSection(summary: summary),
          const SizedBox(height: AqariSpacing.section),
          _FinanceSection(summary: summary),
          if (summary.overduePayments > 0 || summary.expiringIn30Days > 0) ...[
            const SizedBox(height: AqariSpacing.section),
            _AttentionSection(summary: summary),
          ],
        ],
      ),
    );
  }
}

class _PropertySection extends StatelessWidget {
  const _PropertySection({required this.summary});
  final DashboardSummary summary;
  @override
  Widget build(BuildContext context) => AqariCard(
    variant: AqariCardVariant.elevated,
    child: Column(
      children: [
        const AqariSectionHeader(title: 'العقارات والوحدات'),
        Row(
          children: [
            Expanded(
              child: AqariKpi(
                label: 'المباني',
                value: '${summary.totalBuildings}',
                icon: Icons.apartment_rounded,
              ),
            ),
            Expanded(
              child: AqariKpi(
                label: 'الوحدات',
                value: '${summary.totalApartments}',
                icon: Icons.domain_rounded,
              ),
            ),
          ],
        ),
        const AqariDivider(),
        Row(
          children: [
            Expanded(
              child: AqariKpi(
                label: 'مشغولة',
                value: '${summary.occupiedApartments}',
              ),
            ),
            Expanded(
              child: AqariKpi(
                label: 'شاغرة',
                value: '${summary.vacantApartments}',
              ),
            ),
            Expanded(
              child: AqariKpi(
                label: 'الإشغال',
                value: '${summary.occupancyRate.toStringAsFixed(1)}%',
              ),
            ),
          ],
        ),
      ],
    ),
  );
}

class _LeasingSection extends StatelessWidget {
  const _LeasingSection({required this.summary});
  final DashboardSummary summary;
  @override
  Widget build(BuildContext context) => AqariCard(
    variant: AqariCardVariant.elevated,
    child: Column(
      children: [
        const AqariSectionHeader(title: 'التأجير'),
        Row(
          children: [
            Expanded(
              child: AqariKpi(
                label: 'عقود نشطة',
                value: '${summary.activeLeases}',
              ),
            ),
            Expanded(
              child: AqariKpi(
                label: 'جديدة هذا الشهر',
                value: '${summary.newLeasesThisMonth}',
              ),
            ),
            Expanded(
              child: AqariKpi(
                label: 'تنتهي خلال 30 يوماً',
                value: '${summary.expiringIn30Days}',
              ),
            ),
          ],
        ),
      ],
    ),
  );
}

class _FinanceSection extends StatelessWidget {
  const _FinanceSection({required this.summary});
  final DashboardSummary summary;
  String _money(double value) => '${value.toStringAsFixed(2)} د.أ';
  @override
  Widget build(BuildContext context) => AqariCard(
    variant: AqariCardVariant.elevated,
    child: Column(
      children: [
        const AqariSectionHeader(title: 'التحصيل والمالية'),
        AqariKpi(
          label: 'المحصّل هذا الشهر',
          value: _money(summary.collectedThisMonth),
          icon: Icons.payments_outlined,
        ),
        const AqariDivider(),
        AqariKpi(
          label: 'المبلغ المستحق',
          value: _money(summary.outstandingAmount),
        ),
        const AqariDivider(),
        AqariKpi(
          label: 'مصروفات هذا الشهر',
          value: _money(summary.expensesThisMonth),
        ),
      ],
    ),
  );
}

class _AttentionSection extends StatelessWidget {
  const _AttentionSection({required this.summary});
  final DashboardSummary summary;
  @override
  Widget build(BuildContext context) => AqariCard(
    variant: AqariCardVariant.filled,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const AqariSectionHeader(title: 'تحتاج إلى انتباه'),
        if (summary.overduePayments > 0)
          AqariStatusBadge(
            label: '${summary.overduePayments} دفعات متأخرة',
            variant: AqariStatusVariant.warning,
          ),
        if (summary.overduePayments > 0 && summary.expiringIn30Days > 0)
          const SizedBox(height: AqariSpacing.x2),
        if (summary.expiringIn30Days > 0)
          AqariStatusBadge(
            label: '${summary.expiringIn30Days} عقود تنتهي قريباً',
            variant: AqariStatusVariant.info,
          ),
      ],
    ),
  );
}
