import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/lease_models.dart';
import '../domain/lease_behavior.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';
import 'lease_detail_screen.dart';
import 'lease_form_screen.dart';
import 'lease_document_screen.dart';

class LeaseListScreen extends StatefulWidget {
  const LeaseListScreen({required this.scope, this.apartmentId, super.key});
  final LeaseScope scope;
  final String? apartmentId;
  @override
  State<LeaseListScreen> createState() => _LeaseListScreenState();
}

class _LeaseListScreenState extends State<LeaseListScreen> {
  final _search = TextEditingController();
  final _scroll = ScrollController();
  int _tab = 0, _days = 30, _page = 0, _request = 0;
  LeaseSort _sort = LeaseSort.none;
  bool _descending = false, _loading = true;
  List<Lease> _data = [];
  Object? _error;
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final request = ++_request;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final repository = widget.scope.repository;
      final data = widget.apartmentId != null
          ? await repository.history(widget.apartmentId!)
          : _tab == 0
          ? await repository.contracts()
          : await repository.expiring(_days);
      if (mounted && request == _request) setState(() => _data = data);
    } catch (e) {
      if (mounted && request == _request) setState(() => _error = e);
    } finally {
      if (mounted && request == _request) setState(() => _loading = false);
    }
  }

  Future<void> _open(Lease lease, LeaseAction action) async {
    if (action == LeaseAction.activate) {
      final changed = await leaseConfirm(
        context,
        title: lt(context, 'activate'),
        message: '${lease.number}\n${lt(context, 'activateConfirm')}',
        action: () => widget.scope.repository.activate(lease.id),
      );
      if (changed && mounted) {
        widget.scope.properties.clear();
        AqariSnackbar.show(context, lt(context, 'saved'));
        await _load();
      }
      return;
    }
    final Widget screen = switch (action) {
      LeaseAction.view => LeaseDetailScreen(scope: widget.scope, id: lease.id),
      LeaseAction.attach => LeaseDocumentScreen(
        scope: widget.scope,
        contractId: lease.id,
      ),
      LeaseAction.edit => LeaseEditLoader(scope: widget.scope, id: lease.id),
      LeaseAction.renew => LeaseFormScreen(
        scope: widget.scope,
        mode: LeaseFormMode.renew,
        lease: lease,
      ),
      LeaseAction.terminate => LeaseFormScreen(
        scope: widget.scope,
        mode: LeaseFormMode.terminate,
        lease: lease,
      ),
      LeaseAction.activate => throw StateError('handled above'),
    };
    await Navigator.push(
      context,
      MaterialPageRoute<void>(builder: (_) => screen),
    );
    if (mounted) await _load();
  }

  String _sortLabel(LeaseSort sort) => lt(context, switch (sort) {
    LeaseSort.none => 'none',
    LeaseSort.number => 'contractNumber',
    LeaseSort.status => 'status',
    LeaseSort.start => 'startDate',
    LeaseSort.end => 'endDate',
    LeaseSort.rent => 'rent',
  });

  Future<void> _filters() => AqariBottomSheet.show<void>(
    context: context,
    title: lt(context, 'filters'),
    child: StatefulBuilder(
      builder: (sheetContext, refreshSheet) => Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (widget.apartmentId == null) ...[
            AqariTabs(
              labels: [lt(context, 'all'), lt(context, 'expiring')],
              selectedIndex: _tab,
              onSelected: (value) {
                setState(() {
                  _tab = value;
                  _page = 0;
                });
                refreshSheet(() {});
                _load();
              },
            ),
            if (_tab == 1) ...[
              const SizedBox(height: AqariSpacing.x4),
              AqariSelect<int>(
                label: lt(context, 'daysAhead'),
                valueLabel: '$_days ${lt(context, 'days')}',
                items: const [7, 14, 30, 60, 90, 120],
                itemLabel: (value) => '$value ${lt(context, 'days')}',
                onChanged: (value) {
                  setState(() {
                    _days = value;
                    _page = 0;
                  });
                  refreshSheet(() {});
                  _load();
                },
              ),
            ],
            const SizedBox(height: AqariSpacing.x4),
          ],
          AqariSelect<LeaseSort>(
            label: lt(context, 'sort'),
            valueLabel: _sortLabel(_sort),
            items: widget.apartmentId == null
                ? LeaseSort.values
                : const [
                    LeaseSort.none,
                    LeaseSort.number,
                    LeaseSort.status,
                    LeaseSort.rent,
                  ],
            itemLabel: _sortLabel,
            onChanged: (value) {
              setState(() {
                _sort = value;
                _descending = false;
              });
              refreshSheet(() {});
            },
          ),
          const SizedBox(height: AqariSpacing.x3),
          AqariButton(
            label: lt(context, _descending ? 'descending' : 'ascending'),
            icon: _descending ? Icons.arrow_downward : Icons.arrow_upward,
            variant: AqariButtonVariant.outlined,
            onPressed: _sort == LeaseSort.none
                ? null
                : () {
                    setState(() => _descending = !_descending);
                    refreshSheet(() {});
                  },
          ),
          const SizedBox(height: AqariSpacing.x4),
          AqariButton(
            label: lt(context, 'done'),
            expanded: true,
            onPressed: () => Navigator.pop(sheetContext),
          ),
        ],
      ),
    ),
  );
  @override
  Widget build(BuildContext context) {
    final rows = leaseRows(
      _data,
      _search.text,
      _sort,
      _descending,
      (s) => le(context, 'status', s),
    );
    final pages = ((rows.length + 9) ~/ 10).clamp(1, 1000000);
    final page = _page.clamp(0, pages - 1);
    final visible = rows.skip(page * 10).take(10).toList(growable: false);
    final body = SafeArea(
      child: Column(
        children: [
          Expanded(
            child: RefreshIndicator(
              onRefresh: _load,
              child: CustomScrollView(
                controller: _scroll,
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverToBoxAdapter(
                    child: Padding(
                      padding: const EdgeInsets.all(AqariSpacing.x4),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          if (widget.apartmentId == null) ...[
                            if (widget.scope.user.hasPermission(
                              'contracts.create',
                            ))
                              Align(
                                alignment: AlignmentDirectional.centerEnd,
                                child: AqariButton(
                                  label: lt(context, 'create'),
                                  icon: Icons.add,
                                  onPressed: () async {
                                    await Navigator.push(
                                      context,
                                      MaterialPageRoute<void>(
                                        builder: (_) => LeaseFormScreen(
                                          scope: widget.scope,
                                          mode: LeaseFormMode.create,
                                        ),
                                      ),
                                    );
                                    if (mounted) _load();
                                  },
                                ),
                              ),
                          ],
                          const SizedBox(height: AqariSpacing.x3),
                          Row(
                            children: [
                              Expanded(
                                child: AqariTextField(
                                  prefixIcon: Icons.search,
                                  suffix: _search.text.isEmpty
                                      ? null
                                      : AqariIconButton(
                                          icon: Icons.close,
                                          semanticLabel: lt(
                                            context,
                                            'clearSearch',
                                          ),
                                          onPressed: () => setState(() {
                                            _search.clear();
                                            _page = 0;
                                          }),
                                        ),
                                  controller: _search,
                                  hint: lt(context, 'search'),
                                  onChanged: (_) => setState(() => _page = 0),
                                ),
                              ),
                              const SizedBox(width: AqariSpacing.x2),
                              AqariIconButton(
                                icon: Icons.tune_rounded,
                                semanticLabel: lt(context, 'filters'),
                                onPressed: _filters,
                              ),
                            ],
                          ),
                          const SizedBox(height: AqariSpacing.x3),
                          Text(
                            '${lt(context, widget.apartmentId == null && _tab == 1 ? 'expiring' : 'all')}${_tab == 1 ? ' · $_days ${lt(context, 'days')}' : ''} · ${_sortLabel(_sort)}',
                            style: Theme.of(context).textTheme.labelMedium,
                          ),
                          if (widget.apartmentId != null || _tab == 0)
                            Padding(
                              padding: const EdgeInsets.only(
                                top: AqariSpacing.x2,
                              ),
                              child: Text(
                                lt(context, 'window'),
                                style: Theme.of(context).textTheme.bodySmall,
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                  if (_loading)
                    SliverList.list(
                      children: List.generate(
                        3,
                        (_) => const Padding(
                          padding: EdgeInsets.all(AqariSpacing.x4),
                          child: AqariSkeleton(height: 64),
                        ),
                      ),
                    )
                  else if (_error != null)
                    SliverToBoxAdapter(
                      child: LeaseFailure(_error, retry: _load),
                    )
                  else if (rows.isEmpty)
                    SliverToBoxAdapter(
                      child: AqariEmptyState(
                        message: lt(context, 'emptyHint'),
                        title: lt(
                          context,
                          _search.text.trim().isEmpty ? 'empty' : 'emptySearch',
                        ),
                      ),
                    )
                  else
                    SliverList.builder(
                      itemCount: visible.length,
                      itemBuilder: (context, i) {
                        final lease = visible[i];
                        return Padding(
                          padding: const EdgeInsetsDirectional.fromSTEB(
                            AqariSpacing.x4,
                            0,
                            AqariSpacing.x4,
                            AqariSpacing.x2,
                          ),
                          child: AqariListRow(
                            title: lease.number,
                            supportingText: leaseInline(
                              '${lease.start} — ${lease.end}',
                            ),
                            metadata:
                                '${leaseInline('${webNumber(lease.rent)} ${lease.currency}')} · ${le(context, 'frequency', lease.frequency)}',
                            status: LeaseBadge(lease.status),
                            onPressed: () => _open(lease, LeaseAction.view),
                            trailing: widget.apartmentId != null
                                ? null
                                : AqariPopupMenu<LeaseAction>(
                                    label:
                                        '${lt(context, 'actions')}: ${lease.number}',
                                    items:
                                        leaseActions(lease, widget.scope.user)
                                            .map(
                                              (a) => AqariPopupMenuAction(
                                                value: a,
                                                label: lt(context, a.name),
                                              ),
                                            )
                                            .toList(),
                                    onSelected: (a) => _open(lease, a),
                                  ),
                          ),
                        );
                      },
                    ),
                ],
              ),
            ),
          ),
          if (!_loading && _error == null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AqariSpacing.x4),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  AqariIconButton(
                    icon: context.isArabic
                        ? Icons.chevron_right
                        : Icons.chevron_left,
                    semanticLabel: lt(context, 'previous'),
                    onPressed: page == 0 ? null : () => _setPage(page - 1),
                  ),
                  Flexible(
                    child: Text(
                      '${lt(context, 'page')} ${page + 1} ${lt(context, 'of')} $pages · ${rows.length}',
                    ),
                  ),
                  AqariIconButton(
                    icon: context.isArabic
                        ? Icons.chevron_left
                        : Icons.chevron_right,
                    semanticLabel: lt(context, 'next'),
                    onPressed: page + 1 >= pages
                        ? null
                        : () => _setPage(page + 1),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
    return widget.apartmentId == null
        ? body
        : Scaffold(
            appBar: AqariAppBar(
              title: lt(context, 'apartmentHistory'),
              onBack: () => Navigator.maybePop(context),
            ),
            body: body,
          );
  }

  void _setPage(int value) {
    setState(() => _page = value);
    _scroll.jumpTo(0);
  }
}

class LeaseEditLoader extends StatelessWidget {
  const LeaseEditLoader({required this.scope, required this.id, super.key});
  final LeaseScope scope;
  final String id;
  @override
  Widget build(BuildContext context) => LeaseLoad<Lease>(
    standaloneTitle: lt(context, 'edit'),
    load: () => scope.repository.detail(id),
    builder: (lease, _) =>
        LeaseFormScreen(scope: scope, mode: LeaseFormMode.edit, lease: lease),
  );
}
