import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../domain/expense_models.dart';

class ExpensesRepository {
  ExpensesRepository(this.api);

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

  Future<List<Expense>> list(ExpenseFilters filters) => _scoped(
    () async => (await api.getList(
      '/api/v1/expenses',
      query: filters.query,
    )).map(Expense.new).toList(),
  );

  Future<ExpenseDetail> detail(String id) => _scoped(
    () async => ExpenseDetail(await api.getJson('/api/v1/expenses/${_id(id)}')),
  );

  Future<Uri> receiptUrl(String fileId) => _scoped(() async {
    final value = await api.getJson(
      '/api/v1/files/${_id(fileId)}/download-url',
      query: const {'inline': 'false'},
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
}
