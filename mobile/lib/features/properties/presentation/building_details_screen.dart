import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../auth/domain/user_profile.dart';
import '../data/properties_repository.dart';
import '../domain/property_models.dart';
import 'property_widgets.dart';
import 'property_collection_screen.dart';
import 'property_detail_body.dart';
import 'property_actions.dart';

class BuildingDetailsScreen extends StatelessWidget {
  const BuildingDetailsScreen({
    required this.repository,
    required this.user,
    required this.id,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final String id;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: tr(context, 'تفاصيل المبنى', 'Building details'),
      onBack: () => Navigator.maybePop(context),
    ),
    body: PropertyDetailBody<Building>(
      load: (force) => repository.building(id, force: force),
      builder: (context, b, reload) {
        Future<void> open(Widget screen) async {
          final revision = repository.revision;
          await Navigator.push(
            context,
            MaterialPageRoute<void>(builder: (_) => screen),
          );
          if (context.mounted && revision != repository.revision) {
            await reload();
          }
        }

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            PropertyIdentity(
              title: b.name,
              subtitle: b.address?.display,
              icon: Icons.apartment_outlined,
              status: AqariStatusBadge(
                label: b.active
                    ? tr(context, 'نشط', 'Active')
                    : tr(context, 'غير نشط', 'Inactive'),
                variant: b.active
                    ? AqariStatusVariant.brand
                    : AqariStatusVariant.neutral,
              ),
            ),
            AqariSectionHeader(title: tr(context, 'نظرة عامة', 'Overview')),
            PropertyFact(
              tr(context, 'الرمز الداخلي', 'Internal code'),
              b.code,
              ltr: true,
            ),
            PropertyFact(
              tr(context, 'نوع المبنى', 'Building type'),
              buildingTypeLabel(context, b.type),
            ),
            PropertyFact(
              tr(context, 'الطوابق المعلنة', 'Declared floors'),
              '${b.totalFloors}',
              ltr: true,
            ),
            PropertyFact(
              tr(context, 'عدد الوحدات', 'Unit count'),
              '${b.units}',
              ltr: true,
            ),
            const SizedBox(height: AqariSpacing.x4),
            AqariSectionHeader(
              title: tr(context, 'داخل المبنى', 'Inside this building'),
            ),
            AqariListRow(
              title: tr(context, 'الطوابق', 'Floors'),
              leading: const Icon(Icons.layers_outlined),
              showChevron: true,
              onPressed: () => open(
                FloorsScreen(repository: repository, user: user, building: b),
              ),
            ),
            AqariListRow(
              title: tr(context, 'الوحدات', 'Units'),
              leading: const Icon(Icons.door_front_door_outlined),
              showChevron: true,
              onPressed: () => open(
                ApartmentsScreen(
                  repository: repository,
                  user: user,
                  building: b,
                ),
              ),
            ),
            ExpansionTile(
              tilePadding: EdgeInsets.zero,
              childrenPadding: const EdgeInsets.only(bottom: AqariSpacing.x4),
              title: Text(
                tr(context, 'معلومات إضافية', 'Additional information'),
              ),
              children: [
                PropertyFact(
                  tr(context, 'سنة البناء', 'Construction year'),
                  b.year?.toString(),
                  ltr: true,
                ),
                PropertyFact(
                  tr(context, 'الإحداثيات', 'Coordinates'),
                  b.latitude == null ? null : '${b.latitude}, ${b.longitude}',
                  ltr: true,
                ),
                PropertyFact(
                  tr(context, 'الرمز البريدي', 'Postal code'),
                  b.address?.postalCode,
                  ltr: true,
                ),
              ],
            ),
            PropertyActions(
              repository: repository,
              user: user,
              kind: PropertyKind.buildings,
              record: b,
              reload: reload,
            ),
          ],
        );
      },
    ),
  );
}
