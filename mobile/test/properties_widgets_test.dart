import 'dart:async';
import 'dart:convert';
import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/properties/domain/property_models.dart';
import 'package:aqarios_mobile/features/properties/presentation/properties_landing.dart';
import 'package:aqarios_mobile/features/properties/presentation/property_collection_screen.dart';
import 'package:aqarios_mobile/features/properties/presentation/property_editor_screen.dart';
import 'package:aqarios_mobile/features/properties/presentation/building_details_screen.dart';
import 'package:aqarios_mobile/features/properties/presentation/building_location_picker.dart';
import 'package:aqarios_mobile/features/properties/presentation/property_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';
import 'properties_fixtures.dart';

Widget app(Widget child, {bool arabic = false, double scale = 1}) =>
    MaterialApp(
      theme: AqariTheme.light(Locale(arabic ? 'ar' : 'en')),
      locale: Locale(arabic ? 'ar' : 'en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(
          context,
        ).copyWith(textScaler: TextScaler.linear(scale)),
        child: child!,
      ),
      home: child,
    );

void main() {
  testWidgets('building map preserves zero coordinates and reports map taps', (
    tester,
  ) async {
    LatLng? selected;
    await tester.pumpWidget(
      app(
        BuildingLocationPicker(
          latitude: 0,
          longitude: 0,
          onSelected: (value) => selected = value,
        ),
      ),
    );
    final map = tester.widget<FlutterMap>(find.byType(FlutterMap));
    expect(map.options.initialCenter, const LatLng(0, 0));
    expect(find.byIcon(Icons.location_pin), findsOneWidget);
    map.options.onTap!(
      const TapPosition(Offset.zero, Offset.zero),
      const LatLng(31.95, 35.91),
    );
    expect(selected, const LatLng(31.95, 35.91));
  });

  testWidgets(
    'building map synchronizes coordinates and enriches only untouched address fields',
    (tester) async {
      final h = PropertyHarness(
        handler: (request) async {
          if (request.url.path.endsWith('/next-code')) {
            return http.Response('{"value":"BLD-001"}', 200);
          }
          return http.Response(
            jsonEncode({
              'countryCode': 'JO',
              'governorate': 'Amman',
              'city': 'Provider City',
              'neighborhood': 'Provider Area',
              'street': 'Provider Street',
              'postalCode': '11118',
              'formattedAddress': 'Provider address',
              'language': 'en',
            }),
            200,
          );
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: h.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.buildings,
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.enterText(
        find.byKey(const ValueKey('addressCity')),
        'Manual City',
      );
      await tester.scrollUntilVisible(
        find.text('Location'),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      expect(
        find.text(
          'قد تكون بعض تفاصيل العنوان غير دقيقة. يرجى التحقق من العنوان والموقع على الخريطة.',
        ),
        findsOneWidget,
      );
      final map = tester.widget<FlutterMap>(find.byType(FlutterMap));
      map.options.onTap!(
        const TapPosition(Offset.zero, Offset.zero),
        const LatLng(31.1234567, 35.7654321),
      );
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const ValueKey('gpsLatitude')),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      String value(String key) => tester
          .widget<AqariTextField>(find.byKey(ValueKey(key)))
          .controller!
          .text;
      expect(value('gpsLatitude'), '31.123457');
      expect(value('gpsLongitude'), '35.765432');
      await tester.scrollUntilVisible(
        find.byKey(const ValueKey('addressCity')),
        -250,
        scrollable: find.byType(Scrollable).first,
      );
      expect(value('addressCity'), 'Manual City');
      expect(value('addressNeighborhood'), 'Provider Area');
      expect(value('addressStreet'), 'Provider Street');
    },
  );

  testWidgets(
    'reverse geocoding failure does not block selected-coordinate create payload',
    (tester) async {
      final h = PropertyHarness(
        handler: (request) async {
          if (request.url.path.endsWith('/next-code')) {
            return http.Response('{"value":"BLD-001"}', 200);
          }
          if (request.url.path.endsWith('/reverse-geocode')) {
            return http.Response('{"detail":"unavailable"}', 503);
          }
          return http.Response('', 204);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: h.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.buildings,
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const ValueKey('name')), 'Mapped');
      await tester.enterText(find.byKey(const ValueKey('totalFloors')), '2');
      await tester.enterText(
        find.byKey(const ValueKey('addressCity')),
        'Amman',
      );
      await tester.enterText(
        find.byKey(const ValueKey('addressNeighborhood')),
        'West',
      );
      await tester.scrollUntilVisible(
        find.text('Location'),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      tester.widget<FlutterMap>(find.byType(FlutterMap)).options.onTap!(
        const TapPosition(Offset.zero, Offset.zero),
        const LatLng(0, 0),
      );
      await tester.pumpAndSettle();
      expect(
        find.text(
          'The address could not be suggested. You can enter it manually.',
        ),
        findsOneWidget,
      );
      await tester.tap(find.text('Save'));
      await tester.pumpAndSettle();
      final post = h.requests.singleWhere(
        (request) => request.method == 'POST',
      );
      final payload = jsonDecode(post.body) as Map<String, dynamic>;
      expect(payload['gpsLatitude'], 0.0);
      expect(payload['gpsLongitude'], 0.0);
      expect(payload['addressCity'], 'Amman');
      expect(payload['addressNeighborhood'], 'West');
    },
  );

  testWidgets(
    'building create loads one editable suggestion and preserves manual edits',
    (tester) async {
      final pending = Completer<http.Response>();
      final h = PropertyHarness(
        handler: (request) => request.method == 'GET'
            ? pending.future
            : Future.value(http.Response('', 204)),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: h.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.buildings,
          ),
        ),
      );
      expect(h.requests.single.url.path, '/api/v1/buildings/next-code');
      await tester.enterText(
        find.byKey(const ValueKey('internalCode')),
        'BLDG-MAIN',
      );
      await tester.enterText(find.byKey(const ValueKey('internalCode')), '');
      pending.complete(http.Response('{"value":"BLD-012"}', 200));
      await tester.pumpAndSettle();
      expect(
        tester
            .widget<AqariTextField>(find.byKey(const ValueKey('internalCode')))
            .controller!
            .text,
        isEmpty,
      );
      expect(
        h.requests.where((r) => r.url.path.endsWith('/next-code')),
        hasLength(1),
      );
    },
  );

  testWidgets('building suggestion failure keeps manual create usable', (
    tester,
  ) async {
    final h = PropertyHarness(
      handler: (request) async => request.method == 'GET'
          ? http.Response('{"detail":"unavailable"}', 500)
          : http.Response('', 204),
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        PropertyEditorScreen(
          repository: h.repository,
          user: propertyUser({'properties.create'}),
          kind: PropertyKind.buildings,
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const ValueKey('name')), 'Main');
    await tester.enterText(
      find.byKey(const ValueKey('internalCode')),
      'CUSTOM',
    );
    await tester.enterText(find.byKey(const ValueKey('totalFloors')), '2');
    await tester.enterText(find.byKey(const ValueKey('addressCity')), 'Amman');
    await tester.enterText(
      find.byKey(const ValueKey('addressNeighborhood')),
      'West',
    );
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    final post = h.requests.singleWhere((r) => r.method == 'POST');
    expect(jsonDecode(post.body)['internalCode'], 'CUSTOM');
  });

  testWidgets(
    'apartment and floor create use their existing suggestion endpoints',
    (tester) async {
      Future<http.Response> handler(http.Request request) async =>
          http.Response(
            jsonEncode({
              'value': request.url.path.endsWith('/apartments/next-number')
                  ? 'APT-201'
                  : '3',
            }),
            200,
          );
      final floorHarness = PropertyHarness(handler: handler);
      addTearDown(floorHarness.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: floorHarness.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.floors,
            parentId: 'b1',
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        floorHarness.requests.single.url.path,
        '/api/v1/buildings/b1/floors/next-number',
      );
      expect(
        tester
            .widget<AqariTextField>(find.byKey(const ValueKey('floorNumber')))
            .controller!
            .text,
        '3',
      );

      final apartmentHarness = PropertyHarness(handler: handler);
      addTearDown(apartmentHarness.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            key: const ValueKey('apartment-create'),
            repository: apartmentHarness.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.apartments,
            parentId: 'f1',
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        apartmentHarness.requests.single.url.path,
        '/api/v1/floors/f1/apartments/next-number',
      );
      final unit = find.byKey(const ValueKey('unitNumber'));
      expect(tester.widget<AqariTextField>(unit).controller!.text, 'APT-201');
      await tester.enterText(unit, 'G-2');
      expect(tester.widget<AqariTextField>(unit).controller!.text, 'G-2');
    },
  );

  testWidgets('building edit preserves coordinates without location requests', (
    tester,
  ) async {
    final h = PropertyHarness(handler: (_) async => http.Response('', 204));
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        PropertyEditorScreen(
          repository: h.repository,
          user: propertyUser({'properties.update'}),
          kind: PropertyKind.buildings,
          record: Building.fromJson(buildingJson()),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(h.requests, isEmpty);
    expect(
      tester
          .widget<AqariTextField>(find.byKey(const ValueKey('internalCode')))
          .controller!
          .text,
      'B-01',
    );
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    final put = h.requests.single;
    expect(put.method, 'PUT');
    final payload = jsonDecode(put.body) as Map<String, dynamic>;
    expect(payload['gpsLatitude'], 0.0);
    expect(payload['gpsLongitude'], 0.0);
  });

  testWidgets(
    'floor form POST uses the existing body and returns after success',
    (tester) async {
      final h = PropertyHarness(handler: (_) async => http.Response('', 204));
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          Builder(
            builder: (context) => Scaffold(
              body: TextButton(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute<void>(
                    builder: (_) => PropertyEditorScreen(
                      repository: h.repository,
                      user: propertyUser({'properties.create'}),
                      kind: PropertyKind.floors,
                      parentId: 'b1',
                    ),
                  ),
                ),
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const ValueKey('floorNumber')), '-١');
      await tester.enterText(
        find.byKey(const ValueKey('floorLabel')),
        'Lower floor',
      );
      await tester.tap(find.text('Save'));
      await tester.pumpAndSettle();
      final post = h.requests.singleWhere(
        (request) => request.method == 'POST',
      );
      expect(post.url.path, '/api/v1/buildings/b1/floors');
      expect(jsonDecode(post.body), {
        'floorNumber': -1,
        'floorLabel': 'Lower floor',
        'floorType': 2,
      });
      expect(find.text('Open'), findsOneWidget);
      expect(h.repository.revision, 1);
    },
  );

  testWidgets(
    'unit edit sends only rent/currency; validation rejection stays on the form',
    (tester) async {
      final h = PropertyHarness(
        handler: (_) async => http.Response(
          '{"detail":"private diagnostic","errors":{"BaseRentAmount":["invalid"]}}',
          422,
        ),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: h.repository,
            user: propertyUser({'properties.update'}),
            kind: PropertyKind.apartments,
            record: Apartment.fromJson(apartmentJson()),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('unitNumber')), findsNothing);
      await tester.enterText(
        find.byKey(const ValueKey('baseRentAmount')),
        '425',
      );
      await tester.tap(find.text('Save'));
      await tester.pumpAndSettle();
      expect(h.requests.single.method, 'PUT');
      expect(jsonDecode(h.requests.single.body), {
        'baseRentAmount': 425.0,
        'baseRentCurrency': 'JOD',
      });
      expect(find.text('private diagnostic'), findsNothing);
      expect(
        find.text('The server rejected this value. Review this field.'),
        findsOneWidget,
      );
      expect(h.repository.revision, 0);
    },
  );

  testWidgets(
    'archive cancellation sends no DELETE; conflicts leave record visible',
    (tester) async {
      final h = PropertyHarness(
        handler: (r) async => r.method == 'DELETE'
            ? http.Response('{"detail":"private dependency list"}', 409)
            : http.Response(jsonEncode(buildingJson()), 200),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          BuildingDetailsScreen(
            repository: h.repository,
            user: propertyUser({'properties.manage'}),
            id: 'b1',
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.drag(find.byType(ListView), const Offset(0, -900));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(AqariPopupMenu<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Archive'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();
      expect(h.requests.where((r) => r.method == 'DELETE'), isEmpty);
      await tester.tap(find.byType(AqariPopupMenu<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Archive'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Archive').last);
      await tester.pumpAndSettle();
      expect(find.text('Building details'), findsOneWidget);
      expect(h.requests.where((r) => r.method == 'DELETE'), hasLength(1));
      expect(find.text('private dependency list'), findsNothing);
      expect(h.repository.revision, 0);
    },
  );

  testWidgets(
    'governorate select scrolls at large text instead of overflowing',
    (tester) async {
      tester.view.physicalSize = const Size(360, 640);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final h = PropertyHarness();
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: h.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.buildings,
          ),
          scale: 1.6,
        ),
      );
      await tester.tap(find.text('Governorate'));
      await tester.pumpAndSettle();
      await tester.drag(find.byType(ListView).last, const Offset(0, -650));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Mafraq'));
      await tester.pumpAndSettle();
      expect(find.text('Mafraq'), findsOneWidget);
      expect(find.text('Governorate'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets(
    'loading transitions to an honest empty state; read-only has no Add',
    (tester) async {
      final pending = Completer<http.Response>();
      final h = PropertyHarness(handler: (_) => pending.future);
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(BuildingsScreen(repository: h.repository, user: propertyUser())),
      );
      expect(find.byType(AqariSkeleton), findsWidgets);
      pending.complete(http.Response('[]', 200));
      await tester.pumpAndSettle();
      expect(find.text('No records yet'), findsOneWidget);
      expect(find.byIcon(Icons.add_rounded), findsNothing);
    },
  );

  testWidgets(
    'network failure shows Retry and recovers without exposing server internals',
    (tester) async {
      var fail = true;
      final h = PropertyHarness(
        handler: (_) async => fail
            ? http.Response('{"detail":"SQL private stack"}', 500)
            : http.Response('[]', 200),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(BuildingsScreen(repository: h.repository, user: propertyUser())),
      );
      await tester.pumpAndSettle();
      expect(find.text('SQL private stack'), findsNothing);
      expect(find.text('Retry'), findsOneWidget);
      fail = false;
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(find.text('No records yet'), findsOneWidget);
      expect(h.requests, hasLength(2));
    },
  );

  testWidgets(
    'local search debounces without HTTP, distinguishes no results and resets',
    (tester) async {
      final h = PropertyHarness(
        handler: (_) async => http.Response(
          jsonEncode([
            buildingJson(),
            buildingJson(id: 'b2', name: 'بيت الزيتون'),
          ]),
          200,
          headers: {'content-type': 'application/json; charset=utf-8'},
        ),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(BuildingsScreen(repository: h.repository, user: propertyUser())),
      );
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'الزيتون');
      await tester.pump(const Duration(milliseconds: 220));
      expect(find.text('Cedar House'), findsNothing);
      expect(find.text('بيت الزيتون'), findsOneWidget);
      expect(h.requests, hasLength(1));
      await tester.enterText(find.byType(TextField), 'not-present');
      await tester.pumpAndSettle();
      expect(find.text('No matching results'), findsOneWidget);
      await tester.tap(find.text('Reset search and filter'));
      await tester.pumpAndSettle();
      expect(find.text('Cedar House'), findsOneWidget);
    },
  );

  testWidgets('hierarchy navigation keeps building and floor query context', (
    tester,
  ) async {
    final h = PropertyHarness();
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        Scaffold(
          body: PropertiesLanding(
            repository: h.repository,
            user: propertyUser(),
          ),
        ),
      ),
    );
    await tester.tap(find.text('Buildings'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Cedar House'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Floors'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('Floors'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Second floor'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('View units'));
    await tester.pumpAndSettle();
    expect(h.requests.last.url.queryParameters, {
      'buildingId': 'b1',
      'floorId': 'f1',
    });
    await tester.tap(find.textContaining('201-A'));
    await tester.pumpAndSettle();
    expect(find.text('Unit details'), findsOneWidget);
    expect(find.text('Occupied'), findsOneWidget);
    expect(
      h.requests.where((r) => r.url.path == '/api/v1/buildings/b1'),
      hasLength(1),
    );
  });

  testWidgets(
    'create permission and required validation do not send invalid forms',
    (tester) async {
      final h = PropertyHarness();
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          PropertyEditorScreen(
            repository: h.repository,
            user: propertyUser({'properties.create'}),
            kind: PropertyKind.floors,
            parentId: 'b1',
          ),
        ),
      );
      await tester.tap(find.text('Save'));
      await tester.pumpAndSettle();
      expect(find.text('This field is required.'), findsNWidgets(2));
      expect(h.requests.map((request) => (request.method, request.url.path)), [
        ('GET', '/api/v1/buildings/b1/floors/next-number'),
      ]);
      await tester.enterText(find.byKey(const ValueKey('floorNumber')), '2');
      await tester.enterText(
        find.byKey(const ValueKey('floorLabel')),
        'Second',
      );
      await tester.pumpAndSettle();
      expect(find.text('This field is required.'), findsNothing);
    },
  );

  testWidgets('manager has edit/archive actions while reader does not', (
    tester,
  ) async {
    final h = PropertyHarness();
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        BuildingDetailsScreen(
          repository: h.repository,
          user: propertyUser(),
          id: 'b1',
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Edit'), findsNothing);
    expect(find.text('Archive'), findsNothing);
    await tester.pumpWidget(
      app(
        BuildingDetailsScreen(
          key: const ValueKey('manager'),
          repository: h.repository,
          user: propertyUser({'properties.manage'}),
          id: 'b1',
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Edit'),
      250,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.byType(AqariPopupMenu<String>), findsOneWidget);
  });

  testWidgets(
    'Arabic RTL, long rows and large text remain within a small phone',
    (tester) async {
      tester.view.physicalSize = const Size(360, 800);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final h = PropertyHarness(
        handler: (r) async => http.Response(
          jsonEncode(
            r.url.path.endsWith('/buildings')
                ? [
                    buildingJson(
                      name:
                          'مبنى الزيتون السكني ذو الاسم الطويل في المنطقة الغربية',
                    ),
                  ]
                : [apartmentJson()],
          ),
          200,
          headers: {'content-type': 'application/json; charset=utf-8'},
        ),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        app(
          ApartmentsScreen(repository: h.repository, user: propertyUser()),
          arabic: true,
          scale: 1.8,
        ),
      );
      await tester.pumpAndSettle();
      expect(
        Directionality.of(tester.element(find.byType(AqariSearchField))),
        TextDirection.rtl,
      );
      final directional = tester.widget<Transform>(
        find
            .descendant(
              of: find.byType(AqariDirectionalIcon).first,
              matching: find.byType(Transform),
            )
            .first,
      );
      expect(
        directional.transform.storage[0],
        1,
        reason: 'Material directional icons already auto-mirror',
      );
      expect(find.text('مشغولة'), findsOneWidget);
      expect(tester.takeException(), isNull);
      final badge = tester.widget<AqariStatusBadge>(
        find.descendant(
          of: find.byType(OccupancyBadge),
          matching: find.byType(AqariStatusBadge),
        ),
      );
      expect(badge.variant, AqariStatusVariant.brand);
    },
  );

  testWidgets('no-read state makes no API request', (tester) async {
    final h = PropertyHarness();
    addTearDown(h.client.close);
    await tester.pumpWidget(
      app(
        Scaffold(
          body: PropertiesLanding(
            repository: h.repository,
            user: propertyUser({}),
          ),
        ),
      ),
    );
    expect(find.text('Read permission required'), findsOneWidget);
    expect(h.requests, isEmpty);
  });
}
