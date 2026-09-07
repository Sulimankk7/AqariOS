import 'package:flutter/material.dart';

import '../../localization/directional_content.dart';
import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_motion.dart';
import '../../theme/aqari_elevation.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';

enum AqariListRowDensity { compact, standard, rich }

class AqariListRow extends StatelessWidget {
  const AqariListRow({
    required this.title,
    this.supportingText,
    this.metadata,
    this.leading,
    this.status,
    this.trailing,
    this.onPressed,
    this.showChevron = false,
    this.showDivider = true,
    this.enabled = true,
    this.density = AqariListRowDensity.standard,
    super.key,
  });
  final String title;
  final String? supportingText, metadata;
  final Widget? leading, status, trailing;
  final VoidCallback? onPressed;
  final bool showChevron, showDivider, enabled;
  final AqariListRowDensity density;

  double get _height => switch (density) {
    AqariListRowDensity.compact => 64,
    AqariListRowDensity.standard => 72,
    AqariListRowDensity.rich => 88,
  };

  @override
  Widget build(BuildContext context) {
    final c = context.aqariColors;
    final content = AnimatedOpacity(
      duration: AqariMotion.normal,
      opacity: enabled ? 1 : .55,
      child: ConstrainedBox(
        constraints: BoxConstraints(minHeight: _height),
        child: Padding(
          padding: const EdgeInsetsDirectional.symmetric(
            horizontal: AqariSpacing.row,
            vertical: 13,
          ),
          child: Row(
            children: [
              if (leading != null) ...[
                leading!,
                const SizedBox(width: AqariSpacing.x3),
              ],
              Expanded(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(
                        context,
                      ).textTheme.titleSmall?.copyWith(color: c.textPrimary),
                    ),
                    if (supportingText != null)
                      Text(
                        supportingText!,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(
                          context,
                        ).textTheme.bodySmall?.copyWith(color: c.textSecondary),
                      ),
                    if (metadata != null)
                      Text(
                        metadata!,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(
                          context,
                        ).textTheme.labelSmall?.copyWith(color: c.textMuted),
                      ),
                  ],
                ),
              ),
              if (status != null) ...[
                const SizedBox(width: AqariSpacing.x2),
                Flexible(child: status!),
              ],
              if (trailing != null) ...[
                const SizedBox(width: AqariSpacing.x2),
                trailing!,
              ],
              if (showChevron) ...[
                const SizedBox(width: AqariSpacing.x2),
                AqariDirectionalIcon(
                  icon: Icons.chevron_right_rounded,
                  size: 20,
                  color: c.textMuted,
                ),
              ],
            ],
          ),
        ),
      ),
    );
    return Semantics(
      button: onPressed != null,
      enabled: enabled,
      child: Padding(
        padding: EdgeInsets.only(bottom: showDivider ? AqariSpacing.x2 : 0),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: c.card,
            border: Border.all(color: c.outlineVariant),
            borderRadius: AqariRadius.mdBorder,
            boxShadow: AqariElevation.e1,
          ),
          child: Material(
            color: Colors.transparent,
            borderRadius: AqariRadius.mdBorder,
            clipBehavior: Clip.antiAlias,
            child: InkWell(onTap: enabled ? onPressed : null, child: content),
          ),
        ),
      ),
    );
  }
}
