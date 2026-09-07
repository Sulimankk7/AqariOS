class ApiProblem implements Exception {
  const ApiProblem({
    required this.message,
    this.statusCode,
    this.title,
    this.detail,
    this.code,
    this.validationErrors = const {},
    this.isNetworkFailure = false,
    this.retryAfterSeconds,
  });

  final String message;
  final int? statusCode;
  final String? title;
  final String? detail;
  final String? code;
  final Map<String, List<String>> validationErrors;
  final bool isNetworkFailure;
  final int? retryAfterSeconds;

  bool get isUnauthorized => statusCode == 401;

  factory ApiProblem.fromJson(
    int status,
    Object? body, {
    int? retryAfterSeconds,
  }) {
    if (body is! Map<String, dynamic>) {
      return ApiProblem(
        message: 'تعذر إكمال الطلب.',
        statusCode: status,
        retryAfterSeconds: retryAfterSeconds,
      );
    }
    final errors = <String, List<String>>{};
    final rawErrors = body['errors'];
    if (rawErrors is Map) {
      for (final entry in rawErrors.entries) {
        final value = entry.value;
        errors[entry.key.toString()] = value is List
            ? value.map((item) => item.toString()).toList(growable: false)
            : [value.toString()];
      }
    }
    final detail = body['detail']?.toString();
    final title = body['title']?.toString();
    final firstValidation = errors.values.expand((value) => value).firstOrNull;
    return ApiProblem(
      message: firstValidation ?? detail ?? title ?? 'تعذر إكمال الطلب.',
      statusCode: status,
      title: title,
      detail: detail,
      code: body['code']?.toString(),
      validationErrors: errors,
      retryAfterSeconds: retryAfterSeconds,
    );
  }

  factory ApiProblem.network() => const ApiProblem(
    message: 'تعذر الاتصال بالخادم. تحقق من الشبكة وحاول مجدداً.',
    isNetworkFailure: true,
  );

  @override
  String toString() => message;
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
