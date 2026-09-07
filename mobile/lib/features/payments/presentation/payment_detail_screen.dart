import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../../leasing/presentation/lease_detail_screen.dart';
import '../../properties/presentation/building_details_screen.dart';
import '../../properties/presentation/apartment_details_screen.dart';
import '../domain/payment_models.dart';
import '../domain/payment_errors.dart';
import 'payment_scope.dart';
import 'payment_widgets.dart';
import 'payment_submission_screen.dart';

class PaymentDetailScreen extends StatefulWidget {
  const PaymentDetailScreen({
    required this.scope,
    required this.id,
    this.tenant = false,
    this.initial,
    this.verification,
    super.key,
  });
  final PaymentScope scope;
  final String id;
  final bool tenant;
  final RentPayment? initial;
  final VerificationItem? verification;
  @override
  State<PaymentDetailScreen> createState() => _PaymentDetailScreenState();
}

class _PaymentDetailScreenState extends State<PaymentDetailScreen> {
  bool busy = false, changed = false;
  bool _usedTenantInitial = false;
  Future<RentPayment> load() async {
    if (widget.tenant) {
      if (!_usedTenantInitial && widget.initial != null) {
        _usedTenantInitial = true;
        return widget.initial!;
      }
      final items = await widget.scope.repository.tenantPayments();
      final value = items.where((p) => p.id == widget.id).firstOrNull;
      if (value == null) {
        throw const ApiProblem(message: 'Payment missing', statusCode: 404);
      }
      return value;
    }
    final value = await widget.scope.repository.detail(widget.id);
    return value.withListFallback(widget.initial);
  }

  bool get receipts => widget.tenant || widget.scope.can('receipts.read');
  Future<void> perform(
    Future<void> Function() action, {
    bool success = false,
  }) async {
    if (busy) return;
    setState(() => busy = true);
    try {
      await action();
      if (mounted && success) {
        AqariSnackbar.show(
          context,
          pt(context, 'تم تنفيذ الإجراء', 'Action completed'),
        );
      }
    } catch (e) {
      if (mounted) {
        AqariSnackbar.show(context, paymentError(e, context.isArabic));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> file(String id, {bool inline = false}) => perform(() async {
    final url = await widget.scope.repository.fileUrl(id, inline: inline);
    if (mounted) await widget.scope.files.open(url);
  });
  Future<void> receipt(RentPayment p) async {
    if (p.text('receiptFileId').isNotEmpty) {
      await file(p.text('receiptFileId'));
      return;
    }
    await AqariBottomSheet.show<void>(
      context: context,
      title: pt(context, 'سند القبض', 'Receipt'),
      child: SizedBox(
        height: MediaQuery.sizeOf(context).height * .55,
        child: PaymentLoad<PaymentReceipt?>(
          load: () => widget.scope.repository.receipt(p.id),
          builder: (value, reload) => value == null
              ? AqariEmptyState(
                  title: pt(context, 'لم يصدر سند بعد', 'Receipt not issued'),
                  message: pt(
                    context,
                    'لا يوجد سند لهذه الدفعة حالياً.',
                    'No receipt is currently available for this payment.',
                  ),
                )
              : ListView(
                  children: [
                    PaymentFact(
                      pt(context, 'رقم السند', 'Receipt number'),
                      value.text('receiptNumber'),
                      ltr: true,
                    ),
                    PaymentFact(
                      pt(context, 'المبلغ', 'Amount'),
                      paymentMoney(value.number('amount'), value.currency),
                      ltr: true,
                    ),
                    PaymentFact(
                      pt(context, 'تاريخ الإصدار', 'Issue date'),
                      paymentDate(value.text('issueDate')),
                      ltr: true,
                    ),
                    PaymentFact(
                      pt(context, 'ملاحظات', 'Notes'),
                      value.text('notes'),
                    ),
                    if (value.text('fileId').isNotEmpty)
                      AqariButton(
                        label: pt(context, 'فتح السند', 'Open receipt'),
                        onPressed: () => file(value.text('fileId')),
                      )
                    else
                      Text(
                        pt(
                          context,
                          'ملف السند غير متاح بعد.',
                          'The receipt PDF is not available yet.',
                        ),
                      ),
                  ],
                ),
        ),
      ),
    );
  }

  Future<void> settlement(RentPayment p) => perform(() async {
    final bytes = await widget.scope.repository.settlement(
      p.id,
      tenant: widget.tenant,
    );
    if (!mounted) return;
    final box = context.findRenderObject() as RenderBox?;
    await widget.scope.files.sharePdf(
      bytes,
      origin: box == null
          ? const Rect.fromLTWH(0, 0, 1, 1)
          : box.localToGlobal(Offset.zero) & box.size,
    );
  });
  void navigate(Widget child) =>
      Navigator.push(context, MaterialPageRoute(builder: (_) => child));
  Widget fact(String ar, String en, String value, {bool ltr = false}) =>
      PaymentFact(pt(context, ar, en), value, ltr: ltr);
  Widget section(String ar, String en) => Padding(
    padding: const EdgeInsets.only(top: 20, bottom: 8),
    child: AqariSectionHeader(title: pt(context, ar, en)),
  );
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: pt(
        context,
        widget.verification == null ? 'تفاصيل الدفعة' : 'مراجعة الدفعة',
        widget.verification == null ? 'Payment details' : 'Payment review',
      ),
      onBack: () => Navigator.pop(context, changed),
    ),
    body: SafeArea(
      child: PaymentLoad<RentPayment>(
        load: load,
        builder: (p, reload) {
          final summary = p.settlement;
          final v = widget.verification;
          final target = v == null
              ? null
              : p.submissions.where((s) => s.id == v.submissionId).firstOrNull;
          final settled =
              summary?.available == true ||
              (widget.tenant && p.status == 2 && p.paid >= p.due);
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: PaymentBadge(p.status),
              ),
              Wrap(
                spacing: 24,
                children: [
                  fact(
                    'المبلغ المستحق',
                    'Amount due',
                    paymentMoney(p.due, p.currency),
                    ltr: true,
                  ),
                  fact(
                    'المبلغ المدفوع',
                    'Amount paid',
                    paymentMoney(p.paid, p.currency),
                    ltr: true,
                  ),
                  fact(
                    'المتبقي',
                    'Remaining',
                    widget.tenant
                        ? paymentMoney(p.remaining, p.currency)
                        : summary == null
                        ? '—'
                        : paymentMoney(summary.remaining, p.currency),
                    ltr: true,
                  ),
                ],
              ),
              Text(statusText(context, p.purpose, kind: 'purpose')),
              AqariButton(
                label: pt(context, 'تحديث', 'Refresh'),
                icon: Icons.refresh,
                variant: AqariButtonVariant.text,
                onPressed: busy ? null : reload,
              ),
              if (v != null) ...[
                section('الطلب المرسل', 'Submitted payment'),
                fact(
                  'المبلغ المرسل',
                  'Submitted amount',
                  paymentMoney(target?.amount ?? v.submitted, p.currency),
                  ltr: true,
                ),
                fact(
                  'الرصيد المتبقي',
                  'Outstanding balance',
                  paymentMoney(p.remaining, p.currency),
                  ltr: true,
                ),
                fact(
                  'طريقة الدفع',
                  'Payment method',
                  statusText(
                    context,
                    target?.method ??
                        paymentEnum(v.json['paymentMethod'], paymentMethods),
                    kind: 'method',
                  ),
                ),
                fact(
                  'المرجع',
                  'Reference',
                  target?.text('referenceNumber') ?? v.text('referenceNumber'),
                  ltr: true,
                ),
                fact(
                  'تاريخ الإرسال',
                  'Submitted at',
                  v.text('submittedAt'),
                  ltr: true,
                ),
                if ((target?.proofId ?? v.text('proofFileId')).isNotEmpty)
                  AqariButton(
                    label: pt(context, 'عرض إثبات الدفع', 'View payment proof'),
                    icon: Icons.attach_file,
                    loading: busy,
                    onPressed: busy
                        ? null
                        : () => file(
                            target?.proofId ?? v.text('proofFileId'),
                            inline: true,
                          ),
                  ),
                if ((target?.text('chequeNumber') ?? v.text('chequeNumber'))
                    .isNotEmpty) ...[
                  fact(
                    'رقم الشيك',
                    'Cheque number',
                    target?.text('chequeNumber') ?? v.text('chequeNumber'),
                    ltr: true,
                  ),
                  fact(
                    'البنك',
                    'Bank',
                    target?.text('bankName') ?? v.text('bankName'),
                  ),
                  fact(
                    'تاريخ الإصدار',
                    'Issue date',
                    paymentDate(
                      target?.text('chequeIssueDate') ??
                          v.text('chequeIssueDate'),
                    ),
                    ltr: true,
                  ),
                  fact(
                    'تاريخ الاستحقاق',
                    'Due date',
                    paymentDate(
                      target?.text('chequeDueDate') ?? v.text('chequeDueDate'),
                    ),
                    ltr: true,
                  ),
                ],
                if (widget.scope.can('payments.approve') && target?.status == 0)
                  Wrap(
                    spacing: 12,
                    runSpacing: 12,
                    children: [
                      AqariButton(
                        label: pt(context, 'قبول الدفعة', 'Approve payment'),
                        onPressed: busy
                            ? null
                            : () async {
                                final result = await paymentConfirm(
                                  context,
                                  title: pt(
                                    context,
                                    'قبول الدفعة',
                                    'Approve payment',
                                  ),
                                  action: (_) => widget.scope.repository
                                      .approve(p.id, v.submissionId),
                                );
                                if (result && context.mounted) {
                                  widget.scope.onChanged?.call();
                                  AqariSnackbar.show(
                                    context,
                                    pt(
                                      context,
                                      'تم قبول الدفعة',
                                      'Payment approved',
                                    ),
                                  );
                                  Navigator.pop(context, true);
                                }
                              },
                      ),
                      AqariButton(
                        label: pt(context, 'رفض الدفعة', 'Reject payment'),
                        variant: AqariButtonVariant.destructive,
                        onPressed: busy
                            ? null
                            : () async {
                                final result = await paymentConfirm(
                                  context,
                                  title: pt(
                                    context,
                                    'رفض الدفعة',
                                    'Reject payment',
                                  ),
                                  reason: true,
                                  action: (reason) => widget.scope.repository
                                      .reject(p.id, v.submissionId, reason),
                                );
                                if (result && context.mounted) {
                                  widget.scope.onChanged?.call();
                                  AqariSnackbar.show(
                                    context,
                                    pt(
                                      context,
                                      'تم رفض الدفعة',
                                      'Payment rejected',
                                    ),
                                  );
                                  Navigator.pop(context, true);
                                }
                              },
                      ),
                    ],
                  )
                else
                  Text(
                    pt(
                      context,
                      'لم يعد الطلب متاحاً للمراجعة أو لا توجد صلاحية. حدّث البيانات.',
                      'This submission is no longer reviewable or permission is missing. Refresh.',
                    ),
                  ),
              ],
              section('الجهات المرتبطة', 'Related records'),
              if (widget.tenant) ...[
                fact('العقد', 'Lease', p.contractNumber, ltr: true),
                fact('المبنى', 'Building', p.buildingName),
                fact('الوحدة', 'Unit', p.apartmentNumber, ltr: true),
              ] else ...[
                AqariListRow(
                  title: pt(context, 'المستأجر', 'Tenant'),
                  supportingText: p.tenantName,
                  showChevron: true,
                  onPressed: () => AqariDialog.show(
                    context: context,
                    title: pt(context, 'المستأجر', 'Tenant'),
                    content: Text(
                      pt(
                        context,
                        'إدارة المستأجرين ستتاح في وحدتها المخصصة.',
                        'Tenant management will be available in its dedicated module.',
                      ),
                    ),
                    secondaryLabel: pt(context, 'إغلاق', 'Close'),
                  ),
                ),
                AqariListRow(
                  title: pt(context, 'العقد', 'Lease'),
                  supportingText: isolate(p.contractNumber),
                  enabled: p.leaseId.isNotEmpty,
                  showChevron: true,
                  onPressed: () => navigate(
                    LeaseDetailScreen(
                      scope: widget.scope.leasing,
                      id: p.leaseId,
                    ),
                  ),
                ),
                AqariListRow(
                  title: pt(context, 'المبنى', 'Building'),
                  supportingText: p.buildingName,
                  enabled: p.buildingId.isNotEmpty,
                  showChevron: true,
                  onPressed: () => navigate(
                    BuildingDetailsScreen(
                      repository: widget.scope.leasing.properties,
                      user: widget.scope.leasing.user,
                      id: p.buildingId,
                    ),
                  ),
                ),
                AqariListRow(
                  title: pt(context, 'الوحدة', 'Unit'),
                  supportingText: isolate(p.apartmentNumber),
                  enabled: p.apartmentId.isNotEmpty,
                  showChevron: true,
                  onPressed: () => navigate(
                    ApartmentDetailsScreen(
                      repository: widget.scope.leasing.properties,
                      user: widget.scope.leasing.user,
                      id: p.apartmentId,
                    ),
                  ),
                ),
              ],
              section('بيانات الدفعة', 'Payment information'),
              fact(
                'طريقة الدفع',
                'Payment method',
                statusText(context, p.method, kind: 'method'),
              ),
              fact(
                'المرجع',
                'Reference',
                p.text('paymentReferenceNumber'),
                ltr: true,
              ),
              fact(
                'تاريخ الاستحقاق',
                'Due date',
                paymentDate(p.dueDate),
                ltr: true,
              ),
              if (p.text('billingPeriodStart').isNotEmpty)
                fact(
                  'فترة الفوترة',
                  'Billing period',
                  '${paymentDate(p.text('billingPeriodStart'))} — ${paymentDate(p.text('billingPeriodEnd'))}',
                  ltr: true,
                ),
              fact(
                'تاريخ التسجيل',
                'Recorded at',
                p.text('createdAt'),
                ltr: true,
              ),
              if (p.text('latestSubmissionDate').isNotEmpty)
                fact(
                  'تاريخ الدفع',
                  'Payment date',
                  p.text('latestSubmissionDate'),
                  ltr: true,
                ),
              if (p.text('notes').isNotEmpty)
                fact('ملاحظات', 'Notes', p.text('notes')),
              if (p.latestStatus == 1 ||
                  p.latestStatus == 2 ||
                  p.latestStatus == 0) ...[
                PaymentBadge(p.latestStatus, kind: 'submission'),
                if (p.json['latestSubmissionAmount'] != null)
                  fact(
                    'المبلغ المرسل',
                    'Submitted amount',
                    paymentMoney(
                      p.number('latestSubmissionAmount'),
                      p.currency,
                    ),
                    ltr: true,
                  ),
                if (p.text('latestSubmissionRejectionReason').isNotEmpty)
                  fact(
                    'سبب الرفض',
                    'Rejection reason',
                    p.text('latestSubmissionRejectionReason'),
                  ),
              ],
              if (p.cheque case final cheque?) ...[
                section('الشيك', 'Cheque'),
                PaymentBadge(cheque.status, kind: 'cheque'),
                for (final entry in const {
                  'chequeNumber': ('رقم الشيك', 'Cheque number'),
                  'bankName': ('البنك', 'Bank'),
                  'bankBranch': ('الفرع', 'Branch'),
                  'issueDate': ('تاريخ الإصدار', 'Issue date'),
                  'dueDate': ('تاريخ الاستحقاق', 'Due date'),
                  'receivedDate': ('تاريخ الاستلام', 'Received date'),
                  'depositDate': ('تاريخ الإيداع', 'Deposit date'),
                  'clearanceDate': ('تاريخ التحصيل', 'Clearance date'),
                  'bounceDate': ('تاريخ الإرجاع', 'Bounce date'),
                  'bounceReason': ('سبب الإرجاع', 'Bounce reason'),
                  'cancellationReason': ('سبب الإلغاء', 'Cancellation reason'),
                  'notes': ('ملاحظات', 'Notes'),
                }.entries)
                  if (cheque.text(entry.key).isNotEmpty)
                    fact(
                      entry.value.$1,
                      entry.value.$2,
                      cheque.text(entry.key),
                    ),
              ],
              if (p.transactions.isNotEmpty) ...[
                section('حركات الدفع', 'Payment transactions'),
                for (final tx in p.transactions) ...[
                  fact(
                    'رقم السند',
                    'Receipt number',
                    tx.text('receiptNumber'),
                    ltr: true,
                  ),
                  fact(
                    'المبلغ',
                    'Amount',
                    paymentMoney(tx.number('amount'), p.currency),
                    ltr: true,
                  ),
                  fact(
                    'تاريخ الإصدار',
                    'Issued at',
                    tx.text('issuedAt'),
                    ltr: true,
                  ),
                  fact(
                    'طريقة الدفع',
                    'Method',
                    statusText(
                      context,
                      paymentEnum(tx.json['paymentMethod'], paymentMethods),
                      kind: 'method',
                    ),
                  ),
                  fact(
                    'المرجع',
                    'Reference',
                    tx.text('referenceNumber'),
                    ltr: true,
                  ),
                  fact(
                    'مدفوع سابقاً',
                    'Previously paid',
                    paymentMoney(tx.number('previouslyPaid'), p.currency),
                    ltr: true,
                  ),
                  fact(
                    'المتبقي بعد الدفعة',
                    'Remaining after',
                    paymentMoney(tx.number('remainingAfter'), p.currency),
                    ltr: true,
                  ),
                  if (receipts &&
                      (tx.text('fileId').isNotEmpty || widget.tenant))
                    AqariButton(
                      label: pt(
                        context,
                        'فتح سند الحركة',
                        'Open transaction receipt',
                      ),
                      variant: AqariButtonVariant.outlined,
                      onPressed: busy
                          ? null
                          : () => tx.text('fileId').isNotEmpty
                                ? file(tx.text('fileId'))
                                : receipt(p),
                    ),
                  const AqariDivider(),
                ],
              ],
              for (final pair in [
                (
                  p.incoming,
                  pt(context, 'التخصيصات الواردة', 'Incoming allocations'),
                ),
                (
                  p.outgoing,
                  pt(context, 'التخصيصات الصادرة', 'Outgoing allocations'),
                ),
              ])
                if (pair.$1.isNotEmpty) ...[
                  Padding(
                    padding: const EdgeInsets.only(top: 20),
                    child: AqariSectionHeader(title: pair.$2),
                  ),
                  for (final a in pair.$1) ...[
                    fact(
                      'المبلغ المخصص',
                      'Allocated amount',
                      paymentMoney(a.number('allocatedAmount'), p.currency),
                      ltr: true,
                    ),
                    fact(
                      'الحالة',
                      'Status',
                      a.status == 0
                          ? pt(context, 'نشط', 'Active')
                          : pt(context, 'معكوس', 'Reversed'),
                    ),
                    fact(
                      'تاريخ التخصيص',
                      'Allocation date',
                      paymentDate(a.text('allocationDate')),
                      ltr: true,
                    ),
                    if (a.text('reversalReason').isNotEmpty)
                      fact(
                        'سبب العكس',
                        'Reversal reason',
                        a.text('reversalReason'),
                      ),
                    const AqariDivider(),
                  ],
                ],
              if (p.submissions.isNotEmpty) ...[
                section('سجل طلبات الدفع', 'Submission history'),
                for (final s in p.submissions) ...[
                  PaymentBadge(s.status, kind: 'submission'),
                  fact(
                    'تاريخ الإرسال',
                    'Submitted at',
                    s.text('submittedAt'),
                    ltr: true,
                  ),
                  if (s.amount != null)
                    fact(
                      'المبلغ',
                      'Amount',
                      paymentMoney(s.amount!, p.currency),
                      ltr: true,
                    ),
                  fact(
                    'طريقة الدفع',
                    'Method',
                    statusText(context, s.method, kind: 'method'),
                  ),
                  fact(
                    'المرجع',
                    'Reference',
                    s.text('referenceNumber'),
                    ltr: true,
                  ),
                  if (s.text('rejectionReason').isNotEmpty)
                    fact(
                      'سبب الرفض',
                      'Rejection reason',
                      s.text('rejectionReason'),
                    ),
                  const AqariDivider(),
                ],
              ],
              if (receipts &&
                  p.transactions.isEmpty &&
                  (!widget.tenant ||
                      p.text('receiptNumber').isNotEmpty ||
                      p.text('receiptFileId').isNotEmpty)) ...[
                section('السند والتسوية', 'Receipt and settlement'),
                if (p.text('receiptNumber').isNotEmpty)
                  fact(
                    'رقم السند',
                    'Receipt number',
                    p.text('receiptNumber'),
                    ltr: true,
                  ),
                AqariButton(
                  label: pt(context, 'عرض سند القبض', 'View receipt'),
                  variant: AqariButtonVariant.outlined,
                  onPressed: busy ? null : () => receipt(p),
                ),
                if (settled)
                  AqariButton(
                    label: pt(
                      context,
                      'تحميل / مشاركة بيان التسوية',
                      'Download / share settlement PDF',
                    ),
                    icon: Icons.file_download_outlined,
                    loading: busy,
                    onPressed: busy ? null : () => settlement(p),
                  ),
              ] else if (settled) ...[
                section('التسوية', 'Settlement'),
                AqariButton(
                  label: pt(
                    context,
                    'تحميل / مشاركة بيان التسوية',
                    'Download / share settlement PDF',
                  ),
                  icon: Icons.file_download_outlined,
                  loading: busy,
                  onPressed: busy ? null : () => settlement(p),
                ),
              ],
              if (!widget.tenant &&
                  widget.scope.can('payments.approve') &&
                  p.canRemind)
                Padding(
                  padding: const EdgeInsets.only(top: 16),
                  child: AqariButton(
                    label: pt(context, 'تذكير المستأجر', 'Remind tenant'),
                    icon: Icons.notifications_outlined,
                    loading: busy,
                    onPressed: busy
                        ? null
                        : () => perform(
                            () => widget.scope.repository.remind(p.id),
                            success: true,
                          ),
                  ),
                ),
              if (widget.tenant && p.canSubmit)
                Padding(
                  padding: const EdgeInsets.only(top: 16),
                  child: AqariButton(
                    label: pt(
                      context,
                      p.latestStatus == 2
                          ? 'إعادة إرسال الدفعة'
                          : 'إرسال الدفعة للتحقق',
                      p.latestStatus == 2
                          ? 'Resubmit payment'
                          : 'Submit payment',
                    ),
                    onPressed: () async {
                      final result = await Navigator.push<bool>(
                        context,
                        MaterialPageRoute(
                          builder: (_) => PaymentSubmissionScreen(
                            scope: widget.scope,
                            payment: p,
                          ),
                        ),
                      );
                      if (result == true && mounted) {
                        changed = true;
                        await reload();
                      }
                    },
                  ),
                ),
              const SizedBox(height: 24),
            ],
          );
        },
      ),
    ),
  );
}
