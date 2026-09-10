import { useEffect, useRef, useState } from "react";
import { Menu, Moon, Sun, X } from "lucide-react";
import { Link } from "react-router";
import { ROUTES } from "@/config/routes";
import { AqariOSLogo } from "@/shared/components/AqariOSLogo";
import { useTheme } from "@/shared/theme";
import { Button } from "@/shared/ui/button";
import "./LandingNavbar.css";

const navigationItems = [
  { label: "الإمكانيات", href: "#features" },
  { label: "لماذا AqariOS؟", href: "#why-aqarios" },
  { label: "الأسعار", href: "#pricing" },
  { label: "تواصل معنا", href: "#contact" },
];

function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });
}

export function LandingNavbar() {
  const { resolvedTheme, setTheme } = useTheme();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const headerRef = useRef<HTMLElement>(null);
  const toggleButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!isMenuOpen) return;

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsMenuOpen(false);
        toggleButtonRef.current?.focus();
      }
    };

    const closeOnOutsideClick = (event: PointerEvent) => {
      if (headerRef.current && !headerRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    };

    const closeAtDesktop = () => {
      if (window.innerWidth > 1024) setIsMenuOpen(false);
    };

    window.addEventListener("keydown", closeOnEscape);
    window.addEventListener("pointerdown", closeOnOutsideClick);
    window.addEventListener("resize", closeAtDesktop);
    return () => {
      window.removeEventListener("keydown", closeOnEscape);
      window.removeEventListener("pointerdown", closeOnOutsideClick);
      window.removeEventListener("resize", closeAtDesktop);
    };
  }, [isMenuOpen]);

  const closeMenu = () => setIsMenuOpen(false);
  const isDark = resolvedTheme === "dark";
  const themeControlLabel = isDark ? "تفعيل الوضع الفاتح" : "تفعيل الوضع الداكن";
  const toggleTheme = () => setTheme(isDark ? "light" : "dark");

  const themeIcon = isDark ? <Moon aria-hidden="true" /> : <Sun aria-hidden="true" />;

  return (
    <header ref={headerRef} className="landing-header" dir="rtl">
      <nav className="landing-nav" aria-label="التنقل الرئيسي">
        <a className="landing-brand" href="#top" aria-label="AqariOS — بداية الصفحة">
          <AqariOSLogo size={38} />
          <span lang="en">AqariOS</span>
        </a>

        <div className="landing-nav__desktop-links" aria-label="روابط الصفحة">
          {navigationItems.map((item) => (
            <a key={item.href} href={item.href}>
              {item.label}
            </a>
          ))}
        </div>

        <div className="landing-nav__actions">
          <button
            className="landing-nav__theme-toggle landing-nav__theme-toggle--desktop"
            type="button"
            aria-label={themeControlLabel}
            title={themeControlLabel}
            onClick={toggleTheme}
          >
            {themeIcon}
          </button>
          <Link className="landing-nav__login" to={ROUTES.auth.login}>
            تسجيل الدخول
          </Link>
          <Button
            className="landing-nav__cta rounded-sm"
            type="button"
            onClick={() => scrollToSection("contact")}
          >
            اطلب تجربة النظام
          </Button>
          <button
            ref={toggleButtonRef}
            className="landing-nav__menu-button"
            type="button"
            aria-label={isMenuOpen ? "إغلاق قائمة التنقل" : "فتح قائمة التنقل"}
            aria-expanded={isMenuOpen}
            aria-controls="landing-mobile-menu"
            onClick={() => setIsMenuOpen((open) => !open)}
          >
            {isMenuOpen ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}
          </button>
        </div>

        <div
          id="landing-mobile-menu"
          className="landing-nav__mobile-menu"
          data-open={isMenuOpen}
          aria-hidden={!isMenuOpen}
        >
          {navigationItems.map((item) => (
            <a key={item.href} href={item.href} tabIndex={isMenuOpen ? 0 : -1} onClick={closeMenu}>
              {item.label}
            </a>
          ))}
          <Link
            className="landing-nav__mobile-login"
            to={ROUTES.auth.login}
            tabIndex={isMenuOpen ? 0 : -1}
            onClick={closeMenu}
          >
            تسجيل الدخول
          </Link>
          <a
            className="landing-nav__mobile-cta"
            href="#contact"
            tabIndex={isMenuOpen ? 0 : -1}
            onClick={closeMenu}
          >
            اطلب تجربة النظام
          </a>
          <button
            className="landing-nav__theme-toggle landing-nav__theme-toggle--mobile"
            type="button"
            aria-label={themeControlLabel}
            title={themeControlLabel}
            tabIndex={isMenuOpen ? 0 : -1}
            onClick={toggleTheme}
          >
            {themeIcon}
          </button>
        </div>
      </nav>
    </header>
  );
}
