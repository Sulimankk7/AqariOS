import 'package:flutter/material.dart';
import 'dart:ui' as ui;
import 'package:intl/intl.dart';

import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import 'tenant_utility_controller.dart';
import 'tenant_utility_models.dart';

class TenantUtilitiesScreen extends StatefulWidget {
  const TenantUtilitiesScreen({required this.controller, super.key});
  final TenantUtilityController controller;

  @override
  State<TenantUtilitiesScreen> createState() => _TenantUtilitiesScreenState();
}

class _TenantUtilitiesScreenState extends State<TenantUtilitiesScreen> {
  @override
  void initState() {
    super.initState();
    widget.controller.load();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.controller,
    builder: (context, _) {
      final c = widget.controller;
      if (c.loading && c.accounts.isEmpty && c.bills.isEmpty) {
        return AqariLoadingState(
          label: _t(
            context,
            'جارٍ تحميل فواتير الخدمات',
            'Loading utility bills',
          ),
        );
      }
      return RefreshIndicator(
        onRefresh: () => c.load(force: true),
        child: ListView(
          key: const PageStorageKey('tenant-utilities'),
          padding: const EdgeInsets.all(AqariSpacing.page),
          children: [
            AqariTabs(
              labels: [
                _t(context, 'الكل', 'All'),
                _t(context, 'الكهرباء', 'Electricity'),
                _t(context, 'المياه', 'Water'),
              ],
              selectedIndex: c.filter == null
                  ? 0
                  : c.filter == TenantUtilityType.electricity
                  ? 1
                  : 2,
              onSelected: (index) => c.changeFilter(
                index == 0
                    ? null
                    : index == 1
                    ? TenantUtilityType.electricity
                    : TenantUtilityType.water,
              ),
            ),
            const SizedBox(height: AqariSpacing.x3),
            if (c.accountsError != null)
              AqariErrorState(
                title: _t(
                  context,
                  'تعذر تحميل حسابات الخدمات',
                  'Could not load utility accounts',
                ),
                message: _error(context, c.accountsError!),
                retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
                onRetry: () => c.load(force: true),
              )
            else
              for (final type in TenantUtilityType.values.where(
                (type) => c.filter == null || c.filter == type,
              )) ...[
                _accountCard(
                  type,
                  c.accounts
                      .where((item) => item.isCurrent && item.type == type)
                      .firstOrNull,
                ),
                const SizedBox(height: AqariSpacing.x3),
              ],
            const SizedBox(height: AqariSpacing.x2),
            AqariSectionHeader(
              title: _t(context, 'سجل الفواتير', 'Bill history'),
            ),
            if (c.billsError != null)
              AqariErrorState(
                title: _t(
                  context,
                  'تعذر تحميل سجل الفواتير',
                  'Could not load bill history',
                ),
                message: _error(context, c.billsError!),
                retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
                onRetry: () => c.load(force: true),
              )
            else if (c.bills.isEmpty)
              AqariEmptyState(
                title: _t(context, 'لا توجد فواتير', 'No bills'),
                message: _t(
                  context,
                  'ستظهر الفواتير المكتشفة هنا، بما فيها سجلات الحسابات السابقة.',
                  'Discovered bills, including previous-account history, will appear here.',
                ),
              )
            else
              ..._billGroups(c).map(
                (entry) => Padding(
                  padding: const EdgeInsets.only(bottom: AqariSpacing.x3),
                  child: _billGroup(entry.key, entry.value),
                ),
              ),
            if (c.hasMore)
              Align(
                child: AqariButton(
                  label: _t(context, 'تحميل المزيد', 'Load more'),
                  variant: AqariButtonVariant.outlined,
                  loading: c.loadingMore,
                  onPressed: c.loadMore,
                ),
              ),
            const SizedBox(height: AqariSpacing.x8),
          ],
        ),
      );
    },
  );

  Widget _accountCard(TenantUtilityType type, TenantUtilityAccount? account) {
    final c = widget.controller;
    final electricity = type == TenantUtilityType.electricity;
    return AqariCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(
                electricity ? Icons.bolt_outlined : Icons.water_drop_outlined,
                color: electricity
                    ? context.aqariColors.warning
                    : context.aqariColors.info,
              ),
              const SizedBox(width: AqariSpacing.x2),
              Expanded(
                child: Text(
                  _type(context, type),
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              if (account != null) _syncBadge(context, account.syncStatus),
            ],
          ),
          const SizedBox(height: AqariSpacing.x4),
          if (account == null) ...[
            Text(
              _t(
                context,
                'لا يوجد حساب ${electricity ? 'كهرباء' : 'مياه'} مرتبط.',
                'No ${electricity ? 'electricity' : 'water'} account is linked.',
              ),
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                color: context.aqariColors.textSecondary,
              ),
            ),
            const SizedBox(height: AqariSpacing.x3),
            AqariButton(
              label: _t(context, 'ربط حساب', 'Link account'),
              icon: Icons.add_link_rounded,
              expanded: true,
              onPressed: c.mutating ? null : () => _showAccountForm(type),
            ),
          ] else ...[
            AqariDetailRow(
              label: _t(context, 'رقم الحساب', 'Account number'),
              value: account.accountNumber,
              ltr: true,
            ),
            AqariDetailRow(
              label: _t(context, 'رقم العداد', 'Meter number'),
              value: account.meterNumber?.isNotEmpty == true
                  ? account.meterNumber!
                  : '—',
              ltr: true,
            ),
            AqariDetailRow(
              label: _t(context, 'الرصيد المستحق', 'Outstanding balance'),
              value: account.totalOutstandingBalance == null
                  ? '—'
                  : _money(
                      context,
                      account.totalOutstandingBalance!,
                      account.latestBill?.currency ?? 'JOD',
                    ),
            ),
            AqariDetailRow(
              label: _t(context, 'آخر فاتورة', 'Latest bill'),
              value: account.latestBill == null
                  ? '—'
                  : '${_money(context, account.latestBill!.amount, account.latestBill!.currency)} • ${_date(context, account.latestBill!.billDate)}',
            ),
            AqariDetailRow(
              label: _t(context, 'آخر مزامنة ناجحة', 'Last successful sync'),
              value: account.lastSuccessfulSyncAt == null
                  ? '—'
                  : _dateTime(context, account.lastSuccessfulSyncAt!),
            ),
            if (c.isVerifying(account.id))
              Padding(
                padding: const EdgeInsets.only(top: AqariSpacing.x2),
                child: Text(
                  c.verificationTimedOut(account.id)
                      ? _t(
                          context,
                          'انتهت مهلة التحقق. يمكنك إعادة المحاولة.',
                          'Verification timed out. You can retry.',
                        )
                      : _t(
                          context,
                          'جارٍ التحقق من المزامنة…',
                          'Verifying synchronization…',
                        ),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: c.verificationTimedOut(account.id)
                        ? context.aqariColors.warning
                        : context.aqariColors.info,
                  ),
                ),
              )
            else if (account.syncStatus != 'Synced')
              Padding(
                padding: const EdgeInsets.only(top: AqariSpacing.x2),
                child: Text(
                  _syncMessage(context, account.syncStatus),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: context.aqariColors.textSecondary,
                  ),
                ),
              ),
            const SizedBox(height: AqariSpacing.x3),
            Wrap(
              spacing: AqariSpacing.x2,
              runSpacing: AqariSpacing.x2,
              children: [
                AqariButton(
                  label: _t(context, 'استبدال', 'Replace'),
                  variant: AqariButtonVariant.outlined,
                  icon: Icons.edit_outlined,
                  onPressed: c.mutating
                      ? null
                      : () => _showAccountForm(type, account: account),
                ),
                AqariButton(
                  label: _syncAction(context, account.syncStatus),
                  variant: AqariButtonVariant.outlined,
                  icon: Icons.sync_rounded,
                  loading:
                      c.isVerifying(account.id) &&
                      !c.verificationTimedOut(account.id),
                  onPressed: c.mutating || account.syncStatus == 'Syncing'
                      ? null
                      : () => _run(
                          () => c.sync(account),
                          _t(
                            context,
                            'تم طلب المزامنة.',
                            'Synchronization requested.',
                          ),
                        ),
                ),
                AqariButton(
                  label: _t(context, 'إلغاء الربط', 'Unlink'),
                  variant: AqariButtonVariant.destructive,
                  icon: Icons.link_off_rounded,
                  onPressed: c.mutating || account.syncStatus == 'Syncing'
                      ? null
                      : () => _confirmUnlink(account),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _showAccountForm(
    TenantUtilityType type, {
    TenantUtilityAccount? account,
  }) async {
    final number = TextEditingController(text: account?.accountNumber ?? '');
    final meter = TextEditingController(text: account?.meterNumber ?? '');
    String? validation;
    var submitting = false;
    await AqariBottomSheet.show<void>(
      context: context,
      title: account == null
          ? _t(context, 'ربط حساب خدمة', 'Link utility account')
          : _t(context, 'استبدال حساب الخدمة', 'Replace utility account'),
      child: StatefulBuilder(
        builder: (sheetContext, setSheetState) => SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              AqariSelect<TenantUtilityType>(
                label: _t(context, 'نوع الخدمة', 'Utility type'),
                valueLabel: _type(context, type),
                items: TenantUtilityType.values,
                itemLabel: (value) => _type(context, value),
                enabled: false,
                onChanged: (_) {},
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: number,
                label: _t(context, 'رقم الحساب', 'Account number'),
                helperText: type == TenantUtilityType.electricity
                    ? _t(context, '10 أرقام بالضبط', 'Exactly 10 digits')
                    : _t(context, 'من 1 إلى 20 رقماً', '1 to 20 digits'),
                errorText: validation,
                enabled: !submitting,
                keyboardType: TextInputType.number,
                textDirection: ui.TextDirection.ltr,
              ),
              const SizedBox(height: AqariSpacing.x3),
              AqariTextField(
                controller: meter,
                label: _t(
                  context,
                  'رقم العداد (اختياري)',
                  'Meter number (optional)',
                ),
                enabled: !submitting,
                textDirection: ui.TextDirection.ltr,
              ),
              if (account != null) ...[
                const SizedBox(height: AqariSpacing.x3),
                Text(
                  _t(
                    context,
                    'سيبقى سجل فواتير الحساب السابق محفوظاً.',
                    'The previous account bill history will remain preserved.',
                  ),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: context.aqariColors.info,
                  ),
                ),
              ],
              const SizedBox(height: AqariSpacing.x4),
              AqariButton(
                label: account == null
                    ? _t(context, 'ربط الحساب', 'Link account')
                    : _t(context, 'استبدال الحساب', 'Replace account'),
                expanded: true,
                loading: submitting,
                onPressed: submitting
                    ? null
                    : () async {
                        final digits = number.text.trim();
                        final valid = RegExp(
                          type == TenantUtilityType.electricity
                              ? r'^\d{10}$'
                              : r'^\d{1,20}$',
                        ).hasMatch(digits);
                        if (!valid || meter.text.trim().length > 100) {
                          setSheetState(
                            () => validation = !valid
                                ? (type == TenantUtilityType.electricity
                                      ? _t(
                                          context,
                                          'يجب أن يتكون رقم حساب الكهرباء من 10 أرقام.',
                                          'Electricity account must contain exactly 10 digits.',
                                        )
                                      : _t(
                                          context,
                                          'يجب أن يتكون رقم حساب المياه من 1 إلى 20 رقماً.',
                                          'Water account must contain 1 to 20 digits.',
                                        ))
                                : _t(
                                    context,
                                    'رقم العداد لا يتجاوز 100 حرف.',
                                    'Meter number must not exceed 100 characters.',
                                  ),
                          );
                          return;
                        }
                        setSheetState(() {
                          validation = null;
                          submitting = true;
                        });
                        try {
                          final meterValue = meter.text.trim().isEmpty
                              ? null
                              : meter.text.trim();
                          if (account == null) {
                            await widget.controller.link(
                              type,
                              digits,
                              meterValue,
                            );
                          } else {
                            await widget.controller.replace(
                              account,
                              digits,
                              meterValue,
                            );
                          }
                          if (sheetContext.mounted) Navigator.pop(sheetContext);
                          if (mounted) {
                            AqariSnackbar.show(
                              context,
                              account == null
                                  ? _t(
                                      context,
                                      'تم ربط الحساب.',
                                      'Account linked.',
                                    )
                                  : _t(
                                      context,
                                      'تم استبدال الحساب.',
                                      'Account replaced.',
                                    ),
                              kind: AqariStatusKind.success,
                            );
                          }
                        } catch (error) {
                          if (sheetContext.mounted) {
                            setSheetState(() {
                              submitting = false;
                              validation = _error(context, error);
                            });
                          }
                        }
                      },
              ),
            ],
          ),
        ),
      ),
    );
    number.dispose();
    meter.dispose();
  }

  Future<void> _confirmUnlink(TenantUtilityAccount account) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(_t(context, 'إلغاء ربط الحساب؟', 'Unlink account?')),
        content: Text(
          _t(
            context,
            'سيصبح الحساب غير نشط مع بقاء سجل فواتيره محفوظاً.',
            'The account will become inactive while its bill history remains preserved.',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: Text(_t(context, 'إلغاء', 'Cancel')),
          ),
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: Text(_t(context, 'إلغاء الربط', 'Unlink')),
          ),
        ],
      ),
    );
    if (!mounted) return;
    if (confirmed == true) {
      await _run(
        () => widget.controller.unlink(account),
        _t(context, 'تم إلغاء ربط الحساب.', 'Account unlinked.'),
      );
    }
  }

  Future<void> _run(Future<void> Function() action, String success) async {
    try {
      await action();
      if (mounted) {
        AqariSnackbar.show(context, success, kind: AqariStatusKind.success);
      }
    } catch (error) {
      if (mounted) {
        AqariSnackbar.show(
          context,
          _error(context, error),
          kind: AqariStatusKind.error,
        );
      }
    }
  }

  List<MapEntry<String, List<TenantUtilityBill>>> _billGroups(
    TenantUtilityController c,
  ) {
    final map = <String, List<TenantUtilityBill>>{};
    for (final bill in c.bills) {
      map
          .putIfAbsent(bill.utilityAccountId ?? 'unknown:${bill.id}', () => [])
          .add(bill);
    }
    return map.entries.toList(growable: false);
  }

  Widget _billGroup(String id, List<TenantUtilityBill> bills) {
    final first = bills.first;
    final account = widget.controller.accounts
        .where((item) => item.id == id)
        .firstOrNull;
    final current = account?.isCurrent ?? first.isCurrentAccount ?? false;
    final number = account?.accountNumber ?? first.sourceAccountNumber ?? '—';
    final type = account?.type ?? first.sourceUtilityType;
    return AqariCard(
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.all(AqariSpacing.x3),
            child: Wrap(
              spacing: AqariSpacing.x2,
              runSpacing: AqariSpacing.x1,
              children: [
                if (type != null)
                  AqariStatusBadge(
                    label: _type(context, type),
                    variant: AqariStatusVariant.brand,
                  ),
                Text(
                  '${current ? _t(context, 'الحساب الحالي', 'Current account') : _t(context, 'حساب سابق', 'Previous account')}: $number',
                  textDirection: ui.TextDirection.ltr,
                ),
              ],
            ),
          ),
          const AqariDivider(),
          for (final bill in bills)
            Padding(
              padding: const EdgeInsets.all(AqariSpacing.x3),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          _money(context, bill.amount, bill.currency),
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                      ),
                      _paymentBadge(context, bill.paymentStatus),
                    ],
                  ),
                  const SizedBox(height: AqariSpacing.x2),
                  AqariDetailRow(
                    label: _t(context, 'تاريخ الفاتورة', 'Bill date'),
                    value: _date(context, bill.billDate),
                  ),
                  AqariDetailRow(
                    label: _t(context, 'تاريخ الاستحقاق', 'Due date'),
                    value: bill.dueDate == null
                        ? '—'
                        : _date(context, bill.dueDate!),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

String _t(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;
String _type(BuildContext context, TenantUtilityType type) =>
    type == TenantUtilityType.electricity
    ? _t(context, 'الكهرباء', 'Electricity')
    : _t(context, 'المياه', 'Water');
String _error(BuildContext context, Object error) => error is ApiProblem
    ? error.message
    : _t(context, 'تعذر إكمال الطلب.', 'Could not complete the request.');
String _date(BuildContext context, String value) {
  final date = DateTime.tryParse(value);
  return date == null
      ? '—'
      : DateFormat.yMMMd(context.isArabic ? 'ar' : 'en').format(date);
}

String _dateTime(BuildContext context, String value) {
  final date = DateTime.tryParse(value)?.toLocal();
  return date == null
      ? '—'
      : DateFormat.yMMMd(context.isArabic ? 'ar' : 'en').add_jm().format(date);
}

String _money(BuildContext context, num value, String currency) =>
    NumberFormat.currency(
      locale: context.isArabic ? 'ar' : 'en',
      name: currency,
      decimalDigits: 3,
    ).format(value);
AqariStatusBadge _syncBadge(BuildContext context, String status) =>
    AqariStatusBadge(
      label: _syncLabel(context, status),
      variant: switch (status) {
        'Synced' => AqariStatusVariant.success,
        'Syncing' || 'NeverSynced' => AqariStatusVariant.info,
        'RateLimited' || 'Timeout' => AqariStatusVariant.warning,
        'ProviderError' || 'InvalidAccount' => AqariStatusVariant.error,
        _ => AqariStatusVariant.neutral,
      },
    );
String _syncLabel(BuildContext context, String value) => switch (value) {
  'NeverSynced' => _t(context, 'لم تتم المزامنة', 'Never synced'),
  'Syncing' => _t(context, 'جارٍ المزامنة', 'Syncing'),
  'Synced' => _t(context, 'متزامن', 'Synced'),
  'ProviderError' => _t(context, 'خطأ من المزود', 'Provider error'),
  'RateLimited' => _t(context, 'تم تقييد الطلبات', 'Rate limited'),
  'Timeout' => _t(context, 'انتهت المهلة', 'Timed out'),
  'Suspended' => _t(context, 'موقوف', 'Suspended'),
  'InvalidAccount' => _t(context, 'حساب غير صالح', 'Invalid account'),
  _ => value,
};
String _syncMessage(BuildContext context, String value) => switch (value) {
  'ProviderError' => _t(
    context,
    'تعذر الوصول إلى مزود الخدمة. حاول مرة أخرى.',
    'The provider could not be reached. Try again.',
  ),
  'RateLimited' => _t(
    context,
    'تم تقييد الطلب. حاول لاحقاً.',
    'The request was rate limited. Try later.',
  ),
  'Timeout' => _t(
    context,
    'انتهت مهلة المزامنة. يمكنك إعادة المحاولة.',
    'Synchronization timed out. You can retry.',
  ),
  'Suspended' => _t(
    context,
    'الحساب موقوف. اطلب إعادة تفعيله.',
    'The account is suspended. Request reactivation.',
  ),
  'InvalidAccount' => _t(
    context,
    'تحقق من رقم الحساب أو استبدله.',
    'Check or replace the account number.',
  ),
  _ => _t(
    context,
    'لم تكتمل المزامنة بعد.',
    'Synchronization has not completed yet.',
  ),
};
String _syncAction(BuildContext context, String value) => value == 'Suspended'
    ? _t(context, 'إعادة التفعيل', 'Reactivate')
    : const {
        'InvalidAccount',
        'ProviderError',
        'RateLimited',
        'Timeout',
      }.contains(value)
    ? _t(context, 'إعادة المحاولة', 'Retry')
    : _t(context, 'مزامنة', 'Sync');
AqariStatusBadge _paymentBadge(BuildContext context, String value) =>
    AqariStatusBadge(
      label: value == 'Paid'
          ? _t(context, 'مدفوعة', 'Paid')
          : value == 'Unpaid'
          ? _t(context, 'غير مدفوعة', 'Unpaid')
          : _t(context, 'غير معروف', 'Unknown'),
      variant: value == 'Paid'
          ? AqariStatusVariant.success
          : value == 'Unpaid'
          ? AqariStatusVariant.warning
          : AqariStatusVariant.neutral,
    );
