import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { Building2, FileText, Home, LayoutDashboard, Play } from "lucide-react";
import { DemoDashboardContent } from "@/features/demo/components/DemoDashboardContent";
import { NavigationSidebar, type NavigationGroup } from "@/shared/components/layout/NavigationSidebar";
import { Topbar } from "@/shared/components/layout/Topbar";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import "./LandingInteractiveDemo.css";

const PREVIEW_NAVIGATION: NavigationGroup[] = [
  { id: "preview-main", label: "نظرة عامة", items: [{ id: "preview-dashboard", label: "لوحة التحكم", path: "/demo/dashboard", icon: LayoutDashboard }] },
  { id: "preview-assets", label: "إدارة العقارات", items: [
    { id: "preview-buildings", label: "المباني والعقارات", path: "/demo/buildings", icon: Building2 },
    { id: "preview-apartments", label: "الشقق والوحدات", path: "/demo/apartments", icon: Home },
    { id: "preview-leases", label: "عقود الإيجار", path: "/demo/leases", icon: FileText },
  ] },
];

export function LandingInteractiveDemo() {
  const navigate = useNavigate();
  const sectionRef = useRef<HTMLElement>(null);
  const [isVisible, setIsVisible] = useState(false);
  const [isPreviewNavOpen, setIsPreviewNavOpen] = useState(false);

  useEffect(() => {
    const section = sectionRef.current;
    if (!section || typeof IntersectionObserver === "undefined") {
      setIsVisible(true);
      return;
    }

    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        setIsVisible(true);
        observer.disconnect();
      }
    }, { threshold: 0.12, rootMargin: "0px 0px -6%" });

    observer.observe(section);
    return () => observer.disconnect();
  }, []);

  return (
    <section
      id="demo"
      ref={sectionRef}
      className={`landing-interactive-demo${isVisible ? " landing-interactive-demo--visible" : ""}`}
      aria-labelledby="landing-interactive-demo-title"
    >
      <div className="landing-interactive-demo__container">
        <header className="landing-interactive-demo__header">
          <div>
            <p className="landing-interactive-demo__eyebrow">شاهد AqariOS أثناء العمل</p>
            <h2 id="landing-interactive-demo-title">كل ما تحتاجه لإدارة عقاراتك، في مكان واحد.</h2>
            <p>استكشف لوحة التحكم الحقيقية وتعرّف على طريقة إدارة العقارات والعقود والمستأجرين والدفعات من خلال جولة تفاعلية.</p>
          </div>
          <div className="landing-interactive-demo__actions">
            <Button type="button" size="lg" onClick={() => navigate("/demo")}><Play aria-hidden="true" />ابدأ الجولة التفاعلية</Button>
            <Button type="button" variant="outlined" size="lg" onClick={() => { window.location.hash = "demo"; }}>احجز Demo كامل</Button>
          </div>
        </header>

        <div className="landing-interactive-demo__preview" aria-label="معاينة حقيقية للوحة تحكم AqariOS">
          <NavigationSidebar
            identity={{ title: "AqariOS", subtitle: "إدارة العقارات" }}
            groups={PREVIEW_NAVIGATION}
            mobileOpen={isPreviewNavOpen}
            onCloseMobile={() => setIsPreviewNavOpen(false)}
            collapsed={false}
            footer={<div className="landing-interactive-demo__preview-status"><span />بيانات تجريبية</div>}
          />
          <div className="landing-interactive-demo__preview-workspace">
            <Topbar
              onOpenMobileNav={() => setIsPreviewNavOpen(true)}
              showSearch={false}
              showNotifications={false}
              showAccountMenu={false}
              showThemeSwitcherOnMobile
              contextLabelOverride="مؤسسة تجريبية لإدارة العقارات"
              trailingAction={<Badge variant="tonal">وضع التجربة</Badge>}
            />
            <div className="landing-interactive-demo__preview-content">
              <DemoDashboardContent preview />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
