import 'package:flutter/material.dart';

/// The canonical AqariOS Web mark, copied byte-for-byte into Flutter assets.
/// This component deliberately does not recolor, crop, or decorate the asset.
class AqariLogo extends StatelessWidget {
  const AqariLogo({this.size = 40, this.semanticLabel = 'AqariOS', super.key});
  final double size;
  final String? semanticLabel;

  @override
  Widget build(BuildContext context) => Image.asset(
    'assets/branding/aqarios-logo.png',
    width: size,
    height: size,
    fit: BoxFit.contain,
    semanticLabel: semanticLabel,
  );
}
