import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../../properties/presentation/apartment_details_screen.dart';
import '../domain/lease_behavior.dart';
import '../domain/lease_models.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';
import 'lease_form_screen.dart';
import 'lease_document_screen.dart';
import 'lease_list_screen.dart';
import 'lease_utilities.dart';
import 'lease_parking.dart';

class LeaseDetailScreen extends StatefulWidget {
  const LeaseDetailScreen({required this.scope, required this.id, super.key});
  final LeaseScope scope;
  final String id;
  @override
  State<LeaseDetailScreen> createState() => _LeaseDetailScreenState();
}

class _LeaseDetailScreenState extends State<LeaseDetailScreen> {
  Object? _documentError;
  String? _opening;
  Future<void> _action(
    Lease lease,
    LeaseAction action,
    Future<void> Function() reload,
  ) async {
    if (action == LeaseAction.activate) {
      final changed = await leaseConfirm(
        context,
        title: lt(context, 'activate'),
        message: '${lease.number}\n${lt(context, 'activateConfirm')}',
        action: () => widget.scope.repository.activate(lease.id),
      );
      if (changed && mounted) {
        widget.scope.properties.clear();
        AqariSnackbar.show(context, lt(context, 'saved'));
        await reload();
      }
      return;
    }
    await Navigator.push(
      context,
      MaterialPageRoute<void>(
        builder: (_) => action == LeaseAction.attach
            ? LeaseDocumentScreen(scope: widget.scope, contractId: lease.id)
            : LeaseFormScreen(
                scope: widget.scope,
                lease: lease,
                mode: switch (action) {
                  LeaseAction.edit => LeaseFormMode.edit,
                  LeaseAction.renew => LeaseFormMode.renew,
                  _ => LeaseFormMode.terminate,
                },
              ),
      ),
    );
    if (mounted) await reload();
  }

  Future<void> _document(
    Lease lease,
    LeaseDocument doc,
    String action,
    Future<void> Function() reload,
  ) async {
    setState(() => _documentError = null);
    if (action == 'replace') {
      await Navigator.push(
        context,
        MaterialPageRoute<void>(
          builder: (_) => LeaseDocumentScreen(
            scope: widget.scope,
            contractId: lease.id,
            document: doc,
          ),
        ),
      );
      if (mounted) await reload();
    } else if (action == 'delete') {
      final changed = await leaseConfirm(
        context,
        title: lt(context, 'delete'),
        message:
            '${doc.filename ?? le(context, 'document', doc.type)}\n${lt(context, 'deleteConfirm')}',
        destructive: true,
        action: () => widget.scope.repository.deleteDocument(lease.id, doc.id),
      );
      if (changed && mounted) {
        AqariSnackbar.show(context, lt(context, 'documentSaved'));
        await reload();
      }
    } else {
      if (_opening != null) return;
      setState(() => _opening = doc.id);
      try {
        final uri = await widget.scope.repository.documentUrl(
          lease.id,
          doc.id,
          inline: action == 'viewDoc',
        );
        if (mounted) await widget.scope.files.open(uri);
      } catch (e) {
        if (mounted) setState(() => _documentError = e);
      } finally {
        if (mounted) setState(() => _opening = null);
      }
    }
  }

  Widget _section(String key) => Padding(
    padding: const EdgeInsets.symmetric(vertical: AqariSpacing.x4),
    child: AqariSectionHeader(title: lt(context, key)),
  );
  Future<void> _push(Widget screen) =>
      Navigator.push<void>(context, MaterialPageRoute(builder: (_) => screen));
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: lt(context, 'view'),
      onBack: () => Navigator.maybePop(context),
    ),
    body: LeaseLoad<Lease>(
      key: ValueKey(widget.id),
      load: () => widget.scope.repository.detail(widget.id),
      builder: (lease, reload) {
        final termination = lease.termination;
        return SafeArea(
          child: RefreshIndicator(
            onRefresh: reload,
            child: ListView(
              padding: const EdgeInsets.all(AqariSpacing.x4),
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SelectableText(
                            lease.number,
                            style: Theme.of(context).textTheme.headlineSmall,
                          ),
                          const SizedBox(height: AqariSpacing.x2),
                          LeaseBadge(lease.status),
                        ],
                      ),
                    ),
                    if (leaseActions(
                      lease,
                      widget.scope.user,
                    ).any((a) => a != LeaseAction.view))
                      AqariPopupMenu<LeaseAction>(
                        label: lt(context, 'actions'),
                        items: leaseActions(lease, widget.scope.user)
                            .where((a) => a != LeaseAction.view)
                            .map(
                              (action) => AqariPopupMenuAction(
                                value: action,
                                label: lt(context, action.name),
                              ),
                            )
                            .toList(),
                        onSelected: (action) => _action(lease, action, reload),
                      ),
                  ],
                ),
                const SizedBox(height: AqariSpacing.x4),
                AqariSectionSurface(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        lt(context, 'rent'),
                        style: Theme.of(context).textTheme.labelMedium,
                      ),
                      const SizedBox(height: AqariSpacing.x1),
                      AqariLtrContent(
                        child: Text(
                          '${webNumber(lease.rent)} ${lease.currency}',
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                      ),
                      const SizedBox(height: AqariSpacing.x1),
                      Text(
                        le(context, 'frequency', lease.frequency),
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                _section('terms'),
                LeaseFact(lt(context, 'startDate'), lease.start, ltr: true),
                LeaseFact(lt(context, 'endDate'), lease.end, ltr: true),
                if (lease.signed != null)
                  LeaseFact(lt(context, 'signed'), lease.signed!, ltr: true),
                const AqariDivider(),
                _section('financial'),
                LeaseFact(
                  lt(context, 'rent'),
                  '${webNumber(lease.rent)} ${lease.currency}',
                  ltr: true,
                ),
                LeaseFact(
                  lt(context, 'deposit'),
                  '${webNumber(lease.deposit)} ${lease.currency}',
                  ltr: true,
                ),
                LeaseFact(
                  lt(context, 'paymentFrequency'),
                  le(context, 'frequency', lease.frequency),
                ),
                LeaseFact(
                  lt(context, 'paymentDueDay'),
                  '${lease.dueDay}',
                  ltr: true,
                ),
                LeaseFact(
                  lt(context, 'legalRegime'),
                  le(context, 'regime', lease.regime),
                ),
                LeaseFact(
                  lt(context, 'tenantType'),
                  le(context, 'tenantType', lease.tenantType),
                ),
                if (lease.notes?.isNotEmpty == true)
                  LeaseFact(lt(context, 'notes'), lease.notes!),
                const AqariDivider(),
                _section('related'),
                AqariListRow(
                  title: lt(context, 'viewApartment'),
                  showChevron: true,
                  onPressed: () => _push(
                    ApartmentDetailsScreen(
                      repository: widget.scope.properties,
                      user: widget.scope.user,
                      id: lease.apartmentId,
                    ),
                  ),
                ),
                AqariListRow(
                  title: lt(context, 'viewTenants'),
                  supportingText: lt(context, 'tenantDependency'),
                  onPressed: () => AqariDialog.show<void>(
                    context: context,
                    title: lt(context, 'viewTenants'),
                    content: Text(lt(context, 'tenantDependency')),
                    secondaryLabel: lt(context, 'cancel'),
                  ),
                ),
                if (lease.priorId != null)
                  AqariListRow(
                    title: lt(context, 'prior'),
                    supportingText: lease.priorId,
                    showChevron: true,
                    onPressed: () => _push(
                      LeaseDetailScreen(
                        scope: widget.scope,
                        id: lease.priorId!,
                      ),
                    ),
                  ),
                AqariListRow(
                  title: lt(context, 'apartmentHistory'),
                  showChevron: true,
                  onPressed: () => _push(
                    LeaseListScreen(
                      scope: widget.scope,
                      apartmentId: lease.apartmentId,
                    ),
                  ),
                ),
                if (termination != null) ...[
                  _section('termination'),
                  LeaseFact(
                    lt(context, 'terminationType'),
                    le(context, 'termination', termination.type),
                  ),
                  LeaseFact(
                    lt(context, 'terminationDate'),
                    termination.date,
                    ltr: true,
                  ),
                  LeaseFact(
                    lt(context, 'outstandingBalance'),
                    '${webNumber(termination.balance)} ${termination.currency}',
                    ltr: true,
                  ),
                  LeaseFact(
                    lt(context, 'depositReturnedAmount'),
                    '${webNumber(termination.returned)} ${termination.currency}',
                    ltr: true,
                  ),
                  LeaseFact(
                    lt(context, 'depositDeductionAmount'),
                    '${webNumber(termination.deducted)} ${termination.currency}',
                    ltr: true,
                  ),
                  LeaseFact(
                    lt(context, 'finalUtilitySettlementCompleted'),
                    lt(context, termination.settled ? 'yes' : 'no'),
                  ),
                  if (termination.reason?.isNotEmpty == true)
                    LeaseFact(lt(context, 'reason'), termination.reason!),
                ],
                if (widget.scope.user.hasPermission('UtilityBills.Manage'))
                  Padding(
                    padding: const EdgeInsets.symmetric(
                      vertical: AqariSpacing.x4,
                    ),
                    child: LeaseUtilities(
                      scope: widget.scope,
                      leaseId: lease.id,
                    ),
                  ),
                _section('parking'),
                LeaseParking(scope: widget.scope, leaseId: lease.id),
                _section('documents'),
                if (_documentError != null) LeaseFailure(_documentError),
                if (lease.documents.isEmpty) Text(lt(context, 'noDocuments')),
                for (final doc in lease.documents)
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      AqariListRow(
                        title: le(context, 'document', doc.type),
                        supportingText: doc.filename,
                        metadata:
                            '${leaseInline('${(doc.size / (1024 * 1024)).toStringAsFixed(2)} MB')} · ${leaseDateLabel(context, doc.created)}',
                        leading: Icon(
                          Icons.description_outlined,
                          color: context.aqariColors.primary,
                        ),
                        trailing:
                            !widget.scope.user.hasPermission('contracts.create')
                            ? null
                            : IgnorePointer(
                                ignoring: _opening != null,
                                child: AqariPopupMenu<String>(
                                  label:
                                      '${lt(context, 'recordActions')}: ${doc.filename ?? le(context, 'document', doc.type)}',
                                  items: [
                                    for (final action in [
                                      'viewDoc',
                                      'download',
                                      if (lease.status != 5) ...[
                                        'replace',
                                        'delete',
                                      ],
                                    ])
                                      AqariPopupMenuAction(
                                        value: action,
                                        label: lt(context, action),
                                      ),
                                  ],
                                  onSelected: (action) =>
                                      _document(lease, doc, action, reload),
                                ),
                              ),
                      ),
                      if (doc.description?.isNotEmpty == true)
                        Text(doc.description!),
                      if (_opening == doc.id)
                        AqariLoadingState(label: lt(context, 'loading')),
                      const AqariDivider(),
                    ],
                  ),
                _section('history'),
                if (lease.history.isEmpty) Text(lt(context, 'noHistory')),
                for (final event in lease.history)
                  Padding(
                    padding: const EdgeInsets.symmetric(
                      vertical: AqariSpacing.x3,
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        LeaseBadge(event.to),
                        const SizedBox(height: AqariSpacing.x1),
                        Text(
                          leaseDateLabel(context, event.at, time: true),
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                        if (event.reason?.isNotEmpty == true)
                          SelectableText(event.reason!),
                      ],
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    ),
  );
}
