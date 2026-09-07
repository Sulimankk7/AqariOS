import 'package:flutter/material.dart';

import 'aqari_color_scheme.dart';
import 'aqari_radius.dart';
import 'aqari_typography.dart';

abstract final class AqariTheme {
  static ThemeData light(Locale locale) =>
      _build(AqariColors.light, Brightness.light, locale);
  static ThemeData dark(Locale locale) =>
      _build(AqariColors.dark, Brightness.dark, locale);

  static ThemeData _build(
    AqariColors colors,
    Brightness brightness,
    Locale locale,
  ) {
    final scheme = ColorScheme(
      brightness: brightness,
      primary: colors.primary,
      onPrimary: colors.onPrimary,
      primaryContainer: colors.primaryContainer,
      onPrimaryContainer: colors.onPrimaryContainer,
      secondary: colors.secondary,
      onSecondary: colors.onSecondary,
      secondaryContainer: colors.secondaryContainer,
      onSecondaryContainer: colors.onSecondaryContainer,
      tertiary: colors.tertiary,
      onTertiary: colors.onTertiary,
      tertiaryContainer: colors.tertiaryContainer,
      onTertiaryContainer: colors.onTertiaryContainer,
      error: colors.error,
      onError: brightness == Brightness.light
          ? const Color(0xFFFFFFFF)
          : colors.background,
      errorContainer: colors.errorContainer,
      onErrorContainer: brightness == Brightness.light
          ? const Color(0xFF582F0E)
          : const Color(0xFFF3DAD7),
      surface: colors.surface,
      onSurface: colors.onSurface,
      outline: colors.outline,
      outlineVariant: colors.outlineVariant,
      shadow: colors.scrim,
      scrim: colors.scrim,
      inverseSurface: colors.onSurface,
      onInverseSurface: colors.surface,
      inversePrimary: colors.primaryContainer,
    );
    final text = AqariTypography.textTheme(locale, colors.textPrimary);
    OutlineInputBorder fieldBorder(Color color, [double width = 1]) =>
        OutlineInputBorder(
          borderRadius: AqariRadius.smBorder,
          borderSide: BorderSide(color: color, width: width),
        );
    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: colors.background,
      canvasColor: colors.background,
      extensions: [colors],
      fontFamily: AqariTypography.familyFor(locale),
      textTheme: text,
      splashFactory: InkRipple.splashFactory,
      dividerColor: colors.borderMuted,
      focusColor: colors.focus.withValues(alpha: .18),
      disabledColor: colors.textDisabled,
      appBarTheme: AppBarTheme(
        backgroundColor: colors.background,
        foregroundColor: colors.textPrimary,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: text.titleLarge,
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: colors.surfaceHigh,
        elevation: 0,
        shape: const RoundedRectangleBorder(borderRadius: AqariRadius.lgBorder),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: colors.surfaceHigh,
        modalBackgroundColor: colors.surfaceHigh,
        elevation: 0,
        modalElevation: 0,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(AqariRadius.lg),
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationThemeData(
        filled: true,
        fillColor: colors.field,
        contentPadding: const EdgeInsetsDirectional.symmetric(
          horizontal: 16,
          vertical: 13,
        ),
        border: fieldBorder(colors.outline),
        enabledBorder: fieldBorder(colors.outline),
        focusedBorder: fieldBorder(colors.focus, 2),
        errorBorder: fieldBorder(colors.error),
        focusedErrorBorder: fieldBorder(colors.error, 2),
        disabledBorder: fieldBorder(colors.outlineVariant),
        hintStyle: text.bodyLarge?.copyWith(color: colors.textMuted),
        helperStyle: text.bodySmall?.copyWith(color: colors.textMuted),
        errorStyle: text.bodySmall?.copyWith(color: colors.error),
      ),
    );
  }
}
