import { useTheme } from "@/shared/theme";

const HERO_SCREENSHOTS = {
  light: {
    desktop: { src: "/branding/landing/hero-dashboard-desktop-light.webp", width: 1779, height: 884 },
    mobile: { src: "/branding/landing/hero-dashboard-mobile-light.webp", width: 332, height: 694 },
  },
  dark: {
    desktop: { src: "/branding/landing/hero-dashboard-desktop-dark.webp", width: 1778, height: 885 },
    mobile: { src: "/branding/landing/hero-dashboard-mobile-dark.webp", width: 341, height: 720 },
  },
} as const;

export function HeroProductVisual() {
  const { resolvedTheme } = useTheme();
  const screenshots = HERO_SCREENSHOTS[resolvedTheme];

  return (
    <div className="hero-product-visual" role="group" aria-label="مساحة صور منتج AqariOS">
      <div className="hero-product-placeholder hero-product-placeholder--desktop">
        <img
          src={screenshots.desktop.src}
          alt="لوحة تحكم AqariOS لسطح المكتب"
          width={screenshots.desktop.width}
          height={screenshots.desktop.height}
          fetchPriority="high"
          decoding="async"
        />
      </div>

      <div className="hero-product-placeholder hero-product-placeholder--mobile">
        <img
          src={screenshots.mobile.src}
          alt="واجهة AqariOS للهاتف"
          width={screenshots.mobile.width}
          height={screenshots.mobile.height}
          loading="lazy"
          decoding="async"
        />
      </div>
    </div>
  );
}
