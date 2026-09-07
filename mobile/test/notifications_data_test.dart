import 'dart:convert';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/notifications/application/notifications_controller.dart';
import 'package:aqarios_mobile/features/notifications/data/notifications_repository.dart';
import 'package:aqarios_mobile/features/notifications/domain/notification_models.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test(
    'personal inbox uses paired cursor, authoritative count, and PATCH endpoints',
    () async {
      final requests = <http.Request>[];
      final client = ApiClient(
        baseUri: Uri.parse('https://api.example.test'),
        sessionStore: _Store(),
        client: MockClient((request) async {
          requests.add(request);
          if (request.url.path.endsWith('unread-count')) {
            return http.Response('7', 200);
          }
          if (request.url.path.endsWith('read-all')) {
            return http.Response(jsonEncode({'markedCount': 3}), 200);
          }
          if (request.method == 'PATCH') {
            return http.Response('', 204);
          }
          return http.Response.bytes(
            utf8.encode(jsonEncode([_notification()])),
            200,
            headers: {'content-type': 'application/json'},
          );
        }),
      );
      final repository = NotificationsRepository(client);
      final page = await repository.inbox(
        const NotificationPageCursor(
          createdAt: '2026-09-04T10:00:00Z',
          id: 'n1',
        ),
      );
      expect(page.single.canMarkRead, true);
      expect(await repository.unreadCount(), 7);
      await repository.markRead('n1');
      expect(await repository.markAllRead(), 3);
      final inbox = requests.first;
      expect(inbox.url.queryParameters, {
        'pageSize': '50',
        'lastSeenCreatedAt': '2026-09-04T10:00:00Z',
        'lastSeenId': 'n1',
      });
      expect(requests[2].method, 'PATCH');
      expect(requests[2].url.path, '/api/v1/notifications/n1/read');
      expect(requests[3].url.path, '/api/v1/notifications/me/read-all');
      client.close();
    },
  );

  test('pending notifications are never eligible for mark-read', () {
    expect(
      InboxNotification.fromJson({..._notification(), 'status': 0}).canMarkRead,
      false,
    );
    expect(
      InboxNotification.fromJson({
        ..._notification(),
        'readAt': '2026-09-04T11:00:00Z',
      }).canMarkRead,
      false,
    );
  });

  test(
    'notification screen reuses the shell count without requesting it again',
    () async {
      final requests = <http.Request>[];
      final client = ApiClient(
        baseUri: Uri.parse('https://api.example.test'),
        sessionStore: _Store(),
        client: MockClient((request) async {
          requests.add(request);
          if (request.method == 'PATCH') return http.Response('', 204);
          return http.Response.bytes(
            utf8.encode(jsonEncode([_notification()])),
            200,
            headers: {'content-type': 'application/json'},
          );
        }),
      );
      final controller = NotificationsController(
        NotificationsRepository(client),
        unreadCount: 7,
      );

      await controller.load();
      expect(controller.unreadCount, 7);
      expect(
        requests.where((request) => request.url.path.endsWith('unread-count')),
        isEmpty,
      );

      await controller.open(controller.items.single);
      expect(controller.unreadCount, 6);
      expect(
        requests.where((request) => request.url.path.endsWith('unread-count')),
        isEmpty,
      );

      controller.dispose();
      client.close();
    },
  );

  test('notification initialization has one unread-count owner', () async {
    var countRequests = 0;
    final client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: _Store(),
      client: MockClient((request) async {
        if (request.url.path.endsWith('unread-count')) {
          countRequests++;
          return http.Response('2', 200);
        }
        return http.Response('[]', 200);
      }),
    );
    final controller = NotificationsController(
      NotificationsRepository(client),
      pollingInterval: const Duration(days: 1),
    );

    await Future.wait([controller.initialize(), controller.initialize()]);
    await controller.initialize();

    expect(countRequests, 1);
    expect(controller.unreadCount, 2);
    controller.dispose();
    client.close();
  });
}

Map<String, dynamic> _notification() => {
  'id': 'n1',
  'recipientUserId': 'hidden',
  'subject': 'عنوان عربي طويل',
  'body': 'محتوى الإشعار',
  'notificationType': 0,
  'priority': 1,
  'status': 1,
  'createdAt': '2026-09-04T10:00:00Z',
  'readAt': null,
};

class _Store extends SecureSessionStore {
  @override
  Future<String?> readAccessToken() async => 'token';
  @override
  Future<String?> readRefreshToken() async => null;
}
