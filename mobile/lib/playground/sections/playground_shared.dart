import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';

class PlaygroundSection extends StatelessWidget {
  const PlaygroundSection({
    required this.title,
    required this.description,
    required this.child,
    super.key,
  });
  final String title, description;
  final Widget child;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsetsDirectional.only(bottom: AqariSpacing.x8),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(title, style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: AqariSpacing.x1),
        Text(
          description,
          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
            color: context.aqariColors.textSecondary,
          ),
        ),
        const SizedBox(height: AqariSpacing.x4),
        child,
      ],
    ),
  );
}

class PlaygroundWrap extends StatelessWidget {
  const PlaygroundWrap({required this.children, super.key});
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Wrap(
    spacing: AqariSpacing.x3,
    runSpacing: AqariSpacing.x3,
    children: children,
  );
}

class LabeledExample extends StatelessWidget {
  const LabeledExample({
    required this.label,
    required this.child,
    this.width = 220,
    super.key,
  });
  final String label;
  final Widget child;
  final double width;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: width,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          label,
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
            color: context.aqariColors.textMuted,
          ),
        ),
        const SizedBox(height: AqariSpacing.x2),
        child,
      ],
    ),
  );
}
