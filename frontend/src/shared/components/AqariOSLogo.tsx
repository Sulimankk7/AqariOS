/**
 * AqariOS brand logo SVG mark.
 * Extracted from the Figma-generated design — do not modify colours or geometry.
 */

interface AqariOSLogoProps {
  size?: number;
}

export function AqariOSLogo({ size = 32 }: AqariOSLogoProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 32 32"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-label="AqariOS"
    >
      <rect x="3"  y="18" width="6" height="11" rx="1" fill="#656D4A" />
      <rect x="11" y="11" width="6" height="18" rx="1" fill="#414833" />
      <rect x="19" y="6"  width="6" height="23" rx="1" fill="#333D29" />
      <circle cx="27" cy="8" r="2.5" fill="#936639" />
    </svg>
  );
}
