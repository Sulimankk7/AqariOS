import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../domain/property_models.dart';

class ReverseGeocodingResult {
  const ReverseGeocodingResult({
    this.countryCode,
    this.governorate,
    this.city,
    this.district,
    this.neighborhood,
    this.street,
    this.postalCode,
    this.formattedAddress,
  });

  factory ReverseGeocodingResult.fromJson(Json json) => ReverseGeocodingResult(
    countryCode: json['countryCode'] as String?,
    governorate: json['governorate'] as String?,
    city: json['city'] as String?,
    district: json['district'] as String?,
    neighborhood: json['neighborhood'] as String?,
    street: json['street'] as String?,
    postalCode: json['postalCode'] as String?,
    formattedAddress: json['formattedAddress'] as String?,
  );

  final String? countryCode;
  final String? governorate;
  final String? city;
  final String? district;
  final String? neighborhood;
  final String? street;
  final String? postalCode;
  final String? formattedAddress;
}

class PropertiesRepository {
  PropertiesRepository(this.client);
  final ApiClient client;
  final _cache = <String, (DateTime, Object)>{};
  final _inFlight = <String, Future<Object>>{};
  int revision = 0;

  void clear() {
    revision++;
    _cache.clear();
    _inFlight.clear();
  }

  Future<T> _load<T extends Object>(
    String key,
    Future<T> Function() fetch,
    bool force,
  ) {
    final cached = _cache[key];
    if (!force &&
        cached != null &&
        DateTime.now().difference(cached.$1) < const Duration(seconds: 60)) {
      return Future.value(cached.$2 as T);
    }
    final pending = _inFlight[key];
    if (pending != null) return pending.then((value) => value as T);
    final generation = revision;
    late final Future<T> future;
    Future<T> fetchAndCache() async {
      try {
        final value = await fetch();
        // A mutation/session change invalidated this read while it was in flight.
        if (generation != revision) return await _load(key, fetch, true);
        _cache[key] = (DateTime.now(), value);
        return value;
      } on ApiProblem {
        rethrow;
      } on FormatException {
        throw const ApiProblem(message: 'Invalid response');
      } on TypeError {
        throw const ApiProblem(message: 'Invalid response');
      } finally {
        if (identical(_inFlight[key], future)) _inFlight.remove(key);
      }
    }

    future = fetchAndCache();
    _inFlight[key] = future;
    return future;
  }

  Future<List<Building>> buildings({bool force = false}) => _load(
    'buildings',
    () async => (await client.getList(
      '/api/v1/buildings',
    )).map(Building.fromJson).toList(growable: false),
    force,
  );
  Future<List<Floor>> floors(String buildingId, {bool force = false}) => _load(
    'floors:$buildingId',
    () async => (await client.getList(
      '/api/v1/buildings/${Uri.encodeComponent(buildingId)}/floors',
    )).map(Floor.fromJson).toList(growable: false),
    force,
  );
  Future<List<Apartment>> apartments({
    String? buildingId,
    String? floorId,
    bool force = false,
  }) => _load(
    'apartments:$buildingId:$floorId',
    () async => (await client.getList(
      '/api/v1/apartments',
      query: {
        if (buildingId != null) 'buildingId': buildingId,
        if (floorId != null) 'floorId': floorId,
      },
    )).map(Apartment.fromJson).toList(growable: false),
    force,
  );

  Future<Building> building(String id, {bool force = false}) => _load(
    'building:$id',
    () async => Building.fromJson(
      await client.getJson('/api/v1/buildings/${Uri.encodeComponent(id)}'),
    ),
    force,
  );
  Future<Floor> floor(String id, {bool force = false}) => _load(
    'floor:$id',
    () async => Floor.fromJson(
      await client.getJson('/api/v1/floors/${Uri.encodeComponent(id)}'),
    ),
    force,
  );
  Future<Apartment> apartment(String id, {bool force = false}) => _load(
    'apartment:$id',
    () async => Apartment.fromJson(
      await client.getJson('/api/v1/apartments/${Uri.encodeComponent(id)}'),
    ),
    force,
  );

  Future<String> nextBuildingCode() =>
      _suggestion('/api/v1/buildings/next-code');

  Future<String> nextFloorNumber(String buildingId) => _suggestion(
    '/api/v1/buildings/${Uri.encodeComponent(buildingId)}/floors/next-number',
  );

  Future<String> nextApartmentNumber(String floorId) => _suggestion(
    '/api/v1/floors/${Uri.encodeComponent(floorId)}/apartments/next-number',
  );

  Future<String> _suggestion(String path) async {
    final response = await client.getJson(path);
    final value = response['value'];
    if (value is! String) throw const FormatException('Invalid suggestion');
    return value;
  }

  Future<ReverseGeocodingResult> reverseGeocode(
    double latitude,
    double longitude,
    String language,
  ) async => ReverseGeocodingResult.fromJson(
    await client.getJson(
      '/api/v1/locations/reverse-geocode',
      query: {
        'latitude': latitude.toString(),
        'longitude': longitude.toString(),
        'language': language,
      },
    ),
  );

  Future<void> save(String path, Json body, {required bool editing}) async {
    if (editing) {
      await client.putJson(path, body: body);
    } else {
      await client.postJson(path, body: body);
    }
    clear();
  }

  Future<void> archive(PropertyKind kind, String id) async {
    await client.delete('/api/v1/${kind.name}/${Uri.encodeComponent(id)}');
    clear();
  }
}
