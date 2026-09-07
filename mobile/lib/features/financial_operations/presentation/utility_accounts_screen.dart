import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../leasing/domain/lease_models.dart';
import '../../leasing/presentation/lease_scope.dart';
import '../../leasing/presentation/lease_widgets.dart';

class UtilityAccountsScreen extends StatefulWidget {
  const UtilityAccountsScreen({required this.scope, super.key});

  final LeaseScope scope;

  @override
  State<UtilityAccountsScreen> createState() => _UtilityAccountsScreenState();
}

class _UtilityAccountsScreenState extends State<UtilityAccountsScreen> {
  UtilityPage? _page;
  Object? _error;
  bool _loading = true;
  bool _loadingMore = false;
  int _type = -1;
  int _active = 1;
  int _syncStatus = -1;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({bool more = false}) async {
    if (more && (_page?.nextCursor == null || _loadingMore)) return;
    setState(() {
      if (more) {
        _loadingMore = true;
      } else {
        _loading = true;
        _error = null;
      }
    });
    try {
      final result = await widget.scope.repository.managementUtilityAccounts(
        cursor: more ? _page?.nextCursor : null,
        utilityType: _type < 0 ? null : _type,
        syncStatus: _syncStatus < 0 ? null : _syncStatus,
        isActive: _active < 0 ? null : _active == 1,
      );
      if (mounted) {
        setState(() {
          _page = more
              ? UtilityPage.fromJson({
                  'items': [
                    ..._page!.items.map((value) => value.raw),
                    ...result.items.map((value) => value.raw),
                  ],
                  'nextCursor': result.nextCursor,
                  'hasMore': result.hasMore,
                })
              : result;
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) {
        setState(() {
          _loading = false;
          _loadingMore = false;
        });
      }
    }
  }

  Future<void> _link() async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => UtilityAccountFormScreen(scope: widget.scope),
      ),
    );
    if (changed == true && mounted) await _load();
  }

  Future<void> _open(UtilityAccount account) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) =>
            UtilityAccountDetailScreen(scope: widget.scope, id: account.id),
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
        Align(
          alignment: AlignmentDirectional.centerEnd,
          child: AqariButton(
            label: 'ربط حساب',
            icon: Icons.add_link_rounded,
            onPressed: _link,
          ),
        ),
        const SizedBox(height: AqariSpacing.x3),
        Row(
          children: [
            Expanded(
              child: AqariSelect<int>(
                label: 'نوع الخدمة',
                valueLabel: _type < 0
                    ? 'الكل'
                    : _type == 0
                    ? 'كهرباء'
                    : 'مياه',
                items: const [-1, 0, 1],
                itemLabel: (value) => value < 0
                    ? 'الكل'
                    : value == 0
                    ? 'كهرباء'
                    : 'مياه',
                onChanged: (value) {
                  setState(() => _type = value);
                  _load();
                },
              ),
            ),
            const SizedBox(width: AqariSpacing.x2),
            Expanded(
              child: AqariSelect<int>(
                label: 'الحالة',
                valueLabel: _active < 0
                    ? 'الكل'
                    : _active == 1
                    ? 'نشط'
                    : 'غير نشط',
                items: const [-1, 1, 0],
                itemLabel: (value) => value < 0
                    ? 'الكل'
                    : value == 1
                    ? 'نشط'
                    : 'غير نشط',
                onChanged: (value) {
                  setState(() => _active = value);
                  _load();
                },
              ),
            ),
          ],
        ),
        const SizedBox(height: AqariSpacing.x2),
        AqariSelect<int>(
          label: 'حالة المزامنة',
          valueLabel: _syncStatusLabel(_syncStatus),
          items: const [-1, 0, 1, 2, 3, 4, 5, 6, 7],
          itemLabel: _syncStatusLabel,
          onChanged: (value) {
            setState(() => _syncStatus = value);
            _load();
          },
        ),
        const SizedBox(height: AqariSpacing.x3),
        if (_loading)
          ...List.generate(
            3,
            (_) => const Padding(
              padding: EdgeInsets.only(bottom: AqariSpacing.x3),
              child: AqariSkeleton(height: 80),
            ),
          )
        else if (_error != null)
          LeaseFailure(_error, retry: _load)
        else if (_page == null || _page!.items.isEmpty)
          const AqariEmptyState(
            title: 'لا توجد حسابات خدمات',
            message: 'لا توجد حسابات مطابقة لعوامل التصفية.',
          )
        else
          AqariCard(
            padding: EdgeInsets.zero,
            child: Column(
              children: _page!.items
                  .map(
                    (account) => AqariListRow(
                      title: account.number,
                      supportingText:
                          [
                                account.buildingName,
                                account.unitNumber,
                                account.tenantName,
                              ]
                              .whereType<String>()
                              .where((v) => v.isNotEmpty)
                              .join(' · '),
                      metadata: account.leaseNumber,
                      leading: Icon(
                        account.type == 0
                            ? Icons.electric_bolt_outlined
                            : Icons.water_drop_outlined,
                      ),
                      status: AqariStatusBadge(
                        label: account.active ? 'نشط' : 'غير نشط',
                        variant: account.active
                            ? AqariStatusVariant.brand
                            : AqariStatusVariant.neutral,
                      ),
                      showChevron: true,
                      onPressed: () => _open(account),
                    ),
                  )
                  .toList(growable: false),
            ),
          ),
        if (!_loading && _error == null && (_page?.hasMore ?? false))
          Padding(
            padding: const EdgeInsets.only(top: AqariSpacing.x3),
            child: AqariButton(
              label: 'تحميل المزيد',
              variant: AqariButtonVariant.outlined,
              expanded: true,
              loading: _loadingMore,
              onPressed: _loadingMore ? null : () => _load(more: true),
            ),
          ),
      ],
    ),
  );

  String _syncStatusLabel(int value) => switch (value) {
    0 => 'لم تتم المزامنة',
    1 => 'قيد المزامنة',
    2 => 'تمت المزامنة',
    3 => 'خطأ المزوّد',
    4 => 'تحديد المعدل',
    5 => 'انتهاء المهلة',
    6 => 'معلّق',
    7 => 'حساب غير صالح',
    _ => 'الكل',
  };
}

class UtilityAccountFormScreen extends StatefulWidget {
  const UtilityAccountFormScreen({
    required this.scope,
    this.account,
    super.key,
  });

  final LeaseScope scope;
  final UtilityAccount? account;

  @override
  State<UtilityAccountFormScreen> createState() =>
      _UtilityAccountFormScreenState();
}

class _UtilityAccountFormScreenState extends State<UtilityAccountFormScreen> {
  late final _number = TextEditingController(text: widget.account?.number);
  late final _meter = TextEditingController(text: widget.account?.meter);
  List<Lease> _leases = const [];
  Lease? _lease;
  int _type = 0;
  bool _loading = true;
  bool _saving = false;
  Object? _error;

  bool get _replacement => widget.account != null;

  @override
  void initState() {
    super.initState();
    if (_replacement) {
      _loading = false;
    } else {
      _loadLeases();
    }
  }

  @override
  void dispose() {
    _number.dispose();
    _meter.dispose();
    super.dispose();
  }

  Future<void> _loadLeases() async {
    try {
      final leases = await widget.scope.repository.contracts(pageSize: 100);
      if (mounted) {
        setState(() {
          _leases = leases.where((lease) => lease.status == 2).toList();
          if (_leases.isNotEmpty) _lease = _leases.first;
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _save() async {
    if (_number.text.trim().isEmpty || (!_replacement && _lease == null)) {
      setState(() => _error = 'رقم الحساب وعقد الإيجار مطلوبان.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    final body = <String, dynamic>{
      'accountNumber': _number.text.trim(),
      'meterNumber': _meter.text.trim().isEmpty ? null : _meter.text.trim(),
      if (!_replacement) 'leaseContractId': _lease!.id,
      if (!_replacement) 'utilityType': _type,
    };
    try {
      if (_replacement) {
        await widget.scope.repository.replaceUtility(widget.account!.id, body);
      } else {
        await widget.scope.repository.linkUtility(body);
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
      title: _replacement ? 'استبدال حساب الخدمة' : 'ربط حساب خدمة',
      onBack: () => Navigator.maybePop(context),
    ),
    body: SafeArea(
      child: _loading
          ? const LeaseSkeleton()
          : ListView(
              padding: const EdgeInsets.all(AqariSpacing.x4),
              children: [
                if (!_replacement) ...[
                  AqariSelect<Lease>(
                    label: 'عقد الإيجار النشط',
                    valueLabel: _lease?.number ?? 'لا توجد عقود نشطة',
                    items: _leases,
                    itemLabel: (lease) => lease.number,
                    onChanged: (value) => setState(() => _lease = value),
                    enabled: _leases.isNotEmpty,
                  ),
                  const SizedBox(height: AqariSpacing.x3),
                  AqariSelect<int>(
                    label: 'نوع الخدمة',
                    valueLabel: _type == 0 ? 'كهرباء' : 'مياه',
                    items: const [0, 1],
                    itemLabel: (value) => value == 0 ? 'كهرباء' : 'مياه',
                    onChanged: (value) => setState(() => _type = value),
                  ),
                  const SizedBox(height: AqariSpacing.x3),
                ],
                AqariTextField(
                  controller: _number,
                  label: 'رقم الحساب',
                  textDirection: TextDirection.ltr,
                ),
                const SizedBox(height: AqariSpacing.x3),
                AqariTextField(
                  controller: _meter,
                  label: 'رقم العداد (اختياري)',
                  textDirection: TextDirection.ltr,
                ),
                if (_error != null) ...[
                  const SizedBox(height: AqariSpacing.x3),
                  Text(
                    '$_error',
                    style: TextStyle(color: context.aqariColors.error),
                  ),
                ],
                const SizedBox(height: AqariSpacing.x5),
                AqariButton(
                  label: _replacement ? 'استبدال' : 'ربط',
                  expanded: true,
                  loading: _saving,
                  onPressed: _saving ? null : _save,
                ),
              ],
            ),
    ),
  );
}

class UtilityAccountDetailScreen extends StatefulWidget {
  const UtilityAccountDetailScreen({
    required this.scope,
    required this.id,
    super.key,
  });

  final LeaseScope scope;
  final String id;

  @override
  State<UtilityAccountDetailScreen> createState() =>
      _UtilityAccountDetailScreenState();
}

class _UtilityAccountDetailScreenState
    extends State<UtilityAccountDetailScreen> {
  UtilityAccount? _account;
  List<UtilityBill> _bills = const [];
  Object? _error;
  bool _loading = true;
  bool _changed = false;
  int _billStatus = -1;

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
        widget.scope.repository.utilityAccountDetail(widget.id),
        widget.scope.repository.utilityBills(
          widget.id,
          paymentStatus: _billStatus < 0 ? null : _billStatus,
        ),
      ]);
      if (mounted) {
        setState(() {
          _account = results[0] as UtilityAccount;
          _bills = (results[1] as UtilityBillPage).items;
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _replace() async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) =>
            UtilityAccountFormScreen(scope: widget.scope, account: _account),
      ),
    );
    if (changed == true) {
      _changed = true;
      await _load();
    }
  }

  Future<void> _sync() async {
    try {
      await widget.scope.repository.syncUtility(widget.id);
      if (mounted) AqariSnackbar.show(context, 'تم طلب مزامنة الحساب.');
      await _load();
    } catch (error) {
      if (mounted) AqariSnackbar.show(context, '$error');
    }
  }

  Future<void> _unlink() async {
    final changed = await leaseConfirm(
      context,
      title: 'إلغاء ربط الحساب',
      message: 'سيبقى سجل الحساب والفواتير محفوظاً بعد إلغاء الربط.',
      destructive: true,
      action: () => widget.scope.repository.unlinkUtility(widget.id),
    );
    if (changed && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: false,
    onPopInvokedWithResult: (didPop, _) {
      if (!didPop) Navigator.pop(context, _changed);
    },
    child: Scaffold(
      appBar: AqariAppBar(
        title: 'تفاصيل حساب الخدمة',
        onBack: () => Navigator.pop(context, _changed),
      ),
      body: SafeArea(
        child: _loading
            ? const LeaseSkeleton()
            : _error != null
            ? LeaseFailure(_error, retry: _load)
            : _content(context, _account!),
      ),
    ),
  );

  Widget _content(
    BuildContext context,
    UtilityAccount account,
  ) => RefreshIndicator(
    onRefresh: _load,
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AqariSpacing.x4),
      children: [
        if (account.active)
          Wrap(
            spacing: AqariSpacing.x2,
            runSpacing: AqariSpacing.x2,
            children: [
              AqariButton(label: 'مزامنة', icon: Icons.sync, onPressed: _sync),
              AqariButton(
                label: 'استبدال',
                icon: Icons.swap_horiz,
                variant: AqariButtonVariant.tonal,
                onPressed: _replace,
              ),
              AqariButton(
                label: 'إلغاء الربط',
                icon: Icons.link_off,
                variant: AqariButtonVariant.destructive,
                onPressed: _unlink,
              ),
            ],
          ),
        const SizedBox(height: AqariSpacing.x4),
        const AqariSectionHeader(title: 'الحساب'),
        AqariCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              LeaseFact('نوع الخدمة', account.type == 0 ? 'كهرباء' : 'مياه'),
              LeaseFact('رقم الحساب', account.number, ltr: true),
              if (account.meter != null)
                LeaseFact('رقم العداد', account.meter!, ltr: true),
              if (account.buildingName != null)
                LeaseFact('المبنى', account.buildingName!),
              if (account.unitNumber != null)
                LeaseFact('الوحدة', account.unitNumber!),
              if (account.tenantName != null)
                LeaseFact('المستأجر', account.tenantName!),
              if (account.leaseNumber != null)
                LeaseFact('عقد الإيجار', account.leaseNumber!),
              LeaseFact('الحالة', account.active ? 'نشط' : 'غير نشط'),
            ],
          ),
        ),
        const SizedBox(height: AqariSpacing.x4),
        const AqariSectionHeader(title: 'سجل الفواتير'),
        AqariSelect<int>(
          label: 'حالة الدفع',
          valueLabel: switch (_billStatus) {
            0 => 'غير مدفوعة',
            1 => 'مدفوعة',
            2 => 'غير معروفة',
            _ => 'الكل',
          },
          items: const [-1, 0, 1, 2],
          itemLabel: (value) => switch (value) {
            0 => 'غير مدفوعة',
            1 => 'مدفوعة',
            2 => 'غير معروفة',
            _ => 'الكل',
          },
          onChanged: (value) {
            setState(() => _billStatus = value);
            _load();
          },
        ),
        const SizedBox(height: AqariSpacing.x3),
        if (_bills.isEmpty)
          const AqariEmptyState(
            title: 'لا توجد فواتير',
            message: 'لم يتم اكتشاف فواتير لهذا الحساب بعد.',
          )
        else
          AqariCard(
            padding: EdgeInsets.zero,
            child: Column(
              children: _bills
                  .map(
                    (bill) => AqariListRow(
                      title: '${bill.amount} ${bill.currency}',
                      supportingText: 'تاريخ الفاتورة: ${bill.billDate}',
                      metadata: bill.dueDate == null
                          ? null
                          : 'الاستحقاق: ${bill.dueDate}',
                      status: AqariStatusBadge(
                        label: bill.paid ? 'مدفوعة' : 'غير مدفوعة',
                        variant: bill.paid
                            ? AqariStatusVariant.brand
                            : AqariStatusVariant.warning,
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
