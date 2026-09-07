import 'package:aqarios_mobile/core/config/app_environment.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('preserves a configured LAN API origin', () {
    final uri = AppEnvironment.parseApiBaseUri('http://192.168.0.105:5235')!;

    expect(uri.toString(), 'http://192.168.0.105:5235');
    expect(uri.host, '192.168.0.105');
    expect(uri.port, 5235);
  });

  test('normalizes only a trailing slash', () {
    expect(
      AppEnvironment.parseApiBaseUri(
        'https://api.example.test/root/',
      ).toString(),
      'https://api.example.test/root',
    );
  });

  test('rejects missing or unsafe base URL values', () {
    expect(AppEnvironment.parseApiBaseUri(''), isNull);
    expect(AppEnvironment.parseApiBaseUri('localhost:5235'), isNull);
    expect(AppEnvironment.parseApiBaseUri('ftp://api.example.test'), isNull);
    expect(
      AppEnvironment.parseApiBaseUri('https://user:pass@example.test'),
      isNull,
    );
  });
}
