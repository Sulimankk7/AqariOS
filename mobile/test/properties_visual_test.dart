import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/features/properties/presentation/property_collection_screen.dart';
import 'package:aqarios_mobile/features/properties/presentation/apartment_details_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'properties_fixtures.dart';

void main() {
  testWidgets('phone RTL list and dark unit details visual review', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    for (final font in ['Tajawal-Regular', 'Tajawal-Medium', 'Tajawal-Bold']) {
      await (FontLoader(
        'Tajawal',
      )..addFont(rootBundle.load('assets/fonts/$font.ttf'))).load();
    }
    final h = PropertyHarness();
    addTearDown(h.client.close);
    await (FontLoader(
      'MaterialIcons',
    )..addFont(rootBundle.load('fonts/MaterialIcons-Regular.otf'))).load();
    Widget wrap(Widget child, bool dark) => MaterialApp(
      theme: dark
          ? AqariTheme.dark(const Locale('ar'))
          : AqariTheme.light(const Locale('ar')),
      locale: const Locale('ar'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: child,
    );
    await tester.pumpWidget(
      wrap(
        ApartmentsScreen(
          repository: h.repository,
          user: propertyUser({'properties.manage'}),
        ),
        false,
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    if (const bool.fromEnvironment('CAPTURE_PROPERTIES')) {
      await expectLater(
        find.byType(ApartmentsScreen),
        matchesGoldenFile('goldens/properties_units_ar.png'),
      );
    }
    await tester.pumpWidget(
      wrap(
        ApartmentDetailsScreen(
          repository: h.repository,
          user: propertyUser({'properties.manage'}),
          id: 'a1',
        ),
        true,
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('مشغولة'), findsOneWidget);
    expect(tester.takeException(), isNull);
    if (const bool.fromEnvironment('CAPTURE_PROPERTIES')) {
      await expectLater(
        find.byType(ApartmentDetailsScreen),
        matchesGoldenFile('goldens/properties_unit_dark_ar.png'),
      );
    }
  });
}
