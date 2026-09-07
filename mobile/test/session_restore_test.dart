import 'dart:async';
import 'dart:convert';

import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/auth/application/session_controller.dart';
import 'package:aqarios_mobile/features/auth/data/auth_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test(
    'valid persisted session restores the authenticated shell state',
    () async {
      final harness = _Harness((request) async {
        expect(request.url.path, '/api/v1/auth/me');
        return _profileResponse();
      });

      await harness.session.bootstrap();

      expect(harness.session.status, SessionStatus.authenticated);
      expect(harness.session.user?.id, 'user-1');
      harness.close();
    },
  );

  test(
    'expired access token refreshes once and retries auth me once',
    () async {
      var authMeCalls = 0;
      final paths = <String>[];
      final harness = _Harness((request) async {
        paths.add(request.url.path);
        if (request.url.path == '/api/v1/auth/refresh') {
          return http.Response(jsonEncode({'accessToken': 'new-access'}), 200);
        }
        authMeCalls++;
        return authMeCalls == 1 ? http.Response('{}', 401) : _profileResponse();
      });

      await harness.session.bootstrap();

      expect(harness.session.status, SessionStatus.authenticated);
      expect(paths, [
        '/api/v1/auth/me',
        '/api/v1/auth/refresh',
        '/api/v1/auth/me',
      ]);
      expect(harness.store.accessToken, 'new-access');
      harness.close();
    },
  );

  test(
    'invalid refresh token clears the stale session and shows login',
    () async {
      final harness = _Harness((request) async => http.Response('{}', 401));

      await harness.session.bootstrap();

      expect(harness.session.status, SessionStatus.unauthenticated);
      expect(harness.store.clearCalls, greaterThan(0));
      harness.close();
    },
  );

  test('logged-out startup completes without making a request', () async {
    var requested = false;
    final harness = _Harness((request) async {
      requested = true;
      return _profileResponse();
    }, persistent: false);

    await harness.session.bootstrap();

    expect(harness.session.status, SessionStatus.unauthenticated);
    expect(requested, false);
    harness.close();
  });

  test(
    'unavailable backend leaves initialization after bounded timeout',
    () async {
      final harness = _Harness(
        (request) => Completer<http.Response>().future,
        bootstrapTimeout: const Duration(milliseconds: 20),
      );

      await harness.session.bootstrap();

      expect(harness.session.status, SessionStatus.bootstrapFailure);
      expect(harness.session.error?.code, 'TIMEOUT');
      harness.close();
    },
  );
}

http.Response _profileResponse() => http.Response(
  jsonEncode({
    'id': 'user-1',
    'fullName': 'Owner',
    'preferredLanguage': 'ar',
    'activeCompanyId': 'company-1',
    'companyRoles': [
      {'companyId': 'company-1', 'roleCode': 'COMPANY_ADMIN'},
    ],
    'permissions': <String>[],
    'systemRoles': <String>[],
  }),
  200,
  headers: {'content-type': 'application/json'},
);

class _Harness {
  _Harness(
    Future<http.Response> Function(http.Request) handler, {
    bool persistent = true,
    Duration bootstrapTimeout = const Duration(seconds: 1),
  }) : store = _SessionStore(persistent: persistent) {
    client = ApiClient(
      baseUri: Uri.parse('http://10.0.2.2:5235'),
      sessionStore: store,
      client: MockClient(handler),
    );
    session = SessionController(
      AuthRepository(client, store),
      bootstrapTimeout: bootstrapTimeout,
    );
  }

  final _SessionStore store;
  late ApiClient client;
  late SessionController session;

  void close() {
    session.dispose();
    client.close();
  }
}

class _SessionStore extends SecureSessionStore {
  _SessionStore({required this.persistent});

  final bool persistent;
  String? accessToken = 'expired-access';
  String? refreshToken = 'refresh-token';
  int clearCalls = 0;

  @override
  Future<bool> prepareForBootstrap() async => persistent;

  @override
  Future<String?> readAccessToken() async => accessToken;

  @override
  Future<String?> readRefreshToken() async => refreshToken;

  @override
  Future<void> writeAccessToken(String token) async => accessToken = token;

  @override
  Future<void> writeRefreshToken(String token) async => refreshToken = token;

  @override
  Future<void> clear() async {
    clearCalls++;
    accessToken = null;
    refreshToken = null;
  }
}
