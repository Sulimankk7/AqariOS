import 'package:aqarios_mobile/core/design_system/design_system.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('brand and semantic source-of-truth values are exact', () {
    expect(AqariBrand.brown900.toARGB32(), 0xFF582F0E);
    expect(AqariBrand.green600.toARGB32(), 0xFF656D4A);
    expect(AqariColors.light.primary.toARGB32(), 0xFF3F4A2C);
    expect(AqariColors.light.background.toARGB32(), 0xFFF6F2E6);
    expect(AqariColors.light.surface.toARGB32(), 0xFFFFFFFF);
    expect(AqariColors.light.outlineVariant.toARGB32(), 0xFFE5DEC7);
    expect(AqariColors.dark.background.toARGB32(), 0xFF0E1116);
    expect(AqariColors.dark.warning.toARGB32(), 0xFFD0AA76);
    expect(AqariRadius.sm, 10);
    expect(AqariRadius.md, 14);
    expect(AqariRadius.lg, 20);
  });

  test('domain status mapping remains centralized', () {
    expect(
      AqariStatusRegistry.visualVariant(AqariDomainStatus.paid),
      AqariStatusVariant.success,
    );
    expect(
      AqariStatusRegistry.visualVariant(AqariDomainStatus.active),
      AqariStatusVariant.brand,
    );
    expect(
      AqariStatusRegistry.visualVariant(AqariDomainStatus.overdue),
      AqariStatusVariant.error,
    );
    expect(
      AqariStatusRegistry.visualVariant(AqariDomainStatus.pendingVerification),
      AqariStatusVariant.info,
    );
  });
}
