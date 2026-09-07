import 'package:flutter/material.dart';

import '../../theme/aqari_color_scheme.dart';
import '../../theme/aqari_elevation.dart';
import '../../theme/aqari_radius.dart';
import '../../theme/aqari_spacing.dart';
import '../actions/aqari_button.dart';

abstract final class AqariDialog {
  static Future<T?> show<T>({
    required BuildContext context,
    required String title,
    required Widget content,
    String? primaryLabel,
    VoidCallback? onPrimary,
    String? secondaryLabel,
  }) => showDialog<T>(
    context: context,
    barrierColor: context.aqariColors.scrim.withValues(alpha: .40),
    builder: (dialogContext) => Dialog(
      child: Container(
        constraints: const BoxConstraints(maxWidth: 440),
        padding: const EdgeInsets.all(AqariSpacing.x5),
        decoration: BoxDecoration(
          color: context.aqariColors.surfaceHigh,
          borderRadius: AqariRadius.lgBorder,
          border: Border.all(color: context.aqariColors.outlineVariant),
          boxShadow: AqariElevation.e3,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(title, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: AqariSpacing.x3),
            content,
            if (primaryLabel != null || secondaryLabel != null) ...[
              const SizedBox(height: AqariSpacing.x5),
              Wrap(
                alignment: WrapAlignment.end,
                spacing: AqariSpacing.x2,
                runSpacing: AqariSpacing.x2,
                children: [
                  if (secondaryLabel != null)
                    AqariButton(
                      label: secondaryLabel,
                      variant: AqariButtonVariant.outlined,
                      onPressed: () => Navigator.pop(dialogContext),
                    ),
                  if (primaryLabel != null)
                    AqariButton(
                      label: primaryLabel,
                      onPressed:
                          onPrimary ?? () => Navigator.pop(dialogContext),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    ),
  );
}

abstract final class AqariBottomSheet {
  static Future<T?> show<T>({
    required BuildContext context,
    required String title,
    required Widget child,
  }) => showModalBottomSheet<T>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    barrierColor: context.aqariColors.scrim.withValues(alpha: .45),
    builder: (_) => Container(
      width: double.infinity,
      padding: EdgeInsetsDirectional.fromSTEB(
        AqariSpacing.x5,
        AqariSpacing.x2,
        AqariSpacing.x5,
        MediaQuery.viewInsetsOf(context).bottom + AqariSpacing.x6,
      ),
      decoration: BoxDecoration(
        color: context.aqariColors.surfaceHigh,
        borderRadius: const BorderRadius.vertical(
          top: Radius.circular(AqariRadius.lg),
        ),
        boxShadow: AqariElevation.e3,
      ),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: AqariSpacing.x4),
            child,
          ],
        ),
      ),
    ),
  );
}
