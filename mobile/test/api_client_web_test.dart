import 'dart:convert';

import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/network/api_problem.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test('sends Bearer authentication and decodes a typed JSON object', () async {
    final transport = MockClient((request) async {
      expect(
        request.url.toString(),
        'https://api.example.test/api/v1/dashboard/summary',
      );
      expect(request.headers['Authorization'], 'Bearer access-token');
      return http.Response(
        jsonEncode({
          'property': {'totalBuildings': 2},
        }),
        200,
      );
    });
    final client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: _MemorySessionStore(accessToken: 'access-token'),
      client: transport,
    );

    final response = await client.getJson('/api/v1/dashboard/summary');

    expect((response['property'] as Map)['totalBuildings'], 2);
    client.close();
  });

  test('resolves requests against the configured LAN host', () async {
    final transport = MockClient((request) async {
      expect(
        request.url.toString(),
        'http://192.168.0.105:5235/api/v1/auth/login',
      );
      return http.Response(jsonEncode({'accepted': true}), 200);
    });
    final client = ApiClient(
      baseUri: Uri.parse('http://192.168.0.105:5235'),
      sessionStore: _MemorySessionStore(),
      client: transport,
    );

    await client.postJson(
      '/api/v1/auth/login',
      public: true,
      body: {'emailOrPhone': 'test@example.test', 'password': 'redacted'},
    );

    client.close();
  });

  test('translates an RFC 7807 response without exposing internals', () async {
    final transport = MockClient(
      (_) async => http.Response(
        jsonEncode({
          'title': 'Validation failed',
          'detail': 'The submitted request is invalid.',
          'errors': {
            'emailOrPhone': ['الحقل مطلوب'],
          },
        }),
        400,
        headers: {'content-type': 'application/problem+json; charset=utf-8'},
      ),
    );
    final client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: _MemorySessionStore(),
      client: transport,
    );

    await expectLater(
      client.getJson('/api/v1/example'),
      throwsA(
        isA<ApiProblem>()
            .having((problem) => problem.statusCode, 'statusCode', 400)
            .having((problem) => problem.message, 'message', 'الحقل مطلوب'),
      ),
    );
    client.close();
  });
}

class _MemorySessionStore extends SecureSessionStore {
  _MemorySessionStore({this.accessToken});

  String? accessToken;
  String? refreshToken;

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
    accessToken = null;
    refreshToken = null;
  }
}
