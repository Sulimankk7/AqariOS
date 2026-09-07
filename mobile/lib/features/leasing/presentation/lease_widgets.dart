import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../domain/lease_errors.dart';
import 'lease_strings.dart';

class LeaseFailure extends StatelessWidget {
  const LeaseFailure(this.error, {this.retry, super.key});
  final Object? error;
  final VoidCallback? retry;
  @override
  Widget build(BuildContext context) => AqariErrorState(
    title: lt(context, 'failure'),
    message: leaseError(error, context.isArabic),
    onRetry: retry,
    retryLabel: lt(context, 'retry'),
  );
}

class LeaseFact extends StatelessWidget {
  const LeaseFact(this.label, this.value, {this.ltr = false, super.key});
  final String label, value;
  final bool ltr;
  @override
  Widget build(BuildContext context) =>
      AqariDetailRow(label: label, value: value, ltr: ltr);
}

class LeaseBadge extends StatelessWidget {
  const LeaseBadge(this.status, {super.key});
  final int status;
  @override
  Widget build(BuildContext context) => AqariStatusBadge(
    label: le(context, 'status', status),
    variant: switch (status) {
      2 => AqariStatusVariant.brand,
      3 || 5 => AqariStatusVariant.error,
      _ => AqariStatusVariant.neutral,
    },
  );
}

class LeaseLoad<T> extends StatefulWidget {
  const LeaseLoad({
    required this.load,
    required this.builder,
    this.standaloneTitle,
    super.key,
  });
  final String? standaloneTitle;
  final Future<T> Function() load;
  final Widget Function(T, Future<void> Function()) builder;
  @override
  State<LeaseLoad<T>> createState() => _LeaseLoadState<T>();
}

class _LeaseLoadState<T> extends State<LeaseLoad<T>> {
  T? _data;
  Object? _error;
  bool _loading = true;
  int _request = 0;
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final request = ++_request;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await widget.load();
      if (mounted && request == _request) setState(() => _data = result);
    } catch (e) {
      if (mounted && request == _request) setState(() => _error = e);
    } finally {
      if (mounted && request == _request) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading || _error != null) {
      final body = _loading
          ? const LeaseSkeleton()
          : SingleChildScrollView(child: LeaseFailure(_error, retry: _load));
      return widget.standaloneTitle == null
          ? body
          : Scaffold(
              appBar: AqariAppBar(
                title: widget.standaloneTitle!,
                onBack: () => Navigator.maybePop(context),
              ),
              body: SafeArea(child: body),
            );
    }
    return widget.builder(_data as T, _load);
  }
}

class LeaseSkeleton extends StatelessWidget {
  const LeaseSkeleton({super.key});
  @override
  Widget build(BuildContext context) => Semantics(
    label: lt(context, 'loading'),
    child: ListView(
      padding: const EdgeInsets.all(AqariSpacing.x4),
      children: List.generate(
        4,
        (_) => const Padding(
          padding: EdgeInsets.only(bottom: AqariSpacing.x4),
          child: AqariSkeleton(height: 64),
        ),
      ),
    ),
  );
}

Future<bool> leaseConfirm(
  BuildContext context, {
  required String title,
  required String message,
  required Future<void> Function() action,
  bool destructive = false,
}) async =>
    await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) => _LeaseConfirmation(
        title: title,
        message: message,
        action: action,
        destructive: destructive,
      ),
    ) ??
    false;

class _LeaseConfirmation extends StatefulWidget {
  const _LeaseConfirmation({
    required this.title,
    required this.message,
    required this.action,
    required this.destructive,
  });
  final String title, message;
  final Future<void> Function() action;
  final bool destructive;
  @override
  State<_LeaseConfirmation> createState() => _LeaseConfirmationState();
}

class _LeaseConfirmationState extends State<_LeaseConfirmation> {
  bool _busy = false;
  Object? _error;
  Future<void> _submit() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.action();
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !_busy,
    child: AlertDialog(
      title: Text(widget.title),
      scrollable: true,
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(widget.message),
          if (_error != null) LeaseFailure(_error),
        ],
      ),
      actions: [
        AqariButton(
          label: lt(context, 'cancel'),
          variant: AqariButtonVariant.text,
          onPressed: _busy ? null : () => Navigator.pop(context, false),
        ),
        AqariButton(
          label: widget.title,
          loading: _busy,
          variant: widget.destructive
              ? AqariButtonVariant.destructive
              : AqariButtonVariant.primary,
          onPressed: _busy ? null : _submit,
        ),
      ],
    ),
  );
}
