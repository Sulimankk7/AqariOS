import 'dart:convert';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/auth/domain/user_profile.dart';
import 'package:aqarios_mobile/features/properties/data/properties_repository.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

// Test-only records. No production seed data or credentials.
Map<String, dynamic> buildingJson({
  String id = 'b1',
  String name = 'Cedar House',
}) => {
  'id': id,
  'name': name,
  'internalCode': 'B-01',
  'buildingType': 0,
  'totalFloors': 3,
  'totalApartmentsCount': 1,
  'isActive': true,
  'constructionYear': 2020,
  'gpsLatitude': 0,
  'gpsLongitude': 0,
  'address': {
    'governorate': 0,
    'district': 'Amman',
    'area': 'West',
    'streetName': 'Main',
    'postalCode': '11111',
  },
};
Map<String, dynamic> floorJson() => {
  'id': 'f1',
  'buildingId': 'b1',
  'floorNumber': 2,
  'floorLabel': 'Second floor',
  'floorType': 2,
  'apartmentsCount': 1,
};
Map<String, dynamic> apartmentJson() => {
  'id': 'a1',
  'buildingId': 'b1',
  'floorId': 'f1',
  'unitNumber': '201-A',
  'occupancyStatus': 1,
  'ownershipStatus': 0,
  'areaSqm': 95.5,
  'bedrooms': 2,
  'bathrooms': 1,
  'baseRentAmount': 400,
  'baseRentCurrency': 'JOD',
  'isActive': true,
};
UserProfile propertyUser([
  Set<String> permissions = const {'properties.read'},
]) => UserProfile(
  id: 'test-user',
  fullName: 'Test User',
  preferredLanguage: 'en',
  permissions: permissions,
  companyRoles: const [
    UserCompanyRole(companyId: 'test-company', roleCode: 'COMPANY_ADMIN'),
  ],
  systemRoles: const {},
  activeCompanyId: 'test-company',
);

class TestSessionStore extends SecureSessionStore {
  String? access = 'test-access', refresh = 'test-refresh';
  bool persistent = false;
  @override
  Future<String?> readAccessToken() async => access;
  @override
  Future<String?> readRefreshToken() async => refresh;
  @override
  Future<void> writeAccessToken(String token) async {
    access = token;
  }

  @override
  Future<void> writeRefreshToken(String token) async {
    refresh = token;
  }

  @override
  Future<void> writePersistence(bool value) async {
    persistent = value;
  }

  @override
  Future<void> clear() async {
    access = null;
    refresh = null;
    persistent = false;
  }
}

class PropertyHarness {
  PropertyHarness({Future<http.Response> Function(http.Request)? handler}) {
    client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: TestSessionStore(),
      client: MockClient((request) async {
        requests.add(request);
        if (handler != null) return handler(request);
        final Object? data = switch (request.url.path) {
          '/api/v1/buildings' => [buildingJson()],
          '/api/v1/buildings/b1' => buildingJson(),
          '/api/v1/buildings/b1/floors' => [floorJson()],
          '/api/v1/floors/f1' => floorJson(),
          '/api/v1/apartments' => [apartmentJson()],
          '/api/v1/apartments/a1' => apartmentJson(),
          _ => null,
        };
        return http.Response(
          jsonEncode(data ?? {'detail': 'Unknown test route'}),
          data == null ? 404 : 200,
        );
      }),
    );
    repository = PropertiesRepository(client);
  }
  final requests = <http.Request>[];
  late final ApiClient client;
  late final PropertiesRepository repository;
}
