import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../storage/secure_session_store.dart';
import 'api_problem.dart';
import 'http_client_factory.dart';

class ApiClient {
  ApiClient({
    required Uri baseUri,
    required SecureSessionStore sessionStore,
    http.Client? client,
  }) : _baseUri = baseUri,
       _sessionStore = sessionStore,
       _http = client ?? createAqariHttpClient();

  final Uri _baseUri;
  final SecureSessionStore _sessionStore;
  final http.Client _http;
  Future<String?>? _refreshInFlight;
  void Function()? onSessionExpired;

  Future<Map<String, dynamic>> getJson(
    String path, {
    Map<String, String>? query,
    bool public = false,
  }) async =>
      _object(await _requestJson('GET', path, query: query, public: public));

  Future<Map<String, dynamic>?> getOptionalJson(String path) async {
    final value = await _requestJson('GET', path);
    if (value is Map<String, dynamic> && value.isEmpty) return null;
    return _object(value);
  }

  Future<int> getInt(String path, {Map<String, String>? query}) async {
    final value = await _requestJson('GET', path, query: query);
    if (value is num) return value.toInt();
    throw const ApiProblem(message: 'استجابة الخادم غير صالحة.');
  }

  Future<String> postId(
    String path, {
    required Map<String, dynamic> body,
  }) async {
    final value = await _requestJson('POST', path, body: body);
    if (value is String && value.isNotEmpty) return value;
    throw const ApiProblem(message: 'Invalid identifier response');
  }

  Future<void> postVoid(String path, {Map<String, dynamic>? body}) async {
    await _requestJson('POST', path, body: body);
  }

  Future<List<Map<String, dynamic>>> getList(
    String path, {
    Map<String, String>? query,
  }) async {
    final value = await _requestJson('GET', path, query: query);
    if (value is! List || value.any((item) => item is! Map<String, dynamic>)) {
      throw const ApiProblem(message: 'استجابة الخادم غير صالحة.');
    }
    return value.cast<Map<String, dynamic>>();
  }

  Future<void> putJson(
    String path, {
    required Map<String, dynamic> body,
  }) async {
    await _requestJson('PUT', path, body: body);
  }

  Future<void> patchVoid(String path) async {
    await _requestJson('PATCH', path);
  }

  Future<Map<String, dynamic>> patchJson(String path) async =>
      _object(await _requestJson('PATCH', path));

  Future<void> delete(String path) async {
    await _requestJson('DELETE', path);
  }

  Map<String, dynamic> _object(Object? value) {
    if (value is Map<String, dynamic>) return value;
    throw const ApiProblem(message: 'استجابة الخادم غير صالحة.');
  }

  Future<Map<String, dynamic>> postJson(
    String path, {
    Map<String, dynamic>? body,
    bool public = false,
  }) async =>
      _object(await _requestJson('POST', path, body: body, public: public));

  Future<Object?> _requestJson(
    String method,
    String path, {
    Map<String, dynamic>? body,
    Map<String, String>? query,
    bool public = false,
    bool retried = false,
    bool pdf = false,
  }) async {
    try {
      final headers = <String, String>{
        'Accept': pdf
            ? 'application/pdf, application/problem+json'
            : 'application/json',
        'Content-Type': 'application/json',
      };
      if (!public) {
        final token = await _sessionStore.readAccessToken();
        if (token != null && token.isNotEmpty) {
          headers['Authorization'] = 'Bearer $token';
        }
      }
      final request = http.Request(method, _resolve(path, query: query))
        ..headers.addAll(headers);
      if (body != null) request.body = jsonEncode(body);
      final response = await _send(request);
      await _captureRefreshCookie(response);
      if (pdf && _isSuccessful(response.statusCode)) {
        final bytes = response.bodyBytes;
        if (bytes.length < 5 ||
            ascii.decode(bytes.take(5).toList(), allowInvalid: true) !=
                '%PDF-') {
          throw const ApiProblem(
            message: 'Invalid PDF response',
            code: 'INVALID_RESPONSE',
          );
        }
        return bytes;
      }
      final decoded = _decode(response);
      if (_isSuccessful(response.statusCode)) {
        return decoded;
      }
      if (response.statusCode == 401 && !public && !retried) {
        final refreshed = await _refreshAccessToken();
        if (refreshed != null) {
          return await _requestJson(
            method,
            path,
            body: body,
            query: query,
            public: public,
            retried: true,
            pdf: pdf,
          );
        }
        onSessionExpired?.call();
      }
      throw ApiProblem.fromJson(
        response.statusCode,
        decoded,
        retryAfterSeconds: int.tryParse(response.headers['retry-after'] ?? ''),
      );
    } on ApiProblem {
      rethrow;
    } on TimeoutException {
      throw const ApiProblem(
        message: 'انتهت مهلة الطلب.',
        code: 'TIMEOUT',
        isNetworkFailure: true,
      );
    } on http.ClientException {
      throw ApiProblem.network();
    } on FormatException {
      throw const ApiProblem(message: 'استجابة الخادم غير صالحة.');
    }
  }

  /// Uses the same session refresh, error parsing and timeout path as JSON reads.
  Future<Uint8List> getPdf(String path) async =>
      await _requestJson('GET', path, pdf: true) as Uint8List;

  Future<http.Response> _send(http.BaseRequest request) async {
    final streamed = await _http
        .send(request)
        .timeout(const Duration(seconds: 20));
    return http.Response.fromStream(
      streamed,
    ).timeout(const Duration(seconds: 30));
  }

  Object? _decode(http.Response response) {
    if (response.statusCode == 204) return <String, dynamic>{};
    if (response.bodyBytes.isEmpty) return <String, dynamic>{};
    try {
      return jsonDecode(utf8.decode(response.bodyBytes));
    } on FormatException {
      if (_isSuccessful(response.statusCode)) rethrow;
      return null;
    }
  }

  Future<String?> _refreshAccessToken() {
    return _refreshInFlight ??= _performRefresh().whenComplete(() {
      _refreshInFlight = null;
    });
  }

  Future<String?> _performRefresh() async {
    final refreshToken = await _sessionStore.readRefreshToken();
    if (!usesBrowserCookieJar &&
        (refreshToken == null || refreshToken.isEmpty)) {
      return null;
    }
    try {
      final request = http.Request('POST', _resolve('/api/v1/auth/refresh'))
        ..headers.addAll(const {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        })
        ..body = jsonEncode(
          refreshToken == null || refreshToken.isEmpty
              ? <String, dynamic>{}
              : {'refreshToken': refreshToken},
        );
      final response = await _send(request);
      await _captureRefreshCookie(response);
      if (!_isSuccessful(response.statusCode)) {
        await _sessionStore.clear();
        return null;
      }
      final body = _decode(response);
      final accessToken = body is Map ? body['accessToken']?.toString() : null;
      if (accessToken == null || accessToken.isEmpty) return null;
      await _sessionStore.writeAccessToken(accessToken);
      return accessToken;
    } on TimeoutException {
      throw const ApiProblem(
        message: 'انتهت مهلة الطلب.',
        code: 'TIMEOUT',
        isNetworkFailure: true,
      );
    } on http.ClientException {
      throw ApiProblem.network();
    } on FormatException {
      throw const ApiProblem(message: 'استجابة تجديد الجلسة غير صالحة.');
    }
  }

  Future<void> _captureRefreshCookie(http.Response response) async {
    final header = response.headers['set-cookie'];
    if (header == null || header.isEmpty) return;
    final match = RegExp(
      r'(?:^|,\s*)refreshToken=([^;,\s]+)',
    ).firstMatch(header);
    final value = match?.group(1);
    if (value != null && value.isNotEmpty) {
      await _sessionStore.writeRefreshToken(value);
    }
  }

  bool _isSuccessful(int statusCode) => statusCode >= 200 && statusCode < 300;

  Uri _resolve(String path, {Map<String, String>? query}) {
    final basePath = _baseUri.path.endsWith('/')
        ? _baseUri.path.substring(0, _baseUri.path.length - 1)
        : _baseUri.path;
    final requestPath = path.startsWith('/') ? path : '/$path';
    return _baseUri.replace(
      path: '$basePath$requestPath',
      queryParameters: query,
    );
  }

  void close() => _http.close();
}
