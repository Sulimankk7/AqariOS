import 'package:flutter/material.dart';
import '../../core/design_system/design_system.dart';
import '../properties/data/properties_repository.dart';
import '../properties/domain/property_models.dart';
import '../properties/presentation/property_widgets.dart' show tr, isolated;
import '../leasing/data/leasing_repository.dart';
import '../leasing/domain/lease_models.dart';
import '../leasing/presentation/lease_detail_screen.dart';
import '../leasing/presentation/lease_scope.dart';
import 'parking.dart';

String _typeLabel(BuildContext context, int value) => context.isArabic
    ? parkingType(value)
    : const [
            'Standard',
            'Covered',
            'Visitor',
            'Accessible',
          ].elementAtOrNull(value) ??
          'Standard';

class ParkingScreen extends StatefulWidget {
  const ParkingScreen({
    required this.properties,
    required this.leaseScope,
    super.key,
  });
  final PropertiesRepository properties;
  final LeaseScope leaseScope;
  @override
  State<ParkingScreen> createState() => _ParkingScreenState();
}

class _ParkingScreenState extends State<ParkingScreen> {
  late final repo = ParkingRepository(widget.properties.client);
  String? building;
  List<Building> buildings = [];
  List<ParkingSpot> spots = [];
  Object? error;
  bool loading = true;
  @override
  void initState() {
    super.initState();
    _buildings();
  }

  Future<void> _buildings() async {
    try {
      buildings = await widget.properties.buildings();
    } catch (e) {
      error = e;
    }
    if (mounted) setState(() => loading = false);
  }

  Future<void> _load() async {
    if (building == null) return;
    setState(() => loading = true);
    try {
      spots = await repo.list(building!);
      error = null;
    } catch (e) {
      error = e;
    }
    if (mounted) setState(() => loading = false);
  }

  String get suggestion {
    final used = spots.map((s) => s.code.toUpperCase()).toSet();
    for (var i = 1; ; i++) {
      final v = 'P-${i.toString().padLeft(2, '0')}';
      if (!used.contains(v)) return v;
    }
  }

  Future<void> _form([ParkingSpot? item]) async {
    final code = TextEditingController(text: item?.code ?? suggestion),
        location = TextEditingController(text: item?.location ?? '');
    var type = item?.type ?? 0;
    await AqariBottomSheet.show<void>(
      context: context,
      title: item == null
          ? tr(context, 'إنشاء موقف', 'Create parking spot')
          : tr(context, 'تعديل الموقف', 'Edit parking spot'),
      child: StatefulBuilder(
        builder: (ctx, setSheet) => Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AqariFormSection(
              title: tr(context, 'بيانات الموقف', 'Spot information'),
              children: [
                AqariTextField(
                  controller: code,
                  label: tr(context, 'رمز الموقف', 'Spot code'),
                  textDirection: TextDirection.ltr,
                ),
                AqariSelect<int>(
                  label: tr(context, 'نوع الموقف', 'Parking type'),
                  valueLabel: _typeLabel(context, type),
                  items: const [0, 3],
                  itemLabel: (value) => _typeLabel(context, value),
                  onChanged: (v) => setSheet(() => type = v),
                ),
                AqariTextField(
                  controller: location,
                  label: tr(context, 'موقع الموقف', 'Location'),
                  maxLines: 2,
                ),
              ],
            ),
            AqariButton(
              label: tr(context, 'حفظ', 'Save'),
              expanded: true,
              onPressed: () async {
                try {
                  await repo.save(
                    building!,
                    existing: item,
                    code: code.text,
                    type: type,
                    location: location.text,
                  );
                  if (ctx.mounted) Navigator.pop(ctx);
                  await _load();
                } catch (_) {
                  if (ctx.mounted) {
                    AqariSnackbar.show(
                      ctx,
                      tr(
                        ctx,
                        'تعذر حفظ الموقف.',
                        'Unable to save the parking spot.',
                      ),
                      kind: AqariStatusKind.error,
                    );
                  }
                }
              },
            ),
          ],
        ),
      ),
    );
    code.dispose();
    location.dispose();
  }

  Future<void> _detail(ParkingSpot spot) async {
    ParkingAssignment? assignment;
    Lease? lease;
    try {
      assignment = await repo.assignment(spot.id);
      if (assignment != null) {
        lease = await widget.leaseScope.repository.detail(assignment.leaseId);
      }
    } catch (_) {}
    if (!mounted) return;
    await AqariBottomSheet.show<void>(
      context: context,
      title: spot.code,
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AqariDetailRow(
              label: tr(context, 'نوع الموقف', 'Parking type'),
              value: _typeLabel(context, spot.type),
            ),
            if (spot.location?.isNotEmpty == true)
              AqariDetailRow(
                label: tr(context, 'الموقع', 'Location'),
                value: spot.location!,
              ),
            const SizedBox(height: AqariSpacing.x4),
            AqariSectionHeader(
              title: tr(context, 'التخصيص الحالي', 'Current assignment'),
            ),
            const SizedBox(height: AqariSpacing.x3),
            if (assignment == null) ...[
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: AqariStatusBadge(
                  label: tr(context, 'غير مخصص', 'Unassigned'),
                ),
              ),
              const SizedBox(height: AqariSpacing.x2),
              Text(
                tr(
                  context,
                  'لا يوجد تخصيص نشط لهذا الموقف.',
                  'This parking spot has no active assignment.',
                ),
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: context.aqariColors.textSecondary,
                ),
              ),
              const SizedBox(height: AqariSpacing.x4),
              AqariButton(
                label: tr(context, 'تخصيص لعقد', 'Assign to lease'),
                expanded: true,
                onPressed: () => _assign(spot),
              ),
            ] else ...[
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: AqariStatusBadge(
                  label: tr(context, 'مخصص', 'Assigned'),
                  variant: AqariStatusVariant.brand,
                ),
              ),
              const SizedBox(height: AqariSpacing.x2),
              AqariDetailRow(
                label: tr(context, 'المستأجر', 'Tenant'),
                value: assignment.tenant,
              ),
              if (lease != null) ...[
                AqariSectionHeader(
                  title: tr(context, 'العقد', 'Lease contract'),
                ),
                AqariListRow(
                  title: lease.number,
                  supportingText: tr(
                    context,
                    'عرض عقد الإيجار',
                    'View lease contract',
                  ),
                  leading: const Icon(Icons.description_outlined),
                  showChevron: true,
                  onPressed: () {
                    Navigator.pop(context);
                    Navigator.push<void>(
                      context,
                      MaterialPageRoute(
                        builder: (_) => LeaseDetailScreen(
                          scope: widget.leaseScope,
                          id: assignment!.leaseId,
                        ),
                      ),
                    );
                  },
                ),
              ],
              AqariDetailRow(
                label: tr(context, 'تاريخ البدء', 'Start date'),
                value: assignment.start,
                ltr: true,
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariButton(
                label: tr(context, 'إنهاء التخصيص', 'End assignment'),
                variant: AqariButtonVariant.outlined,
                onPressed: () async {
                  await repo.end(assignment!.id);
                  if (!mounted) return;
                  Navigator.pop(context);
                  await _load();
                },
              ),
            ],
            const SizedBox(height: AqariSpacing.x3),
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: AqariPopupMenu<String>(
                label: tr(context, 'إجراءات الموقف', 'Parking spot actions'),
                items: [
                  AqariPopupMenuAction(
                    value: 'archive',
                    label: tr(context, 'أرشفة الموقف', 'Archive parking spot'),
                    icon: Icons.archive_outlined,
                  ),
                ],
                onSelected: (_) => _archive(spot),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _archive(ParkingSpot spot) async {
    final archived = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) =>
          _ArchiveParkingDialog(action: () => repo.archive(spot.id)),
    );
    if (archived != true || !mounted) return;
    Navigator.pop(context);
    await _load();
    if (mounted) {
      AqariSnackbar.show(
        context,
        tr(context, 'تمت أرشفة الموقف.', 'Parking spot archived.'),
      );
    }
  }

  Future<void> _assign(ParkingSpot spot) async {
    final leases = LeasingRepository(widget.properties.client);
    try {
      final values = await leases.contracts(pageSize: 100);
      final tenants = await leases.tenants();
      final active = values
          .where((lease) => lease.status == 2 && lease.buildingId == building)
          .toList();
      if (!mounted) return;
      var selected = active.isEmpty ? null : active.first;
      await AqariBottomSheet.show<void>(
        context: context,
        title: tr(context, 'تخصيص الموقف', 'Assign parking spot'),
        child: StatefulBuilder(
          builder: (ctx, setSheet) => Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (active.isEmpty)
                AqariEmptyState(
                  title: tr(context, 'لا توجد عقود نشطة', 'No active leases'),
                  message: tr(
                    context,
                    'لا توجد عقود إيجار نشطة في هذه العمارة.',
                    'There are no active lease contracts in this building.',
                  ),
                )
              else ...[
                Text(
                  tr(
                    context,
                    'تظهر عقود الإيجار النشطة في المبنى فقط.',
                    'Only active leases in this building are shown.',
                  ),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: context.aqariColors.textSecondary,
                  ),
                ),
                const SizedBox(height: AqariSpacing.x3),
                AqariSelect(
                  label: tr(
                    context,
                    'اختر عقد الإيجار',
                    'Choose lease contract',
                  ),
                  valueLabel: selected!.number,
                  items: active,
                  itemLabel: (lease) {
                    final tenant = tenants
                        .where((t) => t.id == lease.tenantId)
                        .firstOrNull;
                    return '${lease.number} — ${tenant?.name ?? ''}';
                  },
                  onChanged: (value) => setSheet(() => selected = value),
                ),
                const SizedBox(height: AqariSpacing.x4),
                AqariButton(
                  label: tr(context, 'تخصيص لعقد', 'Assign to lease'),
                  expanded: true,
                  onPressed: () async {
                    try {
                      await repo.assign(spot.id, selected!.id);
                      if (ctx.mounted) Navigator.pop(ctx);
                      if (mounted) Navigator.pop(context);
                      await _load();
                    } catch (_) {
                      if (ctx.mounted) {
                        AqariSnackbar.show(
                          ctx,
                          tr(
                            ctx,
                            'تعذر تخصيص الموقف.',
                            'Unable to assign the parking spot.',
                          ),
                          kind: AqariStatusKind.error,
                        );
                      }
                    }
                  },
                ),
              ],
            ],
          ),
        ),
      );
    } catch (_) {
      if (mounted) {
        AqariSnackbar.show(
          context,
          tr(
            context,
            'تعذر تحميل عقود الإيجار.',
            'Unable to load lease contracts.',
          ),
          kind: AqariStatusKind.error,
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: tr(context, 'المواقف والكراجات', 'Parking & Garages'),
      onBack: () => Navigator.pop(context),
    ),
    bottomNavigationBar: building != null && !loading && error == null
        ? AqariActionBar(
            child: AqariButton(
              label: tr(context, 'إضافة موقف', 'Add parking spot'),
              icon: Icons.add_rounded,
              expanded: true,
              onPressed: () => _form(),
            ),
          )
        : null,
    body: loading
        ? Center(
            child: AqariLoadingState(
              label: tr(context, 'جارٍ التحميل', 'Loading parking'),
            ),
          )
        : error != null
        ? Center(
            child: AqariErrorState(
              title: tr(
                context,
                'تعذر تحميل المواقف',
                'Unable to load parking',
              ),
              message: tr(context, 'حاول مرة أخرى.', 'Please try again.'),
              retryLabel: tr(context, 'إعادة المحاولة', 'Retry'),
              onRetry: building == null ? _buildings : _load,
            ),
          )
        : Column(
            children: [
              Padding(
                padding: const EdgeInsets.all(AqariSpacing.x4),
                child: AqariSelect<Building>(
                  label: tr(context, 'اختر العمارة', 'Building'),
                  valueLabel: building == null
                      ? tr(context, 'اختر العمارة', 'Choose a building')
                      : buildings.firstWhere((b) => b.id == building).name,
                  items: buildings,
                  itemLabel: (b) => b.name,
                  onChanged: (b) {
                    setState(() => building = b.id);
                    _load();
                  },
                ),
              ),
              if (building == null)
                Expanded(
                  child: Center(
                    child: AqariEmptyState(
                      title: tr(
                        context,
                        'المواقف والكراجات',
                        'Parking & Garages',
                      ),
                      message: tr(
                        context,
                        'اختر عمارة لعرض المواقف.',
                        'Choose a building to see its parking spots.',
                      ),
                    ),
                  ),
                )
              else
                Expanded(
                  child: RefreshIndicator(
                    onRefresh: _load,
                    child: ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsetsDirectional.fromSTEB(
                        AqariSpacing.x4,
                        0,
                        AqariSpacing.x4,
                        AqariSpacing.x6,
                      ),
                      children: [
                        Padding(
                          padding: const EdgeInsets.only(
                            bottom: AqariSpacing.x3,
                          ),
                          child: AqariSectionHeader(
                            title: tr(
                              context,
                              '${isolated(spots.length)} موقف',
                              '${isolated(spots.length)} parking spots',
                            ),
                          ),
                        ),
                        if (spots.isEmpty)
                          Padding(
                            padding: const EdgeInsets.all(AqariSpacing.x6),
                            child: AqariEmptyState(
                              title: tr(
                                context,
                                'لا توجد مواقف',
                                'No parking spots',
                              ),
                              message: tr(
                                context,
                                'لا توجد مواقف في هذه العمارة حاليًا.',
                                'This building has no parking spots yet.',
                              ),
                            ),
                          ),
                        for (final s in spots)
                          AqariListRow(
                            title: s.code,
                            leading: const Icon(Icons.local_parking_outlined),
                            supportingText:
                                '${_typeLabel(context, s.type)}${s.location?.isNotEmpty == true ? ' · ${s.location}' : ''}',
                            showChevron: true,
                            onPressed: () => _detail(s),
                          ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
  );
}

class _ArchiveParkingDialog extends StatefulWidget {
  const _ArchiveParkingDialog({required this.action});

  final Future<void> Function() action;

  @override
  State<_ArchiveParkingDialog> createState() => _ArchiveParkingDialogState();
}

class _ArchiveParkingDialogState extends State<_ArchiveParkingDialog> {
  bool busy = false;
  Object? error;

  Future<void> submit() async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await widget.action();
      if (mounted) Navigator.pop(context, true);
    } catch (value) {
      if (mounted) setState(() => error = value);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !busy,
    child: AlertDialog(
      title: Text(tr(context, 'أرشفة الموقف؟', 'Archive parking spot?')),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            tr(
              context,
              'سيتم إخفاء هذا الموقف من قائمة المواقف النشطة.',
              'This spot will be hidden from the active parking list.',
            ),
          ),
          if (error != null) ...[
            const SizedBox(height: AqariSpacing.x3),
            Text(error.toString()),
          ],
        ],
      ),
      actions: [
        AqariButton(
          label: tr(context, 'إلغاء', 'Cancel'),
          variant: AqariButtonVariant.text,
          onPressed: busy ? null : () => Navigator.pop(context, false),
        ),
        AqariButton(
          label: tr(context, 'أرشفة', 'Archive'),
          variant: AqariButtonVariant.destructive,
          loading: busy,
          onPressed: busy ? null : submit,
        ),
      ],
    ),
  );
}
