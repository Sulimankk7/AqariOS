import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../payments/presentation/payment_widgets.dart';
import '../domain/expense_models.dart';
import 'expense_widgets.dart';
import 'financial_operations_scope.dart';

class ExpenseDetailScreen extends StatefulWidget {
  const ExpenseDetailScreen({
    required this.scope,
    required this.expense,
    super.key,
  });

  final FinancialOperationsScope scope;
  final Expense expense;

  @override
  State<ExpenseDetailScreen> createState() => _ExpenseDetailScreenState();
}

class _ExpenseDetailScreenState extends State<ExpenseDetailScreen> {
  late Future<ExpenseDetail> future;
  bool opening = false;

  @override
  void initState() {
    super.initState();
    future = widget.scope.expenses.detail(widget.expense.id);
  }

  Future<void> refresh() async {
    setState(() => future = widget.scope.expenses.detail(widget.expense.id));
    await future;
  }

  Future<void> openReceipt(String fileId) async {
    if (opening) return;
    setState(() => opening = true);
    try {
      final url = await widget.scope.expenses.receiptUrl(fileId);
      if (mounted) await widget.scope.files.open(url);
    } catch (e) {
      if (mounted) {
        AqariSnackbar.show(context, e.toString());
      }
    } finally {
      if (mounted) setState(() => opening = false);
    }
  }

  Widget fact(String ar, String en, String value, {bool ltr = false}) =>
      PaymentFact(pt(context, ar, en), value, ltr: ltr);

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: pt(context, 'تفاصيل المصروف', 'Expense details'),
      onBack: () => Navigator.pop(context),
    ),
    body: SafeArea(
      child: FutureBuilder<ExpenseDetail>(
        future: future,
        builder: (context, snapshot) {
          final data = snapshot.data;
          final record = data ?? ExpenseDetail(widget.expense.json);
          if (snapshot.connectionState == ConnectionState.waiting &&
              data == null) {
            return const ExpenseSkeleton();
          }
          if (snapshot.hasError && data == null) {
            return SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: ExpenseFailure(snapshot.error, retry: refresh),
              ),
            );
          }
          return RefreshIndicator(
            onRefresh: refresh,
            child: ListView(
              padding: const EdgeInsets.all(16),
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                FinancialAmountSummary(
                  label: pt(context, 'المصروف', 'Expense amount'),
                  amount: paymentMoney(record.amount, record.currency),
                  subtitle: record.description,
                  status: AqariStatusBadge(
                    label: expenseCategoryText(context, record.category),
                    variant: AqariStatusVariant.brand,
                  ),
                ),
                const SizedBox(height: AqariSpacing.x5),
                AqariFormSection(
                  title: pt(context, 'معلومات المصروف', 'Expense information'),
                  children: [
                    fact(
                      'التاريخ',
                      'Date',
                      paymentDate(record.expenseDate),
                      ltr: true,
                    ),
                    fact(
                      'التصنيف',
                      'Category',
                      expenseCategoryText(context, record.category),
                    ),
                    fact(
                      'طريقة الدفع',
                      'Payment method',
                      expenseMethodText(context, record.method),
                    ),
                  ],
                ),
                AqariFormSection(
                  title: pt(context, 'الأطراف', 'Parties'),
                  children: [
                    fact('المورّد', 'Vendor', record.vendorName),
                    if (record.invoiceNumber.isNotEmpty)
                      fact(
                        'رقم الفاتورة',
                        'Invoice number',
                        record.invoiceNumber,
                        ltr: true,
                      ),
                  ],
                ),
                if (record.text('notes').isNotEmpty) ...[
                  ExpansionTile(
                    tilePadding: EdgeInsets.zero,
                    title: Text(pt(context, 'ملاحظات', 'Notes')),
                    children: [
                      Align(
                        alignment: AlignmentDirectional.centerStart,
                        child: SelectableText(record.text('notes')),
                      ),
                    ],
                  ),
                ],
                const SizedBox(height: 20),
                AqariSectionHeader(
                  title: pt(context, 'إيصالات المصروف', 'Expense receipts'),
                ),
                if (data == null || data.receipts.isEmpty)
                  AqariEmptyState(
                    title: pt(context, 'لا توجد إيصالات', 'No receipts'),
                    message: pt(
                      context,
                      'لا توجد إيصالات مرفقة لهذا المصروف.',
                      'No receipts are attached to this expense.',
                    ),
                  )
                else
                  for (final receipt in data.receipts) ...[
                    AqariListRow(
                      title: receipt.receiptNumber.isEmpty
                          ? pt(context, 'إيصال', 'Receipt')
                          : receipt.receiptNumber,
                      supportingText:
                          '${paymentMoney(receipt.amount, record.currency)} · ${paymentDate(receipt.issuedAt)}',
                      showChevron: true,
                      leading: const Icon(Icons.receipt_long_outlined),
                      onPressed: opening || receipt.fileId.isEmpty
                          ? null
                          : () => openReceipt(receipt.fileId),
                    ),
                    const AqariDivider(),
                  ],
                const SizedBox(height: 24),
              ],
            ),
          );
        },
      ),
    ),
  );
}
