import 'package:flutter/material.dart';

@immutable
class AqariColors extends ThemeExtension<AqariColors> {
  const AqariColors({
    required this.primary,
    required this.onPrimary,
    required this.primaryContainer,
    required this.onPrimaryContainer,
    required this.secondary,
    required this.onSecondary,
    required this.secondaryContainer,
    required this.onSecondaryContainer,
    required this.tertiary,
    required this.onTertiary,
    required this.tertiaryContainer,
    required this.onTertiaryContainer,
    required this.background,
    required this.onBackground,
    required this.surface,
    required this.onSurface,
    required this.surfaceVariant,
    required this.onSurfaceVariant,
    required this.surfaceDim,
    required this.surfaceBright,
    required this.surfaceLowest,
    required this.surfaceLow,
    required this.surfaceContainer,
    required this.surfaceHigh,
    required this.surfaceHighest,
    required this.textPrimary,
    required this.textSecondary,
    required this.textMuted,
    required this.textDisabled,
    required this.outline,
    required this.outlineVariant,
    required this.borderMuted,
    required this.focus,
    required this.disabledContainer,
    required this.scrim,
    required this.success,
    required this.successContainer,
    required this.warning,
    required this.warningContainer,
    required this.error,
    required this.errorContainer,
    required this.info,
    required this.infoContainer,
  });

  final Color primary, onPrimary, primaryContainer, onPrimaryContainer;
  final Color secondary, onSecondary, secondaryContainer, onSecondaryContainer;
  final Color tertiary, onTertiary, tertiaryContainer, onTertiaryContainer;
  final Color background, onBackground, surface, onSurface;
  final Color surfaceVariant, onSurfaceVariant, surfaceDim, surfaceBright;
  final Color surfaceLowest,
      surfaceLow,
      surfaceContainer,
      surfaceHigh,
      surfaceHighest;
  final Color textPrimary, textSecondary, textMuted, textDisabled;
  final Color outline,
      outlineVariant,
      borderMuted,
      focus,
      disabledContainer,
      scrim;
  final Color success, successContainer, warning, warningContainer;
  final Color error, errorContainer, info, infoContainer;

  static const light = AqariColors(
    // aqari-design-system.zip/css/tokens.css :root, mapped by semantic role.
    primary: Color(0xFF3F4A2C),
    onPrimary: Color(0xFFF6F2E6),
    primaryContainer: Color(0xFFE7ECD9),
    onPrimaryContainer: Color(0xFF3F4A2C),
    secondary: Color(0xFFA85F35),
    onSecondary: Color(0xFFF6F2E6),
    secondaryContainer: Color(0xFFF1E1CE),
    onSecondaryContainer: Color(0xFFA85F35),
    tertiary: Color(0xFF8B9C68),
    onTertiary: Color(0xFF2A2A21),
    tertiaryContainer: Color(0xFFE7ECD9),
    onTertiaryContainer: Color(0xFF3F4A2C),
    background: Color(0xFFF6F2E6),
    onBackground: Color(0xFF2A2A21),
    surface: Color(0xFFFFFFFF),
    onSurface: Color(0xFF2A2A21),
    surfaceVariant: Color(0xFFFBF8EE),
    onSurfaceVariant: Color(0xFF726C58),
    surfaceDim: Color(0xFFEBE6D8),
    surfaceBright: Color(0xFFFFFFFF),
    surfaceLowest: Color(0xFFFFFFFF),
    surfaceLow: Color(0xFFFBF8EE),
    surfaceContainer: Color(0xFFEBE6D8),
    surfaceHigh: Color(0xFFFFFFFF),
    surfaceHighest: Color(0xFFFBF8EE),
    textPrimary: Color(0xFF2A2A21),
    textSecondary: Color(0xFF726C58),
    textMuted: Color(0xFFA39C86),
    textDisabled: Color(0xFFA39C86),
    outline: Color(0xFFE5DEC7),
    outlineVariant: Color(0xFFE5DEC7),
    borderMuted: Color(0xFFE5DEC7),
    focus: Color(0xFF3F4A2C),
    disabledContainer: Color(0xFFEDEAE0),
    scrim: Color(0xFF000000),
    success: Color(0xFF4C7350),
    successContainer: Color(0xFFE3ECDF),
    warning: Color(0xFFA85F35),
    warningContainer: Color(0xFFF1E1CE),
    error: Color(0xFF7B3B34),
    errorContainer: Color(0xFFF2E0DD),
    info: Color(0xFF3F4A2C),
    infoContainer: Color(0xFFE7ECD9),
  );

  // Web dark component roles are intentionally distinct from its status roles.
  bool get isDark => background == dark.background;
  Color get card => isDark ? surfaceLow : surface;
  Color get field => isDark ? surfaceContainer : surface;
  Color get buttonPrimary => primary;
  Color get buttonOnPrimary => isDark ? const Color(0xFFFFFFFF) : onPrimary;
  Color get buttonSecondary =>
      isDark ? const Color(0xFF7F4F24) : primaryContainer;
  Color get buttonOnSecondary =>
      isDark ? const Color(0xFFFFFFFF) : onPrimaryContainer;
  Color get buttonDestructive => isDark ? const Color(0xFF8A3B2E) : error;
  Color get buttonOnDestructive => isDark ? const Color(0xFFFFFFFF) : onPrimary;
  Color get buttonDisabled =>
      isDark ? const Color(0xFF2C302A) : disabledContainer;
  Color get buttonOutline => isDark ? const Color(0xFFA4AC86) : outline;
  Color get actionText => isDark ? const Color(0xFFD4D7C9) : primary;
  Color get primaryPressed =>
      isDark ? const Color(0xFF343A29) : const Color(0xFF2C3420);
  Color get neutral => isDark ? textSecondary : const Color(0xFF8F8A7C);
  Color get neutralContainer =>
      isDark ? surfaceContainer : const Color(0xFFEDEAE0);
  Color get late => isDark ? error : const Color(0xFFAE4A2E);
  Color get lateContainer => isDark ? errorContainer : const Color(0xFFF5E1D9);

  static const dark = AqariColors(
    primary: Color(0xFF414833),
    onPrimary: Color(0xFFF2F3F0),
    primaryContainer: Color(0xFF414833),
    onPrimaryContainer: Color(0xFFF2F3F0),
    secondary: Color(0xFF8D745C),
    onSecondary: Color(0xFFF2F3F0),
    secondaryContainer: Color(0xFF2D2925),
    onSecondaryContainer: Color(0xFFD6D2CC),
    tertiary: Color(0xFF7E878F),
    onTertiary: Color(0xFF0E1116),
    tertiaryContainer: Color(0xFF252B32),
    onTertiaryContainer: Color(0xFFF2F3F0),
    background: Color(0xFF0E1116),
    onBackground: Color(0xFFF2F3F0),
    surface: Color(0xFF0E1116),
    onSurface: Color(0xFFF2F3F0),
    surfaceVariant: Color(0xFF181D23),
    onSurfaceVariant: Color(0xFFAEB3AD),
    surfaceDim: Color(0xFF0A0D11),
    surfaceBright: Color(0xFF20262D),
    surfaceLowest: Color(0xFF0B0E12),
    surfaceLow: Color(0xFF14191F),
    surfaceContainer: Color(0xFF161B21),
    surfaceHigh: Color(0xFF181D23),
    surfaceHighest: Color(0xFF1D232A),
    textPrimary: Color(0xFFF2F3F0),
    textSecondary: Color(0xFFAEB3AD),
    textMuted: Color(0xFF6F756F),
    textDisabled: Color(0xFF6F756F),
    outline: Color(0xFF252B32),
    outlineVariant: Color(0xFF252B32),
    borderMuted: Color(0xFF1D2329),
    focus: Color(0xFF414833),
    disabledContainer: Color(0xFF181D23),
    scrim: Color(0xFF000000),
    success: Color(0xFF9BAE91),
    successContainer: Color(0xFF1C2820),
    warning: Color(0xFFD0AA76),
    warningContainer: Color(0xFF2B241B),
    error: Color(0xFFE6A29A),
    errorContainer: Color(0xFF3A2424),
    info: Color(0xFFA8B5C2),
    infoContainer: Color(0xFF1B252E),
  );

  @override
  AqariColors copyWith() => this;

  @override
  AqariColors lerp(covariant AqariColors? other, double t) =>
      t < .5 || other == null ? this : other;
}

extension AqariThemeContext on BuildContext {
  AqariColors get aqariColors => Theme.of(this).extension<AqariColors>()!;
}
