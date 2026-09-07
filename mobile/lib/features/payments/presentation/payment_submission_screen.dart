import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/payment_models.dart';
import 'payment_scope.dart';
import 'payment_widgets.dart';

class PaymentSubmissionScreen extends StatefulWidget {
  const PaymentSubmissionScreen({
    required this.scope,
    required this.payment,
    super.key,
  });
  final PaymentScope scope;
  final RentPayment payment;
  @override
  State<PaymentSubmissionScreen> createState() =>
      _PaymentSubmissionScreenState();
}

class _PaymentSubmissionScreenState extends State<PaymentSubmissionScreen> {
  final draft = PaymentDraft();
  final amount = TextEditingController();
  final reference = TextEditingController();
  final cheque = TextEditingController();
  final bank = TextEditingController();
  Map<String, String> errors = {};
  Object? error;
  bool busy = false, uploading = false, success = false;
  String filename = '', stage = '';
  double? progress;
  @override
  void initState() {
    super.initState();
    amount.text = '${widget.payment.remainingMilli / 1000}';
    draft.amount = amount.text;
  }

  @override
  void dispose() {
    amount.dispose();
    reference.dispose();
    cheque.dispose();
    bank.dispose();
    super.dispose();
  }

  bool get locked => busy || uploading;
  String? validation(String key) {
    final value = errors[key];
    if (value == null) return null;
    return switch (value) {
      'invalidAmount' => pt(
        context,
        'أدخل مبلغاً موجباً بحد أقصى ثلاث منازل عشرية.',
        'Enter a positive amount with at most three decimal places.',
      ),
      'exceeds' => pt(
        context,
        'المبلغ يتجاوز الرصيد المتبقي.',
        'Amount exceeds the remaining balance.',
      ),
      'proofRequired' => pt(
        context,
        'إثبات الدفع مطلوب لكليك.',
        'CliQ requires payment proof.',
      ),
      'dateOrder' => pt(
        context,
        'تاريخ الاستحقاق لا يسبق تاريخ الإصدار.',
        'Due date cannot precede issue date.',
      ),
      'chequeNumberInvalid' => pt(
        context,
        'رقم الشيك مطلوب، بحد أقصى 100 حرف.',
        'Cheque number is required, at most 100 characters.',
      ),
      'bankInvalid' => pt(
        context,
        'اسم البنك مطلوب، بحد أقصى 255 حرفاً.',
        'Bank name is required, at most 255 characters.',
      ),
      _ => pt(context, 'هذا الحقل مطلوب.', 'This field is required.'),
    };
  }

  void method(int value) {
    setState(() {
      draft.method = value;
      reference.clear();
      cheque.clear();
      bank.clear();
      draft.reference = '';
      draft.chequeNumber = '';
      draft.bank = '';
      draft.issueDate = '';
      draft.dueDate = '';
      draft.proofId = '';
      filename = '';
      error = null;
      errors = {};
    });
  }

  Future<void> select() async {
    if (locked) return;
    final source = await AttachmentSourceSheet.show(context);
    if (source == null || !mounted) return;
    setState(() {
      uploading = true;
      error = null;
    });
    try {
      final file = await widget.scope.files.select(source);
      if (file == null) return;
      // Keep confirmed proof on transient selection failure; replace only on success.
      final id = await widget.scope.files.upload(widget.payment.id, file, (
        s,
        p,
      ) {
        if (mounted) {
          setState(() {
            stage = s;
            progress = p;
          });
        }
      });
      if (mounted) {
        setState(() {
          draft.proofId = id;
          filename = file.name;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = e);
    } finally {
      if (mounted) setState(() => uploading = false);
    }
  }

  Future<void> submit() async {
    if (locked) return;
    draft.amount = amount.text;
    draft.reference = reference.text;
    draft.chequeNumber = cheque.text;
    draft.bank = bank.text;
    final validation = draft.validate(widget.payment);
    setState(() => errors = validation);
    if (validation.isNotEmpty) return;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await widget.scope.repository.submit(widget.payment.id, draft);
      if (mounted) {
        widget.scope.onChanged?.call();
        setState(() => success = true);
      }
    } catch (e) {
      if (mounted) setState(() => error = e);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !locked,
    child: Scaffold(
      appBar: AqariAppBar(
        title: pt(context, 'إرسال دفعة', 'Submit payment'),
        onBack: locked ? null : () => Navigator.pop(context, success),
      ),
      body: SafeArea(
        child: success
            ? Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.check_circle_outline, size: 48),
                      Text(
                        pt(
                          context,
                          'تم إرسال الدفعة للمراجعة',
                          'Payment submitted for review',
                        ),
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      Text(
                        pt(
                          context,
                          'سيظهر قرار المراجعة في سجل الدفعات.',
                          'The review result will appear in your payment history.',
                        ),
                      ),
                      AqariButton(
                        label: pt(context, 'العودة للدفعة', 'Back to payment'),
                        onPressed: () => Navigator.pop(context, true),
                      ),
                    ],
                  ),
                ),
              )
            : ListView(
                padding: const EdgeInsets.all(16),
                keyboardDismissBehavior:
                    ScrollViewKeyboardDismissBehavior.onDrag,
                children: [
                  PaymentFact(
                    pt(context, 'العقد والوحدة', 'Contract and unit'),
                    '${isolate(widget.payment.contractNumber)} · ${widget.payment.buildingName} · ${isolate(widget.payment.apartmentNumber)}',
                  ),
                  PaymentFact(
                    pt(context, 'الرصيد المتبقي', 'Remaining balance'),
                    paymentMoney(
                      widget.payment.remaining,
                      widget.payment.currency,
                    ),
                    ltr: true,
                  ),
                  if (error != null) PaymentFailure(error),
                  AqariTextField(
                    label: pt(context, 'المبلغ *', 'Amount *'),
                    controller: amount,
                    errorText: validation('amount'),
                    enabled: !locked,
                    textDirection: TextDirection.ltr,
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                  ),
                  const SizedBox(height: 16),
                  AqariSelect<int>(
                    label: pt(context, 'طريقة الدفع', 'Payment method'),
                    valueLabel: statusText(
                      context,
                      draft.method,
                      kind: 'method',
                    ),
                    items: const [0, 4, 2],
                    itemLabel: (v) => statusText(context, v, kind: 'method'),
                    enabled: !locked,
                    onChanged: method,
                  ),
                  const SizedBox(height: 16),
                  if (draft.method != 2)
                    AqariTextField(
                      label: pt(
                        context,
                        draft.method == 4 ? 'مرجع كليك *' : 'مرجع (اختياري)',
                        draft.method == 4
                            ? 'CliQ reference *'
                            : 'Reference (optional)',
                      ),
                      controller: reference,
                      enabled: !locked,
                      errorText: validation('reference'),
                      textDirection: TextDirection.ltr,
                    ),
                  if (draft.method == 0)
                    Text(
                      pt(
                        context,
                        'سيؤكد المالك استلام المبلغ النقدي.',
                        'The owner will confirm receipt of the cash payment.',
                      ),
                    ),
                  if (draft.method == 2) ...[
                    AqariTextField(
                      label: pt(context, 'رقم الشيك *', 'Cheque number *'),
                      controller: cheque,
                      enabled: !locked,
                      errorText: validation('chequeNumber'),
                      textDirection: TextDirection.ltr,
                    ),
                    const SizedBox(height: 16),
                    AqariTextField(
                      label: pt(context, 'البنك *', 'Bank *'),
                      controller: bank,
                      enabled: !locked,
                      errorText: validation('bank'),
                    ),
                    PaymentDateField(
                      label: pt(context, 'تاريخ الإصدار *', 'Issue date *'),
                      value: draft.issueDate,
                      enabled: !locked,
                      error: validation('issueDate'),
                      onChanged: (v) => setState(() => draft.issueDate = v),
                    ),
                    PaymentDateField(
                      label: pt(context, 'تاريخ الاستحقاق *', 'Due date *'),
                      value: draft.dueDate,
                      enabled: !locked,
                      error: validation('dueDate'),
                      onChanged: (v) => setState(() => draft.dueDate = v),
                    ),
                  ],
                  if (draft.method != 0) ...[
                    const SizedBox(height: 16),
                    Text(
                      pt(
                        context,
                        draft.method == 4
                            ? 'إثبات الدفع مطلوب — PDF / PNG / JPEG، حتى 15 ميغابايت'
                            : 'إثبات اختياري — PDF / PNG / JPEG، حتى 15 ميغابايت',
                        draft.method == 4
                            ? 'Proof required — PDF / PNG / JPEG, up to 15 MB'
                            : 'Optional proof — PDF / PNG / JPEG, up to 15 MB',
                      ),
                    ),
                    if (filename.isNotEmpty)
                      PaymentFact(
                        pt(context, 'الملف المرفق', 'Attached file'),
                        filename,
                      ),
                    if (uploading) ...[
                      LinearProgressIndicator(value: progress),
                      Text(
                        pt(
                          context,
                          stage == 'confirming'
                              ? 'تأكيد رفع الملف'
                              : 'جارٍ رفع الملف',
                          stage == 'confirming'
                              ? 'Confirming upload'
                              : 'Uploading file',
                        ),
                      ),
                    ],
                    if (validation('proof') != null)
                      Text(
                        validation('proof')!,
                        style: TextStyle(color: context.aqariColors.error),
                      ),
                    Wrap(
                      spacing: 12,
                      children: [
                        AqariButton(
                          label: pt(
                            context,
                            draft.proofId.isEmpty
                                ? 'إضافة مرفق'
                                : 'استبدال المرفق',
                            draft.proofId.isEmpty
                                ? 'Add attachment'
                                : 'Replace attachment',
                          ),
                          icon: Icons.attach_file,
                          onPressed: locked ? null : select,
                        ),
                        if (draft.proofId.isNotEmpty)
                          AqariButton(
                            label: pt(
                              context,
                              'إزالة المرفق',
                              'Remove attachment',
                            ),
                            variant: AqariButtonVariant.text,
                            onPressed: locked
                                ? null
                                : () => setState(() {
                                    draft.proofId = '';
                                    filename = '';
                                  }),
                          ),
                      ],
                    ),
                  ],
                  const SizedBox(height: 24),
                  AqariButton(
                    label: pt(context, 'إرسال للمراجعة', 'Submit for review'),
                    loading: busy,
                    onPressed: locked ? null : submit,
                  ),
                  const SizedBox(height: 24),
                ],
              ),
      ),
    ),
  );
}
