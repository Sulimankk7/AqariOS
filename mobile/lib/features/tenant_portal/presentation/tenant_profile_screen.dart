import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../data/tenant_portal_repository.dart';
import '../domain/tenant_portal_models.dart';

class TenantProfileScreen extends StatefulWidget {
  const TenantProfileScreen({required this.repository, super.key});

  final TenantPortalRepository repository;

  @override
  State<TenantProfileScreen> createState() => _TenantProfileScreenState();
}

class _TenantProfileScreenState extends State<TenantProfileScreen> {
  TenantProfile? _profile;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({bool force = false}) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final profile = await widget.repository.profile(force: force);
      if (mounted) setState(() => _profile = profile);
    } catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading && _profile == null) {
      return const AqariLoadingState(label: 'جارٍ تحميل الملف الشخصي');
    }
    if (_error != null && _profile == null) {
      return Center(
        child: AqariErrorState(
          title: _t(
            context,
            'تعذر تحميل الملف الشخصي',
            'Could not load profile',
          ),
          message: _message(context, _error!),
          retryLabel: _t(context, 'إعادة المحاولة', 'Retry'),
          onRetry: () => _load(force: true),
        ),
      );
    }
    final profile = _profile!;
    return RefreshIndicator(
      onRefresh: () => _load(force: true),
      child: ListView(
        key: const PageStorageKey('tenant-profile'),
        padding: const EdgeInsets.all(AqariSpacing.page),
        children: [
          AqariCard(
            child: Row(
              children: [
                AqariAvatar(label: profile.name, size: 48),
                const SizedBox(width: AqariSpacing.x3),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        profile.name,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      Text(
                        _t(
                          context,
                          'ملف مستأجر للعرض فقط',
                          'Read-only tenant profile',
                        ),
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: context.aqariColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: AqariSpacing.section),
          _section(
            context,
            _t(context, 'المعلومات الشخصية', 'Personal information'),
            [
              _row(context, _t(context, 'الاسم', 'Name'), profile.name),
              _row(
                context,
                _t(context, 'الرقم الوطني', 'National ID'),
                profile.nationalId,
                ltr: true,
              ),
              _row(
                context,
                _t(context, 'الهاتف', 'Phone'),
                profile.phone,
                ltr: true,
              ),
              _row(
                context,
                _t(context, 'البريد الإلكتروني', 'Email'),
                profile.email,
                ltr: true,
              ),
              _row(
                context,
                _t(context, 'المهنة', 'Occupation'),
                profile.occupation,
              ),
              _row(
                context,
                _t(context, 'جهة العمل', 'Employer'),
                profile.employer,
              ),
            ],
          ),
          const SizedBox(height: AqariSpacing.section),
          _collection(
            context,
            title: _t(context, 'أفراد العائلة', 'Family members'),
            count: profile.familyMembers.length,
            icon: Icons.people_outline,
            empty: _t(
              context,
              'لا يوجد أفراد عائلة مسجلون.',
              'No family members are registered.',
            ),
            children: profile.familyMembers
                .map(
                  (item) => AqariListRow(
                    title: item.name,
                    supportingText: [item.relationshipType, item.ageBracket]
                        .whereType<String>()
                        .where((v) => v.isNotEmpty)
                        .join(' • '),
                    leading: const Icon(Icons.person_outline),
                    showChevron: false,
                  ),
                )
                .toList(),
          ),
          const SizedBox(height: AqariSpacing.section),
          _collection(
            context,
            title: _t(context, 'جهات اتصال الطوارئ', 'Emergency contacts'),
            count: profile.emergencyContacts.length,
            icon: Icons.contact_phone_outlined,
            empty: _t(
              context,
              'لا توجد جهات اتصال للطوارئ.',
              'No emergency contacts are registered.',
            ),
            children: profile.emergencyContacts
                .map(
                  (item) => AqariListRow(
                    title: item.name,
                    supportingText: '${item.relationshipType} • ${item.phone}',
                    leading: const Icon(Icons.contact_phone_outlined),
                    showChevron: false,
                  ),
                )
                .toList(),
          ),
          const SizedBox(height: AqariSpacing.section),
          _collection(
            context,
            title: _t(context, 'المركبات', 'Vehicles'),
            count: profile.vehicles.length,
            icon: Icons.directions_car_outlined,
            empty: _t(
              context,
              'لا توجد مركبات مسجلة.',
              'No vehicles are registered.',
            ),
            children: profile.vehicles
                .map(
                  (item) => AqariListRow(
                    title: item.plateNumber,
                    supportingText: '${item.makeModel} • ${item.color}',
                    leading: const Icon(Icons.directions_car_outlined),
                    showChevron: false,
                  ),
                )
                .toList(),
          ),
          const SizedBox(height: AqariSpacing.x8),
        ],
      ),
    );
  }

  Widget _section(BuildContext context, String title, List<Widget> children) =>
      Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AqariSectionHeader(title: title),
          AqariCard(child: Column(children: children)),
        ],
      );

  Widget _collection(
    BuildContext context, {
    required String title,
    required int count,
    required IconData icon,
    required String empty,
    required List<Widget> children,
  }) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      AqariSectionHeader(title: '$title ($count)'),
      AqariCard(
        padding: children.isEmpty
            ? const EdgeInsets.all(AqariSpacing.x5)
            : EdgeInsets.zero,
        child: children.isEmpty
            ? Column(
                children: [
                  Icon(icon, color: context.aqariColors.textMuted),
                  const SizedBox(height: AqariSpacing.x2),
                  Text(empty, textAlign: TextAlign.center),
                ],
              )
            : Column(children: children),
      ),
    ],
  );

  Widget _row(
    BuildContext context,
    String label,
    String? value, {
    bool ltr = false,
  }) => AqariDetailRow(
    label: label,
    value: value == null || value.trim().isEmpty ? '—' : value,
    ltr: ltr,
  );
}

String _t(BuildContext context, String ar, String en) =>
    context.isArabic ? ar : en;
String _message(BuildContext context, Object error) => error is ApiProblem
    ? error.message
    : _t(context, 'حدث خطأ غير متوقع.', 'An unexpected error occurred.');
