import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../../auth/domain/user_profile.dart';
import '../../notifications/application/notifications_controller.dart';
import '../../notifications/domain/notification_models.dart';
import '../data/tenant_portal_repository.dart';
import '../domain/tenant_portal_models.dart';
import '../utilities/tenant_utility_controller.dart';
import '../utilities/tenant_utility_models.dart';

class TenantDashboardScreen extends StatefulWidget {
  const TenantDashboardScreen({
    required this.user,
    required this.repository,
    required this.utilities,
    required this.notifications,
    required this.onOpenProfile,
    required this.onOpenPayments,
    required this.onOpenUtilities,
    super.key,
  });
  final UserProfile user;
  final TenantPortalRepository repository;
  final TenantUtilityController utilities;
  final NotificationsController notifications;
  final VoidCallback onOpenProfile, onOpenPayments, onOpenUtilities;

  @override
  State<TenantDashboardScreen> createState() => _TenantDashboardScreenState();
}

class _TenantDashboardScreenState extends State<TenantDashboardScreen> {
  TenantProfile? _profile;
  Object? _profileError;
  bool _profileLoading = true;
  String? _expandedNotification;

  @override
  void initState() {
    super.initState();
    _loadProfile();
    widget.utilities.loadSummary();
    widget.notifications.load();
  }

  Future<void> _loadProfile({bool force = false}) async {
    setState(() {
      _profileLoading = true;
      _profileError = null;
    });
    try {
      final value = await widget.repository.profile(force: force);
      if (mounted) setState(() => _profile = value);
    } catch (error) {
      if (mounted) setState(() => _profileError = error);
    } finally {
      if (mounted) setState(() => _profileLoading = false);
    }
  }

  Future<void> _refresh() async => Future.wait([
    _loadProfile(force: true),
    widget.utilities.loadSummary(force: true),
    widget.notifications.refresh(),
  ]);

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: Listenable.merge([widget.utilities, widget.notifications]),
    builder: (context, _) => RefreshIndicator(
      onRefresh: _refresh,
      child: ListView(
        key: const PageStorageKey('tenant-dashboard'),
        padding: const EdgeInsets.all(AqariSpacing.page),
        children: [
          AqariCard(
            variant: AqariCardVariant.filled,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.verified_user_outlined,
                      size: 18,
                      color: context.aqariColors.primary,
                    ),
                    const SizedBox(width: AqariSpacing.x2),
                    Text(
                      _t(context, 'بوابة المستأجر', 'Tenant portal'),
                      style: Theme.of(context).textTheme.labelMedium?.copyWith(
                        color: context.aqariColors.primary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AqariSpacing.x2),
                Text(
                  _t(
                    context,
                    'مرحباً بك، ${_profile?.name ?? widget.user.fullName}',
                    'Welcome back, ${_profile?.name ?? widget.user.fullName}',
                  ),
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: AqariSpacing.x1),
                Text(
                  _t(
                    context,
                    'تابع حسابك وخدماتك الحالية من مكان واحد.',
                    'Keep track of your current account and services in one place.',
                  ),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: context.aqariColors.textSecondary,
                  ),
                ),
                const SizedBox(height: AqariSpacing.x4),
                AqariButton(
                  label: _t(context, 'ملفي الشخصي', 'My profile'),
                  icon: Icons.person_outline,
                  expanded: true,
                  onPressed: widget.onOpenProfile,
                ),
              ],
            ),
          ),
          const SizedBox(height: AqariSpacing.section),
          Row(
            children: [
              Expanded(
                child: _shortcut(
                  Icons.people_outline,
                  _t(context, 'العائلة', 'Family'),
                  _profileLoading
                      ? '—'
                      : '${_profile?.familyMembers.length ?? 0}',
                  widget.onOpenProfile,
                ),
              ),
              const SizedBox(width: AqariSpacing.x3),
              Expanded(
                child: _shortcut(
                  Icons.directions_car_outlined,
                  _t(context, 'المركبات', 'Vehicles'),
                  _profileLoading ? '—' : '${_profile?.vehicles.length ?? 0}',
                  widget.onOpenProfile,
                ),
              ),
            ],
          ),
          if (_profileError != null)
            Padding(
              padding: const EdgeInsets.only(top: AqariSpacing.x2),
              child: AqariErrorState(
                title: _t(
                  context,
                  'تعذر تحميل بيانات الملف',
                  'Could not load profile data',
                ),
                message: _error(context, _profileError!),
                retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
                onRetry: () => _loadProfile(force: true),
              ),
            ),
          const SizedBox(height: AqariSpacing.x3),
          _wideShortcut(
            Icons.payments_outlined,
            _t(context, 'إرسال إثبات دفعة', 'Submit payment proof'),
            _t(
              context,
              'راجع الدفعات المستحقة وأرسل أو أعد إرسال إثبات الدفع.',
              'Review due payments and submit or resubmit payment proof.',
            ),
            widget.onOpenPayments,
          ),
          const SizedBox(height: AqariSpacing.section),
          AqariSectionHeader(
            title: _t(context, 'الخدمات', 'Utilities'),
            actionLabel: _t(context, 'عرض الكل', 'View all'),
            onAction: widget.onOpenUtilities,
          ),
          _utilities(),
          const SizedBox(height: AqariSpacing.section),
          AqariSectionHeader(
            title: _t(context, 'آخر الإشعارات', 'Recent notifications'),
          ),
          _notifications(),
          const SizedBox(height: AqariSpacing.x8),
        ],
      ),
    ),
  );

  Widget _shortcut(
    IconData icon,
    String label,
    String value,
    VoidCallback onTap,
  ) => AqariCard(
    child: InkWell(
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: context.aqariColors.primary),
          const SizedBox(height: AqariSpacing.x2),
          Text(value, style: Theme.of(context).textTheme.headlineSmall),
          Text(
            label,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: context.aqariColors.textSecondary,
            ),
          ),
        ],
      ),
    ),
  );

  Widget _wideShortcut(
    IconData icon,
    String title,
    String description,
    VoidCallback onTap,
  ) => AqariCard(
    child: InkWell(
      onTap: onTap,
      child: Row(
        children: [
          Icon(icon, color: context.aqariColors.primary),
          const SizedBox(width: AqariSpacing.x3),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: Theme.of(context).textTheme.titleSmall),
                Text(
                  description,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: context.aqariColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          const Icon(Icons.chevron_right_rounded),
        ],
      ),
    ),
  );

  Widget _utilities() {
    final c = widget.utilities;
    if (c.summary == null && c.summaryError == null) {
      return const AqariCard(
        child: Column(
          children: [
            AqariSkeleton(height: 22),
            SizedBox(height: AqariSpacing.x3),
            AqariSkeleton(height: 64),
          ],
        ),
      );
    }
    if (c.summaryError != null) {
      return AqariErrorState(
        title: _t(
          context,
          'تعذر تحميل ملخص الخدمات',
          'Could not load utility summary',
        ),
        message: _error(context, c.summaryError!),
        retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
        onRetry: () => c.loadSummary(force: true),
      );
    }
    final value = c.summary!;
    return Column(
      children: [
        _utilitySummary(
          TenantUtilityType.electricity,
          value.electricityLinked,
          value.electricityAccountNumber,
          value.electricitySyncStatus,
          value.latestElectricityBill,
          value.electricityLastSuccessfulSyncAt,
        ),
        const SizedBox(height: AqariSpacing.x3),
        _utilitySummary(
          TenantUtilityType.water,
          value.waterLinked,
          value.waterAccountNumber,
          value.waterSyncStatus,
          value.latestWaterBill,
          value.waterLastSuccessfulSyncAt,
        ),
      ],
    );
  }

  Widget _utilitySummary(
    TenantUtilityType type,
    bool linked,
    String? number,
    String? status,
    TenantUtilityBill? bill,
    String? lastSync,
  ) => AqariCard(
    child: InkWell(
      onTap: widget.onOpenUtilities,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(
                type == TenantUtilityType.electricity
                    ? Icons.bolt_outlined
                    : Icons.water_drop_outlined,
                color: context.aqariColors.primary,
              ),
              const SizedBox(width: AqariSpacing.x2),
              Expanded(
                child: Text(
                  type == TenantUtilityType.electricity
                      ? _t(context, 'الكهرباء', 'Electricity')
                      : _t(context, 'المياه', 'Water'),
                  style: Theme.of(context).textTheme.titleSmall,
                ),
              ),
              AqariStatusBadge(
                label: linked
                    ? _t(context, 'مرتبط', 'Linked')
                    : _t(context, 'غير مرتبط', 'Not linked'),
                variant: linked
                    ? AqariStatusVariant.success
                    : AqariStatusVariant.neutral,
              ),
            ],
          ),
          const SizedBox(height: AqariSpacing.x3),
          if (!linked)
            Text(
              _t(
                context,
                'اربط حساب الخدمة لعرض الرصيد والفواتير.',
                'Link the utility account to view balance and bills.',
              ),
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: context.aqariColors.textSecondary,
              ),
            )
          else ...[
            AqariDetailRow(
              label: _t(context, 'رقم الحساب', 'Account number'),
              value: number ?? '—',
              ltr: true,
            ),
            AqariDetailRow(
              label: _t(context, 'حالة المزامنة', 'Sync status'),
              value: _sync(context, status),
            ),
            AqariDetailRow(
              label: _t(context, 'آخر فاتورة', 'Latest bill'),
              value: bill == null
                  ? '—'
                  : _money(context, bill.amount, bill.currency),
            ),
            AqariDetailRow(
              label: _t(context, 'حالة الفاتورة', 'Bill status'),
              value: bill == null
                  ? '—'
                  : bill.paymentStatus == 'Paid'
                  ? _t(context, 'مدفوعة', 'Paid')
                  : bill.paymentStatus == 'Unpaid'
                  ? _t(context, 'غير مدفوعة', 'Unpaid')
                  : _t(context, 'غير معروف', 'Unknown'),
            ),
            AqariDetailRow(
              label: _t(context, 'آخر مزامنة ناجحة', 'Last successful sync'),
              value: lastSync == null ? '—' : _dateTime(context, lastSync),
            ),
          ],
        ],
      ),
    ),
  );

  Widget _notifications() {
    final c = widget.notifications;
    if (c.loading && c.items.isEmpty) {
      return const AqariCard(child: AqariSkeleton(height: 96));
    }
    if (c.error != null && c.items.isEmpty) {
      return AqariErrorState(
        title: _t(
          context,
          'تعذر تحميل الإشعارات',
          'Could not load notifications',
        ),
        message: _error(context, c.error!),
        retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
        onRetry: c.refresh,
      );
    }
    if (c.items.isEmpty) {
      return AqariEmptyState(
        title: _t(context, 'لا توجد إشعارات', 'No notifications'),
        message: _t(
          context,
          'ستظهر الإشعارات الجديدة هنا.',
          'New notifications will appear here.',
        ),
      );
    }
    return AqariCard(
      padding: EdgeInsets.zero,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(AqariSpacing.x3),
            child: Row(
              children: [
                Expanded(child: Text(_t(context, 'غير المقروءة', 'Unread'))),
                AqariStatusBadge(
                  label: '${c.unreadCount}',
                  variant: c.unreadCount > 0
                      ? AqariStatusVariant.brand
                      : AqariStatusVariant.neutral,
                ),
              ],
            ),
          ),
          const AqariDivider(),
          for (final item in c.items.take(5)) _notification(item),
        ],
      ),
    );
  }

  Widget _notification(InboxNotification item) {
    final expanded = _expandedNotification == item.id;
    return AqariListRow(
      title: item.subject,
      supportingText: expanded
          ? '${item.body}\n${_dateTime(context, item.createdAt)}'
          : _dateTime(context, item.createdAt),
      leading: Icon(
        item.canMarkRead
            ? Icons.notifications_active_outlined
            : Icons.notifications_none_outlined,
        color: item.canMarkRead
            ? context.aqariColors.primary
            : context.aqariColors.textMuted,
      ),
      showChevron: true,
      onPressed: () async {
        setState(() => _expandedNotification = expanded ? null : item.id);
        if (item.canMarkRead) {
          try {
            await widget.notifications.open(item);
          } catch (_) {}
        }
      },
    );
  }
}

String _t(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;
String _error(BuildContext context, Object value) => value is ApiProblem
    ? value.message
    : _t(context, 'تعذر إكمال الطلب.', 'Could not complete the request.');
String _money(BuildContext context, num value, String currency) =>
    NumberFormat.currency(
      locale: context.isArabic ? 'ar' : 'en',
      name: currency,
      decimalDigits: 3,
    ).format(value);
String _dateTime(BuildContext context, String value) {
  final date = DateTime.tryParse(value)?.toLocal();
  return date == null
      ? '—'
      : DateFormat.yMMMd(context.isArabic ? 'ar' : 'en').add_jm().format(date);
}

String _sync(BuildContext context, String? status) => switch (status) {
  'NeverSynced' => _t(context, 'لم تتم المزامنة', 'Never synced'),
  'Syncing' => _t(context, 'جارٍ المزامنة', 'Syncing'),
  'Synced' => _t(context, 'متزامن', 'Synced'),
  'ProviderError' => _t(context, 'خطأ من المزود', 'Provider error'),
  'RateLimited' => _t(context, 'طلبات مقيدة', 'Rate limited'),
  'Timeout' => _t(context, 'انتهت المهلة', 'Timed out'),
  'Suspended' => _t(context, 'موقوف', 'Suspended'),
  'InvalidAccount' => _t(context, 'حساب غير صالح', 'Invalid account'),
  _ => '—',
};
