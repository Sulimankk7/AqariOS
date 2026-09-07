import '../../leasing/presentation/lease_scope.dart';
import '../data/payments_repository.dart';
import '../data/payment_files.dart';

class PaymentScope {
  const PaymentScope({
    required this.repository,
    required this.leasing,
    required this.files,
    this.onChanged,
  });
  final PaymentsRepository repository;
  final LeaseScope leasing;
  final PaymentFiles files;
  final void Function()? onChanged;
  bool can(String permission) => leasing.user.hasPermission(permission);
}
