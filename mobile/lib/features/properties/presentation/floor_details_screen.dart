import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../auth/domain/user_profile.dart';
import '../data/properties_repository.dart';
import '../domain/property_models.dart';
import 'property_widgets.dart';
import 'property_collection_screen.dart';
import 'property_detail_body.dart';
import 'property_actions.dart';

class FloorDetailsScreen extends StatelessWidget {
  const FloorDetailsScreen({
    required this.repository,
    required this.user,
    required this.id,
    required this.building,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final String id;
  final Building building;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: tr(context, 'تفاصيل الطابق', 'Floor details'),
      onBack: () => Navigator.maybePop(context),
    ),
    body: PropertyDetailBody<Floor>(
      load: (force) => repository.floor(id, force: force),
      builder: (context, floor, reload) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          PropertyIdentity(
            title: floor.label,
            subtitle: building.name,
            icon: Icons.layers_outlined,
          ),
          AqariSectionHeader(title: tr(context, 'نظرة عامة', 'Overview')),
          PropertyFact(
            tr(context, 'رقم الطابق', 'Floor number'),
            '${floor.number}',
            ltr: true,
          ),
          PropertyFact(
            tr(context, 'نوع الطابق', 'Floor type'),
            floorTypeLabel(context, floor.type),
          ),
          PropertyFact(
            tr(context, 'عدد الوحدات', 'Unit count'),
            '${floor.units}',
            ltr: true,
          ),
          const SizedBox(height: AqariSpacing.x4),
          AqariSectionHeader(
            title: tr(context, 'الوحدات في هذا الطابق', 'Units on this floor'),
          ),
          AqariListRow(
            title: tr(context, 'عرض الوحدات', 'View units'),
            leading: const Icon(Icons.door_front_door_outlined),
            showChevron: true,
            supportingText: tr(
              context,
              'التفاصيل وحالة الإشغال',
              'Details and occupancy status',
            ),
            onPressed: () async {
              final revision = repository.revision;
              await Navigator.push(
                context,
                MaterialPageRoute<void>(
                  builder: (_) => ApartmentsScreen(
                    repository: repository,
                    user: user,
                    building: building,
                    floor: floor,
                  ),
                ),
              );
              if (context.mounted && revision != repository.revision) {
                await reload();
              }
            },
          ),
          PropertyActions(
            repository: repository,
            user: user,
            kind: PropertyKind.floors,
            record: floor,
            reload: reload,
          ),
        ],
      ),
    ),
  );
}
