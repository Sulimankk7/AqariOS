import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/design_system/design_system.dart';
import '../application/notifications_controller.dart';
import '../domain/notification_models.dart';

String _nt(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;

class NotificationsEntry extends StatefulWidget {
  const NotificationsEntry({required this.controller, super.key});
  final NotificationsController controller;
  @override
  State<NotificationsEntry> createState() => _NotificationsEntryState();
}

class _NotificationsEntryState extends State<NotificationsEntry> {
  @override
  void initState() {
    super.initState();
    widget.controller.initialize();
  }

  Future<void> _open() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NotificationsScreen(
          controller: widget.controller,
          disposeController: false,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.controller,
    builder: (context, _) {
      final count = widget.controller.unreadCount;
      final label = context.isArabic ? 'الإشعارات' : 'Notifications';
      return Semantics(
        label: count > 0
            ? context.isArabic
                  ? '$label، $count غير مقروءة'
                  : '$label, $count unread'
            : label,
        button: true,
        child: Stack(
          clipBehavior: Clip.none,
          children: [
            AqariIconButton(
              icon: Icons.notifications_outlined,
              semanticLabel: label,
              onPressed: _open,
            ),
            if (count > 0)
              PositionedDirectional(
                end: 2,
                top: 2,
                child: Container(
                  constraints: const BoxConstraints(
                    minWidth: 18,
                    minHeight: 18,
                  ),
                  padding: const EdgeInsets.symmetric(horizontal: 4),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: context.aqariColors.error,
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    count > 99 ? '99+' : '$count',
                    style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: Colors.white,
                      fontSize: 10,
                    ),
                  ),
                ),
              ),
          ],
        ),
      );
    },
  );
}

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({
    required this.controller,
    this.disposeController = true,
    super.key,
  });
  final NotificationsController controller;
  final bool disposeController;
  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    widget.controller.load();
  }

  @override
  void dispose() {
    if (widget.disposeController) widget.controller.dispose();
    super.dispose();
  }

  Future<void> _open(InboxNotification item) async {
    try {
      await widget.controller.open(item);
    } catch (_) {
      if (mounted) {
        AqariSnackbar.show(
          context,
          _nt(
            context,
            'تعذر تحديد الإشعار كمقروء.',
            'Could not mark the notification as read.',
          ),
          kind: AqariStatusKind.error,
        );
      }
    }
    if (!mounted) return;
    await AqariBottomSheet.show<void>(
      context: context,
      title: _nt(context, 'تفاصيل الإشعار', 'Notification details'),
      child: _NotificationDetail(item: item),
    );
  }

  Future<void> _markAll() async {
    try {
      final count = await widget.controller.markAllRead();
      if (mounted && count > 0) {
        AqariSnackbar.show(
          context,
          _nt(
            context,
            'تم تحديد الإشعارات كمقروءة.',
            'Notifications marked as read.',
          ),
          kind: AqariStatusKind.success,
        );
      }
    } catch (_) {
      if (mounted) {
        AqariSnackbar.show(
          context,
          _nt(
            context,
            'تعذر تحديث الإشعارات.',
            'Could not update notifications.',
          ),
          kind: AqariStatusKind.error,
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.controller,
    builder: (context, _) {
      final c = widget.controller;
      return Scaffold(
        appBar: AqariAppBar(
          title: _nt(context, 'الإشعارات', 'Notifications'),
          onBack: () => Navigator.pop(context),
          actions: [
            if (c.unreadCount > 0)
              AqariIconButton(
                icon: Icons.done_all_rounded,
                semanticLabel: _nt(
                  context,
                  'تحديد الكل كمقروء',
                  'Mark all as read',
                ),
                onPressed: c.loading ? null : _markAll,
              ),
          ],
        ),
        body: c.loading && c.items.isEmpty
            ? Center(
                child: AqariLoadingState(
                  label: _nt(
                    context,
                    'جارٍ تحميل الإشعارات',
                    'Loading notifications',
                  ),
                ),
              )
            : c.error != null && c.items.isEmpty
            ? Center(
                child: AqariErrorState(
                  title: _nt(
                    context,
                    'تعذر تحميل الإشعارات',
                    'Could not load notifications',
                  ),
                  message: _nt(
                    context,
                    'تحقق من الاتصال ثم أعد المحاولة.',
                    'Check your connection and try again.',
                  ),
                  retryLabel: _nt(context, 'إعادة المحاولة', 'Retry'),
                  onRetry: c.load,
                ),
              )
            : RefreshIndicator(
                onRefresh: c.refresh,
                child: c.items.isEmpty
                    ? ListView(
                        physics: const AlwaysScrollableScrollPhysics(),
                        children: [
                          const SizedBox(height: 120),
                          AqariEmptyState(
                            title: _nt(
                              context,
                              'لا توجد إشعارات',
                              'No notifications',
                            ),
                            message: _nt(
                              context,
                              'لا توجد إشعارات حاليًا.',
                              'There are no notifications right now.',
                            ),
                          ),
                        ],
                      )
                    : ListView.builder(
                        padding: const EdgeInsets.all(AqariSpacing.page),
                        physics: const AlwaysScrollableScrollPhysics(),
                        itemCount: c.items.length + (c.hasMore ? 1 : 0),
                        itemBuilder: (context, index) {
                          if (index == c.items.length) {
                            return Padding(
                              padding: const EdgeInsets.all(AqariSpacing.x4),
                              child: AqariButton(
                                label: c.loadingMore
                                    ? _nt(context, 'جارٍ التحميل', 'Loading')
                                    : _nt(context, 'تحميل المزيد', 'Load more'),
                                variant: AqariButtonVariant.outlined,
                                loading: c.loadingMore,
                                onPressed: c.loadingMore ? null : c.loadMore,
                              ),
                            );
                          }
                          return _NotificationRow(
                            item: c.items[index],
                            onPressed: () => _open(c.items[index]),
                          );
                        },
                      ),
              ),
      );
    },
  );
}

class _NotificationRow extends StatelessWidget {
  const _NotificationRow({required this.item, required this.onPressed});
  final InboxNotification item;
  final VoidCallback onPressed;
  @override
  Widget build(BuildContext context) {
    final unread = item.canMarkRead;
    final colors = context.aqariColors;
    return Semantics(
      button: true,
      label:
          '${unread ? _nt(context, 'غير مقروء، ', 'Unread, ') : ''}${item.subject}',
      child: Container(
        color: unread ? colors.primary.withValues(alpha: .05) : null,
        child: AqariListRow(
          title: item.subject,
          supportingText: item.body,
          metadata: _date(context, item.createdAt),
          leading: Icon(
            _icon(item.type),
            color: unread ? colors.primary : colors.textSecondary,
          ),
          trailing: unread
              ? Container(
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: colors.primary,
                    shape: BoxShape.circle,
                  ),
                )
              : null,
          onPressed: onPressed,
          showChevron: false,
        ),
      ),
    );
  }
}

class _NotificationDetail extends StatelessWidget {
  const _NotificationDetail({required this.item});
  final InboxNotification item;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x4),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          _type(context, item.type),
          style: Theme.of(
            context,
          ).textTheme.labelMedium?.copyWith(color: context.aqariColors.primary),
        ),
        const SizedBox(height: AqariSpacing.x2),
        Text(item.subject, style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: AqariSpacing.x3),
        Text(item.body, style: Theme.of(context).textTheme.bodyMedium),
        const SizedBox(height: AqariSpacing.x4),
        Text(
          '${_nt(context, 'الحالة', 'Status')}: ${_status(context, item.status)}',
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: context.aqariColors.textSecondary,
          ),
        ),
        if (item.priority >= 2)
          Text(
            '${_nt(context, 'الأولوية', 'Priority')}: ${item.priority == 3 ? _nt(context, 'حرجة', 'Critical') : _nt(context, 'عالية', 'High')}',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: context.aqariColors.textSecondary,
            ),
          ),
        const SizedBox(height: AqariSpacing.x1),
        Text(
          _date(context, item.createdAt),
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: context.aqariColors.textSecondary,
          ),
        ),
      ],
    ),
  );
}

String _type(BuildContext context, int value) =>
    (context.isArabic
            ? const [
                'عقد إيجار جديد',
                'انتهاء عقد إيجار',
                'استحقاق إيجار',
                'تم دفع الإيجار',
                'تأخر في السداد',
                'طلب صيانة جديد',
                'تحديث طلب صيانة',
                'طلب معاينة',
                'مستند على وشك الانتهاء',
                'إشعار عام',
                'فاتورة كهرباء',
                'فاتورة مياه',
              ]
            : const [
                'New lease',
                'Lease expiration',
                'Rent due',
                'Rent paid',
                'Late payment',
                'New maintenance request',
                'Maintenance request updated',
                'Viewing request',
                'Document expiring',
                'General notification',
                'Electricity bill',
                'Water bill',
              ])
        .elementAtOrNull(value) ??
    _nt(context, 'إشعار عام', 'General notification');
IconData _icon(int value) => switch (value) {
  0 || 1 => Icons.description_outlined,
  2 || 3 || 4 => Icons.payments_outlined,
  5 || 6 => Icons.build_outlined,
  10 || 11 => Icons.receipt_long_outlined,
  _ => Icons.notifications_outlined,
};
String _status(BuildContext context, int value) =>
    (context.isArabic
            ? const ['قيد الانتظار', 'تم الإرسال', 'فشل', 'أُلغي']
            : const ['Pending', 'Sent', 'Failed', 'Cancelled'])
        .elementAtOrNull(value) ??
    _nt(context, 'قيد الانتظار', 'Pending');
String _date(BuildContext context, String value) {
  final date = DateTime.tryParse(value)?.toLocal();
  return date == null
      ? '—'
      : DateFormat(
          'd MMM، h:mm a',
          Localizations.localeOf(context).languageCode == 'ar' ? 'ar' : 'en',
        ).format(date);
}
