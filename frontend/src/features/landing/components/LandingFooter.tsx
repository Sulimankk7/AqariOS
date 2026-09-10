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
            <a
              className="landing-footer__whatsapp"
              href="https://wa.me/962789425056"
              target="_blank"
              rel="noopener noreferrer"
              dir="ltr"
              aria-label="تواصل معنا عبر واتساب على الرقم +962 7 8942 5056"
            >
              <svg aria-hidden="true" viewBox="0 0 32 32">
                <path
                  fill="currentColor"
                  d="M16.04 3A12.91 12.91 0 0 0 5.1 22.77L3 29l6.43-2.07A12.96 12.96 0 1 0 16.04 3Zm0 23.73c-2.1 0-4.15-.61-5.9-1.76l-.42-.25-3.82 1.23 1.25-3.71-.27-.43a10.72 10.72 0 1 1 9.16 4.92Zm5.89-8.04c-.32-.16-1.91-.94-2.21-1.05-.29-.11-.51-.16-.72.16-.22.33-.83 1.05-1.02 1.27-.19.21-.38.24-.7.08-1.9-.95-3.15-1.7-4.41-3.86-.34-.59.34-.55.97-1.82.11-.22.05-.41-.03-.57-.08-.16-.72-1.75-.99-2.39-.26-.62-.53-.54-.72-.55h-.62c-.22 0-.57.08-.86.41-.3.32-1.13 1.1-1.13 2.69s1.16 3.12 1.32 3.34c.16.21 2.28 3.48 5.52 4.88 2.05.88 2.86.96 3.89.81 1.25-.19 1.91-1.02 2.18-2 .27-.97.27-1.8.19-1.99-.08-.18-.29-.27-.62-.43Z"
                />
              </svg>
              <span>+962 7 8942 5056</span>
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
