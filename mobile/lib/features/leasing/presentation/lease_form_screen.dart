import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../../properties/domain/property_models.dart';
import '../domain/lease_behavior.dart';
import '../domain/lease_errors.dart';
import '../domain/lease_models.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';

class LeaseFormScreen extends StatefulWidget {
  const LeaseFormScreen({
    required this.scope,
    required this.mode,
    this.lease,
    super.key,
  });
  final LeaseScope scope;
  final LeaseFormMode mode;
  final Lease? lease;
  @override
  State<LeaseFormScreen> createState() => _LeaseFormScreenState();
}

class _LeaseFormScreenState extends State<LeaseFormScreen> {
  late final LeaseFormData _form = LeaseFormData(
    widget.mode,
    lease: widget.lease,
  );
  late final Map<String, TextEditingController> _fields = {
    for (final entry in _form.values.entries)
      entry.key: TextEditingController(text: entry.value),
  };
  final _scroll = ScrollController();
  Map<String, String> _errors = {};
  List<Building> _buildings = [];
  List<LeaseTenant> _tenants = [];
  List<Apartment> _apartments = [];
  String _building = '';
  String? _rentSource;
  bool _numberEdited = false;
  Object? _lookupError, _apartmentError, _error;
  bool _loading = false, _apartmentsLoading = false, _busy = false;
  int _lookupRevision = 0;
  bool get _parties =>
      widget.mode == LeaseFormMode.create || widget.mode == LeaseFormMode.edit;
  bool get _termination => widget.mode == LeaseFormMode.terminate;
  String get _title => switch (widget.mode) {
    LeaseFormMode.create => 'create',
    LeaseFormMode.edit => 'edit',
    LeaseFormMode.renew => 'renew',
    LeaseFormMode.terminate => 'terminate',
  };
  bool get _allowed => widget.scope.user.hasPermission(
    _parties ? 'contracts.create' : 'contracts.approve',
  );
  @override
  void initState() {
    super.initState();
    if (_parties && _allowed) _lookups();
    if (widget.mode == LeaseFormMode.create && _allowed) {
      widget.scope.repository
          .nextNumber()
          .then((value) {
            if (mounted &&
                !_numberEdited &&
                _fields['contractNumber']!.text.isEmpty) {
              _fields['contractNumber']!.text = value;
            }
          })
          .catchError((Object _) {
            /* Web treats number suggestions as optional. */
          });
    }
  }

  @override
  void dispose() {
    for (final field in _fields.values) {
      field.dispose();
    }
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _lookups() async {
    setState(() {
      _loading = true;
      _lookupError = null;
    });
    try {
      final result = await Future.wait<Object>([
        widget.scope.properties.buildings(),
        widget.scope.repository.tenants(),
      ]);
      if (!mounted) return;
      setState(() {
        _buildings = result[0] as List<Building>;
        _tenants = result[1] as List<LeaseTenant>;
      });
      if (widget.lease != null) {
        final apartment = await widget.scope.properties.apartment(
          widget.lease!.apartmentId,
        );
        if (!mounted) return;
        _building = apartment.buildingId;
        await _loadApartments();
      }
    } catch (e) {
      if (mounted) setState(() => _lookupError = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _loadApartments() async {
    final revision = ++_lookupRevision;
    setState(() {
      _apartmentsLoading = true;
      _apartmentError = null;
    });
    try {
      final items = await widget.scope.properties.apartments(
        buildingId: _building,
        force: true,
      );
      if (mounted && revision == _lookupRevision) {
        setState(() => _apartments = items);
      }
    } catch (e) {
      if (mounted && revision == _lookupRevision) {
        setState(() => _apartmentError = e);
      }
    } finally {
      if (mounted && revision == _lookupRevision) {
        setState(() => _apartmentsLoading = false);
      }
    }
  }

  void _sync() {
    for (final entry in _fields.entries) {
      _form.values[entry.key] = entry.value.text;
    }
  }

  Future<void> _submit() async {
    if (_busy || !_allowed) return;
    _sync();
    final errors = _form.validate();
    setState(() {
      _errors = errors;
      _error = null;
    });
    if (errors.isNotEmpty) {
      _top();
      return;
    }
    FocusScope.of(context).unfocus();
    setState(() => _busy = true);
    try {
      final r = widget.scope.repository;
      switch (widget.mode) {
        case LeaseFormMode.create:
          await r.create(_form.toJson());
        case LeaseFormMode.edit:
          await r.edit(widget.lease!.id, _form.toJson());
        case LeaseFormMode.renew:
          await r.renew(widget.lease!.id, _form.toJson());
        case LeaseFormMode.terminate:
          await r.terminate(widget.lease!.id, _form.toJson());
      }
      // Leasing mutations can change apartment occupancy; invalidate existing data, without editing Properties.
      widget.scope.properties.clear();
      if (mounted) {
        AqariSnackbar.show(context, lt(context, 'saved'));
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e;
          if (e is ApiProblem) {
            for (final entry in e.validationErrors.entries) {
              final match = _fields.keys.where(
                (key) =>
                    key.toLowerCase() ==
                    entry.key.split('.').last.toLowerCase(),
              );
              if (match.isNotEmpty && entry.value.isNotEmpty) {
                _errors[match.first] = leaseFieldError(
                  entry.value.first,
                  context.isArabic,
                );
              }
            }
          }
        });
        _top();
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _top() {
    if (_scroll.hasClients) {
      _scroll.animateTo(
        0,
        duration: const Duration(milliseconds: 200),
        curve: Curves.easeOut,
      );
    }
  }

  Widget _text(
    String key, {
    bool numeric = false,
    int lines = 1,
    String? helper,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x4),
    child: AqariTextField(
      controller: _fields[key],
      label: lt(context, key),
      enabled: !_busy,
      maxLines: lines,
      keyboardType: numeric
          ? const TextInputType.numberWithOptions(decimal: true)
          : lines > 1
          ? TextInputType.multiline
          : null,
      textDirection: numeric || key == 'contractNumber'
          ? TextDirection.ltr
          : null,
      errorText: _errors[key] == null ? null : lt(context, _errors[key]!),
      helperText: helper,
      onChanged: (_) {
        if (key == 'contractNumber') _numberEdited = true;
        if (key == 'monthlyRentAmount' && _rentSource != null) {
          setState(() => _rentSource = null);
        }
      },
    ),
  );
  Widget _enum(
    String label,
    String group,
    int value,
    ValueChanged<int> change,
  ) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x4),
    child: AqariSelect<int>(
      label: lt(context, label),
      valueLabel: le(context, group, value),
      items: List.generate(leaseEnumEnglish[group]!.length, (i) => i),
      itemLabel: (i) => le(context, group, i),
      enabled: !_busy,
      onChanged: (value) => setState(() => change(value)),
    ),
  );
  Widget _date(String key) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x4),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AqariButton(
          label:
              '${lt(context, key)}: ${_fields[key]!.text.isEmpty ? lt(context, 'date') : _fields[key]!.text}',
          variant: AqariButtonVariant.outlined,
          icon: Icons.calendar_today_outlined,
          onPressed: _busy
              ? null
              : () async {
                  final selected = await showDatePicker(
                    context: context,
                    initialDate:
                        DateTime.tryParse(_fields[key]!.text) ?? DateTime.now(),
                    firstDate: DateTime(1),
                    lastDate: DateTime(9999, 12, 31),
                  );
                  if (selected != null && mounted) {
                    setState(
                      () =>
                          _fields[key]!.text = LeaseFormData.dateOnly(selected),
                    );
                  }
                },
        ),
        if (_errors[key] != null)
          Text(
            lt(context, _errors[key]!),
            style: TextStyle(color: context.aqariColors.error),
          ),
      ],
    ),
  );
  Widget _partyFields() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      if (_loading) AqariLoadingState(label: lt(context, 'loading')),
      if (_lookupError != null) LeaseFailure(_lookupError, retry: _lookups),
      AqariSelect<Building>(
        label: lt(context, 'building'),
        valueLabel:
            _buildings.where((b) => b.id == _building).firstOrNull?.name ??
            lt(context, 'choose'),
        items: _buildings,
        itemLabel: (b) => '${b.name} (${b.code ?? ''})',
        enabled: !_busy && !_loading && _lookupError == null,
        onChanged: (b) {
          setState(() {
            _building = b.id;
            _fields['apartmentId']!.clear();
            _fields['monthlyRentAmount']!.text = '0';
            _rentSource = null;
            _apartments = [];
          });
          _loadApartments();
        },
      ),
      const SizedBox(height: AqariSpacing.x4),
      AqariSelect<Apartment>(
        label: lt(context, 'apartmentId'),
        valueLabel:
            _apartments
                .where((a) => a.id == _fields['apartmentId']!.text)
                .firstOrNull
                ?.number ??
            lt(context, _building.isEmpty ? 'buildingFirst' : 'choose'),
        items: _apartments,
        itemLabel: (a) => '${a.number} (${webNumber(a.area)} m²)',
        enabled:
            !_busy &&
            !_apartmentsLoading &&
            _building.isNotEmpty &&
            _apartmentError == null,
        onChanged: (a) => setState(() {
          _fields['apartmentId']!.text = a.id;
          final rent = a.currency == 'JOD' && (a.rent ?? 0) > 0 ? a.rent! : 0;
          _fields['monthlyRentAmount']!.text = webNumber(rent);
          _rentSource = rent > 0 ? a.id : null;
        }),
      ),
      if (_apartmentsLoading) AqariLoadingState(label: lt(context, 'loading')),
      if (_apartmentError != null)
        LeaseFailure(_apartmentError, retry: _loadApartments),
      if (_building.isNotEmpty &&
          !_apartmentsLoading &&
          _apartmentError == null &&
          _apartments.isEmpty)
        Text(lt(context, 'noApartments')),
      if (_errors['apartmentId'] != null)
        Text(
          lt(context, _errors['apartmentId']!),
          style: TextStyle(color: context.aqariColors.error),
        ),
      const SizedBox(height: AqariSpacing.x4),
      AqariSelect<LeaseTenant>(
        label: lt(context, 'tenantId'),
        valueLabel:
            _tenants
                .where((t) => t.id == _fields['tenantId']!.text)
                .firstOrNull
                ?.label ??
            lt(context, 'choose'),
        items: _tenants,
        itemLabel: (t) => t.label,
        enabled: !_busy && !_loading && _lookupError == null,
        onChanged: (t) => setState(() => _fields['tenantId']!.text = t.id),
      ),
      if (!_loading && _lookupError == null && _tenants.isEmpty)
        Text(lt(context, 'noTenants')),
      if (_errors['tenantId'] != null)
        Text(
          lt(context, _errors['tenantId']!),
          style: TextStyle(color: context.aqariColors.error),
        ),
    ],
  );
  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !_busy,
    child: Scaffold(
      appBar: AqariAppBar(
        title: lt(context, _title),
        onBack: _busy ? null : () => Navigator.maybePop(context),
      ),
      bottomNavigationBar: !_allowed
          ? null
          : AqariActionBar(
              child: Row(
                children: [
                  AqariButton(
                    label: lt(context, 'cancel'),
                    variant: AqariButtonVariant.text,
                    onPressed: _busy ? null : () => Navigator.pop(context),
                  ),
                  const SizedBox(width: AqariSpacing.x3),
                  Expanded(
                    child: AqariButton(
                      label: lt(context, _termination ? 'terminate' : 'save'),
                      expanded: true,
                      loading: _busy,
                      variant: _termination
                          ? AqariButtonVariant.destructive
                          : AqariButtonVariant.primary,
                      onPressed: _busy || _loading ? null : _submit,
                    ),
                  ),
                ],
              ),
            ),
      body: !_allowed
          ? Center(child: Text(lt(context, 'permission')))
          : SafeArea(
              child: ListView(
                controller: _scroll,
                padding: const EdgeInsets.all(AqariSpacing.x4),
                children: [
                  if (_error != null) LeaseFailure(_error),
                  if (_errors.isNotEmpty)
                    Semantics(
                      liveRegion: true,
                      child: Text(
                        leaseError(
                          const ApiProblem(
                            message: '',
                            validationErrors: {
                              'fields': ['invalid'],
                            },
                          ),
                          context.isArabic,
                        ),
                      ),
                    ),
                  if (widget.lease != null)
                    LeaseFact(
                      lt(context, 'contractNumber'),
                      widget.lease!.number,
                      ltr: true,
                    ),
                  if (_termination) ...[
                    Text(lt(context, 'terminateWarning')),
                    AqariFormSection(
                      title: lt(context, 'termination'),
                      children: [
                        _enum(
                          'terminationType',
                          'termination',
                          _form.terminationType,
                          (v) => _form.terminationType = v,
                        ),
                        _date('terminationDate'),
                      ],
                    ),
                    AqariFormSection(
                      title: lt(context, 'settlement'),
                      children: [
                        for (final key in [
                          'outstandingBalance',
                          'depositReturnedAmount',
                          'depositDeductionAmount',
                        ])
                          _text(key, numeric: true),
                        _text('depositDeductionReason', lines: 3),
                        AqariCheckbox(
                          label: lt(context, 'finalUtilitySettlementCompleted'),
                          value: _form.settled,
                          onChanged: _busy
                              ? null
                              : (v) =>
                                    setState(() => _form.settled = v ?? false),
                        ),
                        _text('reason', lines: 3),
                      ],
                    ),
                  ] else ...[
                    if (widget.mode != LeaseFormMode.edit)
                      _text(
                        'contractNumber',
                        helper: widget.mode == LeaseFormMode.create
                            ? lt(context, 'suggestion')
                            : null,
                      ),
                    if (_parties)
                      AqariFormSection(
                        title: lt(context, 'parties'),
                        children: [_partyFields()],
                      ),
                    AqariFormSection(
                      title: lt(context, 'terms'),
                      children: [
                        _date('startDate'),
                        _date('endDate'),
                        _enum(
                          'legalRegime',
                          'regime',
                          _form.regime,
                          (v) => _form.regime = v,
                        ),
                        _enum(
                          'tenantType',
                          'tenantType',
                          _form.tenantType,
                          (v) => _form.tenantType = v,
                        ),
                      ],
                    ),
                    AqariFormSection(
                      title: lt(context, 'financial'),
                      children: [
                        _text(
                          'monthlyRentAmount',
                          numeric: true,
                          helper: _rentSource != null
                              ? lt(context, 'rentDefault')
                              : null,
                        ),
                        _text('securityDepositAmount', numeric: true),
                        _enum(
                          'paymentFrequency',
                          'frequency',
                          _form.frequency,
                          (v) => _form.frequency = v,
                        ),
                        _text('paymentDueDay', numeric: true),
                      ],
                    ),
                  ],
                  AqariFormSection(
                    title: lt(context, 'notes'),
                    children: [_text('notes', lines: 3)],
                  ),
                ],
              ),
            ),
    ),
  );
}
