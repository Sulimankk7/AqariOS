import 'package:aqarios_mobile/app.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('production app requires explicit API configuration', (
    tester,
  ) async {
    await tester.pumpWidget(const AqariApp());
    await tester.pump();

    expect(find.text('إعداد الخادم مطلوب'), findsOneWidget);
    expect(find.textContaining('AQARIOS_API_BASE_URL'), findsOneWidget);
  });
}
