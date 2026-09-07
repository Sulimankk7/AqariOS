import 'package:flutter/material.dart';

abstract final class AqariRadius {
  static const xs = 10.0;
  static const sm = 10.0;
  static const md = 14.0;
  static const lg = 20.0;
  static const full = 9999.0;
  static const xsBorder = BorderRadius.all(Radius.circular(xs));
  static const smBorder = BorderRadius.all(Radius.circular(sm));
  static const mdBorder = BorderRadius.all(Radius.circular(md));
  static const lgBorder = BorderRadius.all(Radius.circular(lg));
  static const fullBorder = BorderRadius.all(Radius.circular(full));
}
