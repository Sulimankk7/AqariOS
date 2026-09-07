import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../../auth/domain/user_profile.dart';
import '../data/properties_repository.dart';
import '../domain/property_models.dart';
import 'property_editor_screen.dart';
import 'property_widgets.dart';

class PropertyActions extends StatefulWidget {
  const PropertyActions({
    required this.repository,
    required this.user,
    required this.kind,
    required this.record,
    required this.reload,
    super.key,
  });
  final PropertiesRepository repository;
  final UserProfile user;
  final PropertyKind kind;
  final PropertyRecord record;
  final Future<void> Function() reload;
  @override
  State<PropertyActions> createState() => _PropertyActionsState();
}

class _PropertyActionsState extends State<PropertyActions> {
  bool _busy = false;
  ApiProblem? _error;
  Future<void> _edit() async {
    final saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => PropertyEditorScreen(
          repository: widget.repository,
          user: widget.user,
          kind: widget.kind,
          record: widget.record,
        ),
      ),
    );
    if (mounted && saved == true) await widget.reload();
  }

  Future<void> _archive() async {
    final confirmed = await AqariDialog.show<bool>(
      context: context,
      title: tr(context, 'أرشفة السجل؟', 'Archive this record?'),
      content: Builder(
        builder: (dialogContext) => Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(widget.record.title),
            const SizedBox(height: AqariSpacing.x3),
            Text(
              tr(
                context,
                'قد تمنع السجلات المرتبطة الأرشفة. سيطبق الخادم قواعد الأرشفة الحالية.',
                'Linked records may prevent archiving. The server will apply its existing archive rules.',
              ),
            ),
            const SizedBox(height: AqariSpacing.x4),
            Wrap(
              spacing: AqariSpacing.x2,
              runSpacing: AqariSpacing.x2,
              children: [
                AqariButton(
                  label: tr(context, 'إلغاء', 'Cancel'),
                  variant: AqariButtonVariant.text,
                  onPressed: () => Navigator.pop(dialogContext, false),
                ),
                AqariButton(
                  label: tr(context, 'أرشفة', 'Archive'),
                  variant: AqariButtonVariant.destructive,
                  onPressed: () => Navigator.pop(dialogContext, true),
                ),
              ],
            ),
          ],
        ),
      ),
    );
    if (confirmed != true || !mounted) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.repository.archive(widget.kind, widget.record.id);
      if (mounted) {
        AqariSnackbar.show(
          context,
          tr(context, 'تمت الأرشفة', 'Archived'),
          kind: AqariStatusKind.success,
        );
        Navigator.pop(context);
      }
    } on ApiProblem catch (error) {
      if (mounted) setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      if (_error != null) PropertyFailure(_error!),
      const SizedBox(height: AqariSpacing.x4),
      Row(
        children: [
          if (propertyPermission(widget.user, 'update'))
            Expanded(
              child: AqariButton(
                label: tr(context, 'تعديل', 'Edit'),
                icon: Icons.edit_outlined,
                variant: AqariButtonVariant.outlined,
                onPressed: _busy ? null : _edit,
              ),
            ),
          if (propertyPermission(widget.user, 'delete')) ...[
            if (propertyPermission(widget.user, 'update'))
              const SizedBox(width: AqariSpacing.x2),
            if (_busy)
              AqariButton(label: tr(context, 'أرشفة', 'Archive'), loading: true)
            else
              AqariPopupMenu<String>(
                label: tr(context, 'إجراءات السجل', 'Record actions'),
                items: [
                  AqariPopupMenuAction(
                    value: 'archive',
                    label: tr(context, 'أرشفة', 'Archive'),
                    icon: Icons.archive_outlined,
                  ),
                ],
                onSelected: (_) => _archive(),
              ),
          ],
        ],
      ),
    ],
  );
}
