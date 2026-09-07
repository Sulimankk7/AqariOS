import 'dart:convert';

import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/tenant_portal/data/tenant_portal_repository.dart';
import 'package:aqarios_mobile/features/tenant_portal/utilities/tenant_utility_models.dart';
import 'package:aqarios_mobile/features/tenant_portal/utilities/tenant_utility_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test(
    'tenant profile is parsed and cached without duplicate lifecycle fetches',
    () async {
      var requests = 0;
      final api = _api((request) async {
        requests++;
        expect(request.url.path, '/api/v1/tenant-portal/me');
        return _json(_profile());
      });
      final repository = TenantPortalRepository(api);

      final values = await Future.wait([
        repository.profile(),
        repository.profile(),
      ]);
      expect(values.first.name, 'سارة أحمد');
      expect(values.first.familyMembers.single.ageBracket, 'Adult');
      expect(values.first.emergencyContacts.single.phone, '0790000000');
      expect(values.first.vehicles.single.plateNumber, '12-34567');
      expect(requests, 1);
      expect((await repository.profile()).name, 'سارة أحمد');
      expect(requests, 1);
      api.close();
    },
  );

  test('a 404 current lease is a cached valid empty state', () async {
    var requests = 0;
    final api = _api((request) async {
      requests++;
      return http.Response(jsonEncode({'title': 'Not Found'}), 404);
    });
    final repository = TenantPortalRepository(api);

    expect(await repository.currentLease(), isNull);
    expect(await repository.currentLease(), isNull);
    expect(requests, 1);
    api.close();
  });

  test(
    'utility repository preserves the audited tenant API contract',
    () async {
      final requests = <http.Request>[];
      final api = _api((request) async {
        requests.add(request);
        if (request.method == 'DELETE' || request.url.path.endsWith('/sync')) {
          return http.Response('', request.method == 'DELETE' ? 204 : 202);
        }
        if (request.url.path.endsWith('/bills')) {
          return _json({
            'items': [_bill()],
            'nextCursor': 'next',
            'hasMore': true,
          });
        }
        if (request.url.path.endsWith('/dashboard-summary')) {
          return _json({'electricityLinked': true, 'waterLinked': false});
        }
        if (request.method == 'POST') return _json(_account(), status: 202);
        return _json([_account()]);
      });
      final repository = TenantUtilityRepository(api);

      expect(
        (await repository.accounts()).single.type,
        TenantUtilityType.electricity,
      );
      final page = await repository.bills(
        type: TenantUtilityType.water,
        cursor: 'cursor',
      );
      expect(page.hasMore, true);
      expect(page.items.single.paymentStatus, 'Unpaid');
      expect((await repository.summary()).electricityLinked, true);
      await repository.link(TenantUtilityType.electricity, '0020013902', null);
      await repository.replace('account-1', '0020013903', 'M-2');
      await repository.requestSync('account-1');
      await repository.unlink('account-1');

      expect(requests[1].url.queryParameters, {
        'utilityType': 'Water',
        'pageSize': '50',
        'cursor': 'cursor',
      });
      expect(jsonDecode(requests[3].body), {
        'utilityType': 0,
        'accountNumber': '0020013902',
        'meterNumber': null,
      });
      expect(
        requests[4].url.path,
        '/api/v1/utility-bills/my/accounts/account-1/replace',
      );
      expect(
        requests[5].url.path,
        '/api/v1/utility-bills/my/accounts/account-1/sync',
      );
      expect(requests[6].method, 'DELETE');
      api.close();
    },
  );

  test('utility enum parsing accepts both backend numbers and names', () {
    expect(utilityTypeOf(0), TenantUtilityType.electricity);
    expect(utilityTypeOf('Water'), TenantUtilityType.water);
    expect(
      TenantUtilityAccount.fromJson({
        ..._account(),
        'syncStatus': 7,
      }).syncStatus,
      'InvalidAccount',
    );
    expect(
      TenantUtilityBill.fromJson({
        ..._bill(),
        'paymentStatus': 1,
      }).paymentStatus,
      'Paid',
    );
  });
}

ApiClient _api(Future<http.Response> Function(http.Request) handler) =>
    ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: _Store(),
      client: MockClient(handler),
    );

http.Response _json(Object body, {int status = 200}) => http.Response(
  jsonEncode(body),
  status,
  headers: {'content-type': 'application/json'},
);

Map<String, dynamic> _profile() => {
  'id': 'tenant-1',
  'companyId': 'company-1',
  'name': 'سارة أحمد',
  'nationalId': '1234567890',
  'phone': '0791111111',
  'email': 'sara@example.com',
  'occupation': 'Engineer',
  'employer': 'Aqari',
  'createdAt': '2026-01-01T00:00:00Z',
  'updatedAt': '2026-01-01T00:00:00Z',
  'familyMembers': [
    {
      'id': 'f1',
      'name': 'أحمد',
      'relationshipType': 'Spouse',
      'ageBracket': 'Adult',
      'createdAt': '2026-01-01T00:00:00Z',
    },
  ],
  'emergencyContacts': [
    {
      'id': 'e1',
      'name': 'ليلى',
      'relationshipType': 'Sister',
      'phone': '0790000000',
      'createdAt': '2026-01-01T00:00:00Z',
    },
  ],
  'vehicles': [
    {
      'id': 'v1',
      'plateNumber': '12-34567',
      'makeModel': 'Toyota Corolla',
      'color': 'White',
      'createdAt': '2026-01-01T00:00:00Z',
    },
  ],
};

Map<String, dynamic> _account() => {
  'id': 'account-1',
  'utilityType': 0,
  'accountNumber': '0020013902',
  'meterNumber': 'M-1',
  'isActive': true,
  'syncStatus': 'Synced',
  'historicalBootstrapCompleted': true,
  'totalOutstandingBalance': 9.5,
  'latestBill': _bill(),
  'linkedAt': '2026-01-01T00:00:00Z',
};

Map<String, dynamic> _bill() => {
  'id': 'bill-1',
  'billDate': '2026-08-01',
  'dueDate': '2026-08-15',
  'amount': 9.5,
  'currency': 'JOD',
  'isPaid': false,
  'paymentStatus': 'Unpaid',
  'discoveredAt': '2026-08-01T00:00:00Z',
  'utilityAccountId': 'account-1',
  'sourceAccountNumber': '0020013902',
  'isCurrentAccount': true,
  'sourceUtilityType': 0,
};

class _Store extends SecureSessionStore {
  @override
  Future<String?> readAccessToken() async => 'token';
  @override
  Future<String?> readRefreshToken() async => null;
}
