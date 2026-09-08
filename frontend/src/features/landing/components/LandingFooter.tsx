import { AqariOSLogo } from "@/shared/components/AqariOSLogo";
import "./LandingFooter.css";

const footerLinks = [
  { label: "الرئيسية", href: "#top" },
  { label: "الأسعار", href: "#pricing" },
  { label: "إمكانيات AqariOS", href: "#features" },
  { label: "لماذا AqariOS؟", href: "#why-aqarios" },
  { label: "تواصل معنا", href: "#contact" },
];

export function LandingFooter() {
  return (
    <footer className="landing-footer" aria-label="تذييل صفحة AqariOS">
      <div className="landing-footer__container">
        <div className="landing-footer__main">
          <div className="landing-footer__brand">
            <a className="landing-footer__logo" href="#top" aria-label="AqariOS — الرئيسية">
              <AqariOSLogo size={48} />
              <span lang="en">AqariOS</span>
            </a>
            <p>AqariOS — نظام متكامل لإدارة العقارات والإيجارات والعمليات المالية.</p>
          </div>

          <nav className="landing-footer__navigation" aria-label="روابط صفحة AqariOS">
            <h2>روابط التنقل</h2>
            <ul>
              {footerLinks.map((link) => (
                <li key={link.href}>
                  <a href={link.href}>{link.label}</a>
                </li>
              ))}
            </ul>
          </nav>

          <div className="landing-footer__contact">
            <h2>تواصل مباشر</h2>
            <a href="tel:+962789425056" dir="ltr">
              +962 7 8942 5056
            </a>
          </div>
        </div>

        <div className="landing-footer__copyright">
          <p>© 2026 AqariOS. جميع الحقوق محفوظة.</p>
        </div>
      </div>
    </footer>
  );
}
