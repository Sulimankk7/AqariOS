import 'package:aqarios_mobile/features/shell/presentation/app_shell.dart';
import 'package:aqarios_mobile/features/leasing/presentation/leasing_landing.dart';
import 'package:aqarios_mobile/features/financial_operations/presentation/financial_operations_screen.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('Owner shell has exactly the required five destinations in order', () {
    expect(ownerNavigationLabels, [
      'الرئيسية',
      'العقارات',
      'التأجير',
      'المالية',
      'المزيد',
    ]);
    expect(ownerNavigationLabels.length, 5);
    expect(ownerNavigationLabels, isNot(contains('الإشعارات')));
  });

  test('Owner feature sections expose the IA gap-closure destinations', () {
    expect(leasingNavigationLabels, ['عقود الإيجار', 'المستأجرون']);
    expect(financeNavigationLabels, [
      'السجلات المالية',
      'المصاريف',
      'فواتير الخدمات',
    ]);
  });

  test('Tenant shell has its separate audited five destinations', () {
    expect(tenantNavigationLabels, [
      'الرئيسية',
      'عقد الإيجار',
      'الدفعات',
      'الخدمات',
      'الملف الشخصي',
    ]);
    expect(tenantNavigationLabels.length, 5);
    expect(ownerNavigationLabels, isNot(contains('الملف الشخصي')));
  });
}
