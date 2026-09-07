import 'dart:convert';
import 'dart:typed_data';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/features/leasing/data/leasing_repository.dart';
import 'package:aqarios_mobile/features/leasing/data/lease_files.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_scope.dart';
import 'package:aqarios_mobile/features/properties/data/properties_repository.dart';
import 'package:file_selector/file_selector.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'properties_fixtures.dart';

Map<String, dynamic> leaseJson({
  String id = 'lease-1',
  int status = 0,
  String number = 'L-001',
  List<Map<String, dynamic>> documents = const [],
}) => {
  'id': id,
  'companyId': 'test-company',
  'buildingId': 'b1',
  'apartmentId': 'a1',
  'tenantId': 't1',
  'contractNumber': number,
  'startDate': '2026-01-01',
  'endDate': '2027-01-01',
  'monthlyRentAmount': 400,
  'securityDepositAmount': 100,
  'currency': 'JOD',
  'paymentDueDay': 1,
  'paymentFrequency': 0,
  'legalRegime': 0,
  'tenantType': 0,
  'status': status,
  'notes': null,
  'createdAt': '2026-01-01T10:00:00Z',
  'updatedAt': '2026-01-01T10:00:00Z',
  'documents': documents,
  'statusHistory': [],
};
Map<String, dynamic> documentJson() => {
  'id': 'd1',
  'fileId': 'file-1',
  'documentType': 0,
  'description': 'Signed copy',
  'originalFilename': 'lease.pdf',
  'mimeType': 'application/pdf',
  'sizeBytes': 128,
  'createdAt': '2026-01-02T10:00:00Z',
};
Map<String, dynamic> utilityJson() => {
  'id': 'u1',
  'utilityType': 0,
  'accountNumber': '0012345678',
  'meterNumber': null,
  'syncStatus': 2,
  'unlinkedAt': null,
};

class LeasingHarness {
  LeasingHarness({
    Future<http.Response> Function(http.Request)? handler,
    Set<String> permissions = const {'contracts.create', 'contracts.approve'},
    bool fakeFiles = true,
  }) {
    client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: TestSessionStore(),
      client: MockClient(handler ?? defaultResponse),
    );
    repository = LeasingRepository(client);
    files = FakeLeaseFiles(client);
    scope = LeaseScope(
      repository: repository,
      properties: PropertiesRepository(client),
      user: propertyUser(permissions),
      files: fakeFiles ? files : LeaseFiles(client),
    );
  }
  late final ApiClient client;
  late final LeasingRepository repository;
  late final LeaseScope scope;
  late final FakeLeaseFiles files;
  static Future<http.Response> defaultResponse(http.Request request) async {
    final path = request.url.path;
    Object result = <String, dynamic>{};
    if (path.endsWith('/search') ||
        path.endsWith('/expiring') ||
        path.contains('/history/apartment/')) {
      result = [leaseJson()];
    } else if (path.endsWith('/next-number')) {
      result = {'value': 'L-NEXT'};
    } else if (path == '/api/v1/buildings') {
      result = [buildingJson()];
    } else if (path == '/api/v1/apartments') {
      result = [apartmentJson()];
    } else if (path == '/api/v1/apartments/a1') {
      result = apartmentJson();
    } else if (path.endsWith('/leasing/tenants')) {
      result = [
        {
          'id': 't1',
          'name': 'Tenant fixture',
          'phone': '000',
          'nationalId': 'test',
        },
      ];
    } else if (path.endsWith('/utility-bills/accounts')) {
      result = {'items': [], 'hasMore': false, 'nextCursor': null};
    } else if (path.endsWith('/parking')) {
      result = [];
    } else if (path.endsWith('/download')) {
      result = {
        'url': 'https://files.example.test/document',
        'filename': 'lease.pdf',
      };
    } else if (request.method == 'GET') {
      result = leaseJson();
    } else if (request.method == 'POST') {
      result = 'new-id';
    }
    return http.Response(
      jsonEncode(result),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

class FakeLeaseFiles extends LeaseFiles {
  FakeLeaseFiles(super.api);
  int uploads = 0;
  Uri? opened;
  @override
  Future<XFile?> select() async => XFile.fromData(
    Uint8List(10),
    name: 'lease.pdf',
    mimeType: 'application/pdf',
  );
  @override
  Future<String> upload(
    String contractId,
    XFile file,
    void Function(String, double?) progress,
  ) async {
    uploads++;
    progress('uploading', 1);
    return 'confirmed-file';
  }

  @override
  Future<void> open(Uri uri) async {
    opened = uri;
  }
}
