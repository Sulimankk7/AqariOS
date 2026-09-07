import 'package:flutter/material.dart';

import '../core/design_system/design_system.dart';
import 'sections/component_sections.dart';
import 'sections/foundation_sections.dart';
import 'sections/pattern_sections.dart';
import 'sections/playground_shared.dart';

enum PlaygroundDevice {
  small('Small phone', 320),
  normal('Normal phone', 390),
  large('Large phone', 480),
  tablet('Tablet', 768);

  const PlaygroundDevice(this.label, this.width);
  final String label;
  final double width;
}

class DesignSystemPlayground extends StatefulWidget {
  const DesignSystemPlayground({
    required this.themePreference,
    required this.locale,
    required this.onThemeChanged,
    required this.onLocaleChanged,
    super.key,
  });
  final AqariThemePreference themePreference;
  final Locale locale;
  final ValueChanged<AqariThemePreference> onThemeChanged;
  final ValueChanged<Locale> onLocaleChanged;

  @override
  State<DesignSystemPlayground> createState() => _DesignSystemPlaygroundState();
}

class _DesignSystemPlaygroundState extends State<DesignSystemPlayground> {
  PlaygroundDevice device = PlaygroundDevice.normal;

  bool get ar => widget.locale.languageCode == 'ar';

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AqariAppBar(
      title: ar ? 'نظام تصميم AqariOS' : 'AqariOS Design System',
      actions: [
        AqariIconButton(
          icon: Icons.language_rounded,
          semanticLabel: ar ? 'Switch to English' : 'التبديل إلى العربية',
          onPressed: () => widget.onLocaleChanged(Locale(ar ? 'en' : 'ar')),
        ),
      ],
    ),
    body: Column(
      children: [
        _ControlBar(
          theme: widget.themePreference,
          locale: widget.locale,
          device: device,
          onThemeChanged: widget.onThemeChanged,
          onLocaleChanged: widget.onLocaleChanged,
          onDeviceChanged: (value) => setState(() => device = value),
        ),
        Expanded(
          child: ColoredBox(
            color: context.aqariColors.surfaceContainer,
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.all(AqariSpacing.x4),
              child: SizedBox(
                width: device.width,
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: context.aqariColors.background,
                    border: Border.all(
                      color: context.aqariColors.outlineVariant,
                    ),
                    borderRadius: AqariRadius.mdBorder,
                    boxShadow: AqariElevation.e1,
                  ),
                  child: ClipRRect(
                    borderRadius: AqariRadius.mdBorder,
                    child: SingleChildScrollView(
                      padding: EdgeInsetsDirectional.symmetric(
                        horizontal: device == PlaygroundDevice.tablet
                            ? AqariSpacing.x6
                            : AqariSpacing.x4,
                        vertical: AqariSpacing.x6,
                      ),
                      child: const Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          _PlaygroundIntro(),
                          ColorPlaygroundSection(),
                          TypographyPlaygroundSection(),
                          TokenPlaygroundSection(),
                          ButtonPlaygroundSection(),
                          FormPlaygroundSection(),
                          SurfacePlaygroundSection(),
                          ListPlaygroundSection(),
                          StatusPlaygroundSection(),
                          FeedbackPlaygroundSection(),
                          NavigationPlaygroundSection(),
                          OverlayPlaygroundSection(),
                          AccessibilityPlaygroundSection(),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    ),
  );
}

class _ControlBar extends StatelessWidget {
  const _ControlBar({
    required this.theme,
    required this.locale,
    required this.device,
    required this.onThemeChanged,
    required this.onLocaleChanged,
    required this.onDeviceChanged,
  });
  final AqariThemePreference theme;
  final Locale locale;
  final PlaygroundDevice device;
  final ValueChanged<AqariThemePreference> onThemeChanged;
  final ValueChanged<Locale> onLocaleChanged;
  final ValueChanged<PlaygroundDevice> onDeviceChanged;

  @override
  Widget build(BuildContext context) => AqariSurface(
    color: context.aqariColors.surfaceLow,
    padding: const EdgeInsetsDirectional.fromSTEB(12, 8, 12, 12),
    child: SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          for (final value in AqariThemePreference.values)
            Padding(
              padding: const EdgeInsetsDirectional.only(end: AqariSpacing.x2),
              child: AqariButton(
                label: value.name,
                variant: theme == value
                    ? AqariButtonVariant.tonal
                    : AqariButtonVariant.text,
                onPressed: () => onThemeChanged(value),
              ),
            ),
          Container(
            width: 1,
            height: 32,
            color: context.aqariColors.borderMuted,
          ),
          const SizedBox(width: AqariSpacing.x2),
          AqariButton(
            label: 'العربية RTL',
            variant: locale.languageCode == 'ar'
                ? AqariButtonVariant.tonal
                : AqariButtonVariant.text,
            onPressed: () => onLocaleChanged(const Locale('ar')),
          ),
          const SizedBox(width: AqariSpacing.x2),
          AqariButton(
            label: 'English LTR',
            variant: locale.languageCode == 'en'
                ? AqariButtonVariant.tonal
                : AqariButtonVariant.text,
            onPressed: () => onLocaleChanged(const Locale('en')),
          ),
          const SizedBox(width: AqariSpacing.x2),
          Container(
            width: 1,
            height: 32,
            color: context.aqariColors.borderMuted,
          ),
          const SizedBox(width: AqariSpacing.x2),
          for (final value in PlaygroundDevice.values)
            Padding(
              padding: const EdgeInsetsDirectional.only(end: AqariSpacing.x2),
              child: AqariButton(
                label: '${value.label} ${value.width.toInt()}',
                variant: device == value
                    ? AqariButtonVariant.outlined
                    : AqariButtonVariant.text,
                onPressed: () => onDeviceChanged(value),
              ),
            ),
        ],
      ),
    ),
  );
}

class _PlaygroundIntro extends StatelessWidget {
  const _PlaygroundIntro();
  @override
  Widget build(BuildContext context) => const PlaygroundSection(
    title: 'AqariOS on Mobile · عقاري أو إس للموبايل',
    description:
        'Warm-neutral, operational and touch-first. Information hierarchy comes before containers, decoration or cards.',
    child: AqariSurface(
      padding: EdgeInsets.all(AqariSpacing.x4),
      child: Text(
        'Clarity → hierarchy → usability → consistency → brand\nالوضوح ← التسلسل الهرمي ← سهولة الاستخدام ← الاتساق ← الهوية',
      ),
    ),
  );
}
