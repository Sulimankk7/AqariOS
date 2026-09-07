import 'package:flutter/foundation.dart';

import '../data/expenses_repository.dart';
import '../domain/expense_models.dart';

class ExpensesController extends ChangeNotifier {
  ExpensesController(this.repository);

  final ExpensesRepository repository;
  ExpenseFilters filters = const ExpenseFilters();
  List<Expense> items = [];
  final List<ExpenseFilters> _history = [];
  bool loading = false, _disposed = false;
  Object? error;
  int _generation = 0;

  int get page => _history.length + 1;
  bool get hasPrevious => _history.isNotEmpty;
  bool get hasNext => items.length == 50 && items.last.expenseDate.isNotEmpty;

  Future<void> load() async {
    final generation = ++_generation;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final result = await repository.list(filters);
      if (!_disposed && generation == _generation) items = result;
    } catch (e) {
      if (!_disposed && generation == _generation) error = e;
    } finally {
      if (!_disposed && generation == _generation) {
        loading = false;
        notifyListeners();
      }
    }
  }

  Future<void> apply(ExpenseFilters value) {
    filters = value.cursor('', '');
    _history.clear();
    return load();
  }

  Future<void> next() {
    if (!hasNext || loading) return Future.value();
    _history.add(filters);
    filters = filters.cursor(items.last.id, items.last.expenseDate);
    return load();
  }

  Future<void> previous() {
    if (!hasPrevious || loading) return Future.value();
    filters = _history.removeLast();
    return load();
  }

  @override
  void dispose() {
    _disposed = true;
    _generation++;
    super.dispose();
  }
}
