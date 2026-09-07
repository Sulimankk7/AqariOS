import 'dart:async';
import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../properties/domain/property_models.dart';
import '../application/payments_controller.dart';
import '../domain/payment_models.dart';
import 'payment_scope.dart';
import 'payment_widgets.dart';
import 'payment_detail_screen.dart';
import 'verification_screen.dart';

class PaymentsScreen extends StatefulWidget {
  const PaymentsScreen({required this.scope, this.tenant = false, super.key});
  final PaymentScope scope;
  final bool tenant;
  @override
  State<PaymentsScreen> createState() => _PaymentsScreenState();
}

class _PaymentsScreenState extends State<PaymentsScreen> {
  late final PaymentsController controller;
  final search = TextEditingController();
  Timer? debounce;
  @override
  void initState() {
    super.initState();
    controller = PaymentsController(
      widget.scope.repository,
      tenant: widget.tenant,
    );
    if (widget.tenant || widget.scope.can('payments.read')) controller.load();
  }

  @override
  void dispose() {
    debounce?.cancel();
    search.dispose();
    controller.dispose();
    super.dispose();
  }

  void apply(PaymentFilters value) {
    debounce?.cancel();
    controller.apply(value);
  }

  void reset() {
    search.clear();
    apply(const PaymentFilters());
  }

  Future<void> details(RentPayment payment) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => PaymentDetailScreen(
          scope: widget.scope,
          id: payment.id,
          tenant: widget.tenant,
          initial: payment,
        ),
      ),
    );
    if (changed == true && mounted) await controller.load();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: controller,
    builder: (context, _) {
      if (!widget.tenant && !widget.scope.can('payments.read')) {
        return VerificationScreen(scope: widget.scope);
      }
      final items = controller.items;
      final rows = <Widget>[
        if (widget.tenant && items.isNotEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Wrap(
              spacing: 24,
              children: [
                PaymentFact(
                  pt(context, 'إجمالي المستحق', 'Total due'),
                  paymentMoney(
                    items.fold<num>(0, (sum, p) => sum + p.due),
                    items.first.currency,
                  ),
                  ltr: true,
                ),
                PaymentFact(
                  pt(context, 'إجمالي المدفوع', 'Total paid'),
                  paymentMoney(
                    items.fold<num>(0, (sum, p) => sum + p.paid),
                    items.first.currency,
                  ),
                  ltr: true,
                ),
                PaymentFact(
                  pt(context, 'متأخرة', 'Overdue'),
                  '${items.where((p) => [4, 5].contains(p.status)).length}',
                  ltr: true,
                ),
              ],
            ),
          ),
        if (items.isEmpty)
          Padding(
            padding: const EdgeInsets.all(24),
            child: AqariEmptyState(
              title: pt(context, 'لا توجد دفعات', 'No payments'),
              message: pt(
                context,
                'لا توجد دفعات مطابقة للعرض الحالي.',
                'No payments match this view.',
              ),
            ),
          ),
      ];
      final sections = widget.tenant ? [-1] : [0, 1, 2, -1];
      for (final section in sections) {
        final group = widget.tenant
            ? items
            : items.where((p) => p.purpose == section).toList();
        if (group.isEmpty) continue;
        if (!widget.tenant) {
          rows.add(
            Padding(
              padding: const EdgeInsets.all(16),
              child: AqariSectionHeader(
                title: statusText(context, section, kind: 'purpose'),
              ),
            ),
          );
        }
        for (final p in group) {
          rows.add(
            _PaymentRow(
              payment: p,
              onPressed: () => details(p),
              tenant: widget.tenant,
            ),
          );
        }
      }
      if (!widget.tenant) {
        rows.add(
          Padding(
            padding: const EdgeInsets.all(16),
            child: Wrap(
              spacing: 12,
              runSpacing: 12,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                Text(
                  pt(
                    context,
                    'الصفحة ${controller.page}',
                    'Page ${controller.page}',
                  ),
                ),
                AqariButton(
                  label: pt(context, 'السابق', 'Previous'),
                  variant: AqariButtonVariant.outlined,
                  onPressed: controller.hasPrevious && !controller.loading
                      ? controller.previous
                      : null,
                ),
                AqariButton(
                  label: pt(context, 'التالي', 'Next'),
                  variant: AqariButtonVariant.outlined,
                  onPressed: controller.hasNext && !controller.loading
                      ? controller.next
                      : null,
                ),
              ],
            ),
          ),
        );
      }
      return SafeArea(
        top: false,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(12),
              child: Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  IconButton(
                    tooltip: pt(context, 'تحديث', 'Refresh'),
                    icon: const Icon(Icons.refresh),
                    onPressed: controller.loading ? null : controller.load,
                  ),
                  if (!widget.tenant)
                    AqariButton(
                      label: pt(context, 'تصفية', 'Filters'),
                      icon: Icons.filter_list,
                      variant: AqariButtonVariant.outlined,
                      onPressed: () async {
                        final result =
                            await AqariBottomSheet.show<PaymentFilters>(
                              context: context,
                              title: pt(
                                context,
                                'تصفية الدفعات',
                                'Filter payments',
                              ),
                              child: _Filters(
                                scope: widget.scope,
                                filters: controller.filters,
                              ),
                            );
                        if (result != null && mounted) {
                          search.text = result.search;
                          apply(result);
                        }
                      },
                    ),
                  if (!widget.tenant &&
                      (search.text.isNotEmpty ||
                          controller.filters.buildingId.isNotEmpty ||
                          controller.filters.status >= 0 ||
                          controller.filters.from.isNotEmpty ||
                          controller.filters.to.isNotEmpty))
                    AqariButton(
                      label: pt(context, 'مسح', 'Reset'),
                      variant: AqariButtonVariant.text,
                      onPressed: reset,
                    ),
                  if (!widget.tenant && widget.scope.can('payments.approve'))
                    AqariButton(
                      label: pt(context, 'طلبات التحقق', 'Verifications'),
                      icon: Icons.fact_check_outlined,
                      variant: AqariButtonVariant.text,
                      onPressed: () async {
                        await Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => Scaffold(
                              appBar: AqariAppBar(
                                title: pt(
                                  context,
                                  'طلبات التحقق',
                                  'Verifications',
                                ),
                                onBack: () => Navigator.pop(context),
                              ),
                              body: VerificationScreen(scope: widget.scope),
                            ),
                          ),
                        );
                        if (mounted) controller.load();
                      },
                    ),
                ],
              ),
            ),
            if (!widget.tenant) ...[
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: AqariSearchField(
                  controller: search,
                  hint: pt(
                    context,
                    'المستأجر أو العقد أو السند',
                    'Tenant, contract or receipt',
                  ),
                  onChanged: (value) {
                    debounce?.cancel();
                    debounce = Timer(const Duration(milliseconds: 350), () {
                      final f = controller.filters;
                      apply(
                        PaymentFilters(
                          buildingId: f.buildingId,
                          status: f.status,
                          from: f.from,
                          to: f.to,
                          search: value,
                        ),
                      );
                    });
                  },
                ),
              ),
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 8,
                ),
                child: Row(
                  children: [
                    for (final s in [-1, 1, 5, 3])
                      Padding(
                        padding: const EdgeInsetsDirectional.only(end: 8),
                        child: ChoiceChip(
                          label: Text(
                            s == -1
                                ? pt(context, 'الكل', 'All')
                                : statusText(context, s),
                          ),
                          selected: controller.filters.status == s,
                          onSelected: (_) {
                            final f = controller.filters;
                            apply(
                              PaymentFilters(
                                buildingId: f.buildingId,
                                status: s,
                                from: f.from,
                                to: f.to,
                                search: search.text,
                              ),
                            );
                          },
                        ),
                      ),
                  ],
                ),
              ),
            ],
            Expanded(
              child: controller.loading
                  ? const PaymentSkeleton()
                  : controller.error != null
                  ? ListView(
                      children: [
                        PaymentFailure(
                          controller.error,
                          retry: controller.load,
                        ),
                      ],
                    )
                  : RefreshIndicator(
                      onRefresh: controller.load,
                      child: ListView.builder(
                        physics: const AlwaysScrollableScrollPhysics(),
                        keyboardDismissBehavior:
                            ScrollViewKeyboardDismissBehavior.onDrag,
                        itemCount: rows.length,
                        itemBuilder: (_, i) => rows[i],
                      ),
                    ),
            ),
          ],
        ),
      );
    },
  );
}

class _PaymentRow extends StatelessWidget {
  const _PaymentRow({
    required this.payment,
    required this.onPressed,
    required this.tenant,
  });
  final RentPayment payment;
  final VoidCallback onPressed;
  final bool tenant;
  @override
  Widget build(BuildContext context) {
    final p = payment;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AqariSpacing.x4),
      child: FinancialRecordTile(
        title: tenant
            ? (p.contractNumber.isEmpty ? '—' : p.contractNumber)
            : (p.tenantName.isEmpty ? '—' : p.tenantName),
        subtitle: '${p.buildingName} · ${isolate(p.apartmentNumber)}',
        date: p.purpose == 0
            ? paymentDate(p.dueDate)
            : paymentDate(p.text('createdAt')),
        onPressed: onPressed,
        status: p.purpose == 0 ? PaymentBadge(p.status) : null,
        amountLabel: pt(
          context,
          p.purpose == 0 ? 'المتبقي' : 'المبلغ',
          p.purpose == 0 ? 'Remaining' : 'Amount',
        ),
        amount: p.purpose != 0
            ? paymentMoney(p.due, p.currency)
            : tenant
            ? paymentMoney(p.remaining, p.currency)
            : p.settlement == null
            ? '—'
            : paymentMoney(p.settlement!.remaining, p.currency),
        details: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (p.purpose == 0)
              Text(
                '${pt(context, 'المبلغ المستحق', 'Amount due')}: ${paymentMoney(p.due, p.currency)}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            if (p.latestStatus == 2)
              Text(
                pt(context, 'رُفض طلب الدفع', 'Submission rejected'),
                style: TextStyle(color: context.aqariColors.error),
              ),
          ],
        ),
      ),
    );
  }
}

class _Filters extends StatefulWidget {
  const _Filters({required this.scope, required this.filters});
  final PaymentScope scope;
  final PaymentFilters filters;
  @override
  State<_Filters> createState() => _FiltersState();
}

class _FiltersState extends State<_Filters> {
  late String building, from, to;
  late int status;
  List<Building> buildings = [];
  Object? error;
  bool loading = true;
  @override
  void initState() {
    super.initState();
    building = widget.filters.buildingId;
    from = widget.filters.from;
    to = widget.filters.to;
    status = widget.filters.status;
    load();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final value = await widget.scope.leasing.properties.buildings();
      if (mounted) setState(() => buildings = value);
    } catch (e) {
      if (mounted) setState(() => error = e);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => ConstrainedBox(
    constraints: BoxConstraints(
      maxHeight: MediaQuery.sizeOf(context).height * .65,
    ),
    child: SingleChildScrollView(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (error != null) PaymentFailure(error, retry: load),
          AqariSelect<String>(
            label: pt(context, 'المبنى', 'Building'),
            valueLabel:
                buildings.where((b) => b.id == building).firstOrNull?.name ??
                pt(context, 'كل المباني', 'All buildings'),
            items: ['', ...buildings.map((b) => b.id)],
            itemLabel: (id) => id.isEmpty
                ? pt(context, 'كل المباني', 'All buildings')
                : buildings.firstWhere((b) => b.id == id).name,
            enabled: !loading,
            onChanged: (v) => setState(() => building = v),
          ),
          const SizedBox(height: 12),
          AqariSelect<int>(
            label: pt(context, 'الحالة', 'Status'),
            valueLabel: status < 0
                ? pt(context, 'الكل', 'All')
                : statusText(context, status),
            items: [-1, 0, 3, 4, 5, 2, 1, 6],
            itemLabel: (v) =>
                v < 0 ? pt(context, 'الكل', 'All') : statusText(context, v),
            onChanged: (v) => setState(() => status = v),
          ),
          PaymentDateField(
            label: pt(context, 'تاريخ الاستحقاق من', 'Due date from'),
            value: from,
            onChanged: (v) => setState(() => from = v),
          ),
          PaymentDateField(
            label: pt(context, 'تاريخ الاستحقاق إلى', 'Due date to'),
            value: to,
            onChanged: (v) => setState(() => to = v),
          ),
          AqariButton(
            label: pt(context, 'مسح التواريخ', 'Clear dates'),
            variant: AqariButtonVariant.text,
            onPressed: () => setState(() {
              from = '';
              to = '';
            }),
          ),
          AqariButton(
            label: pt(context, 'تطبيق', 'Apply'),
            onPressed: () => Navigator.pop(
              context,
              PaymentFilters(
                buildingId: building,
                status: status,
                from: from,
                to: to,
                search: widget.filters.search,
              ),
            ),
          ),
        ],
      ),
    ),
  );
}
