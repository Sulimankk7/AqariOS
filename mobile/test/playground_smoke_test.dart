import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:aqarios_mobile/playground/design_system_playground.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('playground renders and exposes theme controls', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AqariTheme.light(const Locale('ar')),
        home: DesignSystemPlayground(
          themePreference: AqariThemePreference.system,
          locale: const Locale('ar'),
          onThemeChanged: (_) {},
          onLocaleChanged: (_) {},
        ),
      ),
    );
    // The playground intentionally contains continuously animated loading examples.
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('system'), findsOneWidget);
    expect(find.textContaining('AqariOS'), findsWidgets);
    expect(find.text('العربية RTL'), findsOneWidget);
    expect(find.text('English LTR'), findsOneWidget);
  });
}
