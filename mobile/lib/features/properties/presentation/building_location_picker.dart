import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../../core/design_system/design_system.dart';

class BuildingLocationPicker extends StatelessWidget {
  const BuildingLocationPicker({
    required this.latitude,
    required this.longitude,
    required this.onSelected,
    super.key,
  });

  static const amman = LatLng(31.9539, 35.9106);

  final double? latitude;
  final double? longitude;
  final ValueChanged<LatLng> onSelected;

  bool get hasLocation => latitude != null && longitude != null;

  @override
  Widget build(BuildContext context) {
    final selected = hasLocation ? LatLng(latitude!, longitude!) : null;
    return ClipRRect(
      borderRadius: AqariRadius.mdBorder,
      child: SizedBox(
        height: 280,
        child: FlutterMap(
          key: ValueKey((latitude, longitude)),
          options: MapOptions(
            initialCenter: selected ?? amman,
            initialZoom: selected == null ? 11 : 14,
            onTap: (_, point) => onSelected(point),
          ),
          children: [
            TileLayer(
              urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
              userAgentPackageName: 'com.aqarios.mobile',
              maxNativeZoom: 19,
            ),
            if (selected != null)
              MarkerLayer(
                markers: [
                  Marker(
                    point: selected,
                    width: 48,
                    height: 48,
                    child: Icon(
                      Icons.location_pin,
                      size: 44,
                      color: context.aqariColors.primary,
                    ),
                  ),
                ],
              ),
            const RichAttributionWidget(
              attributions: [
                TextSourceAttribution('OpenStreetMap contributors'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
