import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../../auth/domain/user_profile.dart';
import '../domain/property_models.dart';

String tr(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;
bool propertyPermission(UserProfile user, String operation) =>
    user.hasAnyPermission(['properties.$operation', 'properties.manage']);
String kindLabel(BuildContext c, PropertyKind k) => switch (k) {
  PropertyKind.buildings => tr(c, 'المباني', 'Buildings'),
  PropertyKind.floors => tr(c, 'الطوابق', 'Floors'),
  PropertyKind.apartments => tr(c, 'الوحدات', 'Units'),
};
String occupancyLabel(BuildContext c, int status) => switch (status) {
  0 => tr(c, 'شاغرة', 'Vacant'),
  1 => tr(c, 'مشغولة', 'Occupied'),
  2 => tr(c, 'تحت الصيانة', 'Under maintenance'),
  3 => tr(c, 'معروضة', 'Listed'),
  _ => tr(c, 'غير معروف', 'Unknown'),
};
String buildingTypeLabel(BuildContext c, int type) => switch (type) {
  0 => tr(c, 'سكني', 'Residential'),
  1 => tr(c, 'تجاري', 'Commercial'),
  2 => tr(c, 'متعدد الاستخدام', 'Mixed use'),
  _ => tr(c, 'غير معروف', 'Unknown'),
};
String floorTypeLabel(BuildContext c, int type) => switch (type) {
  0 => tr(c, 'قبو', 'Basement'),
  1 => tr(c, 'أرضي', 'Ground'),
  2 => tr(c, 'طابق عادي', 'Regular'),
  3 => tr(c, 'سطح', 'Roof'),
  _ => tr(c, 'غير معروف', 'Unknown'),
};

class OccupancyBadge extends StatelessWidget {
  const OccupancyBadge(this.status, {super.key});
  final int status;
  @override
  Widget build(BuildContext context) => AqariStatusBadge(
    label: occupancyLabel(context, status),
    variant: switch (status) {
      1 => AqariStatusRegistry.visualVariant(AqariDomainStatus.occupied),
      2 => AqariStatusRegistry.visualVariant(
        AqariDomainStatus.underMaintenance,
      ),
      _ => AqariStatusVariant.neutral,
    },
  );
}

String propertyError(BuildContext c, ApiProblem error) {
  if (error.isNetworkFailure) {
    return tr(
      c,
      'تعذر الاتصال. تحقق من الشبكة وحاول مجدداً.',
      'Unable to connect. Check your connection and retry.',
    );
  }
  return switch (error.statusCode) {
    401 => tr(
      c,
      'انتهت الجلسة. سجّل الدخول مجدداً.',
      'Your session expired. Sign in again.',
    ),
    403 => tr(
      c,
      'ليس لديك صلاحية لهذا الإجراء.',
      'You do not have permission for this action.',
    ),
    404 => tr(
      c,
      'السجل غير متاح أو تمت أرشفته.',
      'This record is unavailable or has been archived.',
    ),
    409 => tr(
      c,
      'تعذر الحفظ أو الأرشفة لوجود تعارض أو سجلات مرتبطة. راجع البيانات.',
      'A conflict or linked records prevents this action. Review the data.',
    ),
    400 || 422 => tr(
      c,
      'راجع الحقول وقواعد السجل ثم حاول مجدداً.',
      'Review the fields and record requirements, then retry.',
    ),
    429 => tr(
      c,
      'طلبات كثيرة. انتظر قليلاً ثم حاول مجدداً.',
      'Too many requests. Wait a moment and retry.',
    ),
    _ => tr(
      c,
      'تعذر تحميل البيانات. حاول مجدداً.',
      'Unable to load data. Please retry.',
    ),
  };
}

class PropertyFailure extends StatelessWidget {
  const PropertyFailure(this.error, {this.retry, super.key});
  final ApiProblem error;
  final VoidCallback? retry;
  @override
  Widget build(BuildContext context) => AqariErrorState(
    title: tr(context, 'تعذر إكمال الطلب', 'Request unsuccessful'),
    message: propertyError(context, error),
    retryLabel: retry == null ? null : tr(context, 'إعادة المحاولة', 'Retry'),
    onRetry: retry,
  );
}

class PropertyLoading extends StatelessWidget {
  const PropertyLoading({super.key});
  @override
  Widget build(BuildContext context) => Semantics(
    liveRegion: true,
    label: tr(context, 'جارٍ تحميل العقارات', 'Loading properties'),
    child: Column(
      children: List.generate(
        3,
        (_) => const Padding(
          padding: EdgeInsets.all(AqariSpacing.x4),
          child: Column(
            children: [
              AqariSkeleton(height: AqariSpacing.x5),
              SizedBox(height: AqariSpacing.x2),
              AqariSkeleton(height: AqariSpacing.x3),
            ],
          ),
        ),
      ),
    ),
  );
}

class PropertyFact extends StatelessWidget {
  const PropertyFact(this.label, this.value, {this.ltr = false, super.key});
  final String label;
  final String? value;
  final bool ltr;
  @override
  Widget build(BuildContext context) => AqariDetailRow(
    label: label,
    value: value == null || value!.isEmpty ? '—' : value!,
    ltr: ltr,
  );
}

class PropertyIdentity extends StatelessWidget {
  const PropertyIdentity({
    required this.title,
    required this.icon,
    this.subtitle,
    this.status,
    super.key,
  });
  final String title;
  final IconData icon;
  final String? subtitle;
  final Widget? status;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x5),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: context.aqariColors.primary),
            const SizedBox(width: AqariSpacing.x3),
            Expanded(
              child: Text(
                title,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
            ),
          ],
        ),
        if (subtitle?.isNotEmpty == true) ...[
          const SizedBox(height: AqariSpacing.x2),
          Text(
            subtitle!,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: context.aqariColors.textSecondary,
            ),
          ),
        ],
        if (status != null) ...[
          const SizedBox(height: AqariSpacing.x3),
          Align(alignment: AlignmentDirectional.centerStart, child: status!),
        ],
      ],
    ),
  );
}

String isolated(Object value) => '\u2068$value\u2069';
