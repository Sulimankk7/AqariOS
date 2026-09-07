import 'aqari_information.dart';

enum AqariDomainStatus {
  active,
  draft,
  pending,
  pendingVerification,
  paid,
  partiallyPaid,
  late,
  overdue,
  cancelled,
  expired,
  terminated,
  renewed,
  occupied,
  vacant,
  underMaintenance,
  success,
  warning,
  error,
  info,
}

abstract final class AqariStatusRegistry {
  static AqariStatusVariant visualVariant(AqariDomainStatus status) =>
      switch (status) {
        AqariDomainStatus.active ||
        AqariDomainStatus.occupied => AqariStatusVariant.brand,
        AqariDomainStatus.paid ||
        AqariDomainStatus.success => AqariStatusVariant.success,
        AqariDomainStatus.partiallyPaid ||
        AqariDomainStatus.late ||
        AqariDomainStatus.warning => AqariStatusVariant.warning,
        AqariDomainStatus.overdue ||
        AqariDomainStatus.expired ||
        AqariDomainStatus.terminated ||
        AqariDomainStatus.underMaintenance ||
        AqariDomainStatus.error => AqariStatusVariant.error,
        AqariDomainStatus.pendingVerification ||
        AqariDomainStatus.info => AqariStatusVariant.info,
        AqariDomainStatus.draft ||
        AqariDomainStatus.pending ||
        AqariDomainStatus.cancelled ||
        AqariDomainStatus.renewed ||
        AqariDomainStatus.vacant => AqariStatusVariant.neutral,
      };
}
