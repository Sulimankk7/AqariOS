import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:file_selector/file_selector.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/network/api_problem.dart';
import 'package:aqarios_mobile/features/leasing/data/lease_files.dart';
import 'package:aqarios_mobile/features/payments/application/payments_controller.dart';
import 'package:aqarios_mobile/features/payments/data/payment_files.dart';
import 'package:aqarios_mobile/features/payments/domain/payment_models.dart';
import 'package:aqarios_mobile/features/payments/domain/payment_errors.dart';
import 'payments_fixtures.dart';
import 'properties_fixtures.dart';

void main() {
  test(
    'detail retains Web list enrichment when server omits settlement/receipts',
    () {
      final list = RentPayment({
        ...paymentJson(),
        'receiptFileId': 'file1',
        'transactionReceipts': [
          {'receiptId': 'r1', 'amount': 50},
        ],
      });
      final detail = RentPayment({
        ...paymentJson(),
        'amountPaid': 75,
        'settlementSummary': null,
        'receiptFileId': null,
        'transactionReceipts': [],
        'tenantName': null,
      });
      final merged = detail.withListFallback(list);
      expect(merged.paid, 75);
      expect(merged.settlement!.remaining, 250);
      expect(merged.text('receiptFileId'), 'file1');
      expect(merged.transactions.single.number('amount'), 50);
      expect(merged.tenantName, list.tenantName);
      final updated = RentPayment({
        ...detail.json,
        'settlementSummary': {'remaining': 225},
      }).withListFallback(list);
      expect(updated.settlement!.remaining, 225);
    },
  );
  test(
    'payment enums and nullable nested financial models preserve backend values',
    () {
      final p = RentPayment({
        ...paymentJson(status: 'PartiallyPaid'),
        'paymentMethod': '4',
        'latestSubmissionStatus': 'Rejected',
        'incomingAllocations': [
          {'id': 'a', 'allocationStatus': 'Reversed', 'allocatedAmount': 25},
        ],
        'transactionReceipts': [
          {'receiptId': 'r', 'amount': 25},
        ],
        'chequeDetails': {'id': 'c', 'status': 'Bounced'},
        'submissions': [submissionJson()],
      });
      expect(p.status, 3);
      expect(p.method, 4);
      expect(p.latestStatus, 2);
      expect(p.remainingMilli, 250000);
      expect(p.canSubmit, true);
      expect(p.incoming.single.status, 1);
      expect(p.transactions.single.number('amount'), 25);
      expect(p.cheque!.status, 4);
      expect(p.submissions.single.amount, 100);
      expect(paymentEnum('new-server-status', dueStatuses), -1);
      expect(paymentEnum('AdjustmentCredit', purposes), 2);
      expect(paymentEnum('AdjustmentDebit', purposes), 3);
      expect(RentPayment(paymentJson(status: 1)).canSubmit, false);
      expect(RentPayment(paymentJson(status: 2)).canRemind, false);
    },
  );
  test(
    'exact thousandths and Arabic digits validate amount without rounding away errors',
    () {
      final p = RentPayment({
        ...paymentJson(),
        'amountDue': 1.001,
        'amountPaid': 0.999,
      });
      expect(p.remainingMilli, 2);
      expect(milliAmount('٠٫٠٠٢'), 2);
      expect(milliAmount('1.0001'), isNull);
      final draft = PaymentDraft()..amount = '٠٫٠٠٢';
      expect(draft.validate(p), isEmpty);
      expect(draft.payload['amount'], .002);
      draft.amount = '0.003';
      expect(draft.validate(p)['amount'], 'exceeds');
      draft.amount = '-1';
      expect(draft.validate(p)['amount'], 'invalidAmount');
    },
  );
  test('tenant submission method requirements and exact payload', () {
    final p = RentPayment(paymentJson());
    final d = PaymentDraft()
      ..amount = '10'
      ..method = 4;
    expect(d.validate(p).keys, containsAll(['reference', 'proof']));
    d.reference = ' CL-01 ';
    d.proofId = 'file1';
    expect(d.validate(p), isEmpty);
    d.method = 2;
    expect(
      d.validate(p).keys,
      containsAll(['chequeNumber', 'bank', 'issueDate', 'dueDate']),
    );
    d.chequeNumber = '001';
    d.bank = 'Bank';
    d.issueDate = '2026-09-02';
    d.dueDate = '2026-09-01';
    expect(d.validate(p)['dueDate'], 'dateOrder');
    d.dueDate = '2026-10-01';
    expect(d.validate(p), isEmpty);
    expect(d.payload['referenceNumber'], '001');
    expect(d.payload['chequeDetails'], {
      'chequeNumber': '001',
      'bankName': 'Bank',
      'issueDate': '2026-09-02',
      'dueDate': '2026-10-01',
    });
    d.method = 0;
    expect(d.payload['proofFileId'], isNull);
    d.method = 1;
    expect(d.validate(p)['method'], 'invalidMethod');
  });
  test('tenant sorting matches Web priority then due-date descending', () {
    final data = [
      paymentJson(id: 'paid', status: 2),
      paymentJson(id: 'partial', status: 3),
      paymentJson(id: 'late', status: 4),
      paymentJson(id: 'overdue', status: 5),
      paymentJson(id: 'pending', status: 0),
    ];
    expect(
      sortTenantPayments(data.map(RentPayment.new).toList()).map((p) => p.id),
      ['overdue', 'late', 'pending', 'partial', 'paid'],
    );
  });
  test(
    'list query uses exact existing server search and paired cursor',
    () async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await h.repository.list(
        const PaymentFilters(
          buildingId: 'b1',
          status: 1,
          from: '2026-01-01',
          to: '2026-12-31',
          search: '  tenant ',
          lastId: 'p2',
          lastDate: '2026-08-01',
        ),
      );
      expect(h.requests.single.url.queryParameters, {
        'pageSize': '50',
        'buildingId': 'b1',
        'status': '1',
        'dateFrom': '2026-01-01',
        'dateTo': '2026-12-31',
        'searchTerm': 'tenant',
        'lastSeenId': 'p2',
        'lastSeenDueDate': '2026-08-01',
      });
    },
  );
  test(
    'record pagination uses final item cursor and resets with filters',
    () async {
      final h = PaymentHarness(
        handler: (r) async =>
            jsonResponse(List.generate(50, (i) => paymentJson(id: 'p$i'))),
      );
      addTearDown(h.client.close);
      final c = PaymentsController(h.repository);
      addTearDown(c.dispose);
      await c.load();
      expect(c.hasNext, true);
      await c.next();
      expect(c.page, 2);
      expect(h.requests.last.url.queryParameters['lastSeenId'], 'p49');
      await c.previous();
      expect(c.page, 1);
      await c.apply(const PaymentFilters(status: 5));
      expect(c.hasPrevious, false);
      expect(
        h.requests.last.url.queryParameters.containsKey('lastSeenId'),
        false,
      );
    },
  );
  test(
    'stale searches and changed session results cannot overwrite current data',
    () async {
      final delayed = Completer<http.Response>();
      final h = PaymentHarness(
        handler: (r) async => r.url.queryParameters['searchTerm'] == 'old'
            ? delayed.future
            : jsonResponse([paymentJson(id: 'new')]),
      );
      addTearDown(h.client.close);
      final c = PaymentsController(h.repository);
      addTearDown(c.dispose);
      final first = c.apply(const PaymentFilters(search: 'old'));
      await c.apply(const PaymentFilters(search: 'new'));
      delayed.complete(jsonResponse([paymentJson(id: 'old')]));
      await first;
      expect(c.items.single.id, 'new');
      final pending = Completer<http.Response>();
      final h2 = PaymentHarness(handler: (_) => pending.future);
      addTearDown(h2.client.close);
      final result = h2.repository.detail('p1');
      h2.repository.clear();
      pending.complete(jsonResponse(paymentJson()));
      await expectLater(
        result,
        throwsA(
          isA<ApiProblem>().having((p) => p.code, 'scope', 'SESSION_CHANGED'),
        ),
      );
    },
  );
  test(
    'approval/rejection/submission and receipt absence preserve contracts',
    () async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      await h.repository.approve('p1', 's1');
      await h.repository.reject('p1', 's1', '  incorrect proof  ');
      await h.repository.submit('p1', PaymentDraft()..amount = '25');
      expect(await h.repository.receipt('p1'), isNull);
      await h.repository.queue('cursor+opaque');
      await h.repository.remind('p1');
      expect(
        h.requests[0].url.path,
        '/api/v1/owner/payments/p1/submissions/s1/approve',
      );
      expect(h.requests[0].body, isEmpty);
      expect(jsonDecode(h.requests[1].body), {'reason': 'incorrect proof'});
      expect(jsonDecode(h.requests[2].body)['amount'], 25);
      expect(h.requests[4].url.queryParameters['cursor'], 'cursor+opaque');
      expect(h.requests[5].url.path, '/api/v1/rent-payments/p1/remind');
    },
  );
  test('receipt permission failure is not mistaken for not-issued', () async {
    final h = PaymentHarness(
      handler: (_) async => jsonResponse({'detail': 'Forbidden'}, 403),
    );
    addTearDown(h.client.close);
    await expectLater(
      h.repository.receipt('p1'),
      throwsA(isA<ApiProblem>().having((p) => p.statusCode, 'status', 403)),
    );
  });
  test(
    'PDF requests share one refresh and retry once with application/pdf',
    () async {
      int refreshes = 0;
      final store = TestSessionStore();
      final client = ApiClient(
        baseUri: Uri.parse('https://api.example.test'),
        sessionStore: store,
        client: MockClient((r) async {
          if (r.url.path.endsWith('/refresh')) {
            refreshes++;
            await Future<void>.delayed(const Duration(milliseconds: 5));
            return jsonResponse({'accessToken': 'new-test-access'});
          }
          expect(r.headers['accept'], contains('application/pdf'));
          if (r.headers['authorization'] == 'Bearer test-access') {
            return jsonResponse({}, 401);
          }
          return http.Response.bytes(
            utf8.encode('%PDF-1.7\n'),
            200,
            headers: {'content-type': 'application/pdf'},
          );
        }),
      );
      addTearDown(client.close);
      final values = await Future.wait([
        client.getPdf('/api/v1/rent-payments/p1/settlement-statement'),
        client.getPdf('/api/v1/rent-payments/p2/settlement-statement'),
      ]);
      expect(refreshes, 1);
      expect(values.every((v) => utf8.decode(v).startsWith('%PDF-')), true);
    },
  );
  test(
    'JSON pretending to be successful PDF is rejected and PDF problem errors preserved',
    () async {
      final client = ApiClient(
        baseUri: Uri.parse('https://api.example.test'),
        sessionStore: TestSessionStore(),
        client: MockClient(
          (r) async => r.url.path == '/bad'
              ? jsonResponse({'items': []})
              : jsonResponse({'code': 'INSTALLMENT_ALREADY_PAID'}, 422),
        ),
      );
      addTearDown(client.close);
      await expectLater(
        client.getPdf('/bad'),
        throwsA(
          isA<ApiProblem>().having((p) => p.code, 'code', 'INVALID_RESPONSE'),
        ),
      );
      await expectLater(
        client.getPdf('/failure'),
        throwsA(
          isA<ApiProblem>().having(
            (p) => p.code,
            'code',
            'INSTALLMENT_ALREADY_PAID',
          ),
        ),
      );
    },
  );
  test(
    'signed proof bytes receive no bearer/cookie; Financials module and confirmation exact',
    () async {
      final requests = <http.Request>[];
      final api = ApiClient(
        baseUri: Uri.parse('https://api.example.test'),
        sessionStore: TestSessionStore(),
        client: MockClient((r) async {
          requests.add(r);
          return r.url.path.endsWith('/upload-request')
              ? jsonResponse({
                  'fileId': 'f1',
                  'storageKey': 'key',
                  'uploadUrl': 'https://storage.example.test/proof?sig=signed',
                })
              : jsonResponse({'id': 'f1'});
        }),
      );
      addTearDown(api.close);
      final transfer = LeaseFiles(
        api,
        binaryClient: () => MockClient((r) async {
          expect(r.method, 'PUT');
          expect(r.headers.containsKey('authorization'), false);
          expect(r.headers.containsKey('cookie'), false);
          expect(r.followRedirects, false);
          expect(r.bodyBytes, [1, 2, 3]);
          return http.Response('', 200);
        }),
      );
      final files = PaymentFiles(transfer);
      expect(
        await files.upload(
          'p1',
          XFile.fromData(
            Uint8List.fromList([1, 2, 3]),
            name: 'proof.pdf',
            mimeType: 'application/pdf',
          ),
          (_, __) {},
        ),
        'f1',
      );
      expect(jsonDecode(requests.first.body)['moduleName'], 'Financials');
      expect(jsonDecode(requests.first.body)['entityId'], 'p1');
      expect(jsonDecode(requests.last.body)['fileId'], 'f1');
    },
  );
  test(
    'proof constraints narrower than Leasing; meaningful financial errors',
    () async {
      final h = PaymentHarness();
      addTearDown(h.client.close);
      final f = PaymentFiles(LeaseFiles(h.client));
      await expectLater(
        f.validate(
          XFile.fromData(
            Uint8List(1),
            name: 'proof.doc',
            mimeType: 'application/msword',
          ),
        ),
        throwsA(isA<ApiProblem>().having((p) => p.code, 'code', 'FILE_TYPE')),
      );
      await expectLater(
        f.validate(
          XFile.fromData(
            Uint8List(15 * 1024 * 1024 + 1),
            name: 'proof.pdf',
            mimeType: 'application/pdf',
          ),
        ),
        throwsA(isA<ApiProblem>().having((p) => p.code, 'code', 'FILE_SIZE')),
      );
      expect(
        paymentError(
          const ApiProblem(message: 'internal', code: 'SUBMISSION_NOT_PENDING'),
          false,
        ),
        contains('already been processed'),
      );
      expect(
        paymentError(
          const ApiProblem(
            message: 'internal',
            code: 'AMOUNT_EXCEEDS_OUTSTANDING_BALANCE',
          ),
          false,
        ),
        contains('remaining balance'),
      );
      expect(
        paymentError(
          const ApiProblem(message: 'secret SQL Exception', statusCode: 500),
          false,
        ),
        isNot(contains('SQL')),
      );
      expect(
        paymentError(
          const ApiProblem(
            message: 'rate',
            statusCode: 429,
            retryAfterSeconds: 7,
          ),
          false,
        ),
        contains('7 seconds'),
      );
    },
  );
}
