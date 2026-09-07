import { useTheme } from "@/shared/theme";

const HERO_SCREENSHOTS = {
  light: {
    desktop: "/branding/landing/hero-dashboard-desktop-light.png",
    mobile: "/branding/landing/hero-dashboard-mobile-light.png",
  },
  dark: {
    desktop: "/branding/landing/hero-dashboard-desktop-dark.png",
    mobile: "/branding/landing/hero-dashboard-mobile-dark.png",
  },
} as const;

export function HeroProductVisual() {
  const { resolvedTheme } = useTheme();
  const screenshots = HERO_SCREENSHOTS[resolvedTheme];

  return (
    <div className="hero-product-visual" role="group" aria-label="مساحة صور منتج AqariOS">
      <div className="hero-product-placeholder hero-product-placeholder--desktop">
        <img
          src={screenshots.desktop}
          alt="لوحة تحكم AqariOS لسطح المكتب"
          decoding="async"
        />
      </div>

      <div className="hero-product-placeholder hero-product-placeholder--mobile">
        <img
          src={screenshots.mobile}
          alt="واجهة AqariOS للهاتف"
          decoding="async"
        />
      </div>
    </div>
  );
}
