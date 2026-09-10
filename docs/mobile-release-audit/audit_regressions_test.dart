import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:aqarios_mobile/core/network/api_client.dart';
import 'package:aqarios_mobile/core/storage/secure_session_store.dart';
import 'package:aqarios_mobile/features/dashboard/data/dashboard_repository.dart';
import 'package:aqarios_mobile/features/payments/presentation/payment_detail_screen.dart';
import '../../mobile/test/payments_fixtures.dart';
import '../../mobile/test/leasing_widgets_test.dart' show leasingApp;

class AuditStore extends SecureSessionStore {
  String token = 'company-A';
  @override
  Future<String?> readAccessToken() async => token;
}

void main() {
  test('SEC-01 dashboard must refetch after authenticated identity changes', () async {
    final store = AuditStore();
    var calls = 0;
    final api = ApiClient(baseUri: Uri.parse('https://audit.invalid'), sessionStore: store,
      client: MockClient((request) async {
        calls++;
        return http.Response(jsonEncode({'payments': {'outstandingAmount':
          request.headers['Authorization'] == 'Bearer company-A' ? 9876 : 123}}), 200);
      }));
    addTearDown(api.close);
    final repository = DashboardRepository(api);
    expect((await repository.load()).outstandingAmount, 9876);
    store.token = 'company-B';
    final second = await repository.load();
    print('AUDIT company B outstanding=${second.outstandingAmount}; requests=$calls');
    expect(second.outstandingAmount, 123, reason: 'Company B must never receive company A cached financial data');
  });

  testWidgets('PAY-01 tenant must see rejection reason from actual tenant DTO', (tester) async {
    const reason = 'QA رفض: قيمة التحويل غير مطابقة';
    final dto = paymentJson(status: 0)..remove('submissions');
    dto.addAll({'latestSubmissionStatus': 2, 'latestSubmissionRejectionReason': reason,
      'latestSubmissionAmount': 100, 'latestSubmissionDate': '2026-09-09T00:00:00Z'});
    final harness = PaymentHarness(handler: (_) async => jsonResponse([dto]));
    addTearDown(harness.client.close);
    tester.view.physicalSize = const Size(430, 4000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(leasingApp(PaymentDetailScreen(scope:harness.scope,id:'p1',tenant:true), arabic:true));
    await tester.pumpAndSettle();
    expect(find.text('إعادة إرسال الدفعة'), findsOneWidget);
    expect(find.text(reason), findsOneWidget, reason: 'Actual RentPaymentDto exposes latestSubmissionRejectionReason, not submissions[]');
  });
}
