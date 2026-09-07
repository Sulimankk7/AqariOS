import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../payments/presentation/payment_widgets.dart';
import '../../properties/domain/property_models.dart';
import '../application/expenses_controller.dart';
import '../domain/expense_models.dart';
import 'expense_detail_screen.dart';
import 'expense_widgets.dart';
import 'financial_operations_scope.dart';

class ExpensesScreen extends StatefulWidget {
  const ExpensesScreen({required this.scope, super.key});

  final FinancialOperationsScope scope;

  @override
  State<ExpensesScreen> createState() => _ExpensesScreenState();
}

class _ExpensesScreenState extends State<ExpensesScreen> {
  late final ExpensesController controller;

  @override
  void initState() {
    super.initState();
    controller = ExpensesController(widget.scope.expenses)..load();
  }

  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  void reset() => controller.apply(const ExpenseFilters());

  Future<void> details(Expense expense) async {
    await Navigator.push<void>(
      context,
      MaterialPageRoute(
        builder: (_) =>
            ExpenseDetailScreen(scope: widget.scope, expense: expense),
      ),
    );
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: controller,
    builder: (context, _) {
      final filtered =
          controller.filters.buildingId.isNotEmpty ||
          controller.filters.category >= 0 ||
          controller.filters.from.isNotEmpty ||
          controller.filters.to.isNotEmpty;
      final rows = <Widget>[
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
              AqariButton(
                label: pt(context, 'تصفية', 'Filters'),
                icon: Icons.filter_list,
                variant: AqariButtonVariant.outlined,
                onPressed: () async {
                  final result = await AqariBottomSheet.show<ExpenseFilters>(
                    context: context,
                    title: pt(context, 'تصفية المصاريف', 'Filter expenses'),
                    child: _ExpenseFilters(
                      scope: widget.scope,
                      filters: controller.filters,
                    ),
                  );
                  if (result != null && mounted) controller.apply(result);
                },
              ),
              if (filtered)
                AqariButton(
                  label: pt(context, 'مسح', 'Reset'),
                  variant: AqariButtonVariant.text,
                  onPressed: reset,
                ),
            ],
          ),
        ),
      ];

      if (controller.loading) {
        return const ExpenseSkeleton();
      }
      if (controller.error != null) {
        return ListView(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: ExpenseFailure(controller.error, retry: controller.load),
            ),
          ],
        );
      }
      if (controller.items.isEmpty) {
        rows.add(
          Padding(
            padding: const EdgeInsets.all(24),
            child: AqariEmptyState(
              title: filtered
                  ? pt(context, 'لا توجد نتائج', 'No matching expenses')
                  : pt(context, 'لا توجد مصاريف', 'No expenses'),
              message: filtered
                  ? pt(
                      context,
                      'لا توجد مصاريف مطابقة للتصفية الحالية.',
                      'No expenses match the current filters.',
                    )
                  : pt(
                      context,
                      'ستظهر المصاريف المسجلة هنا.',
                      'Recorded expenses will appear here.',
                    ),
              actionLabel: filtered
                  ? pt(context, 'مسح التصفية', 'Clear')
                  : null,
              onAction: filtered ? reset : null,
            ),
          ),
        );
      } else {
        for (final expense in controller.items) {
          rows.add(
            _ExpenseRow(expense: expense, onPressed: () => details(expense)),
          );
        }
      }

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

      return SafeArea(
        top: false,
        child: RefreshIndicator(
          onRefresh: controller.load,
          child: ListView.builder(
            physics: const AlwaysScrollableScrollPhysics(),
            itemCount: rows.length,
            itemBuilder: (_, i) => rows[i],
          ),
        ),
      );
    },
  );
}

class _ExpenseRow extends StatelessWidget {
  const _ExpenseRow({required this.expense, required this.onPressed});

  final Expense expense;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: AqariSpacing.x4),
    child: FinancialRecordTile(
      title: expense.description.isEmpty ? '—' : expense.description,
      subtitle: [
        expenseCategoryText(context, expense.category),
        if (expense.vendorName.isNotEmpty) expense.vendorName,
      ].join(' · '),
      date: paymentDate(expense.expenseDate),
      onPressed: onPressed,
      amountLabel: pt(context, 'المصروف', 'Expense'),
      amount: paymentMoney(expense.amount, expense.currency),
      details: Wrap(
        spacing: AqariSpacing.x3,
        runSpacing: AqariSpacing.x1,
        children: [
          Text(
            expenseMethodText(context, expense.method),
            style: Theme.of(context).textTheme.bodySmall,
          ),
          if (expense.invoiceNumber.isNotEmpty)
            AqariLtrContent(
              child: Text(
                expense.invoiceNumber,
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ),
        ],
      ),
    ),
  );
}

class _ExpenseFilters extends StatefulWidget {
  const _ExpenseFilters({required this.scope, required this.filters});

  final FinancialOperationsScope scope;
  final ExpenseFilters filters;

  @override
  State<_ExpenseFilters> createState() => _ExpenseFiltersState();
}

class _ExpenseFiltersState extends State<_ExpenseFilters> {
  late String building, from, to;
  late int category;
  List<Building> buildings = [];
  Object? error;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    building = widget.filters.buildingId;
    category = widget.filters.category;
    from = widget.filters.from;
    to = widget.filters.to;
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
          if (error != null) ExpenseFailure(error, retry: load),
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
            label: pt(context, 'التصنيف', 'Category'),
            valueLabel: category < 0
                ? pt(context, 'كل التصنيفات', 'All categories')
                : expenseCategoryText(context, category),
            items: [-1, for (var i = 0; i < expenseCategories.length; i++) i],
            itemLabel: (v) => v < 0
                ? pt(context, 'كل التصنيفات', 'All categories')
                : expenseCategoryText(context, v),
            onChanged: (v) => setState(() => category = v),
          ),
          PaymentDateField(
            label: pt(context, 'تاريخ المصروف من', 'Expense date from'),
            value: from,
            onChanged: (v) => setState(() => from = v),
          ),
          PaymentDateField(
            label: pt(context, 'تاريخ المصروف إلى', 'Expense date to'),
            value: to,
            onChanged: (v) => setState(() => to = v),
          ),
          AqariButton(
            label: pt(context, 'مسح التصفية', 'Reset filters'),
            variant: AqariButtonVariant.text,
            onPressed: () => Navigator.pop(context, const ExpenseFilters()),
          ),
          AqariButton(
            label: pt(context, 'تطبيق', 'Apply'),
            onPressed: () => Navigator.pop(
              context,
              ExpenseFilters(
                buildingId: building,
                category: category,
                from: from,
                to: to,
              ),
            ),
          ),
        ],
      ),
    ),
  );
}
