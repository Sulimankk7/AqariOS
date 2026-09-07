import 'dart:async';

import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../domain/lease_models.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';

class TenantManagementScreen extends StatefulWidget {
  const TenantManagementScreen({required this.scope, super.key});

  final LeaseScope scope;

  @override
  State<TenantManagementScreen> createState() => _TenantManagementScreenState();
}

class _TenantManagementScreenState extends State<TenantManagementScreen> {
  final _search = TextEditingController();
  Timer? _debounce;
  List<TenantRecord> _tenants = const [];
  Object? _error;
  bool _loading = true;
  int _request = 0;

  bool get _canManage => widget.scope.user.hasPermission('contracts.create');

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final request = ++_request;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await widget.scope.repository.searchTenants(
        searchTerm: _search.text.trim(),
      );
      if (mounted && request == _request) setState(() => _tenants = result);
    } catch (error) {
      if (mounted && request == _request) setState(() => _error = error);
    } finally {
      if (mounted && request == _request) setState(() => _loading = false);
    }
  }

  void _searchChanged(String _) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 350), _load);
  }

  Future<void> _edit([TenantRecord? tenant]) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => TenantFormScreen(scope: widget.scope, tenant: tenant),
      ),
    );
    if (changed == true && mounted) await _load();
  }

  Future<void> _open(TenantRecord tenant) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => TenantDetailScreen(scope: widget.scope, id: tenant.id),
      ),
    );
    if (changed == true && mounted) await _load();
  }

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: _load,
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AqariSpacing.x4),
      children: [
        if (_canManage)
          Align(
            alignment: AlignmentDirectional.centerEnd,
            child: AqariButton(
              label: lt(context, 'newTenant'),
              icon: Icons.person_add_alt_1_outlined,
              onPressed: _edit,
            ),
          ),
        const SizedBox(height: AqariSpacing.x3),
        AqariSearchField(
          controller: _search,
          hint: lt(context, 'tenantSearch'),
          onChanged: _searchChanged,
        ),
        const SizedBox(height: AqariSpacing.x3),
        if (_loading)
          ...List.generate(
            3,
            (_) => const Padding(
              padding: EdgeInsets.only(bottom: AqariSpacing.x3),
              child: AqariSkeleton(height: 72),
            ),
          )
        else if (_error != null)
          LeaseFailure(_error, retry: _load)
        else if (_tenants.isEmpty)
          AqariEmptyState(
            title: lt(context, 'tenantEmpty'),
            message: lt(context, 'tenantEmptyHint'),
          )
        else
          Column(
            children: _tenants
                .map(
                  (tenant) => Padding(
                    padding: const EdgeInsets.only(bottom: AqariSpacing.x2),
                    child: AqariListRow(
                      title: tenant.name,
                      supportingText: leaseInline(tenant.phone),
                      metadata:
                          '${lt(context, 'nationalId')}: ${leaseInline(tenant.nationalId)}',
                      leading: AqariAvatar(label: tenant.name),
                      showChevron: true,
                      onPressed: () => _open(tenant),
                    ),
                  ),
                )
                .toList(growable: false),
          ),
      ],
    ),
  );
}

class TenantFormScreen extends StatefulWidget {
  const TenantFormScreen({required this.scope, this.tenant, super.key});

  final LeaseScope scope;
  final TenantRecord? tenant;

  @override
  State<TenantFormScreen> createState() => _TenantFormScreenState();
}

class _TenantFormScreenState extends State<TenantFormScreen> {
  late final _name = TextEditingController(text: widget.tenant?.name);
  late final _nationalId = TextEditingController(
    text: widget.tenant?.nationalId,
  );
  late final _phone = TextEditingController(text: widget.tenant?.phone);
  late final _email = TextEditingController(text: widget.tenant?.email);
  late final _occupation = TextEditingController(
    text: widget.tenant?.occupation,
  );
  late final _employer = TextEditingController(text: widget.tenant?.employer);
  bool _saving = false;
  Object? _error;

  @override
  void dispose() {
    for (final controller in [
      _name,
      _nationalId,
      _phone,
      _email,
      _occupation,
      _employer,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (_name.text.trim().isEmpty ||
        _nationalId.text.trim().isEmpty ||
        _phone.text.trim().isEmpty ||
        (widget.tenant == null && _email.text.trim().isEmpty)) {
      setState(() => _error = lt(context, 'tenantRequired'));
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    final body = <String, dynamic>{
      'name': _name.text.trim(),
      'nationalId': _nationalId.text.trim(),
      'phone': _phone.text.trim(),
      'email': _email.text.trim().isEmpty ? null : _email.text.trim(),
      'occupation': _occupation.text.trim().isEmpty
          ? null
          : _occupation.text.trim(),
      'employer': _employer.text.trim().isEmpty ? null : _employer.text.trim(),
    };
    try {
      if (widget.tenant == null) {
        await widget.scope.repository.createTenant(body);
      } else {
        await widget.scope.repository.updateTenant(widget.tenant!.id, body);
      }
      if (mounted) Navigator.pop(context, true);
    } catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: lt(context, widget.tenant == null ? 'newTenant' : 'editTenant'),
      onBack: () => Navigator.maybePop(context),
    ),
    bottomNavigationBar: AqariActionBar(
      child: AqariButton(
        label: lt(context, 'save'),
        expanded: true,
        loading: _saving,
        onPressed: _saving ? null : _save,
      ),
    ),
    body: SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(AqariSpacing.x4),
        children: [
          AqariFormSection(
            title: lt(context, 'identity'),
            children: [
              AqariTextField(controller: _name, label: lt(context, 'name')),
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: _nationalId,
                label: lt(context, 'nationalId'),
                textDirection: TextDirection.ltr,
              ),
            ],
          ),
          AqariFormSection(
            title: lt(context, 'contact'),
            children: [
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: _phone,
                label: lt(context, 'phone'),
                keyboardType: TextInputType.phone,
                textDirection: TextDirection.ltr,
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: _email,
                label: lt(context, 'email'),
                keyboardType: TextInputType.emailAddress,
                textDirection: TextDirection.ltr,
              ),
            ],
          ),
          AqariFormSection(
            title: lt(context, 'employment'),
            children: [
              AqariTextField(
                controller: _occupation,
                label: lt(context, 'occupation'),
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: _employer,
                label: lt(context, 'employer'),
              ),
            ],
          ),
          if (_error != null) ...[
            const SizedBox(height: AqariSpacing.x3),
            Text('$_error', style: TextStyle(color: context.aqariColors.error)),
          ],
        ],
      ),
    ),
  );
}

class TenantDetailScreen extends StatefulWidget {
  const TenantDetailScreen({required this.scope, required this.id, super.key});

  final LeaseScope scope;
  final String id;

  @override
  State<TenantDetailScreen> createState() => _TenantDetailScreenState();
}

class _TenantDetailScreenState extends State<TenantDetailScreen> {
  TenantRecord? _tenant;
  List<Lease> _leases = const [];
  Object? _error;
  bool _loading = true;
  bool _changed = false;

  bool get _canManage => widget.scope.user.hasPermission('contracts.create');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait<Object>([
        widget.scope.repository.tenantDetail(widget.id),
        widget.scope.repository.tenantLeaseHistory(widget.id),
      ]);
      if (mounted) {
        setState(() {
          _tenant = results[0] as TenantRecord;
          _leases = results[1] as List<Lease>;
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _edit() async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => TenantFormScreen(scope: widget.scope, tenant: _tenant),
      ),
    );
    if (changed == true) {
      _changed = true;
      await _load();
    }
  }

  Future<void> _delete() async {
    final deleted = await leaseConfirm(
      context,
      title: lt(context, 'deleteTenant'),
      message: '${lt(context, 'deleteTenantHint')}\n${_tenant!.name}',
      destructive: true,
      action: () => widget.scope.repository.deleteTenant(widget.id),
    );
    if (deleted && mounted) Navigator.pop(context, true);
  }

  Future<void> _provision() async {
    final email = TextEditingController(text: _tenant!.email);
    final phone = TextEditingController(text: _tenant!.phone);
    var method = 'Email';
    String? error;
    final result = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(lt(context, 'provisionTenant')),
          scrollable: true,
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              AqariSelect<String>(
                label: lt(context, 'contactMethod'),
                valueLabel: lt(context, method == 'Email' ? 'email' : 'phone'),
                items: const ['Email', 'Phone'],
                itemLabel: (value) =>
                    lt(context, value == 'Email' ? 'email' : 'phone'),
                onChanged: (value) => setDialogState(() => method = value),
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: method == 'Email' ? email : phone,
                label: lt(context, method == 'Email' ? 'email' : 'phone'),
                keyboardType: method == 'Email'
                    ? TextInputType.emailAddress
                    : TextInputType.phone,
                textDirection: TextDirection.ltr,
              ),
              if (error != null)
                Text(
                  error!,
                  style: TextStyle(color: context.aqariColors.error),
                ),
            ],
          ),
          actions: [
            AqariButton(
              label: lt(context, 'cancel'),
              variant: AqariButtonVariant.text,
              onPressed: () => Navigator.pop(dialogContext, false),
            ),
            AqariButton(
              label: lt(context, 'provision'),
              onPressed: () async {
                final value = method == 'Email'
                    ? email.text.trim()
                    : phone.text.trim();
                if (value.isEmpty) {
                  setDialogState(() => error = lt(context, 'contactRequired'));
                  return;
                }
                try {
                  await widget.scope.repository
                      .provisionTenantAccount(widget.id, {
                        'contactMethod': method,
                        if (method == 'Email') 'email': value,
                        if (method == 'Phone') 'phone': value,
                      });
                  if (dialogContext.mounted) {
                    Navigator.pop(dialogContext, true);
                  }
                } catch (failure) {
                  setDialogState(() => error = '$failure');
                }
              },
            ),
          ],
        ),
      ),
    );
    email.dispose();
    phone.dispose();
    if (result == true && mounted) {
      _changed = true;
      AqariSnackbar.show(context, lt(context, 'accountCreated'));
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: false,
    onPopInvokedWithResult: (didPop, _) {
      if (!didPop) Navigator.pop(context, _changed);
    },
    child: Scaffold(
      appBar: AqariAppBar(
        title: lt(context, 'tenantDetails'),
        onBack: () => Navigator.pop(context, _changed),
      ),
      body: SafeArea(
        child: _loading
            ? const LeaseSkeleton()
            : _error != null
            ? LeaseFailure(_error, retry: _load)
            : _content(context, _tenant!),
      ),
    ),
  );

  Widget _content(
    BuildContext context,
    TenantRecord tenant,
  ) => RefreshIndicator(
    onRefresh: _load,
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AqariSpacing.x4),
      children: [
        if (_canManage)
          Wrap(
            spacing: AqariSpacing.x2,
            runSpacing: AqariSpacing.x2,
            children: [
              AqariButton(
                label: 'تعديل',
                icon: Icons.edit_outlined,
                onPressed: _edit,
              ),
              if (tenant.userId == null)
                AqariButton(
                  label: 'إنشاء حساب',
                  icon: Icons.person_add_outlined,
                  variant: AqariButtonVariant.tonal,
                  onPressed: _provision,
                ),
              AqariButton(
                label: 'حذف',
                icon: Icons.delete_outline,
                variant: AqariButtonVariant.destructive,
                onPressed: _delete,
              ),
            ],
          ),
        const SizedBox(height: AqariSpacing.x4),
        const AqariSectionHeader(title: 'البيانات الأساسية'),
        AqariCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              LeaseFact('الاسم', tenant.name),
              LeaseFact('الرقم الوطني', tenant.nationalId, ltr: true),
              LeaseFact('الهاتف', tenant.phone, ltr: true),
              if (tenant.email != null)
                LeaseFact('البريد', tenant.email!, ltr: true),
              if (tenant.occupation != null)
                LeaseFact('المهنة', tenant.occupation!),
              if (tenant.employer != null)
                LeaseFact('جهة العمل', tenant.employer!),
            ],
          ),
        ),
        _relatedSection(
          'أفراد الأسرة',
          tenant.familyMembers.map(
            (v) => _TenantRelatedRow(
              id: v.id,
              title: v.name,
              subtitle:
                  '${v.relationshipType}${v.ageBracket == null ? '' : ' · ${v.ageBracket}'}',
              values: [v.name, v.relationshipType, v.ageBracket ?? ''],
            ),
          ),
          collection: 'family-members',
          labels: const ['الاسم', 'صلة القرابة', 'الفئة العمرية (اختياري)'],
          keys: const ['name', 'relationshipType', 'ageBracket'],
          optionalLast: true,
        ),
        _relatedSection(
          'جهات اتصال الطوارئ',
          tenant.emergencyContacts.map(
            (v) => _TenantRelatedRow(
              id: v.id,
              title: v.name,
              subtitle: '${v.relationshipType} · ${v.phone}',
              values: [v.name, v.relationshipType, v.phone],
            ),
          ),
          collection: 'emergency-contacts',
          labels: const ['الاسم', 'صلة القرابة', 'رقم الهاتف'],
          keys: const ['name', 'relationshipType', 'phone'],
        ),
        _relatedSection(
          'المركبات',
          tenant.vehicles.map(
            (v) => _TenantRelatedRow(
              id: v.id,
              title: v.plateNumber,
              subtitle: '${v.makeModel} · ${v.color}',
              values: [v.plateNumber, v.makeModel, v.color],
            ),
          ),
          collection: 'vehicles',
          labels: const ['رقم اللوحة', 'الصنع والموديل', 'اللون'],
          keys: const ['plateNumber', 'makeModel', 'color'],
        ),
        const SizedBox(height: AqariSpacing.x4),
        const AqariSectionHeader(title: 'سجل عقود الإيجار'),
        if (_leases.isEmpty)
          const AqariEmptyState(
            title: 'لا توجد عقود',
            message: 'لا يوجد سجل عقود لهذا المستأجر.',
          )
        else
          AqariCard(
            padding: EdgeInsets.zero,
            child: Column(
              children: _leases
                  .map(
                    (lease) => AqariListRow(
                      title: lease.number,
                      supportingText: '${lease.start} — ${lease.end}',
                      status: LeaseBadge(lease.status),
                    ),
                  )
                  .toList(growable: false),
            ),
          ),
      ],
    ),
  );

  Widget _relatedSection(
    String title,
    Iterable<_TenantRelatedRow> values, {
    required String collection,
    required List<String> labels,
    required List<String> keys,
    bool optionalLast = false,
  }) {
    final rows = values.toList(growable: false);
    return Padding(
      padding: const EdgeInsets.only(top: AqariSpacing.x4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(child: AqariSectionHeader(title: title)),
              if (_canManage)
                AqariButton(
                  label: 'إضافة',
                  variant: AqariButtonVariant.text,
                  icon: Icons.add,
                  onPressed: () => _editRelated(
                    title: title,
                    collection: collection,
                    labels: labels,
                    keys: keys,
                    optionalLast: optionalLast,
                  ),
                ),
            ],
          ),
          if (rows.isEmpty)
            const Text('لا توجد بيانات مسجلة.')
          else
            AqariCard(
              padding: EdgeInsets.zero,
              child: Column(
                children: rows
                    .map(
                      (row) => AqariListRow(
                        title: row.title,
                        supportingText: row.subtitle,
                        trailing: !_canManage
                            ? null
                            : PopupMenuButton<String>(
                                onSelected: (action) {
                                  if (action == 'edit') {
                                    _editRelated(
                                      title: title,
                                      collection: collection,
                                      labels: labels,
                                      keys: keys,
                                      optionalLast: optionalLast,
                                      row: row,
                                    );
                                  } else {
                                    _deleteRelated(title, collection, row);
                                  }
                                },
                                itemBuilder: (_) => const [
                                  PopupMenuItem(
                                    value: 'edit',
                                    child: Text('تعديل'),
                                  ),
                                  PopupMenuItem(
                                    value: 'delete',
                                    child: Text('حذف'),
                                  ),
                                ],
                              ),
                      ),
                    )
                    .toList(growable: false),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _editRelated({
    required String title,
    required String collection,
    required List<String> labels,
    required List<String> keys,
    required bool optionalLast,
    _TenantRelatedRow? row,
  }) async {
    final controllers = List.generate(
      labels.length,
      (index) =>
          TextEditingController(text: row == null ? '' : row.values[index]),
    );
    String? validation;
    final saved = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text('${row == null ? 'إضافة' : 'تعديل'} $title'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                for (var index = 0; index < labels.length; index++) ...[
                  AqariTextField(
                    controller: controllers[index],
                    label: labels[index],
                    textDirection: keys[index] == 'phone'
                        ? TextDirection.ltr
                        : null,
                  ),
                  if (index + 1 < labels.length)
                    const SizedBox(height: AqariSpacing.x2),
                ],
                if (validation != null)
                  Text(
                    validation!,
                    style: TextStyle(color: context.aqariColors.error),
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('إلغاء'),
            ),
            FilledButton(
              onPressed: () async {
                final requiredValues = optionalLast
                    ? controllers.take(controllers.length - 1)
                    : controllers;
                if (requiredValues.any((value) => value.text.trim().isEmpty)) {
                  setDialogState(() => validation = 'أكمل الحقول المطلوبة.');
                  return;
                }
                final body = <String, dynamic>{
                  for (var index = 0; index < keys.length; index++)
                    keys[index]: controllers[index].text.trim().isEmpty
                        ? null
                        : controllers[index].text.trim(),
                };
                try {
                  if (row == null) {
                    await widget.scope.repository.createTenantChild(
                      widget.id,
                      collection,
                      body,
                    );
                  } else {
                    await widget.scope.repository.updateTenantChild(
                      widget.id,
                      collection,
                      row.id,
                      body,
                    );
                  }
                  if (dialogContext.mounted) {
                    Navigator.pop(dialogContext, true);
                  }
                } catch (error) {
                  setDialogState(() => validation = '$error');
                }
              },
              child: const Text('حفظ'),
            ),
          ],
        ),
      ),
    );
    for (final controller in controllers) {
      controller.dispose();
    }
    if (saved == true && mounted) {
      _changed = true;
      await _load();
    }
  }

  Future<void> _deleteRelated(
    String title,
    String collection,
    _TenantRelatedRow row,
  ) async {
    final deleted = await leaseConfirm(
      context,
      title: 'حذف من $title',
      message: row.title,
      destructive: true,
      action: () => widget.scope.repository.deleteTenantChild(
        widget.id,
        collection,
        row.id,
      ),
    );
    if (deleted && mounted) {
      _changed = true;
      await _load();
    }
  }
}

class _TenantRelatedRow {
  const _TenantRelatedRow({
    required this.id,
    required this.title,
    required this.subtitle,
    required this.values,
  });

  final String id, title, subtitle;
  final List<String> values;
}
