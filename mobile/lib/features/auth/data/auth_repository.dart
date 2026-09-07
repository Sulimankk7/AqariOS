import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../../../core/storage/secure_session_store.dart';
import '../domain/user_profile.dart';

class AuthRepository {
  const AuthRepository(this._client, this._sessionStore);
  final ApiClient _client;
  final SecureSessionStore _sessionStore;

  Future<UserProfile> _acceptSession(Map<String, dynamic> json) async {
    final accessToken = json['accessToken']?.toString();
    final rawUser = json['user'];
    if (accessToken == null || rawUser is! Map) {
      throw const FormatException('Invalid authentication response');
    }
    await _sessionStore.writeAccessToken(accessToken);
    await _sessionStore.writePersistence(json['isPersistentSession'] == true);
    return UserProfile.fromJson(Map<String, dynamic>.from(rawUser));
  }

  Future<UserProfile> login({
    required String emailOrPhone,
    required String password,
    required bool rememberMe,
  }) async {
    final json = await _client.postJson(
      '/api/v1/auth/login',
      public: true,
      body: {
        'emailOrPhone': emailOrPhone,
        'password': password,
        'rememberMe': rememberMe,
      },
    );
    return _acceptSession(json);
  }

  Future<void> register({
    required String fullName,
    required String companyName,
    required String displayName,
    required int companyType,
    required String email,
    required String phone,
    required String password,
    required String countryCode,
    required String preferredLanguage,
  }) => _client
      .postJson(
        '/api/v1/auth/register',
        public: true,
        body: {
          'fullName': fullName.trim(),
          'companyName': companyName.trim(),
          if (displayName.trim().isNotEmpty) 'displayName': displayName.trim(),
          'companyType': companyType,
          if (email.trim().isNotEmpty) 'email': email.trim(),
          if (phone.trim().isNotEmpty) 'phone': phone.trim(),
          'password': password,
          'countryCode': countryCode,
          'preferredLanguage': preferredLanguage,
        },
      )
      .then((_) {});

  Future<void> requestOtp(String phone) => _client
      .postJson(
        '/api/v1/auth/otp/request',
        public: true,
        body: {'phone': phone.trim(), 'purpose': 0},
      )
      .then((_) {});

  Future<UserProfile> verifyOtp(String phone, String code) async =>
      _acceptSession(
        await _client.postJson(
          '/api/v1/auth/otp/verify',
          public: true,
          body: {'phone': phone.trim(), 'code': code, 'purpose': 0},
        ),
      );

  Future<void> requestPasswordReset(String method, String identifier) => _client
      .postJson(
        '/api/v1/auth/password-reset/request',
        public: true,
        body: {'deliveryMethod': method, 'identifier': identifier.trim()},
      )
      .then((_) {});

  Future<String> verifyPasswordResetOtp(String phone, String code) async {
    final json = await _client.postJson(
      '/api/v1/auth/password-reset/verify-otp',
      public: true,
      body: {'phone': phone.trim(), 'code': code},
    );
    final credential = json['resetAuthorization']?.toString();
    if (credential == null || credential.isEmpty) {
      throw const FormatException('Invalid password reset response');
    }
    return credential;
  }

  Future<void> completePasswordReset(String credential, String password) =>
      _client
          .postJson(
            '/api/v1/auth/password-reset/complete',
            public: true,
            body: {'resetCredential': credential, 'newPassword': password},
          )
          .then((_) {});

  Future<Map<String, dynamic>> activationStatus(String token) =>
      _client.getJson(
        '/api/v1/auth/tenant-activation-status',
        query: {'token': token.trim()},
        public: true,
      );

  Future<UserProfile> activateTenant(String token, String password) async =>
      _acceptSession(
        await _client.postJson(
          '/api/v1/auth/tenant-activate',
          public: true,
          body: {'activationToken': token.trim(), 'password': password},
        ),
      );

  Future<UserProfile> restore() async {
    final canRestore = await _sessionStore.prepareForBootstrap();
    if (!canRestore) {
      throw const ApiProblem(message: 'No persistent session', statusCode: 401);
    }
    try {
      final json = await _client.getJson('/api/v1/auth/me');
      return UserProfile.fromJson(json);
    } on ApiProblem catch (problem) {
      if (problem.isUnauthorized) await _sessionStore.clear();
      rethrow;
    }
  }

  Future<void> logout() async {
    final refreshToken = await _sessionStore.readRefreshToken();
    try {
      await _client.postJson(
        '/api/v1/auth/logout',
        body: refreshToken == null ? null : {'refreshToken': refreshToken},
      );
    } finally {
      await _sessionStore.clear();
    }
  }

  Future<void> logoutAll() async {
    try {
      await _client.postVoid('/api/v1/auth/logout-all', body: const {});
    } finally {
      await _sessionStore.clear();
    }
  }
}
