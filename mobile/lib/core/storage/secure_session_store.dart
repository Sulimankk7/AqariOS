import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureSessionStore {
  SecureSessionStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _accessTokenKey = 'aqarios.access_token';
  static const _refreshTokenKey = 'aqarios.refresh_token';
  static const _persistentKey = 'aqarios.persistent_session';
  final FlutterSecureStorage _storage;

  Future<String?> readAccessToken() => _storage.read(key: _accessTokenKey);
  Future<String?> readRefreshToken() => _storage.read(key: _refreshTokenKey);

  Future<void> writeAccessToken(String token) =>
      _storage.write(key: _accessTokenKey, value: token);

  Future<void> writeRefreshToken(String token) =>
      _storage.write(key: _refreshTokenKey, value: token);

  Future<void> writePersistence(bool persistent) =>
      _storage.write(key: _persistentKey, value: persistent.toString());

  Future<bool> prepareForBootstrap() async {
    final persistent = await _storage.read(key: _persistentKey);
    if (persistent == 'true') return true;
    await clear();
    return false;
  }

  Future<void> clear() async {
    await Future.wait([
      _storage.delete(key: _accessTokenKey),
      _storage.delete(key: _refreshTokenKey),
      _storage.delete(key: _persistentKey),
    ]);
  }
}
