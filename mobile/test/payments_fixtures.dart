import 'dart:convert';
import 'dart:typed_data';
import 'dart:ui';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/design_system/components/overlays/attachment_source_sheet.dart';
import 'package:aqarios_mobile/features/payments/data/payments_repository.dart';
import 'package:aqarios_mobile/features/payments/data/payment_files.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_scope.dart';
import 'package:aqarios_mobile/features/leasing/data/lease_files.dart';
import 'package:aqarios_mobile/features/leasing/data/leasing_repository.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_scope.dart';
import 'package:aqarios_mobile/features/properties/data/properties_repository.dart';
import 'package:file_selector/file_selector.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'properties_fixtures.dart';

Map<String, dynamic> paymentJson({
  String id = 'p1',
  Object status = 0,
  Object purpose = 0,
}) => {
  'id': id,
  'companyId': 'test-company',
  'tenantId': 't1',
  'leaseContractId': 'l1',
  'buildingId': 'b1',
  'apartmentId': 'a1',
  'tenantName': 'مستأجر الاختبار ذو الاسم الطويل للغاية',
  'buildingName': 'مبنى الاختبار',
  'apartmentNumber': 'A-201',
  'contractNumber': 'LEASE-2026-001',
  'amountDue': 300,
  'amountPaid': 50,
  'currency': 'JOD',
  'dueDateStatus': status,
  'paymentPurpose': purpose,
  'dueDate': '2026-09-01',
  'createdAt': '2026-08-01T10:00:00Z',
  'settlementSummary': {'remaining': 250, 'isAvailable': false},
  'submissions': <Object>[],
  'transactionReceipts': <Object>[],
};
Map<String, dynamic> verificationJson() => {
  'rentPaymentId': 'p1',
  'paymentSubmissionId': 's1',
  'tenantName': 'Test tenant',
  'buildingName': 'Building',
  'apartmentNumber': 'A-201',
  'contractNumber': 'L-001',
  'amountDue': 300,
  'amountPaid': 50,
  'submittedAmount': 100,
  'currency': 'JOD',
  'paymentMethod': 'CliQ',
  'referenceNumber': 'REF-123',
  'proofFileId': 'file1',
  'submittedAt': '2026-09-01T10:00:00Z',
  'submissionStatus': 'Pending',
};
Map<String, dynamic> submissionJson() => {
  'id': 's1',
  'amount': 100,
  'status': 'Pending',
  'paymentMethod': 'CliQ',
  'referenceNumber': 'REF-123',
  'proofFileId': 'file1',
  'submittedAt': '2026-09-01T10:00:00Z',
};
http.Response jsonResponse(Object? value, [int status = 200]) => http.Response(
  jsonEncode(value),
  status,
  headers: {'content-type': 'application/json; charset=utf-8'},
);

class PaymentHarness {
  PaymentHarness({
    Future<http.Response> Function(http.Request)? handler,
    Set<String> permissions = const {
      'payments.read',
      'payments.approve',
      'receipts.read',
      'properties.read',
    },
  }) {
    client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: TestSessionStore(),
      client: MockClient((r) async {
        requests.add(r);
        return (handler ?? defaultResponse)(r);
      }),
    );
    repository = PaymentsRepository(client);
    files = FakePaymentFiles(LeaseFiles(client));
    scope = PaymentScope(
      repository: repository,
      files: files,
      leasing: LeaseScope(
        repository: LeasingRepository(client),
        properties: PropertiesRepository(client),
        user: propertyUser(permissions),
        files: files.transfer,
      ),
    );
  }
  final requests = <http.Request>[];
  late final ApiClient client;
  late final PaymentsRepository repository;
  late final FakePaymentFiles files;
  late final PaymentScope scope;
  static Future<http.Response> defaultResponse(http.Request r) async {
    if (r.url.path.endsWith('/pending-verifications')) {
      return jsonResponse({
        'items': [verificationJson()],
        'nextCursor': null,
        'hasMore': false,
      });
    }
    if (r.url.path == '/api/v1/buildings') {
      return jsonResponse([buildingJson()]);
    }
    if (r.url.path == '/api/v1/rent-payments' ||
        r.url.path == '/api/v1/tenant-portal/payments') {
      return jsonResponse([paymentJson()]);
    }
    if (r.url.path.endsWith('/download-url')) {
      return jsonResponse({
        'downloadUrl': 'https://storage.example.test/proof?sig=test',
      });
    }
    if (r.url.path.endsWith('/receipt')) return jsonResponse({}, 404);
    if (r.url.path.endsWith('/submit-verification')) {
      return jsonResponse('submission-id');
    }
    if (r.method == 'POST') return http.Response('', 204);
    return jsonResponse({
      ...paymentJson(),
      'submissions': [submissionJson()],
    });
  }
}

class FakePaymentFiles extends PaymentFiles {
  FakePaymentFiles(super.transfer);
  int uploads = 0, opened = 0, shared = 0;
  @override
  Future<XFile?> select(AttachmentSource source) async => XFile.fromData(
    Uint8List.fromList([1, 2, 3]),
    name: 'proof.pdf',
    mimeType: 'application/pdf',
  );
  @override
  Future<String> upload(
    String paymentId,
    XFile file,
    void Function(String, double?) progress,
  ) async {
    uploads++;
    progress('confirming', 1);
    return 'confirmed-file';
  }

  @override
  Future<void> open(Uri uri) async {
    opened++;
  }

  @override
  Future<void> sharePdf(Uint8List bytes, {required Rect origin}) async {
    shared++;
  }
}
