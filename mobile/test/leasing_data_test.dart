import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';
import 'package:aqarios_mobile/core/network/api_problem.dart';
import 'package:aqarios_mobile/features/leasing/data/lease_files.dart';
import 'package:aqarios_mobile/features/leasing/domain/lease_behavior.dart';
import 'package:aqarios_mobile/features/leasing/domain/lease_errors.dart';
import 'package:aqarios_mobile/features/leasing/domain/lease_models.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_strings.dart';
import 'package:file_selector/file_selector.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'leasing_fixtures.dart';
import 'properties_fixtures.dart';

void main() {
  test(
    'repository consumes existing bounded arrays and exact query names',
    () async {
      final requests = <http.Request>[];
      final h = LeasingHarness(
        handler: (r) async {
          requests.add(r);
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      expect((await h.repository.contracts()).single.number, 'L-001');
      expect(requests.last.url.queryParameters, {
        'searchTerm': '',
        'pageSize': '50',
      });
      await h.repository.expiring(120);
      expect(requests.last.url.queryParameters, {'daysAhead': '120'});
      await h.repository.history('a1');
      expect(
        requests.last.url.path,
        '/api/v1/leasing/contracts/history/apartment/a1',
      );
      expect(requests.last.url.queryParameters, {'pageSize': '50'});
      await h.repository.accounts('lease-1', cursor: 'opaque+cursor');
      expect(requests.last.url.queryParameters, {
        'leaseContractId': 'lease-1',
        'includeUnlinked': 'true',
        'pageSize': '25',
        'cursor': 'opaque+cursor',
      });
    },
  );
  test('tenant management uses the existing leasing tenant API', () async {
    final requests = <http.Request>[];
    final h = LeasingHarness(
      handler: (request) async {
        requests.add(request);
        if (request.method == 'POST') return http.Response('"tenant-new"', 201);
        if (request.url.path.endsWith('/tenant-1/leases')) {
          return http.Response(jsonEncode([leaseJson()]), 200);
        }
        if (request.url.path.endsWith('/tenant-1')) {
          return http.Response(
            jsonEncode({
              'id': 'tenant-1',
              'name': 'Tenant',
              'nationalId': 'N1',
              'phone': '079',
              'familyMembers': [],
              'emergencyContacts': [],
              'vehicles': [],
            }),
            200,
          );
        }
        return http.Response(
          jsonEncode([
            {
              'id': 'tenant-1',
              'name': 'Tenant',
              'nationalId': 'N1',
              'phone': '079',
            },
          ]),
          200,
        );
      },
    );
    addTearDown(h.client.close);

    expect(
      (await h.repository.searchTenants(searchTerm: 'Tenant')).single.id,
      'tenant-1',
    );
    expect(requests.last.url.queryParameters, {'searchTerm': 'Tenant'});
    expect((await h.repository.tenantDetail('tenant-1')).name, 'Tenant');
    expect(
      (await h.repository.tenantLeaseHistory('tenant-1')).single.id,
      'lease-1',
    );
    expect(await h.repository.createTenant({'name': 'New'}), 'tenant-new');
    expect(requests.last.url.path, '/api/v1/leasing/tenants');
  });

  test(
    'utility management reuses account detail and bills endpoints',
    () async {
      final requests = <http.Request>[];
      final account = {
        ...utilityJson(),
        'isActive': true,
        'leaseContractId': 'lease-1',
        'leaseContractNumber': 'L-001',
        'tenantName': 'Tenant',
      };
      final h = LeasingHarness(
        handler: (request) async {
          requests.add(request);
          final body = request.url.path.endsWith('/bills')
              ? {
                  'items': [
                    {
                      'id': 'bill-1',
                      'billDate': '2026-01-01',
                      'amount': 20,
                      'currency': 'JOD',
                      'isPaid': false,
                      'paymentStatus': 0,
                    },
                  ],
                  'hasMore': false,
                }
              : request.url.path.endsWith('/u1')
              ? account
              : {
                  'items': [account],
                  'hasMore': false,
                };
          return http.Response(jsonEncode(body), 200);
        },
      );
      addTearDown(h.client.close);

      final page = await h.repository.managementUtilityAccounts(
        utilityType: 0,
        isActive: true,
      );
      expect(page.items.single.leaseNumber, 'L-001');
      expect(requests.last.url.queryParameters, {
        'pageSize': '25',
        'includeUnlinked': 'true',
        'utilityType': '0',
        'isActive': 'true',
      });
      expect(
        (await h.repository.utilityAccountDetail('u1')).tenantName,
        'Tenant',
      );
      expect((await h.repository.utilityBills('u1')).items.single.amount, 20);
      expect(requests.last.url.path, '/api/v1/utility-bills/accounts/u1/bills');
    },
  );
  test(
    'create/renew parse ID responses; lifecycle actions handle empty bodies',
    () async {
      final requests = <http.Request>[];
      final h = LeasingHarness(
        handler: (r) async {
          requests.add(r);
          return r.url.path.endsWith('/renew') ||
                  r.url.path.endsWith('/contracts')
              ? http.Response('"new-lease"', 201)
              : http.Response('', 204);
        },
      );
      addTearDown(h.client.close);
      expect(await h.repository.create({'contractNumber': 'N'}), 'new-lease');
      expect(jsonDecode(requests.last.body), {'contractNumber': 'N'});
      expect(
        await h.repository.renew('old', {'contractNumber': 'R'}),
        'new-lease',
      );
      await h.repository.activate('old');
      expect(requests.last.url.path.endsWith('/old/activate'), true);
      expect(requests.last.body, isEmpty);
      await h.repository.terminate('old', {'terminationType': 1});
      expect(jsonDecode(requests.last.body), {'terminationType': 1});
      await h.repository.edit('old', {'notes': 'updated'});
      expect(requests.last.method, 'PUT');
    },
  );
  test(
    'document attach/replace/delete and fresh view/download queries',
    () async {
      final requests = <http.Request>[];
      final h = LeasingHarness(
        handler: (r) async {
          requests.add(r);
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await h.repository.attach('l', {'fileId': 'f', 'documentType': 0});
      expect(requests.last.url.path, '/api/v1/leasing/contracts/l/documents');
      await h.repository.replace('l', 'd', {'newFileId': 'f2'});
      expect(requests.last.method, 'PUT');
      await h.repository.deleteDocument('l', 'd');
      expect(requests.last.method, 'DELETE');
      await h.repository.documentUrl('l', 'd', inline: true);
      expect(requests.last.url.queryParameters, {
        'inline': 'true',
        'redirect': 'false',
      });
    },
  );
  test('late reads cannot leak into a new session', () async {
    final pending = Completer<http.Response>();
    final h = LeasingHarness(handler: (_) => pending.future);
    addTearDown(h.client.close);
    final read = h.repository.contracts();
    h.repository.clear();
    pending.complete(http.Response(jsonEncode([leaseJson()]), 200));
    await expectLater(
      read,
      throwsA(
        isA<ApiProblem>().having((e) => e.code, 'code', 'SESSION_CHANGED'),
      ),
    );
  });
  test(
    'local search and string-accessor sorting match Web without changing server query',
    () {
      final data = [
        Lease.fromJson({
          ...leaseJson(number: 'CN-Z'),
          'monthlyRentAmount': 1000,
        }),
        Lease.fromJson({
          ...leaseJson(id: 'l2', number: 'A'),
          'monthlyRentAmount': 200,
        }),
      ];
      String label(int s) => leaseEnumLabel('status', s, false);
      expect(
        leaseRows(data, ' cn-z ', LeaseSort.none, false, label).single.number,
        'CN-Z',
      );
      expect(leaseRows(data, '2027', LeaseSort.none, false, label).length, 2);
      expect(
        leaseRows(data, '', LeaseSort.number, false, label).first.number,
        'A',
      );
      expect(
        leaseRows(data, '', LeaseSort.rent, false, label).first.rent,
        1000,
      );
      expect(leaseRows(data, '', LeaseSort.rent, true, label).first.rent, 200);
    },
  );
  test(
    'Web status and existing permissions compose without inventing read permissions',
    () {
      final reader = propertyUser({});
      final creator = propertyUser({'contracts.create'});
      final approver = propertyUser({'contracts.approve'});
      Lease lease(int s) => Lease.fromJson(leaseJson(status: s));
      expect(leaseActions(lease(0), reader), [LeaseAction.view]);
      expect(leaseActions(lease(1), creator), contains(LeaseAction.edit));
      expect(
        leaseActions(lease(2), creator),
        isNot(contains(LeaseAction.edit)),
      );
      expect(
        leaseActions(lease(2), approver),
        containsAll([LeaseAction.renew, LeaseAction.terminate]),
      );
      expect(leaseActions(lease(3), approver), [
        LeaseAction.view,
        LeaseAction.renew,
      ]);
      expect(leaseActions(lease(5), creator), [LeaseAction.view]);
      expect(leaseActions(lease(6), creator), contains(LeaseAction.attach));
    },
  );
  test(
    'form payloads preserve immutable identity/number boundaries and Web defaults',
    () {
      final l = Lease.fromJson(leaseJson());
      final renew = LeaseFormData(LeaseFormMode.renew, lease: l);
      expect(renew.values['startDate'], '2027-01-01');
      expect(renew.values['endDate'], '2028-01-01');
      expect(renew.toJson(), isNot(contains('apartmentId')));
      expect(renew.toJson(), isNot(contains('tenantId')));
      expect(renew.validate()['contractNumber'], 'required');
      final edit = LeaseFormData(LeaseFormMode.edit, lease: l);
      expect(edit.toJson(), isNot(contains('contractNumber')));
      expect(edit.validate(), isEmpty);
      edit.values['paymentDueDay'] = '29';
      edit.values['monthlyRentAmount'] = '0';
      edit.values['securityDepositAmount'] = '-1';
      expect(
        edit.validate().keys,
        containsAll([
          'paymentDueDay',
          'monthlyRentAmount',
          'securityDepositAmount',
        ]),
      );
      edit.values['paymentDueDay'] = '٢٨';
      edit.values['monthlyRentAmount'] = '٤٠٠٫٥';
      edit.values['securityDepositAmount'] = '';
      expect(edit.validate(), isEmpty);
      expect(edit.toJson()['monthlyRentAmount'], 400.5);
    },
  );
  test(
    'termination carries all settlement fields without inventing client business rules',
    () {
      final form = LeaseFormData(
        LeaseFormMode.terminate,
        now: DateTime.utc(2026, 9, 3),
      );
      form.values['depositDeductionAmount'] = '10';
      // Deduction reason/date eligibility are existing server rules, as on Web.
      expect(form.validate(), isEmpty);
      expect(
        form.toJson().keys,
        containsAll([
          'terminationType',
          'terminationDate',
          'outstandingBalance',
          'depositReturnedAmount',
          'depositDeductionAmount',
          'depositDeductionReason',
          'finalUtilitySettlementCompleted',
          'reason',
          'notes',
        ]),
      );
      expect(form.toJson()['terminationDate'], '2026-09-03');
    },
  );
  test(
    'business failures, conflicts, rate limiting and malformed errors remain meaningful',
    () {
      expect(
        leaseError(
          const ApiProblem(message: 'x', code: 'LEASE_EDIT_NOT_DRAFT'),
          false,
        ),
        contains('Only Draft'),
      );
      expect(
        leaseError(
          const ApiProblem(
            message: 'x',
            code: 'LEASE_ACTIVATE_SIGNED_DOCUMENT_REQUIRED',
          ),
          true,
        ),
        contains('موقّع'),
      );
      expect(
        leaseError(
          const ApiProblem(
            message: 'x',
            statusCode: 409,
            detail:
                'An overlapping draft, pending, or active contract already exists for this apartment.',
          ),
          false,
        ),
        contains('overlapping'),
      );
      expect(
        leaseError(
          const ApiProblem(
            message: 'secret SQL stack',
            statusCode: 500,
            detail: 'secret SQL stack',
          ),
          false,
        ),
        isNot(contains('SQL')),
      );
      expect(
        leaseError(
          const ApiProblem(
            message: 'x',
            statusCode: 429,
            retryAfterSeconds: 30,
          ),
          false,
        ),
        contains('30 seconds'),
      );
      expect(
        leaseFieldError(
          'EndDate must be strictly greater than StartDate.',
          true,
        ),
        contains('بعد'),
      );
    },
  );
  test(
    'signed upload is credential-free and preserves binary bytes and phases',
    () async {
      final apiRequests = <http.Request>[];
      final h = LeasingHarness(
        handler: (r) async {
          apiRequests.add(r);
          return http.Response(
            jsonEncode(
              r.url.path.endsWith('/upload-request')
                  ? {
                      'fileId': 'f',
                      'storageKey': 'key',
                      'uploadUrl': 'https://storage.example.test/blob?sig=test',
                      'expirationMinutes': 15,
                    }
                  : {'id': 'f'},
            ),
            200,
          );
        },
      );
      addTearDown(h.client.close);
      var binaryRequests = 0;
      final files = LeaseFiles(
        h.client,
        binaryClient: () => MockClient((r) async {
          binaryRequests++;
          expect(
            r.headers.keys.map((v) => v.toLowerCase()),
            isNot(contains('authorization')),
          );
          expect(
            r.headers.keys.map((v) => v.toLowerCase()),
            isNot(contains('cookie')),
          );
          expect(r.bodyBytes, [1, 2, 3]);
          expect(r.contentLength, 3);
          return http.Response('', 204);
        }),
      );
      final phases = <String>[];
      expect(
        await files.upload(
          'l',
          XFile.fromData(
            Uint8List.fromList([1, 2, 3]),
            name: 'test.pdf',
            mimeType: 'application/pdf',
          ),
          (p, _) => phases.add(p),
        ),
        'f',
      );
      expect(binaryRequests, 1);
      expect(
        phases,
        containsAllInOrder(['preparing', 'uploading', 'confirming']),
      );
      expect(
        apiRequests.every((r) => r.headers.containsKey('Authorization')),
        true,
      );
      expect(jsonDecode(apiRequests.first.body)['moduleName'], 'leasing');
    },
  );
  test('file type and size validation', () async {
    final h = LeasingHarness();
    addTearDown(h.client.close);
    await expectLater(
      h.files.validate(
        XFile.fromData(
          Uint8List(1),
          name: 'x.exe',
          mimeType: 'application/octet-stream',
        ),
      ),
      throwsA(isA<ApiProblem>().having((e) => e.code, 'code', 'FILE_TYPE')),
    );
    await expectLater(
      h.files.validate(
        XFile.fromData(
          Uint8List(LeaseFiles.maxBytes + 1),
          name: 'x.pdf',
          mimeType: 'application/pdf',
        ),
      ),
      throwsA(isA<ApiProblem>().having((e) => e.code, 'code', 'FILE_SIZE')),
    );
  });
}
