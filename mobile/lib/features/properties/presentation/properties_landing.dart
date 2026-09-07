import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../auth/domain/user_profile.dart';
import '../data/properties_repository.dart';
import 'property_widgets.dart';
import 'property_collection_screen.dart';
import '../../parking/parking_screen.dart';
import '../../leasing/presentation/lease_scope.dart';

class PropertiesLanding extends StatelessWidget {
  const PropertiesLanding({
    required this.repository,
    required this.user,
    this.leaseScope,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final LeaseScope? leaseScope;
  @override
  Widget build(BuildContext context) {
    if (!propertyPermission(user, 'read')) {
      return Center(
        child: AqariEmptyState(
          title: tr(context, 'صلاحية العرض مطلوبة', 'Read permission required'),
          message: tr(
            context,
            'تواصل مع مسؤول الشركة للوصول إلى سجلات العقارات.',
            'Contact your company administrator to access property records.',
          ),
        ),
      );
    }
    void open(Widget screen) => Navigator.of(
      context,
    ).push(MaterialPageRoute<void>(builder: (_) => screen));
    return ListView(
      padding: const EdgeInsets.all(AqariSpacing.x4),
      children: [
        PropertyIdentity(
          title: tr(context, 'محفظة العقارات', 'Property portfolio'),
          subtitle: tr(
            context,
            'المباني والوحدات والمواقف في مكان واحد.',
            'Your buildings, units and parking in one place.',
          ),
          icon: Icons.apartment_outlined,
        ),
        AqariSectionHeader(
          title: tr(context, 'استكشف العقارات', 'Explore properties'),
        ),
        const SizedBox(height: AqariSpacing.x3),
        AqariListRow(
          title: tr(context, 'المباني', 'Buildings'),
          leading: const Icon(Icons.apartment_outlined),
          supportingText: tr(
            context,
            'العناوين والتفاصيل والطوابق',
            'Addresses, details and floors',
          ),
          showChevron: true,
          onPressed: () =>
              open(BuildingsScreen(repository: repository, user: user)),
        ),
        AqariListRow(
          title: tr(context, 'الطوابق', 'Floors'),
          leading: const Icon(Icons.layers_outlined),
          supportingText: tr(
            context,
            'اختر مبنى لعرض طوابقه',
            'Choose a building to explore its floors',
          ),
          showChevron: true,
          onPressed: () => open(
            BuildingsScreen(
              repository: repository,
              user: user,
              chooseFloors: true,
            ),
          ),
        ),
        AqariListRow(
          title: tr(context, 'الوحدات', 'Units'),
          leading: const Icon(Icons.door_front_door_outlined),
          supportingText: tr(
            context,
            'الوحدات وحالة الإشغال',
            'Units and occupancy status',
          ),
          showChevron: true,
          onPressed: () =>
              open(ApartmentsScreen(repository: repository, user: user)),
        ),
        AqariListRow(
          title: tr(context, 'المواقف والكراجات', 'Parking & Garages'),
          leading: const Icon(Icons.local_parking_outlined),
          supportingText: tr(
            context,
            'المواقف وتخصيصها لعقود الإيجار',
            'Parking spots and lease assignments',
          ),
          showChevron: true,
          onPressed: leaseScope == null
              ? null
              : () => open(
                  ParkingScreen(
                    properties: repository,
                    leaseScope: leaseScope!,
                  ),
                ),
        ),
      ],
    );
  }
}
