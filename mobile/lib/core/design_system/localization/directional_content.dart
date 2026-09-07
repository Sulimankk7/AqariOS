import 'package:flutter/material.dart';

class AqariLtrContent extends StatelessWidget {
  const AqariLtrContent({required this.child, super.key});
  final Widget child;

  @override
  Widget build(BuildContext context) => Align(
    alignment: AlignmentDirectional.centerStart,
    child: Directionality(textDirection: TextDirection.ltr, child: child),
  );
}

class AqariDirectionalIcon extends StatelessWidget {
  const AqariDirectionalIcon({
    required this.icon,
    this.size = 20,
    this.color,
    super.key,
  });
  final IconData icon;
  final double size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final rtl = Directionality.of(context) == TextDirection.rtl;
    return Transform.flip(
      // Material directional glyphs auto-mirror from Directionality. Only mirror
      // custom/non-directional glyphs here, avoiding a double flip in Arabic.
      flipX: rtl && !icon.matchTextDirection,
      child: Icon(icon, size: size, color: color),
    );
  }
}
