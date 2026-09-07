import 'package:flutter/material.dart';

import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_motion.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';
import '../actions/aqari_button.dart';

class AqariLoadingState extends StatelessWidget {
  const AqariLoadingState({required this.label, super.key});
  final String label;
  @override
  Widget build(BuildContext context) => Semantics(
    liveRegion: true,
    label: label,
    child: Padding(
      padding: const EdgeInsets.all(AqariSpacing.x8),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox.square(
            dimension: 24,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: context.aqariColors.primary,
            ),
          ),
          const SizedBox(height: AqariSpacing.x3),
          Text(
            label,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: context.aqariColors.textSecondary,
            ),
          ),
        ],
      ),
    ),
  );
}

class AqariSkeleton extends StatefulWidget {
  const AqariSkeleton({
    this.width = double.infinity,
    this.height = 16,
    this.radius = AqariRadius.sm,
    super.key,
  });
  final double width, height, radius;
  @override
  State<AqariSkeleton> createState() => _AqariSkeletonState();
}

class _AqariSkeletonState extends State<AqariSkeleton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final disableAnimations = MediaQuery.disableAnimationsOf(context);
    final c = context.aqariColors;
    return ExcludeSemantics(
      child: FadeTransition(
        opacity: disableAnimations
            ? const AlwaysStoppedAnimation(.7)
            : Tween(begin: .45, end: .85).animate(_controller),
        child: Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            color: c.surfaceHighest,
            borderRadius: BorderRadius.circular(widget.radius),
          ),
        ),
      ),
    );
  }
}

class AqariEmptyState extends StatelessWidget {
  const AqariEmptyState({
    required this.title,
    required this.message,
    this.actionLabel,
    this.onAction,
    super.key,
  });
  final String title, message;
  final String? actionLabel;
  final VoidCallback? onAction;
  @override
  Widget build(BuildContext context) => _StateLayout(
    icon: Icons.inbox_outlined,
    color: context.aqariColors.textSecondary,
    title: title,
    message: message,
    action: actionLabel == null
        ? null
        : AqariButton(
            label: actionLabel!,
            onPressed: onAction,
            variant: AqariButtonVariant.tonal,
          ),
  );
}

class AqariErrorState extends StatelessWidget {
  const AqariErrorState({
    required this.title,
    required this.message,
    this.retryLabel,
    this.onRetry,
    super.key,
  });
  final String title, message;
  final String? retryLabel;
  final VoidCallback? onRetry;
  @override
  Widget build(BuildContext context) => _StateLayout(
    icon: Icons.error_outline_rounded,
    color: context.aqariColors.error,
    title: title,
    message: message,
    action: retryLabel == null
        ? null
        : AqariButton(
            label: retryLabel!,
            onPressed: onRetry,
            variant: AqariButtonVariant.outlined,
          ),
  );
}

class _StateLayout extends StatelessWidget {
  const _StateLayout({
    required this.icon,
    required this.color,
    required this.title,
    required this.message,
    this.action,
  });
  final IconData icon;
  final Color color;
  final String title, message;
  final Widget? action;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(AqariSpacing.x6),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: color.withValues(alpha: .12),
            shape: BoxShape.circle,
          ),
          child: Icon(icon, color: color, size: 24),
        ),
        const SizedBox(height: AqariSpacing.x3),
        Text(
          title,
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: AqariSpacing.x1),
        Text(
          message,
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
            color: context.aqariColors.textSecondary,
          ),
        ),
        if (action != null) ...[
          const SizedBox(height: AqariSpacing.x4),
          action!,
        ],
      ],
    ),
  );
}

abstract final class AqariSnackbar {
  static void show(
    BuildContext context,
    String message, {
    AqariStatusKind kind = AqariStatusKind.info,
  }) {
    final c = context.aqariColors;
    final color = switch (kind) {
      AqariStatusKind.success => c.success,
      AqariStatusKind.error => c.error,
      AqariStatusKind.info => c.info,
    };
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        behavior: SnackBarBehavior.floating,
        backgroundColor: c.surfaceHighest,
        duration: AqariMotion.slow * 10,
        shape: RoundedRectangleBorder(
          borderRadius: AqariRadius.smBorder,
          side: BorderSide(color: color.withValues(alpha: .4)),
        ),
        content: Row(
          children: [
            Icon(Icons.info_outline_rounded, color: color, size: 20),
            const SizedBox(width: AqariSpacing.x2),
            Expanded(
              child: Text(message, style: TextStyle(color: c.textPrimary)),
            ),
          ],
        ),
      ),
    );
  }
}

enum AqariStatusKind { success, error, info }
