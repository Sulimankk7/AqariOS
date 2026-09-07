import '../../auth/domain/user_profile.dart';
import 'lease_models.dart';

enum LeaseAction { view, edit, activate, renew, terminate, attach }

List<LeaseAction> leaseActions(Lease lease, UserProfile user) {
  if (user.role != AqariRole.companyAdmin) return const [];
  return [
    LeaseAction.view,
    if (lease.status == 0 || lease.status == 1) ...[
      if (user.hasPermission('contracts.create')) LeaseAction.edit,
      if (user.hasPermission('contracts.approve')) LeaseAction.activate,
    ],
    if ((lease.status == 2 || lease.status == 3) &&
        user.hasPermission('contracts.approve'))
      LeaseAction.renew,
    if (lease.status == 2 && user.hasPermission('contracts.approve'))
      LeaseAction.terminate,
    if (lease.status != 5 && user.hasPermission('contracts.create'))
      LeaseAction.attach,
  ];
}

enum LeaseSort { none, number, status, start, end, rent }

List<Lease> leaseRows(
  List<Lease> source,
  String search,
  LeaseSort sort,
  bool descending,
  String Function(int) statusLabel,
) {
  final query = search.trim().toLowerCase();
  final rows = source
      .where((v) => query.isEmpty || v.searchText.contains(query))
      .toList();
  String value(Lease v) => switch (sort) {
    LeaseSort.number => v.number,
    LeaseSort.status => statusLabel(v.status),
    LeaseSort.start => v.start,
    LeaseSort.end => v.end,
    // Web sorts the formatted accessor string, not the numeric amount.
    LeaseSort.rent => '${webNumber(v.rent)} ${v.currency}',
    LeaseSort.none => '',
  };
  if (sort != LeaseSort.none) {
    final positions = {for (var i = 0; i < rows.length; i++) rows[i].id: i};
    rows.sort((a, b) {
      final comparison = value(a).compareTo(value(b)) * (descending ? -1 : 1);
      return comparison == 0
          ? positions[a.id]!.compareTo(positions[b.id]!)
          : comparison;
    });
  }
  return rows;
}

String webNumber(num value) =>
    value == value.roundToDouble() ? value.toInt().toString() : '$value';

enum LeaseFormMode { create, edit, renew, terminate }

class LeaseFormData {
  LeaseFormData(this.mode, {Lease? lease, DateTime? now}) {
    final today = (now ?? DateTime.now()).toUtc();
    values.addAll({
      'contractNumber': mode == LeaseFormMode.edit ? lease?.number ?? '' : '',
      'apartmentId': lease?.apartmentId ?? '',
      'tenantId': lease?.tenantId ?? '',
      'startDate': lease?.start.split('T').first ?? '',
      'endDate': lease?.end.split('T').first ?? '',
      'monthlyRentAmount': webNumber(lease?.rent ?? 0),
      'securityDepositAmount': webNumber(lease?.deposit ?? 0),
      'paymentDueDay': '${lease?.dueDay ?? 1}',
      'notes': lease?.notes ?? '',
      'terminationDate': dateOnly(today),
      'outstandingBalance': '0',
      'depositReturnedAmount': '0',
      'depositDeductionAmount': '0',
      'depositDeductionReason': '',
      'reason': '',
    });
    frequency = lease?.frequency ?? 0;
    regime = lease?.regime ?? 0;
    tenantType = lease?.tenantType ?? 0;
    if (mode == LeaseFormMode.renew && lease != null) {
      final start = DateTime.parse(lease.end);
      final duration = start.difference(DateTime.parse(lease.start));
      values['startDate'] = dateOnly(start);
      values['endDate'] = dateOnly(
        start.add(duration.inDays > 0 ? duration : const Duration(days: 365)),
      );
    }
  }
  final LeaseFormMode mode;
  final values = <String, String>{};
  int frequency = 0, regime = 0, tenantType = 0, terminationType = 0;
  bool settled = false;
  static String dateOnly(DateTime value) =>
      value.toIso8601String().split('T').first;
  static String digits(String input) {
    const arabic = '٠١٢٣٤٥٦٧٨٩', persian = '۰۱۲۳۴۵۶۷۸۹';
    return input
        .split('')
        .map(
          (c) => arabic.contains(c)
              ? '${arabic.indexOf(c)}'
              : persian.contains(c)
              ? '${persian.indexOf(c)}'
              : c == '٫'
              ? '.'
              : c,
        )
        .join();
  }

  double? amount(String key) {
    final value = digits(values[key] ?? '').trim();
    // Zod coerce.number on the Web treats an empty input as zero.
    return value.isEmpty ? 0 : double.tryParse(value);
  }

  Map<String, String> validate() {
    final errors = <String, String>{};
    void required(String key) {
      if ((values[key] ?? '').trim().isEmpty) errors[key] = 'required';
    }

    void nonnegative(String key, {bool positive = false}) {
      final n = amount(key);
      if (n == null || !n.isFinite || (positive ? n <= 0 : n < 0)) {
        errors[key] = positive ? 'positive' : 'nonnegative';
      }
    }

    if (mode == LeaseFormMode.terminate) {
      required('terminationDate');
      for (final key in [
        'outstandingBalance',
        'depositReturnedAmount',
        'depositDeductionAmount',
      ]) {
        nonnegative(key);
      }
    } else {
      for (final key in ['startDate', 'endDate']) {
        required(key);
      }
      if (mode != LeaseFormMode.renew) {
        required('apartmentId');
        required('tenantId');
      }
      if (mode != LeaseFormMode.edit) required('contractNumber');
      nonnegative('monthlyRentAmount', positive: true);
      nonnegative('securityDepositAmount');
      final due = amount('paymentDueDay');
      if (due == null || due < 1 || due > 28 || due != due.truncateToDouble()) {
        errors['paymentDueDay'] = 'dueDay';
      }
    }
    return errors;
  }

  LeaseJson toJson() => mode == LeaseFormMode.terminate
      ? {
          'terminationType': terminationType,
          'terminationDate': values['terminationDate'],
          'outstandingBalance': amount('outstandingBalance'),
          'depositReturnedAmount': amount('depositReturnedAmount'),
          'depositDeductionAmount': amount('depositDeductionAmount'),
          'depositDeductionReason': values['depositDeductionReason'],
          'finalUtilitySettlementCompleted': settled,
          'reason': values['reason'],
          'notes': values['notes'],
        }
      : {
          if (mode != LeaseFormMode.edit)
            'contractNumber': values['contractNumber'],
          if (mode != LeaseFormMode.renew) ...{
            'apartmentId': values['apartmentId'],
            'tenantId': values['tenantId'],
          },
          'startDate': values['startDate'],
          'endDate': values['endDate'],
          'monthlyRentAmount': amount('monthlyRentAmount'),
          'securityDepositAmount': amount('securityDepositAmount'),
          'paymentFrequency': frequency,
          'paymentDueDay': amount('paymentDueDay')?.toInt(),
          'legalRegime': regime,
          'tenantType': tenantType,
          'notes': values['notes'],
        };
}
