import 'dart:async';
import 'dart:convert';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/network/api_problem.dart';
import 'package:aqarios_mobile/features/properties/application/property_controller.dart';
import 'package:aqarios_mobile/features/properties/domain/property_form.dart';
import 'package:aqarios_mobile/features/properties/domain/property_models.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'properties_fixtures.dart';

void main() {
  test('only the latest reverse-geocoding response is current', () {
    expect(isCurrentLocationResponse(2, 2), isTrue);
    expect(isCurrentLocationResponse(1, 2), isFalse);
  });

  test('reverse geocoding uses the authenticated backend contract', () async {
    final h = PropertyHarness(
      handler: (_) async => http.Response(
        '{"countryCode":"JO","city":"Amman","language":"en"}',
        200,
      ),
    );
    addTearDown(h.client.close);

    final result = await h.repository.reverseGeocode(31.95, 35.91, 'en');

    expect(result.countryCode, 'JO');
    expect(result.city, 'Amman');
    expect(h.requests.single.url.path, '/api/v1/locations/reverse-geocode');
    expect(h.requests.single.url.queryParameters, {
      'latitude': '31.95',
      'longitude': '35.91',
      'language': 'en',
    });
    expect(h.requests.single.headers['Authorization'], 'Bearer test-access');
  });

  test(
    'in-flight results invalidated by a mutation cannot repopulate the cache',
    () async {
      final pending = Completer<http.Response>();
      var calls = 0;
      final h = PropertyHarness(
        handler: (_) {
          calls++;
          return calls == 1
              ? pending.future
              : Future.value(
                  http.Response(jsonEncode([buildingJson(name: 'Fresh')]), 200),
                );
        },
      );
      addTearDown(h.client.close);
      final first = h.repository.buildings();
      final duplicate = h.repository.buildings();
      await Future<void>.delayed(Duration.zero);
      h.repository.clear();
      pending.complete(
        http.Response(jsonEncode([buildingJson(name: 'Stale')]), 200),
      );
      expect((await first).single.name, 'Fresh');
      expect((await duplicate).single.name, 'Fresh');
      expect((await h.repository.buildings()).single.name, 'Fresh');
      expect(calls, 2);
    },
  );

  test('an unsuccessful retry is not refreshed a second time', () async {
    var refreshes = 0, reads = 0;
    final client = ApiClient(
      baseUri: Uri.parse('https://api.example.test'),
      sessionStore: TestSessionStore(),
      client: MockClient((r) async {
        if (r.url.path.endsWith('/refresh')) {
          refreshes++;
          return http.Response('{"accessToken":"retry-token"}', 200);
        }
        reads++;
        return http.Response('{}', 401);
      }),
    );
    addTearDown(client.close);
    await expectLater(
      client.getList('/api/v1/buildings'),
      throwsA(isA<ApiProblem>().having((e) => e.statusCode, 'status', 401)),
    );
    expect(refreshes, 1);
    expect(reads, 2);
  });
  test(
    'maps actual DTOs, zero coordinates, backend floor enum and unknown status honestly',
    () {
      final b = Building.fromJson(buildingJson());
      expect(b.latitude, 0);
      expect(b.address!.city, 'Amman');
      expect(b.units, 1);
      expect(Floor.fromJson({...floorJson(), 'floorType': 'Ground'}).type, 1);
      expect(Floor.fromJson(floorJson()).type, 2);
      expect(
        Apartment.fromJson({
          ...apartmentJson(),
          'occupancyStatus': 'UnderMaintenance',
        }).occupancy,
        2,
      );
      expect(
        Apartment.fromJson({
          ...apartmentJson(),
          'occupancyStatus': 'FutureState',
        }).occupancy,
        -1,
      );
    },
  );

  test(
    'reuses list cache and in-flight loads, preserves v1 query contract',
    () async {
      final h = PropertyHarness();
      addTearDown(h.client.close);
      await Future.wait([h.repository.buildings(), h.repository.buildings()]);
      await h.repository.buildings();
      expect(h.requests, hasLength(1));
      await h.repository.apartments(buildingId: 'b1', floorId: 'f1');
      expect(h.requests.last.url.path, '/api/v1/apartments');
      expect(h.requests.last.url.queryParameters, {
        'buildingId': 'b1',
        'floorId': 'f1',
      });
      await h.repository.buildings(force: true);
      expect(
        h.requests.where((r) => r.url.path == '/api/v1/buildings'),
        hasLength(2),
      );
    },
  );

  test(
    'all supported mutation verbs invalidate cached lists after 204',
    () async {
      final h = PropertyHarness(
        handler: (r) async => r.method == 'GET'
            ? http.Response(jsonEncode([buildingJson()]), 200)
            : http.Response('', 204),
      );
      addTearDown(h.client.close);
      await h.repository.buildings();
      await h.repository.save('/api/v1/buildings', {
        'name': 'New',
      }, editing: false);
      await h.repository.buildings();
      await h.repository.save('/api/v1/buildings/b1', {
        'name': 'Edited',
      }, editing: true);
      await h.repository.archive(PropertyKind.buildings, 'b1');
      expect(h.requests.map((r) => r.method), [
        'GET',
        'POST',
        'GET',
        'PUT',
        'DELETE',
      ]);
      expect(h.repository.revision, 3);
    },
  );

  test(
    'malformed array response is an error, not an empty collection',
    () async {
      final h = PropertyHarness(
        handler: (_) async => http.Response('{"items":[]}', 200),
      );
      addTearDown(h.client.close);
      await expectLater(h.repository.buildings(), throwsA(isA<ApiProblem>()));
    },
  );

  test(
    'concurrent 401s share refresh and retry their original filtered requests once',
    () async {
      final refreshStarted = Completer<void>(),
          releaseRefresh = Completer<void>();
      final store = TestSessionStore();
      int refreshes = 0, firstAttempts = 0;
      final requests = <http.Request>[];
      final client = ApiClient(
        baseUri: Uri.parse('https://api.example.test'),
        sessionStore: store,
        client: MockClient((r) async {
          requests.add(r);
          if (r.url.path.endsWith('/refresh')) {
            refreshes++;
            refreshStarted.complete();
            await releaseRefresh.future;
            return http.Response('{"accessToken":"renewed-test-access"}', 200);
          }
          if (r.headers['Authorization'] == 'Bearer test-access') {
            firstAttempts++;
            return http.Response('{}', 401);
          }
          return http.Response('[]', 200);
        }),
      );
      addTearDown(client.close);
      final results = Future.wait([
        client.getList('/api/v1/apartments', query: {'floorId': 'f1'}),
        client.getList('/api/v1/buildings'),
      ]);
      await refreshStarted.future;
      await Future<void>.delayed(Duration.zero);
      expect(firstAttempts, 2);
      releaseRefresh.complete();
      await results;
      expect(refreshes, 1);
      expect(
        requests
            .where((r) => r.url.path == '/api/v1/apartments')
            .map((r) => r.url.queryParameters),
        everyElement({'floorId': 'f1'}),
      );
    },
  );

  test('request mapping excludes immutable fields and company scope', () {
    final values = initialPropertyValues(Building.fromJson(buildingJson()));
    final create = BuildingWriteRequest(
      values,
      type: 0,
      governorate: 0,
      editing: false,
    ).toJson();
    final edit = BuildingWriteRequest(
      values,
      type: 0,
      governorate: 0,
      editing: true,
    ).toJson();
    expect(create['totalFloors'], 3);
    expect(edit.containsKey('totalFloors'), false);
    expect(edit['gpsLatitude'], 0);
    expect(edit.containsKey('companyId'), false);
    expect(
      FloorWriteRequest(
        {'floorNumber': '2', 'floorLabel': 'Second'},
        type: 2,
        editing: true,
      ).toJson(),
      {'floorLabel': 'Second', 'floorType': 2},
    );
    expect(
      ApartmentWriteRequest(
        {'baseRentAmount': '٤٥٠٫٥'},
        editing: true,
        ownership: 0,
        currency: 'JOD',
      ).toJson(),
      {'baseRentAmount': 450.5, 'baseRentCurrency': 'JOD'},
    );
    final unit = ApartmentWriteRequest(
      {
        'unitNumber': 'A',
        'areaSqm': '50',
        'bedrooms': '0',
        'bathrooms': '1',
        'externalOwnerName': 'Stale',
      },
      editing: false,
      ownership: 0,
      currency: 'JOD',
    ).toJson();
    expect(unit['externalOwnerName'], null);
    expect(unit['areaSqm'], 50);
  });

  test(
    'validation uses effective API field limits and allows Arabic numbers',
    () {
      final fields = propertyFields(
        PropertyKind.buildings,
        editing: false,
        ownership: 0,
      );
      expect(
        fields
            .firstWhere((f) => f.key == 'internalCode')
            .validate('x' * 21, false),
        isNotNull,
      );
      final floorNumber = propertyFields(
        PropertyKind.floors,
        editing: false,
        ownership: 0,
      ).first;
      expect(floorNumber.validate('-٥٠', true), isNull);
      expect(floorNumber.validate('-51', false), isNotNull);
      final rent = propertyFields(
        PropertyKind.apartments,
        editing: true,
        ownership: 0,
      ).single;
      expect(rent.validate('0', false), isNotNull);
      expect(rent.validate('NaN', false), isNotNull);
    },
  );

  test(
    'route state deduplicates loads and never notifies after dispose',
    () async {
      final pending = Completer<int>();
      var calls = 0;
      final state = PropertyController<int>((_) {
        calls++;
        return pending.future;
      });
      final first = state.load();
      await state.load();
      expect(calls, 1);
      state.dispose();
      pending.complete(3);
      await first;
    },
  );
}
