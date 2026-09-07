import 'dart:convert';

import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/auth/data/auth_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test('registration preserves the existing Web contract', () async {
    final harness = _Harness((request) async {
      expect(request.url.path, '/api/v1/auth/register');
      final body = jsonDecode(request.body) as Map<String, dynamic>;
      expect(body['companyType'], 0);
      expect(body['countryCode'], 'JO');
      expect(body['preferredLanguage'], 'ar');
      expect(body.containsKey('email'), isFalse);
      return http.Response(jsonEncode({'registrationId': 'r1'}), 200);
    });
    await harness.repository.register(
      fullName: 'Tenant Owner',
      companyName: 'Company',
      displayName: '',
      companyType: 0,
      email: '',
      phone: '+962790000000',
      password: 'Password1!',
      countryCode: 'JO',
      preferredLanguage: 'ar',
    );
    harness.close();
  });

  test('OTP verify stores the authenticated session', () async {
    final harness = _Harness((request) async {
      expect(request.url.path, '/api/v1/auth/otp/verify');
      expect(jsonDecode(request.body), {
        'phone': '+962790000000',
        'code': '123456',
        'purpose': 0,
      });
      return http.Response(jsonEncode(_loginResponse), 200);
    });
    final user = await harness.repository.verifyOtp('+962790000000', '123456');
    expect(user.id, 'u1');
    expect(harness.store.accessToken, 'access');
    harness.close();
  });

  test(
    'password reset request, verify and complete use exact endpoints',
    () async {
      final paths = <String>[];
      final harness = _Harness((request) async {
        paths.add(request.url.path);
        if (request.url.path.endsWith('verify-otp')) {
          return http.Response(
            jsonEncode({'resetAuthorization': 'credential'}),
            200,
          );
        }
        return http.Response(jsonEncode({'message': 'ok'}), 200);
      });
      await harness.repository.requestPasswordReset('Phone', '+962790000000');
      final credential = await harness.repository.verifyPasswordResetOtp(
        '+962790000000',
        '123456',
      );
      await harness.repository.completePasswordReset(credential, 'Password1!');
      expect(paths, [
        '/api/v1/auth/password-reset/request',
        '/api/v1/auth/password-reset/verify-otp',
        '/api/v1/auth/password-reset/complete',
      ]);
      harness.close();
    },
  );
}

const _loginResponse = {
  'accessToken': 'access',
  'isPersistentSession': true,
  'user': {
    'id': 'u1',
    'fullName': 'User',
    'preferredLanguage': 'ar',
    'activeCompanyId': 'c1',
    'companyRoles': [
      {'companyId': 'c1', 'roleCode': 'COMPANY_ADMIN'},
    ],
    'permissions': <String>[],
    'systemRoles': <String>[],
  },
};

class _Harness {
  _Harness(Future<http.Response> Function(http.Request) handler) {
    client = ApiClient(
      baseUri: Uri.parse('https://example.test'),
      sessionStore: store,
      client: MockClient(handler),
    );
    repository = AuthRepository(client, store);
  }
  final store = _Store();
  late final ApiClient client;
  late final AuthRepository repository;
  void close() => client.close();
}

class _Store extends SecureSessionStore {
  String? accessToken;
  @override
  Future<String?> readAccessToken() async => accessToken;
  @override
  Future<String?> readRefreshToken() async => null;
  @override
  Future<void> writeAccessToken(String token) async => accessToken = token;
  @override
  Future<void> writePersistence(bool persistent) async {}
  @override
  Future<void> clear() async => accessToken = null;
}
