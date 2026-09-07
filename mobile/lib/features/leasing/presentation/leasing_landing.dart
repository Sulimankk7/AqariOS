import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import 'lease_list_screen.dart';
import 'lease_scope.dart';
import 'lease_strings.dart';
import 'tenant_management_screen.dart';

const leasingNavigationLabels = ['عقود الإيجار', 'المستأجرون'];

class LeasingLanding extends StatefulWidget {
  const LeasingLanding({required this.scope, super.key});

  final LeaseScope scope;

  @override
  State<LeasingLanding> createState() => _LeasingLandingState();
}

class _LeasingLandingState extends State<LeasingLanding> {
  int _selected = 0;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Padding(
        padding: const EdgeInsetsDirectional.fromSTEB(
          AqariSpacing.x4,
          AqariSpacing.x3,
          AqariSpacing.x4,
          0,
        ),
        child: AqariTabs(
          labels: [lt(context, 'contracts'), lt(context, 'tenants')],
          selectedIndex: _selected,
          onSelected: (value) => setState(() => _selected = value),
        ),
      ),
      Expanded(
        child: IndexedStack(
          index: _selected,
          children: [
            LeaseListScreen(scope: widget.scope),
            TenantManagementScreen(scope: widget.scope),
          ],
        ),
      ),
    ],
  );
}
