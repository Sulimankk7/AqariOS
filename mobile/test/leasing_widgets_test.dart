import 'dart:async';
import 'dart:convert';
import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/leasing/domain/lease_behavior.dart';
import 'package:aqarios_mobile/features/leasing/domain/lease_models.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_list_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_detail_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_document_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_form_screen.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_utilities.dart';
import 'package:aqarios_mobile/features/leasing/presentation/lease_parking.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'leasing_fixtures.dart';

Widget leasingApp(
  Widget screen, {
  bool arabic = false,
  bool dark = false,
  double scale = 1,
}) {
  final locale = Locale(arabic ? 'ar' : 'en');
  return MaterialApp(
    locale: locale,
    supportedLocales: const [Locale('ar'), Locale('en')],
    localizationsDelegates: const [
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    theme: dark ? AqariTheme.dark(locale) : AqariTheme.light(locale),
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(
        context,
      ).copyWith(textScaler: TextScaler.linear(scale)),
      child: child!,
    ),
    home: Scaffold(body: screen),
  );
}

Finder button(String label) => find.widgetWithText(AqariButton, label);
Future<void> reveal(WidgetTester tester, Finder finder) async {
  await tester.scrollUntilVisible(
    finder,
    400,
    scrollable: find.byType(Scrollable).first,
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('lease parking renders multiple spots without technical IDs', (
    tester,
  ) async {
    final h = LeasingHarness(
      handler: (request) async => request.url.path.endsWith('/parking')
          ? http.Response(
              jsonEncode([
                {
                  'assignmentId': 'assignment-secret',
                  'parkingSpotId': 'spot-secret',
                  'parkingSpotCode': 'P-01',
                  'parkingType': 'Standard',
                },
                {
                  'assignmentId': 'assignment-secret-2',
                  'parkingSpotId': 'spot-secret-2',
                  'parkingSpotCode': 'P-02',
                  'parkingType': 'DisabledAccess',
                },
              ]),
              200,
            )
          : LeasingHarness.defaultResponse(request),
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(
      leasingApp(
        LeaseParking(scope: h.scope, leaseId: 'lease-secret'),
        arabic: true,
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('P-01'), findsOneWidget);
    expect(find.text('موقف عادي'), findsOneWidget);
    expect(find.text('P-02'), findsOneWidget);
    expect(find.text('ذوي احتياجات خاصة'), findsOneWidget);
    expect(find.textContaining('secret'), findsNothing);
  });

  testWidgets('lease parking handles loading, empty, error, and retry', (
    tester,
  ) async {
    final pending = Completer<http.Response>();
    var reads = 0;
    final h = LeasingHarness(
      handler: (_) {
        reads++;
        if (reads == 1) return pending.future;
        if (reads == 2) return Future.value(http.Response('{}', 500));
        return Future.value(http.Response('[]', 200));
      },
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(
      leasingApp(LeaseParking(scope: h.scope, leaseId: 'lease-1')),
    );
    await tester.pump();
    expect(find.byType(AqariLoadingState), findsOneWidget);
    pending.complete(http.Response('{}', 500));
    await tester.pumpAndSettle();
    expect(find.byType(AqariErrorState), findsOneWidget);
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.byType(AqariErrorState), findsOneWidget);
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(
      find.text('No parking spots are assigned to this contract.'),
      findsOneWidget,
    );
  });

  testWidgets(
    'list keeps Web ten-row paging and local search without another API request',
    (tester) async {
      var reads = 0;
      final h = LeasingHarness(
        handler: (r) async {
          reads++;
          return http.Response(
            jsonEncode(
              List.generate(
                12,
                (i) => leaseJson(id: 'l$i', number: 'Contract-${i + 1}'),
              ),
            ),
            200,
          );
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(leasingApp(LeaseListScreen(scope: h.scope)));
      await tester.pumpAndSettle();
      expect(find.text('Page 1 of 2 · 12'), findsOneWidget);
      await tester.tap(
        find.byWidgetPredicate(
          (w) => w is AqariIconButton && w.semanticLabel == 'Next',
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Page 2 of 2 · 12'), findsOneWidget);
      await tester.enterText(find.byType(TextField), 'Contract-12');
      await tester.pumpAndSettle();
      expect(find.text('Page 1 of 1 · 1'), findsOneWidget);
      expect(reads, 1);
      await tester.tap(
        find.byWidgetPredicate(
          (w) => w is AqariIconButton && w.semanticLabel == 'Clear search',
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Page 1 of 2 · 12'), findsOneWidget);
    },
  );
  testWidgets('expiring choices use the existing daysAhead endpoint', (
    tester,
  ) async {
    final requests = <http.Request>[];
    final h = LeasingHarness(
      handler: (r) async {
        requests.add(r);
        return LeasingHarness.defaultResponse(r);
      },
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(leasingApp(LeaseListScreen(scope: h.scope)));
    await tester.pumpAndSettle();
    await tester.tap(find.bySemanticsLabel('Filter and sort'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Expiring'));
    await tester.pumpAndSettle();
    expect(requests.last.url.queryParameters, {'daysAhead': '30'});
    await tester.tap(find.text('30 days'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('120 days'),
      100,
      scrollable: find.byType(Scrollable).last,
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('120 days'));
    await tester.pumpAndSettle();
    expect(requests.last.url.queryParameters, {'daysAhead': '120'});
  });
  testWidgets(
    'list loading error retry and empty state use real state transitions',
    (tester) async {
      final pending = Completer<http.Response>();
      var reads = 0;
      final h = LeasingHarness(
        handler: (_) {
          reads++;
          return reads == 1
              ? pending.future
              : Future.value(http.Response('[]', 200));
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(leasingApp(LeaseListScreen(scope: h.scope)));
      await tester.pump();
      expect(find.byType(AqariSkeleton), findsWidgets);
      pending.complete(http.Response('{}', 503));
      await tester.pumpAndSettle();
      await reveal(tester, find.text('Retry'));
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      await reveal(tester, find.text('No contracts'));
      expect(find.text('No contracts'), findsOneWidget);
      expect(reads, 2);
    },
  );
  testWidgets('list popup exposes allowed actions and opens a detail route', (
    tester,
  ) async {
    final h = LeasingHarness();
    addTearDown(h.client.close);
    await tester.pumpWidget(leasingApp(LeaseListScreen(scope: h.scope)));
    await tester.pumpAndSettle();
    await reveal(tester, find.byType(AqariPopupMenu<LeaseAction>));
    await tester.tap(find.byType(AqariPopupMenu<LeaseAction>));
    await tester.pumpAndSettle();
    expect(find.text('Edit draft'), findsOneWidget);
    expect(find.text('Activate'), findsOneWidget);
    await tester.tap(find.text('View details'));
    await tester.pumpAndSettle();
    expect(find.byType(LeaseDetailScreen), findsOneWidget);
    await tester.tap(find.bySemanticsLabel('Back').first);
    await tester.pumpAndSettle();
    expect(find.byType(LeaseListScreen), findsOneWidget);
  });
  testWidgets(
    'activation failure keeps confirmation open then retries successfully',
    (tester) async {
      var activates = 0;
      final h = LeasingHarness(
        handler: (r) async {
          if (r.url.path.endsWith('/activate')) {
            activates++;
            return activates == 1
                ? http.Response(
                    '{"code":"LEASE_ACTIVATE_SIGNED_DOCUMENT_REQUIRED"}',
                    422,
                  )
                : http.Response('', 204);
          }
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(LeaseDetailScreen(scope: h.scope, id: 'lease-1')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byType(AqariPopupMenu<LeaseAction>));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ListTile, 'Activate'));
      await tester.pumpAndSettle();
      await tester.tap(
        find.descendant(
          of: find.byType(AlertDialog),
          matching: button('Activate'),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        find.text('Attach a signed contract document before activation.'),
        findsOneWidget,
      );
      await tester.tap(
        find.descendant(
          of: find.byType(AlertDialog),
          matching: button('Activate'),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byType(AlertDialog), findsNothing);
      expect(activates, 2);
    },
  );
  testWidgets(
    'read-only permissions hide mutation actions and Tenant dependency stays explicit',
    (tester) async {
      final h = LeasingHarness(permissions: {});
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(LeaseDetailScreen(scope: h.scope, id: 'lease-1')),
      );
      await tester.pumpAndSettle();
      expect(button('Activate'), findsNothing);
      expect(button('Edit draft'), findsNothing);
      await reveal(tester, find.text('View tenants'));
      await tester.tap(find.text('View tenants'));
      await tester.pumpAndSettle();
      expect(find.byType(Dialog), findsOneWidget);
      expect(find.textContaining('separate Tenants module'), findsWidgets);
    },
  );
  testWidgets(
    'Pending Signature edit sends immutable-number-free payload and surfaces actual rejection',
    (tester) async {
      http.Request? update;
      final h = LeasingHarness(
        handler: (r) async {
          if (r.method == 'PUT') {
            update = r;
            return http.Response('{"code":"LEASE_EDIT_NOT_DRAFT"}', 422);
          }
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(
          LeaseFormScreen(
            scope: h.scope,
            mode: LeaseFormMode.edit,
            lease: Lease.fromJson(leaseJson(status: 1)),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await reveal(tester, button('Save'));
      await tester.tap(button('Save'));
      await tester.pumpAndSettle();
      expect(update, isNotNull);
      expect(jsonDecode(update!.body), isNot(contains('contractNumber')));
      expect(find.textContaining('Only Draft'), findsOneWidget);
      expect(find.byType(LeaseFormScreen), findsOneWidget);
    },
  );
  testWidgets(
    'document attachment retries confirmed file without uploading twice',
    (tester) async {
      var attaches = 0;
      final h = LeasingHarness(
        handler: (r) async {
          if (r.url.path.endsWith('/documents')) {
            attaches++;
            return attaches == 1
                ? http.Response('{}', 409)
                : http.Response('"d1"', 201);
          }
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(LeaseDocumentScreen(scope: h.scope, contractId: 'lease-1')),
      );
      await tester.pumpAndSettle();
      await tester.tap(button('Select file'));
      await tester.pumpAndSettle();
      await reveal(tester, button('Attach document'));
      await tester.tap(button('Attach document'));
      await tester.pumpAndSettle();
      expect(find.textContaining('Conflict with current data'), findsOneWidget);
      await reveal(tester, button('Attach document'));
      await tester.tap(button('Attach document'));
      await tester.pumpAndSettle();
      expect(attaches, 2);
      expect(h.files.uploads, 1);
    },
  );
  testWidgets(
    'utility link validates account length and sends lease-scoped numeric type',
    (tester) async {
      http.Request? linked;
      final h = LeasingHarness(
        permissions: {'UtilityBills.Manage'},
        handler: (r) async {
          if (r.method == 'POST') {
            linked = r;
            return http.Response('"u1"', 201);
          }
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(
          ListView(
            children: [LeaseUtilities(scope: h.scope, leaseId: 'lease-1')],
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(button('Link account').first);
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).first, '123');
      await tester.tap(button('Save'));
      await tester.pumpAndSettle();
      expect(linked, isNull);
      expect(find.textContaining('Electricity: 10 digits'), findsOneWidget);
      await tester.enterText(find.byType(TextField).first, '0012345678');
      await tester.tap(button('Save'));
      await tester.pumpAndSettle();
      expect(jsonDecode(linked!.body), {
        'leaseContractId': 'lease-1',
        'utilityType': 0,
        'accountNumber': '0012345678',
        'meterNumber': null,
      });
    },
  );
  testWidgets('RTL dark long text and text scaling do not overflow', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(411, 914);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final h = LeasingHarness(
      handler: (_) async => http.Response(
        jsonEncode({
          ...leaseJson(number: 'عقد إيجار طويل جداً للمبنى السكني رقم 123-ABC'),
          'notes': 'ملاحظات طويلة '.padRight(800, 'ن'),
        }),
        200,
        headers: {'content-type': 'application/json; charset=utf-8'},
      ),
    );
    addTearDown(h.client.close);
    await tester.pumpWidget(
      leasingApp(
        LeaseDetailScreen(scope: h.scope, id: 'lease-1'),
        arabic: true,
        dark: true,
        scale: 1.5,
      ),
    );
    await tester.pumpAndSettle();
    expect(
      Directionality.of(tester.element(find.byType(LeaseDetailScreen))),
      TextDirection.rtl,
    );
    expect(tester.takeException(), isNull);
    await reveal(tester, find.text('سجل الحالات'));
    expect(tester.takeException(), isNull);
  });
  testWidgets(
    'renew submits Web-prefilled terms without changing apartment or tenant',
    (tester) async {
      http.Request? renewal;
      final h = LeasingHarness(
        handler: (r) async {
          if (r.url.path.endsWith('/renew')) {
            renewal = r;
            return http.Response('"renewal-id"', 201);
          }
          return LeasingHarness.defaultResponse(r);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(
          LeaseFormScreen(
            scope: h.scope,
            mode: LeaseFormMode.renew,
            lease: Lease.fromJson(leaseJson(status: 2)),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await reveal(tester, button('Save'));
      await tester.tap(button('Save'));
      await tester.pumpAndSettle();
      expect(renewal, isNull);
      expect(find.text('This field is required.'), findsOneWidget);
      await tester.enterText(find.byType(TextField).first, 'RENEWAL-2027');
      await reveal(tester, button('Save'));
      await tester.tap(button('Save'));
      await tester.pumpAndSettle();
      final body = jsonDecode(renewal!.body) as Map;
      expect(body['contractNumber'], 'RENEWAL-2027');
      expect(body['startDate'], '2027-01-01');
      expect(body, isNot(contains('apartmentId')));
      expect(body, isNot(contains('tenantId')));
    },
  );
  testWidgets(
    'terminate sends settlement fields and retains meaningful server failure',
    (tester) async {
      http.Request? termination;
      final h = LeasingHarness(
        handler: (r) async {
          termination = r;
          return http.Response(
            '{"code":"LEASE_TERMINATE_DATE_IN_FUTURE"}',
            422,
          );
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(
          LeaseFormScreen(
            scope: h.scope,
            mode: LeaseFormMode.terminate,
            lease: Lease.fromJson(leaseJson(status: 2)),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await reveal(tester, button('Terminate'));
      await tester.tap(button('Terminate'));
      await tester.pumpAndSettle();
      expect(
        jsonDecode(termination!.body)['finalUtilitySettlementCompleted'],
        false,
      );
      expect(
        find.text('Termination date cannot be in the future.'),
        findsOneWidget,
      );
    },
  );
  testWidgets(
    'a late number suggestion never overwrites an edited then cleared number',
    (tester) async {
      final suggestion = Completer<http.Response>();
      final h = LeasingHarness(
        handler: (r) => r.url.path.endsWith('/next-number')
            ? suggestion.future
            : LeasingHarness.defaultResponse(r),
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(LeaseFormScreen(scope: h.scope, mode: LeaseFormMode.create)),
      );
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).first, 'MINE');
      await tester.enterText(find.byType(TextField).first, '');
      suggestion.complete(http.Response('{"value":"SERVER-NUMBER"}', 200));
      await tester.pumpAndSettle();
      expect(
        (tester.widget<TextField>(
          find.byType(TextField).first,
        )).controller!.text,
        isEmpty,
      );
    },
  );
  testWidgets(
    'document view/download uses fresh URLs; deletion requires confirmation',
    (tester) async {
      final requests = <http.Request>[];
      final h = LeasingHarness(
        handler: (r) async {
          requests.add(r);
          if (r.url.path.endsWith('/download')) {
            return http.Response(
              '{"url":"https://storage.example.test/file","filename":"lease.pdf"}',
              200,
            );
          }
          if (r.method == 'DELETE') return http.Response('', 204);
          return http.Response(
            jsonEncode(leaseJson(documents: [documentJson()])),
            200,
          );
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(LeaseDetailScreen(scope: h.scope, id: 'lease-1')),
      );
      await tester.pumpAndSettle();
      await reveal(tester, find.byType(AqariPopupMenu<String>));
      await tester.tap(find.byType(AqariPopupMenu<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ListTile, 'View'));
      await tester.pumpAndSettle();
      expect(h.files.opened?.host, 'storage.example.test');
      expect(requests.last.url.queryParameters['inline'], 'true');
      await tester.tap(find.byType(AqariPopupMenu<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ListTile, 'Download'));
      await tester.pumpAndSettle();
      expect(requests.last.url.queryParameters['inline'], 'false');
      await tester.tap(find.byType(AqariPopupMenu<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ListTile, 'Delete'));
      await tester.pumpAndSettle();
      expect(requests.where((r) => r.method == 'DELETE'), isEmpty);
      await tester.tap(
        find.descendant(
          of: find.byType(AlertDialog),
          matching: button('Delete'),
        ),
      );
      await tester.pumpAndSettle();
      expect(requests.where((r) => r.method == 'DELETE').length, 1);
    },
  );
  testWidgets(
    'utility sync and unlink consume existing endpoints and confirm unlink',
    (tester) async {
      final requests = <http.Request>[];
      final h = LeasingHarness(
        permissions: {'UtilityBills.Manage'},
        handler: (r) async {
          requests.add(r);
          return r.method == 'GET'
              ? http.Response(
                  jsonEncode({
                    'items': [utilityJson()],
                    'hasMore': false,
                    'nextCursor': null,
                  }),
                  200,
                )
              : http.Response('', r.method == 'DELETE' ? 204 : 202);
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(
          ListView(
            children: [LeaseUtilities(scope: h.scope, leaseId: 'lease-1')],
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(button('Sync'));
      await tester.pumpAndSettle();
      expect(
        requests.any(
          (r) => r.url.path.endsWith('/u1/sync') && r.method == 'POST',
        ),
        true,
      );
      await tester.tap(button('Unlink'));
      await tester.pumpAndSettle();
      expect(requests.any((r) => r.method == 'DELETE'), false);
      await tester.tap(
        find.descendant(
          of: find.byType(AlertDialog),
          matching: button('Unlink'),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        requests.any(
          (r) => r.url.path.endsWith('/accounts/u1') && r.method == 'DELETE',
        ),
        true,
      );
    },
  );
  testWidgets(
    'prior contract and apartment history remain navigable within Leasing',
    (tester) async {
      final requests = <http.Request>[];
      final h = LeasingHarness(
        handler: (r) async {
          requests.add(r);
          if (r.url.path.contains('/history/apartment/')) {
            return http.Response(jsonEncode([leaseJson()]), 200);
          }
          return http.Response(
            jsonEncode({...leaseJson(), 'priorContractId': 'prior-id'}),
            200,
          );
        },
      );
      addTearDown(h.client.close);
      await tester.pumpWidget(
        leasingApp(LeaseDetailScreen(scope: h.scope, id: 'lease-1')),
      );
      await tester.pumpAndSettle();
      await reveal(tester, find.text('Prior contract'));
      await tester.tap(find.text('Prior contract'));
      await tester.pumpAndSettle();
      expect(requests.last.url.path, '/api/v1/leasing/contracts/prior-id');
      await tester.tap(find.bySemanticsLabel('Back').first);
      await tester.pumpAndSettle();
      await reveal(tester, find.text('Apartment lease history'));
      await tester.tap(find.text('Apartment lease history'));
      await tester.pumpAndSettle();
      expect(
        requests.last.url.path,
        '/api/v1/leasing/contracts/history/apartment/a1',
      );
    },
  );
}
