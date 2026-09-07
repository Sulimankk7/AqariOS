import '../../core/network/api_client.dart';

class CompanyProfile {
  CompanyProfile.fromJson(Map<String, dynamic> json)
    : id = json['id']?.toString() ?? '',
      legalName = json['legalName']?.toString() ?? '',
      displayName = json['displayName']?.toString() ?? '',
      registration = json['commercialRegistrationNo']?.toString(),
      taxNumber = json['taxNumber']?.toString(),
      companyType = (json['companyType'] as num?)?.toInt() ?? 0,
      phone = json['primaryPhone']?.toString() ?? '',
      email = json['primaryEmail']?.toString(),
      countryCode = json['countryCode']?.toString() ?? '',
      active = json['isActive'] == true;

  final String id, legalName, displayName, phone, countryCode;
  final String? registration, taxNumber, email;
  final int companyType;
  final bool active;
}

class CompanyOperationalSettings {
  CompanyOperationalSettings.fromJson(Map<String, dynamic> json)
    : currency = json['defaultCurrency']?.toString() ?? '',
      graceDays = (json['rentGracePeriodDays'] as num?)?.toInt() ?? 0,
      lateFeeType = (json['lateFeeType'] as num?)?.toInt() ?? 0,
      lateFeeValue = (json['lateFeeValue'] as num?)?.toDouble(),
      fiscalMonth = (json['fiscalYearStartMonth'] as num?)?.toInt() ?? 1,
      language = json['defaultLanguage']?.toString() ?? '',
      timezone = json['timezone']?.toString() ?? '';

  final String currency, language, timezone;
  final int graceDays, lateFeeType, fiscalMonth;
  final double? lateFeeValue;
}

class SettingsRepository {
  SettingsRepository(this.client);
  final ApiClient client;

  Future<CompanyProfile> company() async =>
      CompanyProfile.fromJson(await client.getJson('/api/v1/companies/me'));

  Future<void> updateCompany(
    String id, {
    required String legalName,
    required String displayName,
    required String phone,
    String? email,
  }) => client.putJson(
    '/api/v1/companies/${Uri.encodeComponent(id)}',
    body: {
      'legalName': legalName.trim(),
      'displayName': displayName.trim(),
      'primaryPhone': phone.trim(),
      'primaryEmail': email?.trim().isEmpty == true ? null : email?.trim(),
    },
  );

  Future<CompanyOperationalSettings> operations(String id) async =>
      CompanyOperationalSettings.fromJson(
        await client.getJson(
          '/api/v1/companies/${Uri.encodeComponent(id)}/settings',
        ),
      );

  Future<void> updateOperations(
    String id, {
    required int graceDays,
    required int lateFeeType,
    required double? lateFeeValue,
    required int fiscalMonth,
  }) => client.putJson(
    '/api/v1/companies/${Uri.encodeComponent(id)}/settings',
    body: {
      'rentGracePeriodDays': graceDays,
      'lateFeeType': lateFeeType,
      'lateFeeValue': lateFeeType == 0 ? null : lateFeeValue,
      'fiscalYearStartMonth': fiscalMonth,
    },
  );
}
