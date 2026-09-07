import '../../../core/network/api_problem.dart';

String leaseError(Object? error, bool arabic) {
  String choose(String ar, String en) => arabic ? ar : en;
  if (error is! ApiProblem) {
    return choose(
      'تعذر إكمال العملية. حاول مجدداً.',
      'Unable to complete the action. Please retry.',
    );
  }
  final known = leaseErrorMessages[error.code];
  if (known != null) return known[arabic ? 0 : 1];
  if (error.statusCode == 429 && error.retryAfterSeconds != null) {
    return choose(
      'طلبات كثيرة. حاول بعد ${error.retryAfterSeconds} ثانية.',
      'Too many requests. Retry after ${error.retryAfterSeconds} seconds.',
    );
  }
  if (error.isNetworkFailure) {
    return choose(
      'تعذر الاتصال بالخادم. تحقق من الشبكة وحاول مجدداً.',
      'Cannot reach the server. Check your connection and retry.',
    );
  }
  final detail = error.detail ?? '';
  if ((error.statusCode == 422 || error.statusCode == 409) &&
      detail.contains('while it is being synchronised')) {
    return choose(
      'انتظر اكتمال المزامنة الحالية ثم أعد المحاولة.',
      'Wait for the current utility sync to finish, then retry.',
    );
  }
  if (error.statusCode == 409) {
    if (detail.contains('overlapping') ||
        detail.contains('active contract already')) {
      return choose(
        'يوجد عقد متداخل لهذه الوحدة. راجع التواريخ والعقود الحالية.',
        'An overlapping contract exists for this apartment. Review its dates and contracts.',
      );
    }
    if (detail.contains('renewal successor')) {
      return choose(
        'يوجد بالفعل عقد تجديد لهذا العقد.',
        'This contract already has a renewal successor.',
      );
    }
    if (detail.contains('already attached')) {
      return choose(
        'هذا الملف مرفق بالعقد بالفعل.',
        'This file is already attached to the contract.',
      );
    }
  }
  if (error.validationErrors.isNotEmpty) {
    return choose(
      'راجع الحقول المشار إليها وأعد المحاولة.',
      'Review the highlighted fields and try again.',
    );
  }
  // Same Web boundary: only uncoded user-facing domain/validation details can
  // pass through; authentication, infrastructure and coded failures stay localized.
  if (error.code == null &&
      const {400, 404, 409, 422}.contains(error.statusCode) &&
      detail.isNotEmpty &&
      detail.length <= 500 &&
      !RegExp(
        r'exception|stack|sql|trace|system\.|https?://|password|token',
        caseSensitive: false,
      ).hasMatch(detail) &&
      !const {
        'one or more validation errors occurred.',
        'an unexpected error occurred.',
        'a database error occurred while processing the request.',
      }.contains(detail.trim().toLowerCase())) {
    return detail.trim();
  }
  return switch (error.statusCode) {
    401 => choose(
      'انتهت الجلسة. يرجى تسجيل الدخول.',
      'Your session expired. Please sign in.',
    ),
    403 => choose(
      'ليس لديك صلاحية لهذا الإجراء.',
      'You do not have permission for this action.',
    ),
    404 => choose(
      'لم يعد السجل متاحاً أو لا يمكن الوصول إليه.',
      'The record is unavailable or cannot be accessed.',
    ),
    409 => choose(
      'تعارض مع البيانات الحالية. حدّث الصفحة وراجع العملية.',
      'Conflict with current data. Refresh and review the action.',
    ),
    400 || 422 => choose(
      'لم يقبل الخادم البيانات. راجع القيم وشروط العقد.',
      'The server rejected the data. Review the values and contract requirements.',
    ),
    429 => choose(
      'طلبات كثيرة. انتظر قليلاً ثم حاول مجدداً.',
      'Too many requests. Wait before retrying.',
    ),
    411 => choose(
      'تعذر إرسال حجم الملف. أعد اختيار الملف.',
      'File length could not be sent. Select the file again.',
    ),
    413 => choose(
      'حجم الملف يتجاوز الحد المسموح.',
      'The file exceeds the allowed size.',
    ),
    _ => choose(
      'تعذر إكمال العملية. حاول مجدداً.',
      'Unable to complete the action. Please retry.',
    ),
  };
}

String leaseFieldError(String message, bool arabic) {
  final m = message.toLowerCase();
  String choose(String ar, String en) => arabic ? ar : en;
  if (m.contains('strictly greater') || m.contains('enddate must')) {
    return choose(
      'يجب أن تكون النهاية بعد البداية.',
      'End date must be after start date.',
    );
  }
  if (m.contains('required') ||
      m.contains('empty') ||
      m.contains('whitespace')) {
    return choose('هذا الحقل مطلوب.', 'This field is required.');
  }
  if (m.contains('1 and 28') || m.contains('between 1')) {
    return choose('أدخل يوماً بين 1 و28.', 'Enter a day between 1 and 28.');
  }
  if (m.contains('negative')) {
    return choose(
      'لا يمكن أن يكون المبلغ سالباً.',
      'The amount cannot be negative.',
    );
  }
  if (m.contains('greater than zero')) {
    return choose(
      'يجب أن يكون المبلغ أكبر من صفر.',
      'The amount must be greater than zero.',
    );
  }
  // A validation response is user data, but do not display arbitrary technical output.
  if (message.length <= 300 &&
      !RegExp(
        r'exception|stack|sql|trace|system\.|https?://',
        caseSensitive: false,
      ).hasMatch(message)) {
    return message;
  }
  return choose('القيمة غير صالحة.', 'Invalid value.');
}

const leaseErrorMessages = <String, List<String>>{
  'UPLOAD_URL_REJECTED': [
    'رابط الرفع غير صالح أو انتهت صلاحيته. أعد المحاولة للحصول على رابط جديد.',
    'The upload URL was rejected or expired. Retry to obtain a new URL.',
  ],
  'LEASE_EDIT_NOT_DRAFT': [
    'يمكن تعديل شروط المسودة فقط؛ لا يقبل الخادم تعديل هذه الحالة.',
    'Only Draft contract terms can be edited; the server rejected this status.',
  ],
  'LEASE_EDIT_SIGNED_DOCUMENT_ATTACHED': [
    'لا يمكن تعديل الشروط بعد إرفاق عقد موقّع.',
    'Terms cannot be edited after a signed contract document is attached.',
  ],
  'LEASE_EDIT_RENEWAL_IDENTITY_LOCKED': [
    'لا يمكن تغيير الوحدة أو المستأجر في مسودة التجديد.',
    'The apartment and tenant cannot change on a renewal draft.',
  ],
  'LEASE_RENEW_START_BEFORE_PRIOR_END': [
    'يجب أن يبدأ التجديد في نهاية العقد السابق أو بعدها.',
    'Renewal must start on or after the prior contract ends.',
  ],
  'LEASE_ACTIVATE_INVALID_STATUS': [
    'حالة العقد الحالية لا تسمح بالتفعيل.',
    'The current contract status does not allow activation.',
  ],
  'LEASE_ACTIVATE_BEFORE_START_DATE': [
    'لا يمكن التفعيل قبل تاريخ بداية العقد.',
    'The contract cannot activate before its start date.',
  ],
  'LEASE_ACTIVATE_SIGNED_DOCUMENT_REQUIRED': [
    'أرفق مستند عقد موقّع قبل التفعيل.',
    'Attach a signed contract document before activation.',
  ],
  'LEASE_RENEW_INVALID_STATUS': [
    'لا يمكن تجديد العقد في حالته الحالية.',
    'This contract cannot be renewed in its current status.',
  ],
  'LEASE_TERMINATE_INVALID_STATUS': [
    'لا يمكن إنهاء العقد في حالته الحالية.',
    'This contract cannot be terminated in its current status.',
  ],
  'LEASE_TERMINATE_DATE_BEFORE_START': [
    'تاريخ الإنهاء لا يمكن أن يسبق بداية العقد.',
    'Termination cannot precede the contract start.',
  ],
  'LEASE_TERMINATE_DATE_IN_FUTURE': [
    'تاريخ الإنهاء لا يمكن أن يكون في المستقبل.',
    'Termination date cannot be in the future.',
  ],
  'LEASE_TERMINATE_DEDUCTION_REASON_REQUIRED': [
    'أدخل سبب اقتطاع مبلغ من التأمين.',
    'Enter a reason for the deposit deduction.',
  ],
  'UTILITY_ACCOUNT_ALREADY_LINKED': [
    'يوجد حساب مرتبط من هذا النوع بالفعل.',
    'An account of this type is already linked.',
  ],
  'UTILITY_ACCOUNT_NUMBER_INVALID': [
    'رقم اشتراك المرفق غير صالح.',
    'The utility account number is invalid.',
  ],
  'INVALID_RESPONSE': [
    'تعذر قراءة استجابة الخادم. حاول مجدداً.',
    'Unable to read the server response. Retry.',
  ],
  'SESSION_CHANGED': [
    'تغيّرت الجلسة. أعد فتح الصفحة.',
    'The session changed. Reopen this screen.',
  ],
  'FILE_TYPE': ['نوع الملف غير مدعوم.', 'Unsupported file type.'],
  'FILE_SIZE': [
    'اختر ملفاً لا يتجاوز 25 MB.',
    'Choose a file no larger than 25 MB.',
  ],
  'FILE_OPEN': [
    'تعذر فتح الملف على هذا الجهاز. حاول التنزيل.',
    'The file could not open on this device. Try downloading.',
  ],
  'TIMEOUT': [
    'انتهت مهلة الطلب. حاول مجدداً.',
    'The request timed out. Please retry.',
  ],
};
