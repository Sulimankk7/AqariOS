import '../../../core/network/api_client.dart';
import '../domain/dashboard_summary.dart';

class DashboardRepository {
  DashboardRepository(this._client);
  final ApiClient _client;
  DashboardSummary? _cached;
  DateTime? _cachedAt;

  Future<DashboardSummary> load({bool force = false}) async {
    final now = DateTime.now();
    if (!force &&
        _cached != null &&
        _cachedAt != null &&
        now.difference(_cachedAt!) < const Duration(minutes: 5)) {
      return _cached!;
    }
    final json = await _client.getJson('/api/v1/dashboard/summary');
    _cached = DashboardSummary.fromJson(json);
    _cachedAt = now;
    return _cached!;
  }
}
