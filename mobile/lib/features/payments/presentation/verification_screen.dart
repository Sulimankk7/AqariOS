import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/payment_models.dart';
import 'payment_scope.dart';
import 'payment_widgets.dart';
import 'payment_detail_screen.dart';

class VerificationScreen extends StatefulWidget {
  const VerificationScreen({required this.scope, super.key});
  final PaymentScope scope;
  @override
  State<VerificationScreen> createState() => _VerificationScreenState();
}

class _VerificationScreenState extends State<VerificationScreen> {
  List<VerificationItem> items = [];
  String? cursor;
  bool hasMore = false, busy = false;
  Object? error;
  int generation = 0;
  @override
  void initState() {
    super.initState();
    if (widget.scope.can('payments.approve')) load();
  }

  Future<void> load({bool more = false}) async {
    if (busy && more) return;
    final g = ++generation;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final page = await widget.scope.repository.queue(more ? cursor : null);
      if (mounted && g == generation) {
        setState(() {
          final values = more ? [...items, ...page.items] : page.items;
          final ids = <String>{};
          items = values.where((v) => ids.add(v.submissionId)).toList();
          cursor = page.nextCursor;
          hasMore = page.hasMore && cursor != null;
        });
      }
    } catch (e) {
      if (mounted && g == generation) setState(() => error = e);
    } finally {
      if (mounted && g == generation) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!widget.scope.can('payments.approve')) {
      return AqariEmptyState(
        title: pt(context, 'لا توجد صلاحية لعرض الدفعات', 'No payment access'),
        message: pt(
          context,
          'تواصل مع مسؤول الشركة بشأن الصلاحيات.',
          'Contact your company administrator about permissions.',
        ),
      );
    }
    if (busy && items.isEmpty) return const PaymentSkeleton();
    return SafeArea(
      top: false,
      child: RefreshIndicator(
        onRefresh: load,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: AqariSectionHeader(
                title: pt(context, 'بانتظار المراجعة', 'Awaiting review'),
                actionLabel: pt(context, 'تحديث', 'Refresh'),
                onAction: busy ? null : load,
              ),
            ),
            if (error != null)
              PaymentFailure(error, retry: () => load(more: items.isNotEmpty)),
            if (!busy && error == null && items.isEmpty)
              AqariEmptyState(
                title: pt(
                  context,
                  'لا توجد طلبات تحقق معلّقة',
                  'No pending verifications',
                ),
                message: pt(
                  context,
                  'ستظهر الطلبات الجديدة هنا.',
                  'New submissions will appear here.',
                ),
              ),
            for (final item in items)
              Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AqariSpacing.x4,
                ),
                child: FinancialRecordTile(
                  title: item.text('tenantName').isEmpty
                      ? pt(context, 'مستأجر غير معروف', 'Unknown tenant')
                      : item.text('tenantName'),
                  subtitle:
                      '${item.text('buildingName')} · ${isolate(item.text('apartmentNumber'))}',
                  amount: paymentMoney(item.submitted, item.currency),
                  amountLabel: pt(context, 'المبلغ المرسل', 'Submitted amount'),
                  status: const PaymentBadge(0, kind: 'submission'),
                  date:
                      '${isolate(item.text('contractNumber'))} · ${paymentDate(item.text('submittedAt'))}',
                  onPressed: () async {
                    await Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => PaymentDetailScreen(
                          scope: widget.scope,
                          id: item.paymentId,
                          verification: item,
                        ),
                      ),
                    );
                    if (mounted) load();
                  },
                ),
              ),
            if (hasMore)
              Padding(
                padding: const EdgeInsets.all(16),
                child: AqariButton(
                  label: pt(context, 'تحميل المزيد', 'Load more'),
                  loading: busy,
                  onPressed: busy ? null : () => load(more: true),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
