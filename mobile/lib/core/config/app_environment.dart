class AppEnvironment {
  const AppEnvironment._();

  static const String apiBaseUrl = String.fromEnvironment(
    'AQARIOS_API_BASE_URL',
    defaultValue: '',
  );

  static final Uri? apiBaseUri = parseApiBaseUri(apiBaseUrl);

  static Uri? parseApiBaseUri(String rawValue) {
    final value = rawValue.trim();
    if (value.isEmpty) return null;
    final uri = Uri.tryParse(value);
    if (uri == null ||
        (uri.scheme != 'http' && uri.scheme != 'https') ||
        uri.host.isEmpty ||
        uri.hasQuery ||
        uri.hasFragment ||
        uri.userInfo.isNotEmpty) {
      return null;
    }
    final normalizedPath = uri.path == '/'
        ? ''
        : uri.path.endsWith('/')
        ? uri.path.substring(0, uri.path.length - 1)
        : uri.path;
    return uri.replace(path: normalizedPath);
  }
}
