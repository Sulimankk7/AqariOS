import 'dart:async';
import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../auth/domain/user_profile.dart';
import '../application/property_controller.dart';
import '../data/properties_repository.dart';
import '../domain/property_models.dart';
import 'property_widgets.dart';
import 'building_details_screen.dart';
import 'floor_details_screen.dart';
import 'apartment_details_screen.dart';
import 'property_editor_screen.dart';

class BuildingsScreen extends PropertyCollectionScreen {
  const BuildingsScreen({
    required super.repository,
    required super.user,
    super.chooseFloors,
    super.key,
  }) : super(kind: PropertyKind.buildings);
}

class FloorsScreen extends PropertyCollectionScreen {
  const FloorsScreen({
    required super.repository,
    required super.user,
    required Building super.building,
    super.key,
  }) : super(kind: PropertyKind.floors);
}

class ApartmentsScreen extends PropertyCollectionScreen {
  const ApartmentsScreen({
    required super.repository,
    required super.user,
    super.building,
    super.floor,
    super.key,
  }) : super(kind: PropertyKind.apartments);
}

class PropertyCollectionScreen extends StatefulWidget {
  const PropertyCollectionScreen({
    required this.repository,
    required this.user,
    required this.kind,
    this.building,
    this.floor,
    this.chooseFloors = false,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final PropertyKind kind;
  final Building? building;
  final Floor? floor;
  final bool chooseFloors;
  @override
  State<PropertyCollectionScreen> createState() =>
      _PropertyCollectionScreenState();
}

class _CollectionData {
  _CollectionData(this.rows, this.buildings);
  final List<PropertyRecord> rows;
  final Map<String, String> buildings;
}

class _PropertyCollectionScreenState extends State<PropertyCollectionScreen> {
  late final PropertyController<_CollectionData> _controller;
  final _search = TextEditingController();
  Timer? _debounce;
  String _query = '';
  int _filter = -1;
  @override
  void initState() {
    super.initState();
    _controller = PropertyController(_fetch);
    if (propertyPermission(widget.user, 'read')) _controller.load();
  }

  Future<_CollectionData> _fetch(bool force) async {
    final repo = widget.repository;
    if (widget.kind == PropertyKind.buildings) {
      return _CollectionData(await repo.buildings(force: force), {});
    }
    if (widget.kind == PropertyKind.floors) {
      return _CollectionData(
        await repo.floors(widget.building!.id, force: force),
        {},
      );
    }
    final results = await Future.wait<Object>([
      repo.apartments(
        buildingId: widget.building?.id,
        floorId: widget.floor?.id,
        force: force,
      ),
      if (widget.building == null) repo.buildings(force: force),
    ]);
    final buildings = widget.building == null
        ? results[1] as List<Building>
        : [widget.building!];
    return _CollectionData(results[0] as List<Apartment>, {
      for (final b in buildings) b.id: b.name,
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    _controller.dispose();
    super.dispose();
  }

  Future<void> _create() async {
    final revision = widget.repository.revision;
    final needsFloor =
        widget.kind == PropertyKind.apartments && widget.floor == null;
    final Widget screen = needsFloor
        ? (widget.building == null
              ? BuildingsScreen(
                  repository: widget.repository,
                  user: widget.user,
                  chooseFloors: true,
                )
              : FloorsScreen(
                  repository: widget.repository,
                  user: widget.user,
                  building: widget.building!,
                ))
        : PropertyEditorScreen(
            repository: widget.repository,
            user: widget.user,
            kind: widget.kind,
            parentId: widget.kind == PropertyKind.floors
                ? widget.building?.id
                : widget.floor?.id,
            parentLabel: widget.floor?.label ?? widget.building?.name,
          );
    await Navigator.push(
      context,
      MaterialPageRoute<void>(builder: (_) => screen),
    );
    if (mounted && revision != widget.repository.revision) {
      await _controller.load(force: true);
    }
  }

  String get _createLabel =>
      widget.kind == PropertyKind.apartments && widget.floor == null
      ? tr(context, 'اختر طابقاً لإضافة وحدة', 'Choose a floor to add a unit')
      : tr(context, 'إضافة', 'Add');
  Future<void> _open(PropertyRecord row) async {
    final before = widget.repository.revision;
    final screen = switch (row) {
      Building b when widget.chooseFloors => FloorsScreen(
        repository: widget.repository,
        user: widget.user,
        building: b,
      ),
      Building b => BuildingDetailsScreen(
        repository: widget.repository,
        user: widget.user,
        id: b.id,
      ),
      Floor f => FloorDetailsScreen(
        repository: widget.repository,
        user: widget.user,
        id: f.id,
        building: widget.building!,
      ),
      Apartment a => ApartmentDetailsScreen(
        repository: widget.repository,
        user: widget.user,
        id: a.id,
      ),
    };
    await Navigator.of(
      context,
    ).push(MaterialPageRoute<void>(builder: (_) => screen));
    if (mounted && widget.repository.revision != before) {
      await _controller.load(force: true);
    }
  }

  Future<void> _showFilter() async {
    final selected = await AqariBottomSheet.show<int>(
      context: context,
      title: widget.kind == PropertyKind.buildings
          ? tr(context, 'نوع المبنى', 'Building type')
          : tr(context, 'حالة الإشغال', 'Occupancy status'),
      child: Builder(
        builder: (sheetContext) => Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final value
                in widget.kind == PropertyKind.buildings
                    ? [-1, 0, 1, 2]
                    : [-1, 0, 1, 2, 3])
              AqariListRow(
                title: _filterLabel(context, value),
                trailing: value == _filter
                    ? Icon(
                        Icons.check_rounded,
                        color: context.aqariColors.primary,
                      )
                    : null,
                onPressed: () => Navigator.pop(sheetContext, value),
              ),
          ],
        ),
      ),
    );
    if (selected != null && mounted) setState(() => _filter = selected);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: widget.chooseFloors
          ? tr(context, 'اختر مبنى', 'Choose a building')
          : kindLabel(context, widget.kind),
      onBack: () => Navigator.maybePop(context),
      actions: [
        if (!widget.chooseFloors && propertyPermission(widget.user, 'create'))
          AqariIconButton(
            icon: Icons.add_rounded,
            semanticLabel: _createLabel,
            onPressed: _create,
          ),
      ],
    ),
    body: !propertyPermission(widget.user, 'read')
        ? AqariEmptyState(
            title: tr(
              context,
              'لا توجد صلاحية عرض',
              'Read permission required',
            ),
            message: tr(
              context,
              'تواصل مع مسؤول الشركة.',
              'Contact your company administrator.',
            ),
          )
        : SafeArea(
            child: Column(
              children: [
                Padding(
                  padding: const EdgeInsets.all(AqariSpacing.x4),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (widget.building != null) ...[
                        Text(
                          widget.building!.name,
                          style: Theme.of(context).textTheme.titleSmall,
                        ),
                        if (widget.floor != null) Text(widget.floor!.label),
                        const SizedBox(height: AqariSpacing.x3),
                      ],
                      Row(
                        children: [
                          Expanded(
                            child: AqariSearchField(
                              controller: _search,
                              hint: tr(
                                context,
                                'ابحث في القائمة المحمّلة',
                                'Search this loaded list',
                              ),
                              onChanged: (text) {
                                _debounce?.cancel();
                                _debounce = Timer(
                                  const Duration(milliseconds: 200),
                                  () {
                                    if (mounted) {
                                      setState(
                                        () =>
                                            _query = text.trim().toLowerCase(),
                                      );
                                    }
                                  },
                                );
                              },
                            ),
                          ),
                          if (widget.kind != PropertyKind.floors) ...[
                            const SizedBox(width: AqariSpacing.x2),
                            AqariIconButton(
                              icon: Icons.tune_rounded,
                              semanticLabel: tr(
                                context,
                                'تصفية القائمة',
                                'Filter list',
                              ),
                              onPressed: _showFilter,
                            ),
                          ],
                        ],
                      ),
                      if (widget.kind != PropertyKind.floors) ...[
                        if (_filter != -1)
                          Align(
                            alignment: AlignmentDirectional.centerStart,
                            child: Padding(
                              padding: const EdgeInsets.only(
                                top: AqariSpacing.x2,
                              ),
                              child: InputChip(
                                label: Text(_filterLabel(context, _filter)),
                                onPressed: _showFilter,
                                onDeleted: () => setState(() => _filter = -1),
                                deleteButtonTooltipMessage: tr(
                                  context,
                                  'مسح التصفية',
                                  'Clear filter',
                                ),
                              ),
                            ),
                          ),
                      ],
                    ],
                  ),
                ),
                Expanded(
                  child: AnimatedBuilder(
                    animation: _controller,
                    builder: (context, _) {
                      final data = _controller.value;
                      final error = _controller.error;
                      if (error != null) {
                        return ListView(
                          children: [
                            PropertyFailure(
                              error,
                              retry: () => _controller.load(force: true),
                            ),
                          ],
                        );
                      }
                      if (data == null) {
                        return ListView(children: const [PropertyLoading()]);
                      }
                      final visible = data.rows
                          .where((row) {
                            final status = row is Apartment
                                ? occupancyLabel(context, row.occupancy)
                                : '';
                            final buildingName = row is Apartment
                                ? data.buildings[row.buildingId] ?? ''
                                : '';
                            return '${row.searchText} $status $buildingName'
                                    .toLowerCase()
                                    .contains(_query) &&
                                (_filter == -1 ||
                                    (row is Building && row.type == _filter) ||
                                    (row is Apartment &&
                                        row.occupancy == _filter));
                          })
                          .toList(growable: false);
                      return RefreshIndicator(
                        onRefresh: () => _controller.load(force: true),
                        child: visible.isEmpty
                            ? ListView(
                                physics: const AlwaysScrollableScrollPhysics(),
                                children: [
                                  AqariEmptyState(
                                    title: _query.isNotEmpty || _filter != -1
                                        ? tr(
                                            context,
                                            'لا توجد نتائج',
                                            'No matching results',
                                          )
                                        : tr(
                                            context,
                                            'لا توجد سجلات بعد',
                                            'No records yet',
                                          ),
                                    message: _query.isNotEmpty || _filter != -1
                                        ? tr(
                                            context,
                                            'غيّر البحث أو أعد ضبط التصفية.',
                                            'Change your search or reset the filter.',
                                          )
                                        : tr(
                                            context,
                                            'لم تُضف سجلات لهذا النطاق بعد.',
                                            'No records have been added in this scope.',
                                          ),
                                    actionLabel:
                                        _query.isNotEmpty || _filter != -1
                                        ? tr(
                                            context,
                                            'مسح البحث والتصفية',
                                            'Reset search and filter',
                                          )
                                        : propertyPermission(
                                                widget.user,
                                                'create',
                                              ) &&
                                              !widget.chooseFloors
                                        ? _createLabel
                                        : null,
                                    onAction: () {
                                      if (_query.isNotEmpty || _filter != -1) {
                                        _debounce?.cancel();
                                        _search.clear();
                                        setState(() {
                                          _query = '';
                                          _filter = -1;
                                        });
                                      } else {
                                        _create();
                                      }
                                    },
                                  ),
                                ],
                              )
                            : ListView.builder(
                                padding: const EdgeInsetsDirectional.fromSTEB(
                                  AqariSpacing.x4,
                                  0,
                                  AqariSpacing.x4,
                                  AqariSpacing.x6,
                                ),
                                physics: const AlwaysScrollableScrollPhysics(),
                                itemCount: visible.length,
                                itemBuilder: (context, index) {
                                  final row = visible[index];
                                  return AqariListRow(
                                    key: ValueKey(row.id),
                                    leading: Icon(switch (row) {
                                      Building _ => Icons.apartment_outlined,
                                      Floor _ => Icons.layers_outlined,
                                      Apartment _ =>
                                        Icons.door_front_door_outlined,
                                    }),
                                    title: row is Apartment
                                        ? '${tr(context, 'وحدة', 'Unit')} ${isolated(row.title)}'
                                        : row.title,
                                    supportingText: switch (row) {
                                      Building b => b.address?.display,
                                      Floor f =>
                                        '${isolated(f.number)} · ${isolated(f.units)} ${tr(context, 'وحدة', 'units')}',
                                      Apartment a =>
                                        '${data.buildings[a.buildingId] ?? ''} · ${isolated(a.area)} ${tr(context, 'م²', 'm²')}',
                                    },
                                    metadata: row is Building ? row.code : null,
                                    status: row is Apartment
                                        ? OccupancyBadge(row.occupancy)
                                        : null,
                                    showChevron: true,
                                    onPressed: () => _open(row),
                                  );
                                },
                              ),
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
  );
  String _filterLabel(BuildContext c, int value) => value == -1
      ? tr(c, 'الكل', 'All')
      : widget.kind == PropertyKind.buildings
      ? buildingTypeLabel(c, value)
      : occupancyLabel(c, value);
}
