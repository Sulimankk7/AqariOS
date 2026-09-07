import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AppPreferences {
  AppPreferences({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _themeKey = 'aqarios.theme';
  static const _languageKey = 'aqarios.language';
  final FlutterSecureStorage _storage;

  Future<String?> readTheme() => _storage.read(key: _themeKey);
  Future<String?> readLanguage() => _storage.read(key: _languageKey);
  Future<void> writeTheme(String value) =>
      _storage.write(key: _themeKey, value: value);
  Future<void> writeLanguage(String value) =>
      _storage.write(key: _languageKey, value: value);
}
