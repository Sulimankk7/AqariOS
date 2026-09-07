import 'dart:async';
import 'package:file_selector/file_selector.dart';
import 'package:http/http.dart' as http;
import 'package:url_launcher/url_launcher.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';

class LeaseFiles {
  LeaseFiles(this.api, {http.Client Function()? binaryClient})
    : _binaryClient = binaryClient ?? http.Client.new;
  final ApiClient api;
  final http.Client Function() _binaryClient;
  int _scope = 0;
  final _active = <http.Client>{};
  void clear() {
    _scope++;
    for (final client in _active) {
      client.close();
    }
    _active.clear();
  }

  void _checkScope(int scope) {
    if (_scope != scope) {
      throw const ApiProblem(
        message: 'Session changed',
        code: 'SESSION_CHANGED',
      );
    }
  }

  static const maxBytes = 25 * 1024 * 1024;
  static const mimeTypes = {
    'pdf': 'application/pdf',
    'png': 'image/png',
    'jpg': 'image/jpeg',
    'jpeg': 'image/jpeg',
    'doc': 'application/msword',
    'docx':
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  };
  Future<XFile?> select() => openFile(
    acceptedTypeGroups: [
      XTypeGroup(
        label: 'Documents',
        extensions: mimeTypes.keys.toList(),
        mimeTypes: mimeTypes.values.toSet().toList(),
      ),
    ],
  );
  Future<(int, String)> validate(XFile file) async {
    final length = await file.length();
    if (length > maxBytes || length == 0) {
      throw const ApiProblem(message: 'Invalid file size', code: 'FILE_SIZE');
    }
    final mime =
        file.mimeType ?? mimeTypes[file.name.split('.').last.toLowerCase()];
    if (mime == null ||
        !(mimeTypes.values.contains(mime) || mime == 'image/jpg')) {
      throw const ApiProblem(
        message: 'Unsupported file type',
        code: 'FILE_TYPE',
      );
    }
    return (length, mime);
  }

  Future<String> upload(
    String contractId,
    XFile file,
    void Function(String, double?) progress,
  ) => uploadFor('leasing', contractId, file, progress);

  /// Shared transport for existing module upload contracts.
  Future<String> uploadFor(
    String moduleName,
    String entityId,
    XFile file,
    void Function(String, double?) progress,
  ) async {
    final scope = _scope;
    final (size, mime) = await validate(file);
    _checkScope(scope);
    progress('preparing', null);
    final init = await api.postJson(
      '/api/v1/files/upload-request',
      body: {
        'moduleName': moduleName,
        'entityId': entityId,
        'filename': file.name,
        'mimeType': mime,
        'sizeBytes': size,
      },
    );
    _checkScope(scope);
    final uri = Uri.tryParse(init['uploadUrl'] as String);
    if (uri == null ||
        !uri.hasAuthority ||
        !['http', 'https'].contains(uri.scheme) ||
        uri.userInfo.isNotEmpty) {
      throw const ApiProblem(
        message: 'Invalid upload response',
        code: 'INVALID_RESPONSE',
      );
    }
    final client = _binaryClient();
    _active.add(client);
    try {
      // Dedicated credential-free transport: never use ApiClient or its browser cookie jar here.
      final request = http.StreamedRequest('PUT', uri)
        ..followRedirects = false
        ..contentLength = size
        ..headers['Content-Type'] = mime;
      if (uri.host.endsWith('.blob.core.windows.net') ||
          uri.queryParameters.containsKey('sv')) {
        request.headers['x-ms-blob-type'] = 'BlockBlob';
      }
      progress('uploading', 0);
      var sent = 0;
      final responseFuture = client.send(request);
      Future<void> write() async {
        try {
          await for (final bytes in file.openRead()) {
            request.sink.add(bytes);
            sent += bytes.length;
            progress('uploading', (sent / size).clamp(0, 1));
          }
        } finally {
          await request.sink.close();
        }
      }

      final results = await Future.wait<Object?>([
        responseFuture,
        write(),
      ]).timeout(const Duration(minutes: 2));
      final response = results[0] as http.StreamedResponse;
      await response.stream.drain<void>().timeout(const Duration(seconds: 30));
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw ApiProblem(
          message: 'Upload failed',
          statusCode: response.statusCode,
          code: response.statusCode == 401 || response.statusCode == 403
              ? 'UPLOAD_URL_REJECTED'
              : null,
        );
      }
    } on TimeoutException {
      throw const ApiProblem(message: 'Upload timed out', code: 'TIMEOUT');
    } on http.ClientException {
      throw ApiProblem.network();
    } finally {
      _active.remove(client);
      client.close();
    }
    _checkScope(scope);
    progress('confirming', null);
    final confirmed = await api.postJson(
      '/api/v1/files/confirm',
      body: {
        'fileId': init['fileId'],
        'storageKey': init['storageKey'],
        'originalFilename': file.name,
        'mimeType': mime,
        'sizeBytes': size,
      },
    );
    _checkScope(scope);
    final id = confirmed['id'] ?? init['fileId'];
    if (id is! String || id.isEmpty) {
      throw const ApiProblem(
        message: 'Invalid file confirmation',
        code: 'INVALID_RESPONSE',
      );
    }
    return id;
  }

  Future<void> open(Uri uri) async {
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      throw const ApiProblem(message: 'Unable to open', code: 'FILE_OPEN');
    }
  }
}
