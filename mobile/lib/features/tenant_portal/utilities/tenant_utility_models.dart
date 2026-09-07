typedef UtilityJson = Map<String, dynamic>;

enum TenantUtilityType { electricity, water }

extension TenantUtilityTypeValue on TenantUtilityType {
  int get apiValue => this == TenantUtilityType.electricity ? 0 : 1;
  String get queryValue =>
      this == TenantUtilityType.electricity ? 'Electricity' : 'Water';
}

TenantUtilityType utilityTypeOf(Object? value) =>
    value == 1 || value?.toString().toLowerCase() == 'water'
    ? TenantUtilityType.water
    : TenantUtilityType.electricity;

String enumName(
  Object? value,
  List<String> names, {
  String fallback = 'Unknown',
}) {
  if (value is num) {
    final index = value.toInt();
    return index >= 0 && index < names.length ? names[index] : fallback;
  }
  final text = value?.toString();
  return text == null || text.isEmpty ? fallback : text;
}

class TenantUtilityBill {
  const TenantUtilityBill({
    required this.id,
    required this.billDate,
    required this.amount,
    required this.currency,
    required this.isPaid,
    required this.paymentStatus,
    required this.discoveredAt,
    this.dueDate,
    this.utilityAccountId,
    this.sourceAccountNumber,
    this.isCurrentAccount,
    this.sourceUtilityType,
    this.sourceAccountUnlinkedAt,
  });
  final String id, billDate, currency, paymentStatus, discoveredAt;
  final String? dueDate,
      utilityAccountId,
      sourceAccountNumber,
      sourceAccountUnlinkedAt;
  final num amount;
  final bool isPaid;
  final bool? isCurrentAccount;
  final TenantUtilityType? sourceUtilityType;

  factory TenantUtilityBill.fromJson(UtilityJson json) => TenantUtilityBill(
    id: _text(json, 'id'),
    billDate: _text(json, 'billDate'),
    dueDate: _optional(json, 'dueDate'),
    amount: _number(json, 'amount'),
    currency: _text(json, 'currency'),
    isPaid: json['isPaid'] == true,
    paymentStatus: enumName(json['paymentStatus'], const [
      'Unpaid',
      'Paid',
      'Unknown',
    ]),
    discoveredAt: _text(json, 'discoveredAt'),
    utilityAccountId: _optional(json, 'utilityAccountId'),
    sourceAccountNumber: _optional(json, 'sourceAccountNumber'),
    isCurrentAccount: json['isCurrentAccount'] is bool
        ? json['isCurrentAccount'] as bool
        : null,
    sourceUtilityType: json['sourceUtilityType'] == null
        ? null
        : utilityTypeOf(json['sourceUtilityType']),
    sourceAccountUnlinkedAt: _optional(json, 'sourceAccountUnlinkedAt'),
  );
}

class TenantUtilityAccount {
  const TenantUtilityAccount({
    required this.id,
    required this.type,
    required this.accountNumber,
    required this.isActive,
    required this.syncStatus,
    required this.historicalBootstrapCompleted,
    this.meterNumber,
    this.lastKnownBillDate,
    this.lastSuccessfulSyncAt,
    this.averageBillingIntervalDays,
    this.estimatedNextBillDate,
    this.totalOutstandingBalance,
    this.latestBill,
    this.unlinkedAt,
    this.linkedAt,
  });
  final String id, accountNumber, syncStatus;
  final TenantUtilityType type;
  final bool isActive, historicalBootstrapCompleted;
  final String? meterNumber,
      lastKnownBillDate,
      lastSuccessfulSyncAt,
      estimatedNextBillDate,
      unlinkedAt,
      linkedAt;
  final int? averageBillingIntervalDays;
  final num? totalOutstandingBalance;
  final TenantUtilityBill? latestBill;

  bool get isCurrent => unlinkedAt == null;
  bool get isTransient =>
      syncStatus == 'NeverSynced' || syncStatus == 'Syncing';

  factory TenantUtilityAccount.fromJson(UtilityJson json) =>
      TenantUtilityAccount(
        id: _text(json, 'id'),
        type: utilityTypeOf(json['utilityType']),
        accountNumber: _text(json, 'accountNumber'),
        meterNumber: _optional(json, 'meterNumber'),
        isActive: json['isActive'] == true,
        lastKnownBillDate: _optional(json, 'lastKnownBillDate'),
        lastSuccessfulSyncAt: _optional(json, 'lastSuccessfulSyncAt'),
        syncStatus: enumName(json['syncStatus'], const [
          'NeverSynced',
          'Syncing',
          'Synced',
          'ProviderError',
          'RateLimited',
          'Timeout',
          'Suspended',
          'InvalidAccount',
        ]),
        averageBillingIntervalDays: (json['averageBillingIntervalDays'] as num?)
            ?.toInt(),
        estimatedNextBillDate: _optional(json, 'estimatedNextBillDate'),
        historicalBootstrapCompleted:
            json['historicalBootstrapCompleted'] == true,
        totalOutstandingBalance: json['totalOutstandingBalance'] as num?,
        latestBill: json['latestBill'] is Map
            ? TenantUtilityBill.fromJson(
                Map<String, dynamic>.from(json['latestBill'] as Map),
              )
            : null,
        unlinkedAt: _optional(json, 'unlinkedAt'),
        linkedAt: _optional(json, 'linkedAt'),
      );
}

class TenantUtilityPage {
  const TenantUtilityPage(this.items, this.nextCursor, this.hasMore);
  final List<TenantUtilityBill> items;
  final String? nextCursor;
  final bool hasMore;
  factory TenantUtilityPage.fromJson(UtilityJson json) => TenantUtilityPage(
    ((json['items'] as List?) ?? const [])
        .map(
          (item) => TenantUtilityBill.fromJson(
            Map<String, dynamic>.from(item as Map),
          ),
        )
        .toList(growable: false),
    _optional(json, 'nextCursor'),
    json['hasMore'] == true,
  );
}

class TenantUtilitySummary {
  const TenantUtilitySummary({
    required this.electricityLinked,
    required this.waterLinked,
    this.latestElectricityBill,
    this.latestWaterBill,
    this.electricityAccountNumber,
    this.electricitySyncStatus,
    this.electricityLastSuccessfulSyncAt,
    this.waterAccountNumber,
    this.waterSyncStatus,
    this.waterLastSuccessfulSyncAt,
  });
  final bool electricityLinked, waterLinked;
  final TenantUtilityBill? latestElectricityBill, latestWaterBill;
  final String? electricityAccountNumber,
      electricitySyncStatus,
      electricityLastSuccessfulSyncAt,
      waterAccountNumber,
      waterSyncStatus,
      waterLastSuccessfulSyncAt;
  factory TenantUtilitySummary.fromJson(UtilityJson json) =>
      TenantUtilitySummary(
        electricityLinked: json['electricityLinked'] == true,
        waterLinked: json['waterLinked'] == true,
        latestElectricityBill: _bill(json['latestElectricityBill']),
        latestWaterBill: _bill(json['latestWaterBill']),
        electricityAccountNumber: _optional(json, 'electricityAccountNumber'),
        electricitySyncStatus: json['electricitySyncStatus'] == null
            ? null
            : enumName(json['electricitySyncStatus'], const [
                'NeverSynced',
                'Syncing',
                'Synced',
                'ProviderError',
                'RateLimited',
                'Timeout',
                'Suspended',
                'InvalidAccount',
              ]),
        electricityLastSuccessfulSyncAt: _optional(
          json,
          'electricityLastSuccessfulSyncAt',
        ),
        waterAccountNumber: _optional(json, 'waterAccountNumber'),
        waterSyncStatus: json['waterSyncStatus'] == null
            ? null
            : enumName(json['waterSyncStatus'], const [
                'NeverSynced',
                'Syncing',
                'Synced',
                'ProviderError',
                'RateLimited',
                'Timeout',
                'Suspended',
                'InvalidAccount',
              ]),
        waterLastSuccessfulSyncAt: _optional(json, 'waterLastSuccessfulSyncAt'),
      );
}

TenantUtilityBill? _bill(Object? value) => value is Map
    ? TenantUtilityBill.fromJson(Map<String, dynamic>.from(value))
    : null;
String _text(UtilityJson json, String key) =>
    json[key]?.toString() ?? (throw FormatException('Invalid $key'));
String? _optional(UtilityJson json, String key) => json[key]?.toString();
num _number(UtilityJson json, String key) => json[key] is num
    ? json[key] as num
    : (throw FormatException('Invalid $key'));
