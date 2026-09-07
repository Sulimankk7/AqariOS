import 'package:flutter/material.dart';

abstract final class AqariTypography {
  // Replaceable architecture: the exact numeric mono family remains a product
  // decision, so components reference this one seam instead of a permanent asset.
  static const numericFamily = 'monospace';
  static String familyFor(Locale locale) =>
      locale.languageCode == 'ar' ? 'Tajawal' : 'RobotoFlex';

  static TextTheme textTheme(Locale locale, Color color) {
    final family = familyFor(locale);
    TextStyle style(
      double size,
      double lineHeight,
      FontWeight weight, [
      double? spacing,
    ]) => TextStyle(
      fontFamily: family,
      fontSize: size,
      height: lineHeight / size,
      fontWeight: weight,
      letterSpacing: locale.languageCode == 'ar' ? 0 : spacing,
      color: color,
    );
    return TextTheme(
      displayLarge: style(32, 40, FontWeight.w800),
      displayMedium: style(28, 36, FontWeight.w800),
      displaySmall: style(24, 32, FontWeight.w800),
      headlineLarge: style(24, 32, FontWeight.w800),
      headlineMedium: style(20, 28, FontWeight.w800),
      headlineSmall: style(19, 28, FontWeight.w800),
      titleLarge: style(19, 28, FontWeight.w800),
      titleMedium: style(14.5, 22, FontWeight.w800),
      titleSmall: style(14, 21, FontWeight.w700),
      bodyLarge: style(14, 22, FontWeight.w500),
      bodyMedium: style(13.5, 21, FontWeight.w500),
      bodySmall: style(12.5, 19, FontWeight.w500),
      labelLarge: style(14, 21, FontWeight.w700),
      labelMedium: style(12.5, 18, FontWeight.w600),
      labelSmall: style(11, 16, FontWeight.w600),
    );
  }

  static TextStyle numeric(BuildContext context) =>
      Theme.of(context).textTheme.headlineSmall!.copyWith(
        fontSize: 20,
        height: 1.2,
        fontWeight: FontWeight.w800,
        fontFeatures: const [FontFeature.tabularFigures()],
      );
}
