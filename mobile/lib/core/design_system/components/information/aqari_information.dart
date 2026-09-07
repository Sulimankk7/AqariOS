import 'package:flutter/material.dart';

import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_elevation.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';
import '../../theme/aqari_typography.dart';

enum AqariStatusVariant { brand, success, warning, error, info, neutral }

class AqariStatusStyle {
  const AqariStatusStyle(this.foreground, this.background, this.border);
  final Color foreground, background, border;
}

extension AqariStatusColors on AqariStatusVariant {
  AqariStatusStyle resolve(AqariColors c) => switch (this) {
    AqariStatusVariant.brand => AqariStatusStyle(
      c.onPrimaryContainer,
      c.primaryContainer,
      c.primary.withValues(alpha: .3),
    ),
    AqariStatusVariant.success => AqariStatusStyle(
      c.success,
      c.successContainer,
      c.success.withValues(alpha: .3),
    ),
    AqariStatusVariant.warning => AqariStatusStyle(
      c.warning,
      c.warningContainer,
      c.warning.withValues(alpha: .3),
    ),
    AqariStatusVariant.error => AqariStatusStyle(
      c.error,
      c.errorContainer,
      c.error.withValues(alpha: .3),
    ),
    AqariStatusVariant.info => AqariStatusStyle(
      c.info,
      c.infoContainer,
      c.info.withValues(alpha: .3),
    ),
    AqariStatusVariant.neutral => AqariStatusStyle(
      c.textSecondary,
      c.surfaceContainer,
      c.outlineVariant,
    ),
  };
}

class AqariStatusBadge extends StatelessWidget {
  const AqariStatusBadge({
    required this.label,
    this.variant = AqariStatusVariant.neutral,
    this.showDot = true,
    super.key,
  });
  final String label;
  final AqariStatusVariant variant;
  final bool showDot;
  @override
  Widget build(BuildContext context) {
    final style = variant.resolve(context.aqariColors);
    return Semantics(
      label: label,
      child: Container(
        padding: const EdgeInsetsDirectional.symmetric(
          horizontal: AqariSpacing.x2,
          vertical: AqariSpacing.x1,
        ),
        decoration: BoxDecoration(
          color: style.background,
          borderRadius: AqariRadius.fullBorder,
          border: Border.all(color: style.border),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (showDot) ...[
              Container(
                width: 6,
                height: 6,
                decoration: BoxDecoration(
                  color: style.foreground,
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: AqariSpacing.x1),
            ],
            Flexible(
              child: Text(
                label,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(
                  context,
                ).textTheme.labelMedium?.copyWith(color: style.foreground),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class AqariAvatar extends StatelessWidget {
  const AqariAvatar({
    required this.label,
    this.image,
    this.size = 40,
    super.key,
  });
  final String label;
  final ImageProvider? image;
  final double size;
  @override
  Widget build(BuildContext context) => Semantics(
    image: image != null,
    label: label,
    child: Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: context.aqariColors.primary,
        image: image == null
            ? null
            : DecorationImage(image: image!, fit: BoxFit.cover),
      ),
      child: image == null
          ? Text(
              label.characters.first.toUpperCase(),
              style: Theme.of(context).textTheme.labelLarge?.copyWith(
                color: context.aqariColors.onPrimary,
                fontWeight: FontWeight.w700,
              ),
            )
          : null,
    ),
  );
}

class AqariKpi extends StatelessWidget {
  const AqariKpi({
    required this.label,
    required this.value,
    this.note,
    this.icon,
    super.key,
  });
  final String label, value;
  final String? note;
  final IconData? icon;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsetsDirectional.symmetric(vertical: AqariSpacing.x2),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            if (icon != null) ...[
              Icon(icon, size: 18, color: context.aqariColors.primary),
              const SizedBox(width: AqariSpacing.x2),
            ],
            Expanded(
              child: Text(
                label,
                style: Theme.of(context).textTheme.labelMedium?.copyWith(
                  color: context.aqariColors.textSecondary,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: AqariSpacing.x1),
        Text(value, style: AqariTypography.numeric(context)),
        if (note != null)
          Text(
            note!,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: context.aqariColors.textMuted,
            ),
          ),
      ],
    ),
  );
}

class AqariSectionHeader extends StatelessWidget {
  const AqariSectionHeader({
    required this.title,
    this.actionLabel,
    this.onAction,
    super.key,
  });
  final String title;
  final String? actionLabel;
  final VoidCallback? onAction;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.x2),
    child: Row(
      children: [
        Expanded(
          child: Text(title, style: Theme.of(context).textTheme.titleSmall),
        ),
        if (actionLabel != null)
          TextButton(onPressed: onAction, child: Text(actionLabel!)),
      ],
    ),
  );
}

class AqariDivider extends StatelessWidget {
  const AqariDivider({this.indent = 0, super.key});
  final double indent;
  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsetsDirectional.only(start: indent),
    child: Divider(
      height: 1,
      thickness: 1,
      color: context.aqariColors.borderMuted,
    ),
  );
}

class AqariSurface extends StatelessWidget {
  const AqariSurface({
    required this.child,
    this.padding = EdgeInsets.zero,
    this.color,
    super.key,
  });
  final Widget child;
  final EdgeInsetsGeometry padding;
  final Color? color;
  @override
  Widget build(BuildContext context) => ColoredBox(
    color: color ?? context.aqariColors.surface,
    child: Padding(padding: padding, child: child),
  );
}

enum AqariCardVariant { outlined, filled, elevated }

class AqariCard extends StatelessWidget {
  const AqariCard({
    required this.child,
    this.variant = AqariCardVariant.outlined,
    this.padding = const EdgeInsets.all(AqariSpacing.x4),
    super.key,
  });
  final Widget child;
  final AqariCardVariant variant;
  final EdgeInsetsGeometry padding;
  @override
  Widget build(BuildContext context) {
    final c = context.aqariColors;
    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: variant == AqariCardVariant.filled ? c.surfaceLow : c.card,
        borderRadius: AqariRadius.lgBorder,
        border: Border.all(color: c.outlineVariant),
        boxShadow: variant == AqariCardVariant.elevated
            ? AqariElevation.e1
            : AqariElevation.e0,
      ),
      child: child,
    );
  }
}

class AqariDetailRow extends StatelessWidget {
  const AqariDetailRow({
    required this.label,
    required this.value,
    this.ltr = false,
    super.key,
  });
  final String label, value;
  final bool ltr;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: AqariSpacing.x2),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Text(
            label,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: context.aqariColors.textSecondary,
            ),
          ),
        ),
        const SizedBox(width: AqariSpacing.x4),
        Flexible(
          flex: 2,
          child: ltr
              ? Directionality(
                  textDirection: TextDirection.ltr,
                  child: SelectableText(value, textAlign: TextAlign.end),
                )
              : SelectableText(value, textAlign: TextAlign.end),
        ),
      ],
    ),
  );
}

class AqariFormSection extends StatelessWidget {
  const AqariFormSection({
    required this.title,
    required this.children,
    super.key,
  });
  final String title;
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AqariSpacing.section),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AqariSectionHeader(title: title),
        ...children,
      ],
    ),
  );
}

class AqariSectionSurface extends StatelessWidget {
  const AqariSectionSurface({
    required this.child,
    this.padding = const EdgeInsets.all(AqariSpacing.x4),
    super.key,
  });
  final Widget child;
  final EdgeInsetsGeometry padding;
  @override
  Widget build(BuildContext context) => Container(
    padding: padding,
    decoration: BoxDecoration(
      color: context.aqariColors.card,
      borderRadius: AqariRadius.mdBorder,
      border: Border.all(color: context.aqariColors.outlineVariant),
    ),
    child: child,
  );
}

class AqariActionBar extends StatelessWidget {
  const AqariActionBar({required this.child, super.key});
  final Widget child;
  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    child: Container(
      padding: const EdgeInsetsDirectional.fromSTEB(
        AqariSpacing.page,
        AqariSpacing.x3,
        AqariSpacing.page,
        AqariSpacing.x3,
      ),
      decoration: BoxDecoration(
        color: context.aqariColors.card,
        border: Border(
          top: BorderSide(color: context.aqariColors.outlineVariant),
        ),
      ),
      child: child,
    ),
  );
}
