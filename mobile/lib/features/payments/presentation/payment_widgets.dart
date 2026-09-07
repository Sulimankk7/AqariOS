import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/payment_errors.dart';

String pt(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;
String isolate(String value) => '\u2068$value\u2069';
String paymentMoney(num value, String currency) =>
    isolate('${NumberFormat('#,##0.00', 'en').format(value)} $currency');
String paymentDate(String value) {
  final date = DateTime.tryParse(value);
  return date == null ? '—' : isolate(DateFormat('yyyy-MM-dd').format(date));
}

String statusText(BuildContext context, int status, {String kind = 'due'}) {
  if (kind == 'method' && status < 0) return '—';
  final labels = kind == 'submission'
      ? [
          ('بانتظار المراجعة', 'Pending'),
          ('مقبول', 'Approved'),
          ('مرفوض', 'Rejected'),
        ]
      : kind == 'method'
      ? [
          ('نقداً', 'Cash'),
          ('تحويل بنكي', 'Bank transfer'),
          ('شيك', 'Cheque'),
          ('إي فواتيركم', 'Efawateercom'),
          ('كليك', 'CliQ'),
        ]
      : kind == 'purpose'
      ? [
          ('الأقساط المستحقة', 'Scheduled obligations'),
          ('الدفعات المستلمة', 'Received payments'),
          ('تسوية دائنة', 'Adjustment credit'),
          ('تسوية مدينة', 'Adjustment debit'),
        ]
      : kind == 'cheque'
      ? [
          ('صادر', 'Issued'),
          ('مستلم', 'Received'),
          ('مودع', 'Deposited'),
          ('محصّل', 'Cleared'),
          ('مرتجع', 'Bounced'),
          ('ملغى', 'Cancelled'),
        ]
      : [
          ('مستحق', 'Pending'),
          ('بانتظار التحقق', 'Pending verification'),
          ('مدفوع', 'Paid'),
          ('مدفوع جزئياً', 'Partially paid'),
          ('متأخر', 'Late'),
          ('متأخر غير مدفوع', 'Overdue unpaid'),
          ('ملغى', 'Cancelled'),
        ];
  return status < 0 || status >= labels.length
      ? pt(context, 'غير معروف', 'Unknown')
      : pt(context, labels[status].$1, labels[status].$2);
}

class PaymentBadge extends StatelessWidget {
  const PaymentBadge(this.status, {this.kind = 'due', super.key});
  final int status;
  final String kind;
  @override
  Widget build(BuildContext context) => AqariStatusBadge(
    label: statusText(context, status, kind: kind),
    variant:
        (kind == 'due' && [4, 5].contains(status)) ||
            (kind == 'submission' && status == 2)
        ? AqariStatusVariant.error
        : (kind == 'due' && status == 2) ||
              (kind == 'submission' && status == 1)
        ? AqariStatusVariant.success
        : AqariStatusVariant.neutral,
  );
}

class PaymentFact extends StatelessWidget {
  const PaymentFact(this.label, this.value, {this.ltr = false, super.key});
  final String label, value;
  final bool ltr;
  @override
  Widget build(BuildContext context) => AqariDetailRow(
    label: label,
    value: value.isEmpty ? '—' : value,
    ltr: ltr,
  );
}

/// The financial-card hierarchy from the mobile design-system package.
class FinancialRecordTile extends StatelessWidget {
  const FinancialRecordTile({
    required this.title,
    required this.subtitle,
    required this.amount,
    required this.amountLabel,
    required this.date,
    this.status,
    this.details,
    this.onPressed,
    super.key,
  });

  final String title, subtitle, amount, amountLabel, date;
  final Widget? status, details;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x3),
    child: AqariSectionSurface(
      padding: EdgeInsets.zero,
      child: InkWell(
        onTap: onPressed,
        borderRadius: AqariRadius.mdBorder,
        child: Padding(
          padding: const EdgeInsets.all(AqariSpacing.x4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        if (subtitle.isNotEmpty) ...[
                          const SizedBox(height: AqariSpacing.x1),
                          Text(
                            subtitle,
                            style: Theme.of(context).textTheme.bodySmall
                                ?.copyWith(
                                  color: context.aqariColors.textSecondary,
                                ),
                          ),
                        ],
                      ],
                    ),
                  ),
                  if (onPressed != null)
                    Icon(
                      context.isArabic
                          ? Icons.chevron_left
                          : Icons.chevron_right,
                      color: context.aqariColors.textMuted,
                      size: 20,
                    ),
                ],
              ),
              if (status != null) ...[
                const SizedBox(height: AqariSpacing.x2),
                Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: status!,
                ),
              ],
              const SizedBox(height: AqariSpacing.x3),
              Wrap(
                alignment: WrapAlignment.spaceBetween,
                crossAxisAlignment: WrapCrossAlignment.end,
                spacing: AqariSpacing.x4,
                runSpacing: AqariSpacing.x2,
                children: [
                  Text(
                    date,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: context.aqariColors.textSecondary,
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        amountLabel,
                        style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          color: context.aqariColors.textMuted,
                        ),
                      ),
                      AqariLtrContent(
                        child: Text(
                          amount,
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
              if (details != null) ...[
                const SizedBox(height: AqariSpacing.x2),
                details!,
              ],
            ],
          ),
        ),
      ),
    ),
  );
}

class FinancialAmountSummary extends StatelessWidget {
  const FinancialAmountSummary({
    required this.label,
    required this.amount,
    this.subtitle,
    this.status,
    this.details = const [],
    super.key,
  });
  final String label, amount;
  final String? subtitle;
  final Widget? status;
  final List<Widget> details;

  @override
  Widget build(BuildContext context) => AqariSectionSurface(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (status != null)
          Align(alignment: AlignmentDirectional.centerStart, child: status!),
        const SizedBox(height: AqariSpacing.x2),
        Text(
          label,
          style: Theme.of(context).textTheme.labelMedium?.copyWith(
            color: context.aqariColors.textSecondary,
          ),
        ),
        const SizedBox(height: AqariSpacing.x1),
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: AqariLtrContent(
            child: Text(
              amount,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
          ),
        ),
        if (subtitle != null) ...[
          const SizedBox(height: AqariSpacing.x2),
          Text(subtitle!, style: Theme.of(context).textTheme.bodyMedium),
        ],
        if (details.isNotEmpty) ...[
          const SizedBox(height: AqariSpacing.x3),
          const AqariDivider(),
          ...details,
        ],
      ],
    ),
  );
}

class PaymentFailure extends StatelessWidget {
  const PaymentFailure(this.error, {this.retry, super.key});
  final Object? error;
  final VoidCallback? retry;
  @override
  Widget build(BuildContext context) => AqariErrorState(
    title: pt(context, 'تعذر إكمال الطلب', 'Request failed'),
    message: paymentError(error, context.isArabic),
    onRetry: retry,
    retryLabel: pt(context, 'إعادة المحاولة', 'Retry'),
  );
}

class PaymentSkeleton extends StatelessWidget {
  const PaymentSkeleton({super.key});
  @override
  Widget build(BuildContext context) => Semantics(
    label: pt(context, 'جارٍ تحميل الدفعات', 'Loading payments'),
    child: ListView(
      padding: const EdgeInsets.all(16),
      children: List.generate(
        5,
        (_) => const Padding(
          padding: EdgeInsets.only(bottom: 16),
          child: AqariSkeleton(height: 70),
        ),
      ),
    ),
  );
}

class PaymentLoad<T> extends StatefulWidget {
  const PaymentLoad({required this.load, required this.builder, super.key});
  final Future<T> Function() load;
  final Widget Function(T, Future<void> Function()) builder;
  @override
  State<PaymentLoad<T>> createState() => _PaymentLoadState<T>();
}

class _PaymentLoadState<T> extends State<PaymentLoad<T>> {
  T? data;
  Object? error;
  bool busy = true;
  int generation = 0;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    final g = ++generation;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final value = await widget.load();
      if (mounted && g == generation) setState(() => data = value);
    } catch (e) {
      if (mounted && g == generation) setState(() => error = e);
    } finally {
      if (mounted && g == generation) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => busy
      ? const PaymentSkeleton()
      : error != null
      ? SingleChildScrollView(child: PaymentFailure(error, retry: load))
      : widget.builder(data as T, load);
}

class PaymentDateField extends StatelessWidget {
  const PaymentDateField({
    required this.label,
    required this.value,
    required this.onChanged,
    this.error,
    this.enabled = true,
    super.key,
  });
  final String label, value;
  final ValueChanged<String> onChanged;
  final String? error;
  final bool enabled;
  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      AqariListRow(
        title: label,
        supportingText: paymentDate(value),
        leading: const Icon(Icons.calendar_today_outlined),
        enabled: enabled,
        onPressed: () async {
          final initial = DateTime.tryParse(value) ?? DateTime.now();
          final result = await showDatePicker(
            context: context,
            initialDate: initial,
            firstDate: DateTime(1900),
            lastDate: DateTime(2200),
          );
          if (result != null) {
            onChanged(DateFormat('yyyy-MM-dd').format(result));
          }
        },
      ),
      if (error != null)
        Text(error!, style: TextStyle(color: context.aqariColors.error)),
    ],
  );
}

Future<bool> paymentConfirm(
  BuildContext context, {
  required String title,
  required Future<void> Function(String) action,
  bool reason = false,
}) async =>
    await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) =>
          _Confirmation(title: title, action: action, reason: reason),
    ) ??
    false;

class _Confirmation extends StatefulWidget {
  const _Confirmation({
    required this.title,
    required this.action,
    required this.reason,
  });
  final String title;
  final Future<void> Function(String) action;
  final bool reason;
  @override
  State<_Confirmation> createState() => _ConfirmationState();
}

class _ConfirmationState extends State<_Confirmation> {
  final reason = TextEditingController();
  bool busy = false;
  Object? error;
  @override
  void dispose() {
    reason.dispose();
    super.dispose();
  }

  Future<void> submit() async {
    if (busy) return;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await widget.action(reason.text.trim());
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() => error = e);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !busy,
    child: AlertDialog(
      scrollable: true,
      title: Text(widget.title),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            pt(
              context,
              'تأكد من تفاصيل الدفعة قبل التأكيد. لا يمكن التراجع عن هذا الإجراء من هنا.',
              'Check the payment before confirming. This action cannot be undone here.',
            ),
          ),
          if (widget.reason)
            AqariTextField(
              label: pt(context, 'سبب الرفض *', 'Rejection reason *'),
              controller: reason,
              maxLines: 4,
              enabled: !busy,
              onChanged: (_) => setState(() {}),
            ),
          if (error != null) PaymentFailure(error),
        ],
      ),
      actions: [
        AqariButton(
          label: pt(context, 'إلغاء', 'Cancel'),
          variant: AqariButtonVariant.text,
          onPressed: busy ? null : () => Navigator.pop(context, false),
        ),
        AqariButton(
          label: widget.title,
          loading: busy,
          variant: widget.reason
              ? AqariButtonVariant.destructive
              : AqariButtonVariant.primary,
          onPressed: busy || (widget.reason && reason.text.trim().isEmpty)
              ? null
              : submit,
        ),
      ],
    ),
  );
}
