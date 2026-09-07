import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';
import 'playground_shared.dart';

class ListPlaygroundSection extends StatelessWidget {
  const ListPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Lists · القوائم',
    description:
        'AqariOS Mobile’s primary data architecture: flat rows, recognition data, status and dividers.',
    child: AqariSurface(
      color: context.aqariColors.surfaceLow,
      child: Column(
        children: [
          AqariListRow(
            title: 'Simple row · صف بسيط',
            supportingText: 'Secondary information',
            onPressed: () {},
            showChevron: true,
            density: AqariListRowDensity.compact,
          ),
          AqariListRow(
            title: 'ليان الخطيب',
            supportingText: 'مستأجرة · برج الزيتونة',
            leading: const AqariAvatar(label: 'ليان'),
            onPressed: () {},
            showChevron: true,
          ),
          AqariListRow(
            title: 'Apartment 4B · شقة ٤ب',
            supportingText: 'Building A · الطابق الرابع',
            metadata: 'AQ-APT-004B',
            leading: Icon(
              Icons.apartment_rounded,
              color: context.aqariColors.primary,
            ),
            status: const AqariStatusBadge(
              label: 'مشغولة',
              variant: AqariStatusVariant.brand,
            ),
            onPressed: () {},
            showChevron: true,
            density: AqariListRowDensity.rich,
          ),
          AqariListRow(
            title: 'Rent payment · دفعة الإيجار',
            supportingText: '480.00 JOD · due 05 Sep',
            leading: Icon(
              Icons.payments_outlined,
              color: context.aqariColors.primary,
            ),
            status: const AqariStatusBadge(
              label: 'مدفوع',
              variant: AqariStatusVariant.success,
            ),
            onPressed: () {},
          ),
          AqariListRow(
            title: 'Lease AQ-LS-128',
            supportingText: '01 Jan 2026 — 31 Dec 2026',
            status: const AqariStatusBadge(
              label: 'قيد التوقيع',
              variant: AqariStatusVariant.neutral,
            ),
            onPressed: () {},
            showChevron: true,
          ),
          const AqariListRow(
            title: 'Disabled row',
            supportingText: 'Unavailable action',
            enabled: false,
          ),
          const Padding(
            padding: EdgeInsets.all(AqariSpacing.x4),
            child: Column(
              children: [
                AqariSkeleton(height: 14),
                SizedBox(height: 8),
                AqariSkeleton(width: 180, height: 12),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}

class FeedbackPlaygroundSection extends StatelessWidget {
  const FeedbackPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Feedback · الملاحظات',
    description: 'Minimal, accessible and action-oriented states.',
    child: Column(
      children: [
        const PlaygroundWrap(
          children: [
            LabeledExample(
              label: 'Loading',
              width: 260,
              child: AqariLoadingState(label: 'جارٍ تحميل البيانات…'),
            ),
            LabeledExample(
              label: 'Skeleton',
              width: 260,
              child: Column(
                children: [
                  AqariSkeleton(height: 18),
                  SizedBox(height: 12),
                  AqariSkeleton(height: 12),
                  SizedBox(height: 8),
                  AqariSkeleton(width: 150, height: 12),
                ],
              ),
            ),
          ],
        ),
        PlaygroundWrap(
          children: [
            LabeledExample(
              label: 'Empty',
              width: 300,
              child: AqariEmptyState(
                title: 'لا توجد سجلات',
                message: 'ستظهر السجلات هنا عند إضافتها.',
                actionLabel: 'إضافة سجل',
                onAction: () {},
              ),
            ),
            LabeledExample(
              label: 'Error',
              width: 300,
              child: AqariErrorState(
                title: 'تعذر تحميل البيانات',
                message: 'تحقق من الاتصال ثم حاول مرة أخرى.',
                retryLabel: 'إعادة المحاولة',
                onRetry: () {},
              ),
            ),
          ],
        ),
        const SizedBox(height: AqariSpacing.x3),
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: AqariButton(
            label: 'Show snackbar',
            variant: AqariButtonVariant.outlined,
            onPressed: () => AqariSnackbar.show(
              context,
              'تم حفظ التغييرات بنجاح',
              kind: AqariStatusKind.success,
            ),
          ),
        ),
      ],
    ),
  );
}

class NavigationPlaygroundSection extends StatefulWidget {
  const NavigationPlaygroundSection({super.key});
  @override
  State<NavigationPlaygroundSection> createState() =>
      _NavigationPlaygroundSectionState();
}

class _NavigationPlaygroundSectionState
    extends State<NavigationPlaygroundSection> {
  int tab = 0, destination = 0;
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Navigation · التنقل',
    description: 'Primitives only—no production routing or business shell.',
    child: Column(
      children: [
        SizedBox(
          height: 56,
          child: AqariAppBar(
            title: 'تفاصيل العقار',
            onBack: () {},
            actions: [
              AqariPopupMenu<String>(
                label: 'More actions',
                items: const [
                  AqariPopupMenuAction(
                    value: 'edit',
                    label: 'تعديل',
                    icon: Icons.edit_outlined,
                  ),
                  AqariPopupMenuAction(
                    value: 'archive',
                    label: 'أرشفة',
                    icon: Icons.archive_outlined,
                  ),
                ],
                onSelected: (_) {},
              ),
            ],
          ),
        ),
        AqariTabs(
          labels: const ['نظرة عامة', 'العقود', 'المدفوعات', 'المستندات'],
          selectedIndex: tab,
          onSelected: (v) => setState(() => tab = v),
        ),
        const SizedBox(height: AqariSpacing.x4),
        AqariBottomNavigation(
          items: const [
            AqariNavigationDestination(
              label: 'الرئيسية',
              icon: Icons.space_dashboard_outlined,
            ),
            AqariNavigationDestination(
              label: 'العقارات',
              icon: Icons.apartment_outlined,
            ),
            AqariNavigationDestination(
              label: 'العقود',
              icon: Icons.description_outlined,
            ),
            AqariNavigationDestination(
              label: 'المالية',
              icon: Icons.account_balance_wallet_outlined,
            ),
            AqariNavigationDestination(
              label: 'المزيد',
              icon: Icons.more_horiz_rounded,
            ),
          ],
          selectedIndex: destination,
          onSelected: (v) => setState(() => destination = v),
        ),
      ],
    ),
  );
}

class OverlayPlaygroundSection extends StatelessWidget {
  const OverlayPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Overlays · الطبقات',
    description:
        'Dialogs for concise decisions; bottom sheets for touch-native contextual tasks.',
    child: PlaygroundWrap(
      children: [
        AqariButton(
          label: 'Open dialog',
          variant: AqariButtonVariant.outlined,
          onPressed: () => AqariDialog.show<void>(
            context: context,
            title: 'تأكيد الإجراء',
            content: const Text('هل تريد متابعة هذا الإجراء؟'),
            secondaryLabel: 'إلغاء',
            primaryLabel: 'متابعة',
          ),
        ),
        AqariButton(
          label: 'Open bottom sheet',
          variant: AqariButtonVariant.tonal,
          onPressed: () => AqariBottomSheet.show<void>(
            context: context,
            title: 'تصفية النتائج',
            child: Column(
              children: [
                const AqariCheckbox(
                  label: 'العقود النشطة',
                  value: true,
                  onChanged: null,
                ),
                const AqariCheckbox(
                  label: 'الدفعات المتأخرة',
                  value: false,
                  onChanged: null,
                ),
                const SizedBox(height: AqariSpacing.x4),
                AqariButton(
                  label: 'تطبيق الفلاتر',
                  expanded: true,
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
          ),
        ),
        AqariPopupMenu<String>(
          label: 'Open popup menu',
          items: const [
            AqariPopupMenuAction(
              value: 'view',
              label: 'عرض التفاصيل',
              icon: Icons.visibility_outlined,
            ),
            AqariPopupMenuAction(
              value: 'share',
              label: 'مشاركة',
              icon: Icons.share_outlined,
            ),
          ],
          onSelected: (_) {},
        ),
      ],
    ),
  );
}

class AccessibilityPlaygroundSection extends StatelessWidget {
  const AccessibilityPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Accessibility stress cases',
    description:
        'Long Arabic/English, mixed direction, icon semantics, errors and disabled states.',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'نص عربي طويل للتحقق من التفاف الأسطر والمحاذاة وسهولة القراءة عند تكبير حجم الخط في أجهزة الهاتف الصغيرة.',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: AqariSpacing.x3),
        Text(
          'A deliberately long English label validates wrapping and readable hierarchy without fixed-height clipping.',
          style: Theme.of(context).textTheme.bodyLarge,
        ),
        const SizedBox(height: AqariSpacing.x3),
        const AqariLtrContent(
          child: Text('tenant@example.com · +962 7 9000 0000 · AQ-LEASE-2048'),
        ),
        const SizedBox(height: AqariSpacing.x3),
        Row(
          children: [
            AqariIconButton(
              icon: Icons.delete_outline_rounded,
              semanticLabel: 'Delete record',
              onPressed: () {},
            ),
            const SizedBox(width: AqariSpacing.x2),
            const Expanded(
              child: AqariTextField(
                label: 'Required value',
                errorText: 'This field is required · هذا الحقل مطلوب',
              ),
            ),
          ],
        ),
      ],
    ),
  );
}
