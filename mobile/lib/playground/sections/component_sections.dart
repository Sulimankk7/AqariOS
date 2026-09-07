import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';
import 'playground_shared.dart';

class ButtonPlaygroundSection extends StatelessWidget {
  const ButtonPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Buttons · الأزرار',
    description:
        'Pill-shaped, calm, 48px touch targets. One primary action per context.',
    child: PlaygroundWrap(
      children: [
        AqariButton(
          label: 'Primary · أساسي',
          icon: Icons.add_rounded,
          onPressed: () {},
        ),
        AqariButton(
          label: 'Tonal · ثانوي',
          variant: AqariButtonVariant.tonal,
          onPressed: () {},
        ),
        AqariButton(
          label: 'Outlined',
          variant: AqariButtonVariant.outlined,
          onPressed: () {},
        ),
        AqariButton(
          label: 'Text',
          variant: AqariButtonVariant.text,
          onPressed: () {},
        ),
        AqariButton(
          label: 'Destructive',
          variant: AqariButtonVariant.destructive,
          onPressed: () {},
        ),
        AqariButton(
          label: 'Link',
          variant: AqariButtonVariant.link,
          onPressed: () {},
        ),
        const AqariButton(label: 'Loading', loading: true),
        const AqariButton(label: 'Disabled'),
        AqariIconButton(
          icon: Icons.notifications_none_rounded,
          semanticLabel: 'Notifications',
          onPressed: () {},
        ),
      ],
    ),
  );
}

class FormPlaygroundSection extends StatefulWidget {
  const FormPlaygroundSection({super.key});
  @override
  State<FormPlaygroundSection> createState() => _FormPlaygroundSectionState();
}

class _FormPlaygroundSectionState extends State<FormPlaygroundSection> {
  bool checked = true, switched = true;
  String radio = 'monthly', select = 'Amman';
  late final TextEditingController filledController;

  @override
  void initState() {
    super.initState();
    filledController = TextEditingController(text: 'AQ-2048');
  }

  @override
  void dispose() {
    filledController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Forms · النماذج',
    description:
        'Flat form flow with persistent labels, helpers, validation and logical direction.',
    child: LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth < 620
            ? constraints.maxWidth
            : (constraints.maxWidth - 16) / 2;
        return PlaygroundWrap(
          children: [
            LabeledExample(
              label: 'Default',
              width: width,
              child: const AqariTextField(
                label: 'اسم العقار',
                hint: 'مثال: برج عمّان',
                helperText: 'استخدم الاسم المسجل',
              ),
            ),
            LabeledExample(
              label: 'Focused',
              width: width,
              child: const AqariTextField(
                label: 'اسم المبنى',
                hint: 'ابدأ الكتابة',
                autofocus: true,
              ),
            ),
            LabeledExample(
              label: 'Filled technical LTR',
              width: width,
              child: AqariTextField(
                controller: filledController,
                label: 'Account ID',
                textDirection: TextDirection.ltr,
              ),
            ),
            LabeledExample(
              label: 'Error',
              width: width,
              child: const AqariTextField(
                label: 'البريد الإلكتروني',
                errorText: 'أدخل بريدًا إلكترونيًا صالحًا',
                textDirection: TextDirection.ltr,
              ),
            ),
            LabeledExample(
              label: 'Disabled',
              width: width,
              child: const AqariTextField(
                label: 'رقم العقد',
                hint: 'AQ-LS-128',
                enabled: false,
              ),
            ),
            LabeledExample(
              label: 'Search',
              width: width,
              child: const AqariSearchField(hint: 'ابحث عن مستأجر أو عقار'),
            ),
            LabeledExample(
              label: 'Loading',
              width: width,
              child: AqariTextField(
                label: 'التحقق من الحساب',
                enabled: false,
                suffix: Padding(
                  padding: const EdgeInsets.all(14),
                  child: SizedBox.square(
                    dimension: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: context.aqariColors.primary,
                    ),
                  ),
                ),
              ),
            ),
            LabeledExample(
              label: 'Select',
              width: width,
              child: AqariSelect<String>(
                label: 'المدينة',
                valueLabel: select,
                items: const ['Amman', 'Irbid', 'Aqaba'],
                itemLabel: (v) => v,
                onChanged: (v) => setState(() => select = v),
              ),
            ),
            LabeledExample(
              label: 'Checkbox',
              width: width,
              child: AqariCheckbox(
                label: 'إرسال نسخة إلى المستأجر',
                value: checked,
                onChanged: (v) => setState(() => checked = v ?? false),
              ),
            ),
            LabeledExample(
              label: 'Switch',
              width: width,
              child: AqariSwitch(
                label: 'تفعيل الإشعارات',
                value: switched,
                onChanged: (v) => setState(() => switched = v),
              ),
            ),
            LabeledExample(
              label: 'Radio',
              width: width,
              child: Column(
                children: [
                  AqariRadio<String>(
                    label: 'شهري',
                    value: 'monthly',
                    groupValue: radio,
                    onChanged: (v) => setState(() => radio = v!),
                  ),
                  AqariRadio<String>(
                    label: 'سنوي',
                    value: 'yearly',
                    groupValue: radio,
                    onChanged: (v) => setState(() => radio = v!),
                  ),
                ],
              ),
            ),
          ],
        );
      },
    ),
  );
}

class SurfacePlaygroundSection extends StatelessWidget {
  const SurfacePlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Surfaces, not cards everywhere',
    description:
        'Flat sections and divided list groups are the default. Cards require a meaningful boundary.',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const AqariSectionHeader(title: 'Flat section · قسم مسطح'),
        Text(
          'Primary information uses hierarchy and whitespace without an outer container.',
          style: Theme.of(context).textTheme.bodyMedium,
        ),
        const SizedBox(height: AqariSpacing.x5),
        AqariSurface(
          color: context.aqariColors.surfaceLow,
          child: const Column(
            children: [
              AqariListRow(
                title: 'List group row',
                supportingText: 'One shared surface and internal dividers',
              ),
              AqariListRow(
                title: 'Second row',
                supportingText: 'No card per item',
                showDivider: false,
              ),
            ],
          ),
        ),
        const SizedBox(height: AqariSpacing.x5),
        PlaygroundWrap(
          children: [
            LabeledExample(
              label: 'Meaningful card',
              width: 280,
              child: AqariCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Lease summary',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 8),
                    const Text('A bounded, independently meaningful summary.'),
                  ],
                ),
              ),
            ),
            LabeledExample(
              label: 'Elevated interactive card',
              width: 280,
              child: AqariCard(
                variant: AqariCardVariant.elevated,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Actionable exception',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 8),
                    const Text('Elevation is restrained and purposeful.'),
                  ],
                ),
              ),
            ),
          ],
        ),
      ],
    ),
  );
}

class StatusPlaygroundSection extends StatelessWidget {
  const StatusPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) {
    const labels = <AqariDomainStatus, String>{
      AqariDomainStatus.active: 'Active · نشط',
      AqariDomainStatus.draft: 'Draft · مسودة',
      AqariDomainStatus.pending: 'Pending · قيد الانتظار',
      AqariDomainStatus.pendingVerification:
          'Pending verification · قيد التحقق من الدفعة',
      AqariDomainStatus.paid: 'Paid · مدفوع',
      AqariDomainStatus.partiallyPaid: 'Partially paid · مدفوع جزئيًا',
      AqariDomainStatus.late: 'Late · متأخر',
      AqariDomainStatus.overdue: 'Overdue · متأخر وغير مدفوع',
      AqariDomainStatus.cancelled: 'Cancelled · ملغي',
      AqariDomainStatus.expired: 'Expired · منتهي',
      AqariDomainStatus.terminated: 'Terminated · تم إنهاؤه',
      AqariDomainStatus.renewed: 'Renewed · مجدد',
      AqariDomainStatus.occupied: 'Occupied · مشغولة',
      AqariDomainStatus.vacant: 'Vacant · شاغرة',
      AqariDomainStatus.underMaintenance: 'Under maintenance · قيد الصيانة',
    };
    return PlaygroundSection(
      title: 'Status system · الحالات',
      description:
          'Domain statuses map through a registry to finite semantic visual variants.',
      child: PlaygroundWrap(
        children: labels.entries
            .map(
              (e) => AqariStatusBadge(
                label: e.value,
                variant: AqariStatusRegistry.visualVariant(e.key),
              ),
            )
            .toList(),
      ),
    );
  }
}
