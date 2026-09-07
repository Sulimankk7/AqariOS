class DashboardSummary {
  const DashboardSummary({
    required this.totalBuildings,
    required this.totalApartments,
    required this.occupiedApartments,
    required this.vacantApartments,
    required this.occupancyRate,
    required this.activeLeases,
    required this.expiringIn30Days,
    required this.newLeasesThisMonth,
    required this.collectedThisMonth,
    required this.outstandingAmount,
    required this.overduePayments,
    required this.expensesThisMonth,
  });

  final int totalBuildings,
      totalApartments,
      occupiedApartments,
      vacantApartments;
  final double occupancyRate;
  final int activeLeases, expiringIn30Days, newLeasesThisMonth, overduePayments;
  final double collectedThisMonth, outstandingAmount, expensesThisMonth;

  factory DashboardSummary.fromJson(Map<String, dynamic> json) {
    final property = _map(json['property']);
    final leasing = _map(json['leasing']);
    final payments = _map(json['payments']);
    final financials = _map(json['financials']);
    return DashboardSummary(
      totalBuildings: _integer(property['totalBuildings']),
      totalApartments: _integer(property['totalApartments']),
      occupiedApartments: _integer(property['occupiedApartments']),
      vacantApartments: _integer(property['vacantApartments']),
      occupancyRate: _decimal(property['occupancyRate']),
      activeLeases: _integer(leasing['activeLeases']),
      expiringIn30Days: _integer(leasing['expiringIn30Days']),
      newLeasesThisMonth: _integer(leasing['newLeasesThisMonth']),
      collectedThisMonth: _decimal(payments['collectedThisMonth']),
      outstandingAmount: _decimal(payments['outstandingAmount']),
      overduePayments: _integer(payments['overduePayments']),
      expensesThisMonth: _decimal(financials['expensesThisMonth']),
    );
  }

  static Map<String, dynamic> _map(Object? value) =>
      value is Map ? Map<String, dynamic>.from(value) : const {};
  static int _integer(Object? value) => value is num ? value.toInt() : 0;
  static double _decimal(Object? value) => value is num ? value.toDouble() : 0;
}
