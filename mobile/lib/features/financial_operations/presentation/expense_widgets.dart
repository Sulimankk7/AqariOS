import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../payments/presentation/payment_widgets.dart';

String expenseCategoryText(BuildContext context, int category) {
  final labels = [
    ('المبنى', 'Building'),
    ('مشترك', 'Shared'),
    ('طارئ', 'Emergency'),
    ('مرافق المناطق المشتركة', 'Common area utilities'),
    ('صيانة', 'Maintenance'),
    ('تنظيف', 'Cleaning'),
    ('أمن', 'Security'),
    ('مصعد', 'Elevator'),
    ('خزان مياه', 'Water tank'),
    ('مولد', 'Generator'),
    ('إداري', 'Administrative'),
    ('أخرى', 'Other'),
  ];
  return category < 0 || category >= labels.length
      ? pt(context, 'غير معروف', 'Unknown')
      : pt(context, labels[category].$1, labels[category].$2);
}

String expenseMethodText(BuildContext context, int method) {
  final labels = [
    ('نقداً', 'Cash'),
    ('تحويل بنكي', 'Bank transfer'),
    ('شيك', 'Cheque'),
    ('أخرى', 'Other'),
  ];
  return method < 0 || method >= labels.length
      ? pt(context, 'غير معروف', 'Unknown')
      : pt(context, labels[method].$1, labels[method].$2);
}

class ExpenseSkeleton extends StatelessWidget {
  const ExpenseSkeleton({super.key});

  @override
  Widget build(BuildContext context) => Semantics(
    label: pt(context, 'جارٍ تحميل المصاريف', 'Loading expenses'),
    child: ListView(
      padding: const EdgeInsets.all(16),
      children: List.generate(
        5,
        (_) => const Padding(
          padding: EdgeInsets.only(bottom: 16),
          child: AqariSkeleton(height: 72),
        ),
      ),
    ),
  );
}

class ExpenseFailure extends StatelessWidget {
  const ExpenseFailure(this.error, {this.retry, super.key});

  final Object? error;
  final VoidCallback? retry;

  @override
  Widget build(BuildContext context) => AqariErrorState(
    title: pt(context, 'تعذر تحميل المصاريف', 'Could not load expenses'),
    message:
        error?.toString() ?? pt(context, 'حدث خطأ.', 'Something went wrong.'),
    retryLabel: pt(context, 'إعادة المحاولة', 'Retry'),
    onRetry: retry,
  );
}
