import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('attachment source sheet exposes camera gallery and files', (
    tester,
  ) async {
    AttachmentSource? result;
    await tester.pumpWidget(
      MaterialApp(
        theme: AqariTheme.light(const Locale('en')),
        home: Builder(
          builder: (context) => TextButton(
            onPressed: () async =>
                result = await AttachmentSourceSheet.show(context),
            child: const Text('open'),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();
    expect(find.text('Camera'), findsOneWidget);
    expect(find.text('Gallery'), findsOneWidget);
    expect(find.text('Files'), findsOneWidget);
    await tester.tap(find.text('Camera'));
    await tester.pumpAndSettle();
    expect(result, AttachmentSource.camera);
  });
}
