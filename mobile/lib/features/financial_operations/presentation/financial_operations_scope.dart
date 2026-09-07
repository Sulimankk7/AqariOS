import '../../leasing/presentation/lease_scope.dart';
import '../../payments/data/payment_files.dart';
import '../../payments/presentation/payment_scope.dart';
import '../data/expenses_repository.dart';

class FinancialOperationsScope {
  const FinancialOperationsScope({
    required this.payments,
    required this.expenses,
    required this.leasing,
    required this.files,
  });

  final PaymentScope payments;
  final ExpensesRepository expenses;
  final LeaseScope leasing;
  final PaymentFiles files;
}
