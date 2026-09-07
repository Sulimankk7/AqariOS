import 'package:flutter/foundation.dart';
import '../../../core/network/api_problem.dart';

/// Route-owned state: one load at a time; no notification after disposal.
class PropertyController<T> extends ChangeNotifier {
  PropertyController(this.fetch);
  final Future<T> Function(bool force) fetch;
  T? value;
  ApiProblem? error;
  bool loading = false;
  bool _disposed = false;
  Future<void> load({bool force = false}) async {
    if (loading || _disposed) return;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final result = await fetch(force);
      if (!_disposed) value = result;
    } on ApiProblem catch (problem) {
      if (!_disposed) error = problem;
    } catch (_) {
      if (!_disposed) error = const ApiProblem(message: 'Unexpected response');
    } finally {
      if (!_disposed) {
        loading = false;
        notifyListeners();
      }
    }
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
