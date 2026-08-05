import React, { useEffect } from 'react';
import { MapContainer, TileLayer, Marker, useMapEvents, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// Fix Leaflet default marker icon paths for React / Vite bundler
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

export interface MapPickerProps {
  lat?: number;
  lng?: number;
  onLocationSelect?: (lat: number, lng: number) => void;
  readOnly?: boolean;
  height?: string;
}

function LocationMarker({ lat, lng, onLocationSelect, readOnly }: MapPickerProps) {
  const map = useMap();

  useEffect(() => {
    if (lat && lng) {
      map.setView([lat, lng], map.getZoom() || 14);
    }
  }, [lat, lng, map]);

  useMapEvents({
    click(e) {
      if (!readOnly && onLocationSelect) {
        onLocationSelect(Number(e.latlng.lat.toFixed(6)), Number(e.latlng.lng.toFixed(6)));
      }
    },
  });

  return lat && lng ? (
    <Marker
      position={[lat, lng]}
      draggable={!readOnly}
      eventHandlers={{
        dragend(e) {
          const marker = e.target;
          const position = marker.getLatLng();
          if (onLocationSelect) {
            onLocationSelect(Number(position.lat.toFixed(6)), Number(position.lng.toFixed(6)));
          }
        },
      }}
    />
  ) : null;
}

export function MapPicker({ lat, lng, onLocationSelect, readOnly = false, height = "280px" }: MapPickerProps) {
  // Default center: Amman, Jordan (31.9539, 35.9106)
  const defaultCenter: [number, number] = [lat || 31.9539, lng || 35.9106];

  return (
    <div className="w-full rounded-lg overflow-hidden border border-border shadow-xs relative" style={{ height }}>
      <MapContainer
        center={defaultCenter}
        zoom={lat && lng ? 14 : 11}
        style={{ height: '100%', width: '100%', zIndex: 10 }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <LocationMarker lat={lat} lng={lng} onLocationSelect={onLocationSelect} readOnly={readOnly} />
      </MapContainer>
      {!readOnly && (
        <div className="absolute bottom-2 start-2 z-[400] bg-background/90 backdrop-blur-xs px-2.5 py-1 rounded text-xs font-medium text-muted-foreground border border-border shadow-xs">
          Click on map to select GPS coordinates
        </div>
      )}
    </div>
  );
}
