import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../domain/notification_models.dart';

class NotificationsRepository {
  NotificationsRepository(this._api);
  final ApiClient _api;
  int _scope = 0;
  void clear() => _scope++;

  Future<T> _scoped<T>(Future<T> Function() request) async {
    final scope = _scope;
    final result = await request();
    if (scope != _scope) {
      throw const ApiProblem(
        message: 'Session changed',
        code: 'SESSION_CHANGED',
      );
    }
    return result;
  }

  Future<List<InboxNotification>> inbox(NotificationPageCursor cursor) =>
      _scoped(
        () async =>
            (await _api.getList(
                  '/api/v1/notifications/me',
                  query: cursor.query,
                ))
                .map(InboxNotification.fromJson)
                .where((item) => item.id.isNotEmpty)
                .toList(),
      );
  Future<int> unreadCount() =>
      _scoped(() => _api.getInt('/api/v1/notifications/me/unread-count'));
  Future<void> markRead(String id) => _scoped(
    () =>
        _api.patchVoid('/api/v1/notifications/${Uri.encodeComponent(id)}/read'),
  );
  Future<int> markAllRead() => _scoped(() async {
    final value = await _api.patchJson('/api/v1/notifications/me/read-all');
    return (value['markedCount'] as num?)?.toInt() ?? 0;
  });
}
