import '../../auth/domain/user_profile.dart';
import '../../properties/data/properties_repository.dart';
import '../data/leasing_repository.dart';
import '../data/lease_files.dart';

class LeaseScope {
  const LeaseScope({
    required this.repository,
    required this.properties,
    required this.user,
    required this.files,
  });
  final LeasingRepository repository;
  final PropertiesRepository properties;
  final UserProfile user;
  final LeaseFiles files;
}
