import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router";
import { Bell, Building2, Calculator, FileText, FolderOpen, Home, LayoutDashboard, LogOut, Play, ReceiptText, Users, Wallet } from "lucide-react";
import { toast } from "sonner";
import { AppShell } from "@/shared/components/layout/AppShell";
import { NavigationSidebar, type NavigationGroup } from "@/shared/components/layout/NavigationSidebar";
import { Topbar } from "@/shared/components/layout/Topbar";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { DemoDashboardContent } from "../components/DemoDashboardContent";
import { DemoModulePage } from "../components/DemoModulePage";
import { DemoTour, type DemoTourPhase, type DemoTourStep } from "../components/DemoTour";
import type { DemoModuleKey } from "../data/demoData";
import "./DemoExperience.css";

const DEMO_TOUR_SEEN_KEY = "aqarios_demo_tour_seen";

const TOUR_STEPS: readonly DemoTourStep[] = [
  { title: "لوحة التحكم", path: "/demo/dashboard", message: "من لوحة التحكم تتابع الصورة العامة لعقاراتك، العقود، الدفعات والعمليات المالية." },
  { title: "المباني والعقارات", path: "/demo/buildings", message: "أدر مبانيك وعقاراتك ووحداتك من مكان واحد." },
  { title: "الشقق والوحدات", path: "/demo/apartments", message: "اعرف حالة كل وحدة وما يرتبط بها من مستأجر وعقد." },
  { title: "عقود الإيجار", path: "/demo/leases", message: "أنشئ وتابع عقود الإيجار وحالتها." },
  { title: "المستأجرون", path: "/demo/tenants", message: "احتفظ ببيانات المستأجرين وربطها بالعقود والوحدات." },
  { title: "الدفعات والتحصيل", path: "/demo/payments", message: "تابع الدفعات المستحقة والمدفوعة والمتأخرة." },
  { title: "العمليات المالية", path: "/demo/financial-operations", message: "تابع العمليات المالية المرتبطة بإدارة عقاراتك." },
  { title: "المستندات والإشعارات", path: "/demo/documents", message: "كل المستندات والتنبيهات المهمة تبقى مرتبطة بسير عملك." },
] as const;

const DEMO_MODULE_KEYS = new Set<DemoModuleKey>([
  "buildings", "apartments", "leases", "tenants", "payments", "cheques", "financial-operations", "documents", "notifications",
]);

function getModuleKey(pathname: string): "dashboard" | DemoModuleKey {
  const segment = pathname.replace(/^\/demo\/?/, "").split("/")[0];
  return DEMO_MODULE_KEYS.has(segment as DemoModuleKey) ? segment as DemoModuleKey : "dashboard";
}

export function DemoExperience() {
  const navigate = useNavigate();
  const location = useLocation();
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isOpenMobile, setIsOpenMobile] = useState(false);
  const [tourStep, setTourStep] = useState(0);
  const [tourPhase, setTourPhase] = useState<DemoTourPhase>(() => {
    try {
      return sessionStorage.getItem(DEMO_TOUR_SEEN_KEY) ? "idle" : "intro";
    } catch {
      return "intro";
    }
  });

  const navigationGroups = useMemo<NavigationGroup[]>(() => [
    { id: "demo-main", label: "نظرة عامة", items: [{ id: "demo-dashboard", label: "لوحة التحكم", path: "/demo/dashboard", icon: LayoutDashboard }] },
    { id: "demo-assets", label: "إدارة العقارات", items: [
      { id: "demo-buildings", label: "المباني والعقارات", path: "/demo/buildings", icon: Building2 },
      { id: "demo-apartments", label: "الشقق والوحدات", path: "/demo/apartments", icon: Home },
    ] },
    { id: "demo-leasing", label: "الإيجارات والمستأجرون", items: [
      { id: "demo-leases", label: "عقود الإيجار", path: "/demo/leases", icon: FileText },
      { id: "demo-tenants", label: "المستأجرون", path: "/demo/tenants", icon: Users },
    ] },
    { id: "demo-finance", label: "الإدارة المالية", items: [
      { id: "demo-payments", label: "الدفعات والتحصيل", path: "/demo/payments", icon: Wallet },
      { id: "demo-cheques", label: "الشيكات", path: "/demo/cheques", icon: ReceiptText },
      { id: "demo-financials", label: "العمليات المالية", path: "/demo/financial-operations", icon: Calculator },
    ] },
    { id: "demo-operations", label: "المستندات والتنبيهات", items: [
      { id: "demo-documents", label: "المستندات", path: "/demo/documents", icon: FolderOpen },
      { id: "demo-notifications", label: "الإشعارات", path: "/demo/notifications", icon: Bell },
    ] },
  ], []);

  const rememberTour = () => {
    try { sessionStorage.setItem(DEMO_TOUR_SEEN_KEY, "true"); } catch { /* Storage is optional. */ }
  };

  const startTour = () => {
    setTourStep(0);
    setTourPhase("active");
    navigate(TOUR_STEPS[0].path);
  };

  const skipTour = () => {
    rememberTour();
    setTourPhase("idle");
  };

  const previousTourStep = () => {
    const nextIndex = Math.max(0, tourStep - 1);
    setTourStep(nextIndex);
    navigate(TOUR_STEPS[nextIndex].path);
  };

  const nextTourStep = () => {
    if (tourStep === TOUR_STEPS.length - 1) {
      rememberTour();
      setTourPhase("complete");
      return;
    }
    const nextIndex = tourStep + 1;
    setTourStep(nextIndex);
    navigate(TOUR_STEPS[nextIndex].path);
  };

  const moduleKey = getModuleKey(location.pathname);

  useEffect(() => {
    if (location.pathname === "/demo" || location.pathname === "/demo/") {
      navigate("/demo/dashboard", { replace: true });
    }
  }, [location.pathname, navigate]);

  return (
    <div className="demo-experience" dir="rtl" data-demo-tour-active={tourPhase === "active" || undefined}>
      <AppShell
        navigation={
          <NavigationSidebar
            identity={{ title: "AqariOS", subtitle: "إدارة العقارات" }}
            groups={navigationGroups}
            mobileOpen={isOpenMobile}
            onCloseMobile={() => setIsOpenMobile(false)}
            collapsed={isCollapsed}
            onToggleCollapsed={() => setIsCollapsed((value) => !value)}
            footer={<div className="demo-experience__sidebar-status"><span className="demo-experience__status-dot" />وضع التجربة · للقراءة فقط</div>}
          />
        }
        topbar={
          <Topbar
            onOpenMobileNav={() => setIsOpenMobile(true)}
            showSearch={false}
            showNotifications={false}
            showAccountMenu={false}
            showThemeSwitcherOnMobile
            contextLabelOverride="مؤسسة تجريبية لإدارة العقارات"
            trailingAction={
              <Button type="button" variant="text" size="sm" onClick={() => navigate("/")}>
                <LogOut aria-hidden="true" />الخروج من التجربة
              </Button>
            }
          />
        }
        beforeContent={
          <div className="demo-experience__banner">
            <div><Badge variant="tonal">وضع التجربة</Badge><span>بيانات آمنة وتجريبية — لن يتم حفظ أي تغييرات.</span></div>
            <Button type="button" variant="outlined" size="sm" onClick={startTour}><Play aria-hidden="true" />ابدأ الجولة</Button>
          </div>
        }
        contentClassName="demo-experience__content"
      >
        {moduleKey === "dashboard" ? <DemoDashboardContent /> : <DemoModulePage moduleKey={moduleKey} />}
      </AppShell>

      <DemoTour
        phase={tourPhase}
        stepIndex={tourStep}
        steps={TOUR_STEPS}
        onStart={startTour}
        onSkip={skipTour}
        onPrevious={previousTourStep}
        onNext={nextTourStep}
        onExplore={() => setTourPhase("idle")}
        onBookDemo={() => {
          rememberTour();
          toast.info("سنساعدك في حجز Demo كامل.");
          navigate("/#demo");
        }}
      />
    </div>
  );
}
