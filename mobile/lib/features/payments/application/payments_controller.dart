import 'package:flutter/foundation.dart';
import '../data/payments_repository.dart';
import '../domain/payment_models.dart';

class PaymentsController extends ChangeNotifier {
  PaymentsController(this.repository, {this.tenant = false});
  final PaymentsRepository repository;
  final bool tenant;
  PaymentFilters filters = const PaymentFilters();
  List<RentPayment> items = [];
  final List<PaymentFilters> _history = [];
  bool loading = false, _disposed = false;
  Object? error;
  int _generation = 0;
  int get page => _history.length + 1;
  bool get hasPrevious => _history.isNotEmpty;
  bool get hasNext =>
      !tenant && items.length == 50 && items.last.dueDate.isNotEmpty;
  Future<void> load() async {
    final generation = ++_generation;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final result = tenant
          ? sortTenantPayments(await repository.tenantPayments())
          : await repository.list(filters);
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

  Future<void> apply(PaymentFilters value) {
    filters = value.cursor('', '');
    _history.clear();
    return load();
  }

  Future<void> next() {
    if (!hasNext || loading) return Future.value();
    _history.add(filters);
    filters = filters.cursor(items.last.id, items.last.dueDate);
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
