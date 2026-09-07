import '../../../core/network/api_client.dart';
import 'tenant_utility_models.dart';

class TenantUtilityRepository {
  TenantUtilityRepository(this.api);
  final ApiClient api;
  static const base = '/api/v1/utility-bills/my';

  Future<List<TenantUtilityAccount>> accounts() async => (await api.getList(
    '$base/accounts',
  )).map(TenantUtilityAccount.fromJson).toList(growable: false);
  Future<TenantUtilityPage> bills({
    TenantUtilityType? type,
    int pageSize = 50,
    String? cursor,
  }) async => TenantUtilityPage.fromJson(
    await api.getJson(
      '$base/bills',
      query: {
        if (type != null) 'utilityType': type.queryValue,
        'pageSize': '$pageSize',
        if (cursor != null) 'cursor': cursor,
      },
    ),
  );
  Future<TenantUtilitySummary> summary() async => TenantUtilitySummary.fromJson(
    await api.getJson('$base/dashboard-summary'),
  );
  Future<TenantUtilityAccount> link(
    TenantUtilityType type,
    String accountNumber,
    String? meterNumber,
  ) async => TenantUtilityAccount.fromJson(
    await api.postJson(
      '$base/accounts',
      body: {
        'utilityType': type.apiValue,
        'accountNumber': accountNumber,
        'meterNumber': meterNumber,
      },
    ),
  );
  Future<TenantUtilityAccount> replace(
    String id,
    String accountNumber,
    String? meterNumber,
  ) async => TenantUtilityAccount.fromJson(
    await api.postJson(
      '$base/accounts/${Uri.encodeComponent(id)}/replace',
      body: {'accountNumber': accountNumber, 'meterNumber': meterNumber},
    ),
  );
  Future<void> unlink(String id) =>
      api.delete('$base/accounts/${Uri.encodeComponent(id)}');
  Future<void> requestSync(String id) =>
      api.postVoid('$base/accounts/${Uri.encodeComponent(id)}/sync');
}
