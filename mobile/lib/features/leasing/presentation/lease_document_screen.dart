import 'package:flutter/material.dart';
import 'package:file_selector/file_selector.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/lease_models.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';

class LeaseDocumentScreen extends StatefulWidget {
  const LeaseDocumentScreen({
    required this.scope,
    required this.contractId,
    this.document,
    super.key,
  });
  final LeaseScope scope;
  final String contractId;
  final LeaseDocument? document;
  @override
  State<LeaseDocumentScreen> createState() => _LeaseDocumentScreenState();
}

class _LeaseDocumentScreenState extends State<LeaseDocumentScreen> {
  final _description = TextEditingController();
  XFile? _file;
  String? _confirmedFile;
  int _type = 0;
  Object? _error;
  bool _busy = false;
  String _phase = 'preparing';
  double? _progress;
  @override
  void dispose() {
    _description.dispose();
    super.dispose();
  }

  Future<void> _select() async {
    try {
      final file = await widget.scope.files.select();
      if (file == null) return;
      await widget.scope.files.validate(file);
      if (mounted) {
        setState(() {
          _file = file;
          _confirmedFile = null;
          _error = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _error = e);
    }
  }

  Future<void> _save() async {
    if (_file == null || _busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      _confirmedFile ??= await widget.scope.files.upload(
        widget.contractId,
        _file!,
        (phase, value) {
          if (mounted) {
            setState(() {
              _phase = phase;
              _progress = value;
            });
          }
        },
      );
      if (!mounted) return;
      setState(() {
        _phase = 'attaching';
        _progress = null;
      });
      final document = widget.document;
      if (document == null) {
        await widget.scope.repository.attach(widget.contractId, {
          'fileId': _confirmedFile,
          'documentType': _type,
          'description': _description.text.isEmpty ? null : _description.text,
        });
      } else {
        await widget.scope.repository.replace(widget.contractId, document.id, {
          'newFileId': _confirmedFile,
          'description': _description.text.trim().isEmpty
              ? document.description
              : _description.text.trim(),
        });
      }
      if (mounted) {
        AqariSnackbar.show(context, lt(context, 'documentSaved'));
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !_busy,
    child: Scaffold(
      appBar: AqariAppBar(
        title: lt(context, widget.document == null ? 'attach' : 'replace'),
        onBack: _busy ? null : () => Navigator.maybePop(context),
      ),
      bottomNavigationBar: !widget.scope.user.hasPermission('contracts.create')
          ? null
          : AqariActionBar(
              child: AqariButton(
                label: lt(
                  context,
                  widget.document == null ? 'attach' : 'replace',
                ),
                loading: _busy,
                expanded: true,
                onPressed: _busy || _file == null ? null : _save,
              ),
            ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AqariSpacing.x4),
          children: [
            if (!widget.scope.user.hasPermission('contracts.create'))
              Text(lt(context, 'permission'))
            else ...[
              AqariFormSection(
                title: lt(context, 'description'),
                children: [
                  if (widget.document != null)
                    LeaseFact(
                      lt(context, 'file'),
                      widget.document!.filename ??
                          le(context, 'document', widget.document!.type),
                    ),
                  if (widget.document == null)
                    AqariSelect<int>(
                      label: lt(context, 'documentType'),
                      valueLabel: le(context, 'document', _type),
                      items: const [0, 1, 2, 3, 4],
                      itemLabel: (v) => le(context, 'document', v),
                      enabled: !_busy,
                      onChanged: (v) => setState(() => _type = v),
                    ),
                  const SizedBox(height: AqariSpacing.x4),
                  AqariTextField(
                    controller: _description,
                    label: lt(context, 'description'),
                    hint: widget.document?.description,
                    enabled: !_busy,
                    maxLines: 3,
                  ),
                ],
              ),
              AqariFormSection(
                title: lt(context, 'file'),
                children: [
                  const SizedBox(height: AqariSpacing.x4),
                  Text(lt(context, 'fileSupport')),
                  if (_file != null)
                    AqariListRow(
                      title: _file!.name,
                      trailing: AqariIconButton(
                        icon: Icons.close,
                        semanticLabel: lt(context, 'removeFile'),
                        onPressed: _busy
                            ? null
                            : () => setState(() {
                                _file = null;
                                _confirmedFile = null;
                              }),
                      ),
                    ),
                  AqariButton(
                    label: lt(context, 'selectFile'),
                    variant: AqariButtonVariant.outlined,
                    icon: Icons.attach_file,
                    onPressed: _busy ? null : _select,
                  ),
                ],
              ),
              if (_busy)
                Padding(
                  padding: const EdgeInsets.symmetric(
                    vertical: AqariSpacing.x4,
                  ),
                  child: Semantics(
                    liveRegion: true,
                    child: Column(
                      children: [
                        Text(
                          '${lt(context, _phase)}${_progress == null ? '' : ' ${(_progress! * 100).round()}%'}',
                        ),
                        LinearProgressIndicator(value: _progress),
                      ],
                    ),
                  ),
                ),
              if (_error != null) LeaseFailure(_error),
            ],
          ],
        ),
      ),
    ),
  );
}
