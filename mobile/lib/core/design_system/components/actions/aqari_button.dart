import 'package:flutter/material.dart';

import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_motion.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';

enum AqariButtonVariant { primary, tonal, outlined, text, destructive, link }

class AqariButton extends StatelessWidget {
  const AqariButton({
    required this.label,
    this.onPressed,
    this.variant = AqariButtonVariant.primary,
    this.icon,
    this.loading = false,
    this.expanded = false,
    super.key,
  });

  final String label;
  final VoidCallback? onPressed;
  final AqariButtonVariant variant;
  final IconData? icon;
  final bool loading;
  final bool expanded;

  @override
  Widget build(BuildContext context) {
    final c = context.aqariColors;
    final enabled = onPressed != null && !loading;
    final (background, foreground, border) = !enabled
        ? (c.buttonDisabled, c.textDisabled, Colors.transparent)
        : switch (variant) {
            AqariButtonVariant.primary => (
              c.buttonPrimary,
              c.buttonOnPrimary,
              Colors.transparent,
            ),
            AqariButtonVariant.tonal => (
              c.buttonSecondary,
              c.buttonOnSecondary,
              Colors.transparent,
            ),
            AqariButtonVariant.outlined => (
              c.card,
              c.actionText,
              c.buttonOutline,
            ),
            AqariButtonVariant.text || AqariButtonVariant.link => (
              Colors.transparent,
              c.actionText,
              Colors.transparent,
            ),
            AqariButtonVariant.destructive => (
              c.buttonDestructive,
              c.buttonOnDestructive,
              Colors.transparent,
            ),
          };
    final child = Semantics(
      button: true,
      enabled: enabled,
      label: label,
      child: AnimatedContainer(
        duration: AqariMotion.normal,
        constraints: const BoxConstraints(
          minHeight: 48,
          minWidth: AqariSpacing.x12,
        ),
        padding: const EdgeInsetsDirectional.symmetric(
          horizontal: AqariSpacing.x5,
        ),
        decoration: BoxDecoration(
          color: background,
          borderRadius: AqariRadius.fullBorder,
          border: Border.all(color: border),
        ),
        child: Row(
          mainAxisSize: expanded ? MainAxisSize.max : MainAxisSize.min,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            if (loading)
              SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  color: foreground,
                ),
              )
            else if (icon != null)
              Icon(icon, size: 18, color: foreground),
            if (loading || icon != null) const SizedBox(width: AqariSpacing.x2),
            Flexible(
              child: Text(
                label,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.labelLarge?.copyWith(
                  color: foreground,
                  decoration: variant == AqariButtonVariant.link
                      ? TextDecoration.underline
                      : null,
                  decorationColor: foreground,
                ),
              ),
            ),
          ],
        ),
      ),
    );
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: enabled ? onPressed : null,
        borderRadius: AqariRadius.fullBorder,
        overlayColor: WidgetStatePropertyAll(foreground.withValues(alpha: .10)),
        child: expanded
            ? SizedBox(width: double.infinity, child: child)
            : child,
      ),
    );
  }
}

class AqariIconButton extends StatelessWidget {
  const AqariIconButton({
    required this.icon,
    required this.semanticLabel,
    this.onPressed,
    super.key,
  });
  final IconData icon;
  final String semanticLabel;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    label: semanticLabel,
    enabled: onPressed != null,
    child: SizedBox.square(
      dimension: AqariSpacing.x12,
      child: Material(
        color: Colors.transparent,
        shape: const CircleBorder(),
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: onPressed,
          child: Icon(
            icon,
            size: 20,
            color: onPressed == null
                ? context.aqariColors.textDisabled
                : context.aqariColors.textPrimary,
          ),
        ),
      ),
    ),
  );
}
