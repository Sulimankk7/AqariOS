import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/lease_models.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';

class LeaseUtilities extends StatefulWidget {
  const LeaseUtilities({required this.scope, required this.leaseId, super.key});
  final LeaseScope scope;
  final String leaseId;
  @override
  State<LeaseUtilities> createState() => _LeaseUtilitiesState();
}

class _LeaseUtilitiesState extends State<LeaseUtilities> {
  List<UtilityAccount> _accounts = [];
  UtilityPage? _page;
  bool _loading = true, _busy = false;
  Object? _error;
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({bool more = false}) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final page = await widget.scope.repository.accounts(
        widget.leaseId,
        cursor: more ? _page?.nextCursor : null,
      );
      if (mounted) {
        setState(() {
          _accounts = more ? [..._accounts, ...page.items] : page.items;
          _page = page;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _edit(int type, [UtilityAccount? account]) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => _UtilityForm(
          scope: widget.scope,
          leaseId: widget.leaseId,
          type: type,
          account: account,
        ),
      ),
    );
    if (changed == true && mounted) _load();
  }

  Future<void> _sync(UtilityAccount a) async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.scope.repository.syncUtility(a.id);
      if (mounted) {
        AqariSnackbar.show(context, lt(context, 'syncQueued'));
        await _load();
      }
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _unlink(UtilityAccount a) async {
    final changed = await leaseConfirm(
      context,
      title: lt(context, 'unlink'),
      message: '${a.number}\n${lt(context, 'unlinkConfirm')}',
      destructive: true,
      action: () => widget.scope.repository.unlinkUtility(a.id),
    );
    if (changed && mounted) {
      AqariSnackbar.show(context, lt(context, 'saved'));
      _load();
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      AqariSectionHeader(
        title: lt(context, 'utilities'),
        actionLabel: lt(context, 'retry'),
        onAction: _loading ? null : _load,
      ),
      if (_loading) const AqariSkeleton(height: 80),
      if (_error != null) LeaseFailure(_error, retry: _load),
      if (!_loading && _error == null)
        for (final type in [0, 1]) _typeSection(type),
      if (_page?.hasMore == true && _page?.nextCursor != null)
        AqariButton(
          label: lt(context, 'next'),
          variant: AqariButtonVariant.text,
          onPressed: _loading ? null : () => _load(more: true),
        ),
    ],
  );
  Widget _typeSection(int type) {
    final matching = _accounts.where((a) => a.type == type);
    final current = matching.where((a) => a.unlinkedAt == null).firstOrNull;
    final previous = matching
        .where((a) => a.unlinkedAt != null)
        .toList(growable: false);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AqariSpacing.x3),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AqariSectionHeader(
            title: lt(context, type == 0 ? 'electricity' : 'water'),
          ),
          if (current == null)
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: AqariButton(
                label: lt(context, 'link'),
                onPressed: _busy ? null : () => _edit(type),
              ),
            )
          else ...[
            LeaseFact(lt(context, 'number'), current.number, ltr: true),
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: AqariStatusBadge(
                label: le(context, 'sync', current.syncStatus),
                variant: current.syncStatus == 2
                    ? AqariStatusVariant.success
                    : current.syncStatus >= 3
                    ? AqariStatusVariant.warning
                    : AqariStatusVariant.neutral,
              ),
            ),
            const SizedBox(height: AqariSpacing.x2),
            Wrap(
              spacing: AqariSpacing.x2,
              runSpacing: AqariSpacing.x2,
              children: [
                AqariButton(
                  label: lt(context, 'replace'),
                  variant: AqariButtonVariant.outlined,
                  onPressed: _busy ? null : () => _edit(type, current),
                ),
                AqariButton(
                  label: lt(context, 'sync'),
                  variant: AqariButtonVariant.outlined,
                  onPressed: _busy || current.syncStatus == 1
                      ? null
                      : () => _sync(current),
                ),
                AqariButton(
                  label: lt(context, 'unlink'),
                  variant: AqariButtonVariant.destructive,
                  onPressed: _busy ? null : () => _unlink(current),
                ),
              ],
            ),
          ],
          if (previous.isNotEmpty)
            ExpansionTile(
              tilePadding: EdgeInsets.zero,
              title: Text(
                '${lt(context, 'previousAccounts')} (${previous.length})',
              ),
              children: [
                for (final account in previous)
                  AqariListRow(
                    title: account.number,
                    supportingText: account.unlinkedAt,
                  ),
              ],
            ),
          const SizedBox(height: AqariSpacing.x3),
          const AqariDivider(),
        ],
      ),
    );
  }
}

class _UtilityForm extends StatefulWidget {
  const _UtilityForm({
    required this.scope,
    required this.leaseId,
    required this.type,
    this.account,
  });
  final LeaseScope scope;
  final String leaseId;
  final int type;
  final UtilityAccount? account;
  @override
  State<_UtilityForm> createState() => _UtilityFormState();
}

class _UtilityFormState extends State<_UtilityForm> {
  late int _type = widget.type;
  late final _number = TextEditingController(
    text: widget.account?.number ?? '',
  );
  late final _meter = TextEditingController(text: widget.account?.meter ?? '');
  bool _busy = false, _invalid = false;
  Object? _error;
  @override
  void dispose() {
    _number.dispose();
    _meter.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final number = _number.text.trim();
    if (!RegExp(_type == 0 ? r'^\d{10}$' : r'^\d{1,20}$').hasMatch(number)) {
      setState(() => _invalid = true);
      return;
    }
    setState(() {
      _busy = true;
      _invalid = false;
      _error = null;
    });
    try {
      final body = <String, dynamic>{
        'accountNumber': number,
        'meterNumber': _meter.text.trim().isEmpty ? null : _meter.text.trim(),
      };
      if (widget.account == null) {
        await widget.scope.repository.linkUtility({
          ...body,
          'leaseContractId': widget.leaseId,
          'utilityType': _type,
        });
      } else {
        await widget.scope.repository.replaceUtility(widget.account!.id, body);
      }
      if (mounted) {
        AqariSnackbar.show(context, lt(context, 'saved'));
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
        title: lt(context, widget.account == null ? 'link' : 'replace'),
        onBack: _busy ? null : () => Navigator.maybePop(context),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AqariSpacing.x4),
          children: [
            if (widget.account == null)
              AqariSelect<int>(
                label: lt(context, 'utilities'),
                valueLabel: lt(context, _type == 0 ? 'electricity' : 'water'),
                items: const [0, 1],
                itemLabel: (v) => lt(context, v == 0 ? 'electricity' : 'water'),
                enabled: !_busy,
                onChanged: (v) => setState(() => _type = v),
              ),
            const SizedBox(height: AqariSpacing.x4),
            AqariTextField(
              controller: _number,
              label: lt(context, 'number'),
              enabled: !_busy,
              keyboardType: TextInputType.number,
              textDirection: TextDirection.ltr,
              errorText: _invalid ? lt(context, 'utilityNumber') : null,
            ),
            const SizedBox(height: AqariSpacing.x4),
            AqariTextField(
              controller: _meter,
              label: lt(context, 'meter'),
              enabled: !_busy,
              textDirection: TextDirection.ltr,
            ),
            if (_error != null) LeaseFailure(_error),
            const SizedBox(height: AqariSpacing.x4),
            AqariButton(
              label: lt(context, 'save'),
              expanded: true,
              loading: _busy,
              onPressed: _busy ? null : _submit,
            ),
          ],
        ),
      ),
    ),
  );
}
