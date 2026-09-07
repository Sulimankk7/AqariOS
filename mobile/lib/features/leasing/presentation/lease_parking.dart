import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../domain/lease_models.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'lease_widgets.dart';

class LeaseParking extends StatefulWidget {
  const LeaseParking({required this.scope, required this.leaseId, super.key});

  final LeaseScope scope;
  final String leaseId;

  @override
  State<LeaseParking> createState() => _LeaseParkingState();
}

class _LeaseParkingState extends State<LeaseParking> {
  List<LeaseParkingSpot>? _spots;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _spots = null;
      _error = null;
    });
    try {
      final spots = await widget.scope.repository.parking(widget.leaseId);
      if (mounted) setState(() => _spots = spots);
    } catch (error) {
      if (mounted) setState(() => _error = error);
    }
  }

  String _type(String value) => switch (value.toLowerCase()) {
    'standard' => context.isArabic ? 'موقف عادي' : 'Standard parking',
    'covered' => context.isArabic ? 'موقف مسقوف' : 'Covered parking',
    'visitor' => context.isArabic ? 'موقف زوار' : 'Visitor parking',
    'disabledaccess' =>
      context.isArabic ? 'ذوي احتياجات خاصة' : 'Disabled access',
    _ => value,
  };

  @override
  Widget build(BuildContext context) {
    if (_error != null) return LeaseFailure(_error, retry: _load);
    final spots = _spots;
    if (spots == null) {
      return AqariLoadingState(label: lt(context, 'loading'));
    }
    if (spots.isEmpty) return Text(lt(context, 'noParking'));
    return Column(
      children: [
        for (final spot in spots)
          AqariListRow(title: spot.code, supportingText: _type(spot.type)),
      ],
    );
  }
}
