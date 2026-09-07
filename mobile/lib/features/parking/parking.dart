import '../../core/network/api_client.dart';

class ParkingSpot {
  ParkingSpot(this.json);
  final Map<String, dynamic> json;
  String get id => json['id'].toString();
  String get code => json['spotCode']?.toString() ?? '';
  int get type => (json['parkingType'] as num?)?.toInt() ?? 0;
  String? get location => json['locationDescription']?.toString();
}

class ParkingAssignment {
  ParkingAssignment(this.json);
  final Map<String, dynamic> json;
  String get id => json['assignmentId'].toString();
  String get tenant => json['tenantName']?.toString() ?? '—';
  String get leaseId => json['leaseContractId'].toString();
  String get start => json['startDate']?.toString() ?? '';
}

class ParkingRepository {
  ParkingRepository(this.api);
  final ApiClient api;
  Future<List<ParkingSpot>> list(String buildingId) async => (await api.getList(
    '/api/v1/buildings/${Uri.encodeComponent(buildingId)}/parking-spots',
  )).map(ParkingSpot.new).toList();
  Future<void> save(
    String buildingId, {
    ParkingSpot? existing,
    required String code,
    required int type,
    required String location,
  }) => existing == null
      ? api.postVoid(
          '/api/v1/buildings/${Uri.encodeComponent(buildingId)}/parking-spots',
          body: _body(code, type, location),
        )
      : api.putJson(
          '/api/v1/parking-spots/${Uri.encodeComponent(existing.id)}',
          body: _body(code, type, location),
        );
  Future<void> archive(String id) =>
      api.delete('/api/v1/parking-spots/${Uri.encodeComponent(id)}');
  Future<ParkingAssignment?> assignment(String id) async {
    final value = await api.getOptionalJson(
      '/api/v1/parking-spots/${Uri.encodeComponent(id)}/assignment',
    );
    return value == null ? null : ParkingAssignment(value);
  }

  Future<void> assign(String spotId, String leaseId) => api.postVoid(
    '/api/v1/parking-spots/${Uri.encodeComponent(spotId)}/assignment',
    body: {'leaseContractId': leaseId},
  );
  Future<void> end(String assignmentId) => api.postVoid(
    '/api/v1/parking-assignments/${Uri.encodeComponent(assignmentId)}/end',
  );
  Map<String, dynamic> _body(String code, int type, String location) => {
    'spotCode': code.trim(),
    'parkingType': type,
    'locationDescription': location.trim().isEmpty ? null : location.trim(),
    'defaultApartmentId': null,
  };
}

String parkingType(int value) =>
    const [
      'عادي',
      'مسقوف',
      'زوار',
      'ذوي احتياجات خاصة',
    ].elementAtOrNull(value) ??
    'عادي';
