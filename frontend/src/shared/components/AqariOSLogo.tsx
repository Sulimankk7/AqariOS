/**
 * Official AqariOS brand logo.
 * This component intentionally references the single canonical static asset.
 */

interface AqariOSLogoProps {
  size?: number;
  className?: string;
  alt?: string;
}

export function AqariOSLogo({ size = 32, className, alt = "AqariOS" }: AqariOSLogoProps) {
  return (
    <img
      src="/branding/aqarios-logo.png"
      width={size}
      height={size}
      alt={alt}
      className={className ?? "shrink-0 object-contain"}
    />
  );
}
