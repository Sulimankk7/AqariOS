import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../auth/domain/user_profile.dart';
import '../data/properties_repository.dart';
import '../domain/property_models.dart';
import 'property_widgets.dart';
import 'property_detail_body.dart';
import 'property_actions.dart';

class ApartmentDetailsScreen extends StatelessWidget {
  const ApartmentDetailsScreen({
    required this.repository,
    required this.user,
    required this.id,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final String id;
  Future<(Apartment, Building, Floor)> _load(bool force) async {
    final a = await repository.apartment(id, force: force);
    final parents = await Future.wait<PropertyRecord>([
      repository.building(a.buildingId),
      repository.floor(a.floorId),
    ]);
    return (a, parents[0] as Building, parents[1] as Floor);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: tr(context, 'تفاصيل الوحدة', 'Unit details'),
      onBack: () => Navigator.maybePop(context),
    ),
    body: PropertyDetailBody<(Apartment, Building, Floor)>(
      load: _load,
      builder: (context, data, reload) {
        final (a, building, floor) = data;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            PropertyIdentity(
              title: '${tr(context, 'وحدة', 'Unit')} ${isolated(a.number)}',
              subtitle: '${building.name} · ${floor.label}',
              icon: Icons.door_front_door_outlined,
              status: OccupancyBadge(a.occupancy),
            ),
            AqariSectionSurface(
              child: AqariKpi(
                label: tr(context, 'الإيجار الأساسي', 'Base rent'),
                value: a.rent == null
                    ? '—'
                    : isolated('${a.rent!.toStringAsFixed(3)} ${a.currency}'),
              ),
            ),
            const SizedBox(height: AqariSpacing.x5),
            AqariSectionHeader(
              title: tr(context, 'مواصفات الوحدة', 'Unit facts'),
            ),
            PropertyFact(
              tr(context, 'المساحة', 'Area'),
              '${a.area} m²',
              ltr: true,
            ),
            PropertyFact(
              tr(context, 'غرف النوم', 'Bedrooms'),
              '${a.bedrooms}',
              ltr: true,
            ),
            PropertyFact(
              tr(context, 'الحمامات', 'Bathrooms'),
              '${a.bathrooms}',
              ltr: true,
            ),
            PropertyFact(
              tr(context, 'الحالة التشغيلية', 'Operational status'),
              a.active
                  ? tr(context, 'نشطة', 'Active')
                  : tr(context, 'غير نشطة', 'Inactive'),
            ),
            ExpansionTile(
              tilePadding: EdgeInsets.zero,
              childrenPadding: const EdgeInsets.only(bottom: AqariSpacing.x4),
              title: Text(tr(context, 'الملكية', 'Ownership')),
              children: [
                PropertyFact(
                  tr(context, 'نوع الملكية', 'Ownership type'),
                  a.ownership == 0
                      ? tr(context, 'ملك الشركة', 'Company owned')
                      : a.ownership == 1
                      ? tr(context, 'ملك طرف ثالث', 'Third-party owned')
                      : tr(context, 'غير معروف', 'Unknown'),
                ),
                if (a.owner != null)
                  PropertyFact(
                    tr(context, 'اسم المالك', 'Owner name'),
                    a.owner,
                  ),
                if (a.ownerPhone != null)
                  PropertyFact(
                    tr(context, 'هاتف المالك', 'Owner phone'),
                    a.ownerPhone,
                    ltr: true,
                  ),
              ],
            ),
            PropertyActions(
              repository: repository,
              user: user,
              kind: PropertyKind.apartments,
              record: a,
              reload: reload,
            ),
          ],
        );
      },
    ),
  );
}
