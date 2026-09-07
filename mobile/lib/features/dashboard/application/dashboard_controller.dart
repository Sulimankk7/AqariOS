import 'package:flutter/foundation.dart';

import '../../../core/network/api_problem.dart';
import '../data/dashboard_repository.dart';
import '../domain/dashboard_summary.dart';

class DashboardController extends ChangeNotifier {
  DashboardController(this._repository);
  final DashboardRepository _repository;
  DashboardSummary? summary;
  ApiProblem? error;
  bool loading = false;

  Future<void> load({bool force = false}) async {
    if (loading) return;
    loading = true;
    error = null;
    notifyListeners();
    try {
      summary = await _repository.load(force: force);
    } on ApiProblem catch (problem) {
      error = problem;
    } finally {
      loading = false;
      notifyListeners();
    }
  }
}
