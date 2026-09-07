import 'dart:typed_data';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../domain/payment_models.dart';

class PaymentsRepository {
  PaymentsRepository(this.api);
  final ApiClient api;
  int _scope = 0;
  void clear() => _scope++;
  Future<T> _scoped<T>(Future<T> Function() request) async {
    final scope = _scope;
    try {
      final result = await request();
      if (_scope != scope) {
        throw const ApiProblem(
          message: 'Session changed',
          code: 'SESSION_CHANGED',
        );
      }
      return result;
    } on TypeError {
      throw const ApiProblem(
        message: 'Invalid response',
        code: 'INVALID_RESPONSE',
      );
    }
  }

  String _id(String value) => Uri.encodeComponent(value);
  Future<List<RentPayment>> list(PaymentFilters filters) => _scoped(
    () async => (await api.getList(
      '/api/v1/rent-payments',
      query: filters.query,
    )).map(RentPayment.new).toList(),
  );
  Future<List<RentPayment>> tenantPayments() => _scoped(
    () async => (await api.getList(
      '/api/v1/tenant-portal/payments',
    )).map(RentPayment.new).toList(),
  );
  Future<RentPayment> detail(String id) => _scoped(
    () async =>
        RentPayment(await api.getJson('/api/v1/rent-payments/${_id(id)}')),
  );
  Future<VerificationPage> queue([String? cursor]) => _scoped(
    () async => VerificationPage(
      await api.getJson(
        '/api/v1/owner/payments/pending-verifications',
        query: {'pageSize': '50', if (cursor != null) 'cursor': cursor},
      ),
    ),
  );
  Future<void> approve(String paymentId, String submissionId) => _scoped(
    () => api.postVoid(
      '/api/v1/owner/payments/${_id(paymentId)}/submissions/${_id(submissionId)}/approve',
    ),
  );
  Future<void> reject(
    String paymentId,
    String submissionId,
    String reason,
  ) => _scoped(
    () => api.postVoid(
      '/api/v1/owner/payments/${_id(paymentId)}/submissions/${_id(submissionId)}/reject',
      body: {'reason': reason.trim()},
    ),
  );
  Future<String> submit(String id, PaymentDraft draft) => _scoped(
    () => api.postId(
      '/api/v1/tenant/payments/${_id(id)}/submit-verification',
      body: draft.payload,
    ),
  );
  Future<void> remind(String id) => _scoped(
    () => api.postVoid('/api/v1/rent-payments/${_id(id)}/remind', body: {}),
  );
  Future<PaymentReceipt?> receipt(String id) => _scoped(() async {
    try {
      return PaymentReceipt(
        await api.getJson('/api/v1/rent-payments/${_id(id)}/receipt'),
      );
    } on ApiProblem catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  });
  Future<Uri> fileUrl(String id, {bool inline = false}) => _scoped(() async {
    final value = await api.getJson(
      '/api/v1/files/${_id(id)}/download-url',
      query: {'inline': '$inline'},
    );
    final uri = Uri.tryParse(value['downloadUrl']?.toString() ?? '');
    if (uri == null ||
        !uri.hasAuthority ||
        !['https', 'http'].contains(uri.scheme) ||
        uri.userInfo.isNotEmpty) {
      throw const ApiProblem(
        message: 'Invalid file URL',
        code: 'INVALID_RESPONSE',
      );
    }
    return uri;
  });
  Future<Uint8List> settlement(String id, {required bool tenant}) => _scoped(
    () => api.getPdf(
      tenant
          ? '/api/v1/tenant-portal/payments/${_id(id)}/settlement-statement'
          : '/api/v1/rent-payments/${_id(id)}/settlement-statement',
    ),
  );
}
