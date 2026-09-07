import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../domain/lease_models.dart';

class LeasingRepository {
  LeasingRepository(this.client);
  final ApiClient client;
  static const base = '/api/v1/leasing/contracts';
  static const utilities = '/api/v1/utility-bills/accounts';
  int _scope = 0;
  void clear() => _scope++;
  // Reads belong to the session that started them. Never retry a stale read in a new tenant scope.
  Future<T> _read<T>(Future<T> Function() load) async {
    final scope = _scope;
    try {
      final result = await load();
      if (_scope != scope) {
        throw const ApiProblem(
          message: 'Session changed',
          code: 'SESSION_CHANGED',
        );
      }
      return result;
    } on FormatException {
      throw const ApiProblem(
        message: 'Invalid response',
        code: 'INVALID_RESPONSE',
      );
    } on TypeError {
      throw const ApiProblem(
        message: 'Invalid response',
        code: 'INVALID_RESPONSE',
      );
    }
  }

  Future<List<Lease>> contracts({String searchTerm = '', int pageSize = 50}) =>
      _read(
        () async => (await client.getList(
          '$base/search',
          query: {'searchTerm': searchTerm, 'pageSize': '$pageSize'},
        )).map(Lease.fromJson).toList(growable: false),
      );
  Future<List<Lease>> expiring(int days) => _read(
    () async => (await client.getList(
      '$base/expiring',
      query: {'daysAhead': '$days'},
    )).map(Lease.fromJson).toList(growable: false),
  );
  Future<List<Lease>> history(String apartmentId) => _read(
    () async => (await client.getList(
      '$base/history/apartment/${Uri.encodeComponent(apartmentId)}',
      query: {'pageSize': '50'},
    )).map(Lease.fromJson).toList(growable: false),
  );
  Future<Lease> detail(String id) => _read(
    () async => Lease.fromJson(
      await client.getJson('$base/${Uri.encodeComponent(id)}'),
    ),
  );
  Future<String> nextNumber() => _read(
    () async => (await client.getJson('$base/next-number'))['value'] as String,
  );
  Future<List<LeaseTenant>> tenants() => _read(
    () async => (await client.getList(
      '/api/v1/leasing/tenants',
    )).map(LeaseTenant.fromJson).toList(growable: false),
  );
  Future<List<TenantRecord>> searchTenants({String searchTerm = ''}) => _read(
    () async => (await client.getList(
      '/api/v1/leasing/tenants',
      query: {if (searchTerm.isNotEmpty) 'searchTerm': searchTerm},
    )).map(TenantRecord.fromJson).toList(growable: false),
  );
  Future<TenantRecord> tenantDetail(String id) => _read(
    () async => TenantRecord.fromJson(
      await client.getJson(
        '/api/v1/leasing/tenants/${Uri.encodeComponent(id)}',
      ),
    ),
  );
  Future<String> createTenant(LeaseJson body) =>
      client.postId('/api/v1/leasing/tenants', body: body);
  Future<void> updateTenant(String id, LeaseJson body) => client.putJson(
    '/api/v1/leasing/tenants/${Uri.encodeComponent(id)}',
    body: body,
  );
  Future<void> deleteTenant(String id) =>
      client.delete('/api/v1/leasing/tenants/${Uri.encodeComponent(id)}');
  Future<List<Lease>> tenantLeaseHistory(String id) => _read(
    () async => (await client.getList(
      '/api/v1/leasing/tenants/${Uri.encodeComponent(id)}/leases',
      query: const {'pageSize': '50'},
    )).map(Lease.fromJson).toList(growable: false),
  );
  Future<Map<String, dynamic>> provisionTenantAccount(
    String id,
    LeaseJson body,
  ) => client.postJson(
    '/api/v1/leasing/tenants/${Uri.encodeComponent(id)}/account',
    body: body,
  );
  Future<String> createTenantChild(
    String tenantId,
    String collection,
    LeaseJson body,
  ) => client.postId(
    '/api/v1/leasing/tenants/${Uri.encodeComponent(tenantId)}/$collection',
    body: body,
  );
  Future<void> updateTenantChild(
    String tenantId,
    String collection,
    String id,
    LeaseJson body,
  ) => client.putJson(
    '/api/v1/leasing/tenants/${Uri.encodeComponent(tenantId)}/$collection/${Uri.encodeComponent(id)}',
    body: body,
  );
  Future<void> deleteTenantChild(
    String tenantId,
    String collection,
    String id,
  ) => client.delete(
    '/api/v1/leasing/tenants/${Uri.encodeComponent(tenantId)}/$collection/${Uri.encodeComponent(id)}',
  );
  Future<List<LeaseParkingSpot>> parking(String id) => _read(
    () async => (await client.getList(
      '$base/${Uri.encodeComponent(id)}/parking',
    )).map(LeaseParkingSpot.fromJson).toList(growable: false),
  );
  Future<String> create(LeaseJson body) => client.postId(base, body: body);
  Future<void> edit(String id, LeaseJson body) =>
      client.putJson('$base/$id', body: body);
  Future<void> activate(String id) => client.postVoid('$base/$id/activate');
  Future<String> renew(String id, LeaseJson body) =>
      client.postId('$base/$id/renew', body: body);
  Future<void> terminate(String id, LeaseJson body) =>
      client.postVoid('$base/$id/terminate', body: body);
  Future<String> attach(String id, LeaseJson body) =>
      client.postId('$base/$id/documents', body: body);
  Future<void> replace(String id, String docId, LeaseJson body) =>
      client.putJson('$base/$id/documents/$docId/replace', body: body);
  Future<void> deleteDocument(String id, String docId) =>
      client.delete('$base/$id/documents/$docId');
  Future<Uri> documentUrl(String id, String docId, {required bool inline}) =>
      _read(() async {
        final response = await client.getJson(
          '$base/$id/documents/$docId/download',
          query: {'inline': '$inline', 'redirect': 'false'},
        );
        final uri = Uri.parse(response['url'] as String);
        if (!uri.hasAuthority ||
            !['http', 'https'].contains(uri.scheme) ||
            uri.userInfo.isNotEmpty) {
          throw const FormatException('Invalid URL');
        }
        return uri;
      });
  Future<UtilityPage> accounts(String leaseId, {String? cursor}) => _read(
    () async => UtilityPage.fromJson(
      await client.getJson(
        utilities,
        query: {
          'leaseContractId': leaseId,
          'includeUnlinked': 'true',
          'pageSize': '25',
          if (cursor != null) 'cursor': cursor,
        },
      ),
    ),
  );
  Future<UtilityPage> managementUtilityAccounts({
    String? cursor,
    int? utilityType,
    int? syncStatus,
    bool? isActive,
    bool includeUnlinked = true,
  }) => _read(
    () async => UtilityPage.fromJson(
      await client.getJson(
        utilities,
        query: {
          'pageSize': '25',
          'includeUnlinked': '$includeUnlinked',
          if (cursor != null) 'cursor': cursor,
          if (utilityType != null) 'utilityType': '$utilityType',
          if (syncStatus != null) 'syncStatus': '$syncStatus',
          if (isActive != null) 'isActive': '$isActive',
        },
      ),
    ),
  );
  Future<UtilityAccount> utilityAccountDetail(String id) => _read(
    () async => UtilityAccount.fromJson(
      await client.getJson('$utilities/${Uri.encodeComponent(id)}'),
    ),
  );
  Future<UtilityBillPage> utilityBills(
    String id, {
    String? cursor,
    int? paymentStatus,
  }) => _read(
    () async => UtilityBillPage.fromJson(
      await client.getJson(
        '$utilities/${Uri.encodeComponent(id)}/bills',
        query: {
          'pageSize': '25',
          if (cursor != null) 'cursor': cursor,
          if (paymentStatus != null) 'paymentStatus': '$paymentStatus',
        },
      ),
    ),
  );
  Future<String> linkUtility(LeaseJson body) =>
      client.postId(utilities, body: body);
  Future<String> replaceUtility(String id, LeaseJson body) =>
      client.postId('$utilities/$id/replace', body: body);
  Future<void> syncUtility(String id) => client.postVoid('$utilities/$id/sync');
  Future<void> unlinkUtility(String id) => client.delete('$utilities/$id');
}
