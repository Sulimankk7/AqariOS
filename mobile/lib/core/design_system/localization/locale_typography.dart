import 'package:flutter/material.dart';

extension AqariLocaleContext on BuildContext {
  bool get isArabic => Localizations.localeOf(this).languageCode == 'ar';
  TextDirection get localeDirection =>
      isArabic ? TextDirection.rtl : TextDirection.ltr;
}
