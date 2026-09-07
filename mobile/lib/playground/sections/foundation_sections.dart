import 'package:flutter/material.dart';

import '../../core/design_system/design_system.dart';
import 'playground_shared.dart';

class ColorPlaygroundSection extends StatelessWidget {
  const ColorPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) {
    final c = context.aqariColors;
    final semantic = <String, Color>{
      'primary': c.primary,
      'onPrimary': c.onPrimary,
      'primaryContainer': c.primaryContainer,
      'secondary': c.secondary,
      'tertiary': c.tertiary,
      'background': c.background,
      'surface': c.surface,
      'surfaceLow': c.surfaceLow,
      'surfaceContainer': c.surfaceContainer,
      'surfaceHigh': c.surfaceHigh,
      'surfaceHighest': c.surfaceHighest,
      'textPrimary': c.textPrimary,
      'textSecondary': c.textSecondary,
      'textMuted': c.textMuted,
      'outline': c.outline,
      'outlineVariant': c.outlineVariant,
      'borderMuted': c.borderMuted,
      'success': c.success,
      'successContainer': c.successContainer,
      'warning': c.warning,
      'warningContainer': c.warningContainer,
      'error': c.error,
      'errorContainer': c.errorContainer,
      'info': c.info,
      'infoContainer': c.infoContainer,
    };
    const brand = <String, Color>{
      'brown900': AqariBrand.brown900,
      'brown700': AqariBrand.brown700,
      'brown600': AqariBrand.brown600,
      'brown400': AqariBrand.brown400,
      'brown200': AqariBrand.brown200,
      'green900': AqariBrand.green900,
      'green800': AqariBrand.green800,
      'green600': AqariBrand.green600,
      'green400': AqariBrand.green400,
      'green200': AqariBrand.green200,
    };
    return PlaygroundSection(
      title: 'Colors · الألوان',
      description:
          'Brand primitives and semantic roles. Components consume semantic colors only.',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Brand', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: AqariSpacing.x3),
          PlaygroundWrap(
            children: brand.entries
                .map((e) => _ColorTile(name: e.key, color: e.value))
                .toList(),
          ),
          const SizedBox(height: AqariSpacing.x6),
          Text(
            'Semantic / Surface / Status',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: AqariSpacing.x3),
          PlaygroundWrap(
            children: semantic.entries
                .map((e) => _ColorTile(name: e.key, color: e.value))
                .toList(),
          ),
        ],
      ),
    );
  }
}

class _ColorTile extends StatelessWidget {
  const _ColorTile({required this.name, required this.color});
  final String name;
  final Color color;
  String get hex =>
      '#${color.toARGB32().toRadixString(16).substring(2).toUpperCase()}';
  @override
  Widget build(BuildContext context) => SizedBox(
    width: 148,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          height: 56,
          decoration: BoxDecoration(
            color: color,
            borderRadius: AqariRadius.smBorder,
            border: Border.all(color: context.aqariColors.outlineVariant),
          ),
        ),
        const SizedBox(height: AqariSpacing.x1),
        Text(
          name,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: Theme.of(context).textTheme.labelMedium,
        ),
        Text(
          hex,
          textDirection: TextDirection.ltr,
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
            color: context.aqariColors.textMuted,
          ),
        ),
      ],
    ),
  );
}

class TypographyPlaygroundSection extends StatelessWidget {
  const TypographyPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    final styles = <String, TextStyle?>{
      'displayLarge': t.displayLarge,
      'displayMedium': t.displayMedium,
      'displaySmall': t.displaySmall,
      'headlineLarge': t.headlineLarge,
      'headlineMedium': t.headlineMedium,
      'headlineSmall': t.headlineSmall,
      'titleLarge': t.titleLarge,
      'titleMedium': t.titleMedium,
      'titleSmall': t.titleSmall,
      'bodyLarge': t.bodyLarge,
      'bodyMedium': t.bodyMedium,
      'bodySmall': t.bodySmall,
      'labelLarge': t.labelLarge,
      'labelMedium': t.labelMedium,
      'labelSmall': t.labelSmall,
    };
    return PlaygroundSection(
      title: 'Typography · الخطوط',
      description:
          'Tajawal Arabic, Roboto Flex Latin, exact Web-derived scale.',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          for (final entry in styles.entries)
            Padding(
              padding: const EdgeInsetsDirectional.only(
                bottom: AqariSpacing.x4,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    entry.key,
                    style: t.labelSmall?.copyWith(
                      color: context.aqariColors.textMuted,
                    ),
                  ),
                  Text(
                    'إدارة العقارات · Property operations · AQ-2048',
                    style: entry.value,
                  ),
                ],
              ),
            ),
          Text(
            'Numeric / KPI',
            style: t.labelSmall?.copyWith(color: context.aqariColors.textMuted),
          ),
          Text(
            '12,480.50 JOD',
            textDirection: TextDirection.ltr,
            style: AqariTypography.numeric(context),
          ),
        ],
      ),
    );
  }
}

class TokenPlaygroundSection extends StatelessWidget {
  const TokenPlaygroundSection({super.key});
  @override
  Widget build(BuildContext context) => PlaygroundSection(
    title: 'Spacing, radius, elevation & motion',
    description: 'The compact token foundation behind every component.',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        PlaygroundWrap(
          children: const [
            4,
            8,
            12,
            16,
            20,
            24,
            32,
            40,
            48,
            64,
          ].map((v) => _TokenBox(label: '$v', width: 54, height: 40)).toList(),
        ),
        const SizedBox(height: AqariSpacing.x4),
        const PlaygroundWrap(
          children: [
            _TokenBox(label: 'xs 4', radius: 4),
            _TokenBox(label: 'sm 8', radius: 8),
            _TokenBox(label: 'md 16', radius: 16),
            _TokenBox(label: 'lg 28', radius: 28),
            _TokenBox(label: 'full', radius: 999),
          ],
        ),
        const SizedBox(height: AqariSpacing.x4),
        const PlaygroundWrap(
          children: [
            _ElevationBox(label: 'e0', shadow: AqariElevation.e0),
            _ElevationBox(label: 'e1', shadow: AqariElevation.e1),
            _ElevationBox(label: 'e2', shadow: AqariElevation.e2),
            _ElevationBox(label: 'e3', shadow: AqariElevation.e3),
          ],
        ),
        const SizedBox(height: AqariSpacing.x3),
        Text(
          'Motion: 75ms · 150ms · 200ms · 300ms',
          style: Theme.of(context).textTheme.bodyMedium,
        ),
      ],
    ),
  );
}

class _TokenBox extends StatelessWidget {
  const _TokenBox({
    required this.label,
    this.width = 92,
    this.height = 48,
    this.radius = 8,
  });
  final String label;
  final double width, height, radius;
  @override
  Widget build(BuildContext context) => Container(
    width: width,
    height: height,
    alignment: Alignment.center,
    decoration: BoxDecoration(
      color: context.aqariColors.surfaceContainer,
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(color: context.aqariColors.outlineVariant),
    ),
    child: Text(label, style: Theme.of(context).textTheme.labelMedium),
  );
}

class _ElevationBox extends StatelessWidget {
  const _ElevationBox({required this.label, required this.shadow});
  final String label;
  final List<BoxShadow> shadow;
  @override
  Widget build(BuildContext context) => Container(
    width: 92,
    height: 56,
    alignment: Alignment.center,
    decoration: BoxDecoration(
      color: context.aqariColors.surfaceLowest,
      borderRadius: AqariRadius.smBorder,
      boxShadow: shadow,
    ),
    child: Text(label),
  );
}
