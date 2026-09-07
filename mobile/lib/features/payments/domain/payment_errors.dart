import '../../../core/network/api_problem.dart';

String paymentError(Object? error, bool ar) {
  String pick(String arabic, String english) => ar ? arabic : english;
  if (error is! ApiProblem) {
    return pick(
      'تعذر إكمال العملية. حاول مجدداً.',
      'Unable to complete the operation. Try again.',
    );
  }
  final code = error.code;
  if (code == 'TIMEOUT') {
    return pick(
      'انتهت مهلة الطلب. تحقق من حالة الدفعة قبل إعادة إرسالها.',
      'The request timed out. Check the payment status before submitting again.',
    );
  }
  if (error.isNetworkFailure) {
    return pick(
      'تعذر الاتصال بالخادم. تحقق من الشبكة وأعد المحاولة.',
      'Cannot connect. Check your network and retry.',
    );
  }
  final messages = <String, (String, String)>{
    'SESSION_CHANGED': (
      'تغيّرت الجلسة. افتح الدفعات مجدداً.',
      'Session changed. Reopen payments.',
    ),
    'INVALID_RESPONSE': (
      'استجابة الخادم غير صالحة. أعد المحاولة.',
      'Invalid server response. Retry.',
    ),
    'SUBMISSION_NOT_PENDING': (
      'تمت معالجة هذا الطلب بالفعل. حدّث الدفعة.',
      'This submission has already been processed. Refresh the payment.',
    ),
    'INSTALLMENT_ALREADY_PAID': (
      'تمت تسوية القسط بالكامل.',
      'This installment is already settled.',
    ),
    'INVALID_AMOUNT': (
      'أدخل مبلغاً موجباً صالحاً.',
      'Enter a valid positive amount.',
    ),
    'INVALID_SUBMISSION_AMOUNT': (
      'مبلغ الطلب غير صالح. حدّث الدفعة.',
      'The submission amount is invalid. Refresh the payment.',
    ),
    'AMOUNT_EXCEEDS_OUTSTANDING': (
      'المبلغ أكبر من الرصيد المتبقي. حدّث الدفعة.',
      'Amount exceeds the remaining balance. Refresh the payment.',
    ),
    'AMOUNT_EXCEEDS_OUTSTANDING_BALANCE': (
      'تغيّر الرصيد المتبقي وأصبح أقل من المبلغ المرسل. حدّث الدفعة.',
      'The remaining balance is now below the submitted amount. Refresh the payment.',
    ),
    'TENANT_ACCOUNT_UNAVAILABLE': (
      'حساب المستأجر غير متاح لإرسال التذكير.',
      'The tenant account is unavailable for reminders.',
    ),
    'PAYMENT_ALREADY_PAID': (
      'تم دفع هذه الدفعة بالفعل.',
      'This payment is already paid.',
    ),
    'PAYMENT_CANCELLED': ('هذه الدفعة ملغاة.', 'This payment is cancelled.'),
    'PAYMENT_FULLY_SETTLED': (
      'تمت تسوية هذه الدفعة بالكامل.',
      'This payment is fully settled.',
    ),
    'FILE_SIZE': (
      'اختر ملفاً غير فارغ لا يتجاوز 15 ميغابايت.',
      'Choose a non-empty file up to 15 MB.',
    ),
    'FILE_TYPE': (
      'الملفات المدعومة: PDF وPNG وJPEG.',
      'Supported files: PDF, PNG and JPEG.',
    ),
    'UPLOAD_URL_REJECTED': (
      'انتهت صلاحية رابط رفع الملف أو تم رفضه. أعد اختيار الملف.',
      'The upload URL expired or was rejected. Select the file again.',
    ),
    'FILE_OPEN': (
      'تعذر فتح الملف. تحقق من وجود تطبيق مناسب.',
      'Unable to open the file. Check for a compatible app.',
    ),
    'CHEQUE_DETAILS_REQUIRED': (
      'بيانات الشيك مطلوبة.',
      'Cheque details are required.',
    ),
    'CHEQUE_INVALID_TRANSITION': (
      'لا يسمح وضع الشيك الحالي بهذا الإجراء. حدّث البيانات.',
      'The cheque state does not allow this action. Refresh.',
    ),
  };
  if (messages.containsKey(code)) {
    final m = messages[code]!;
    return pick(m.$1, m.$2);
  }
  if (error.statusCode == 401) {
    return pick(
      'انتهت الجلسة. سجّل الدخول مجدداً.',
      'Session expired. Sign in again.',
    );
  }
  if (error.statusCode == 403) {
    return pick(
      'ليس لديك صلاحية تنفيذ هذا الإجراء.',
      'You do not have permission for this action.',
    );
  }
  if (error.statusCode == 404) {
    return pick(
      'السجل غير موجود أو غير متاح لهذا الحساب.',
      'The record is missing or unavailable for this account.',
    );
  }
  if (error.statusCode == 429) {
    return pick(
      'طلبات كثيرة. أعد المحاولة بعد ${error.retryAfterSeconds ?? 60} ثانية.',
      'Too many requests. Retry after ${error.retryAfterSeconds ?? 60} seconds.',
    );
  }
  // Surface safe validation/domain details, never stack traces or technical exceptions.
  final details = error.validationErrors.values.expand((e) => e).join('\n');
  final detail = details.isEmpty ? error.detail : details;
  if ([400, 409, 422].contains(error.statusCode) &&
      detail != null &&
      detail.length < 1200 &&
      !RegExp(
        r'exception|stack|sql|System\.|Microsoft\.| at \w+\.',
        caseSensitive: false,
      ).hasMatch(detail)) {
    return detail;
  }
  if (error.statusCode == 409) {
    return pick(
      'تغيّرت الدفعة أو تمت معالجتها. حدّث البيانات قبل المحاولة.',
      'The payment changed or was processed. Refresh before retrying.',
    );
  }
  if ([400, 422].contains(error.statusCode)) {
    return pick(
      'راجع المبلغ والتواريخ وحالة الدفعة ثم أعد المحاولة.',
      'Check the amount, dates and payment state, then retry.',
    );
  }
  return pick(
    'تعذر إكمال العملية على الخادم. أعد المحاولة لاحقاً.',
    'The server could not complete the operation. Retry later.',
  );
}
