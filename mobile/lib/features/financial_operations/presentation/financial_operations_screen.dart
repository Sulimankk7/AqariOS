import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../payments/presentation/payment_widgets.dart';
import '../../payments/presentation/payments_screen.dart';
import 'expenses_screen.dart';
import 'financial_operations_scope.dart';
import 'utility_accounts_screen.dart';

const financeNavigationLabels = [
  'السجلات المالية',
  'المصاريف',
  'فواتير الخدمات',
];

class FinancialOperationsScreen extends StatefulWidget {
  const FinancialOperationsScreen({required this.scope, super.key});

  final FinancialOperationsScope scope;

  @override
  State<FinancialOperationsScreen> createState() =>
      _FinancialOperationsScreenState();
}

class _FinancialOperationsScreenState extends State<FinancialOperationsScreen> {
  int selected = 0;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      Padding(
        padding: const EdgeInsetsDirectional.fromSTEB(16, 12, 16, 0),
        child: Align(
          alignment: AlignmentDirectional.centerStart,
          child: Text(
            pt(context, 'العمليات المالية', 'Financial Operations'),
            style: Theme.of(context).textTheme.titleMedium,
          ),
        ),
      ),
      AqariTabs(
        labels: context.isArabic
            ? financeNavigationLabels
            : const ['Financial records', 'Expenses', 'Utility bills'],
        selectedIndex: selected,
        onSelected: (value) => setState(() => selected = value),
      ),
      Expanded(
        child: IndexedStack(
          index: selected,
          children: [
            PaymentsScreen(scope: widget.scope.payments),
            ExpensesScreen(scope: widget.scope),
            widget.scope.leasing.user.hasPermission('UtilityBills.Manage')
                ? UtilityAccountsScreen(scope: widget.scope.leasing)
                : const Center(
                    child: AqariEmptyState(
                      title: 'لا يوجد وصول',
                      message: 'لا تملك صلاحية إدارة فواتير الخدمات.',
                    ),
                  ),
          ],
        ),
      ),
    ],
  );
}
