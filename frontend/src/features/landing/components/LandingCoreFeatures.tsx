import { useState } from "react";
import { ArrowLeft } from "lucide-react";
import { DemoModulePage } from "@/features/demo/components/DemoModulePage";
import type { DemoModuleKey } from "@/features/demo/data/demoData";
import "./LandingCoreFeatures.css";

type CoreFeatureId =
  | "properties"
  | "leasing"
  | "payments"
  | "financials"
  | "documents"
  | "notifications"
  | "security"
  | "arabic";

interface CoreFeature {
  id: CoreFeatureId;
  number: string;
  title: string;
  description: string;
  visual: DemoModuleKey;
}

const CORE_FEATURES: readonly CoreFeature[] = [
  {
    id: "properties",
    number: "01",
    title: "العقارات",
    description: "إدارة المباني والوحدات والعقارات من مكان واحد.",
    visual: "buildings",
  },
  {
    id: "leasing",
    number: "02",
    title: "التأجير",
    description: "إدارة عقود الإيجار والمستأجرين ومتابعة دورة العقد.",
    visual: "leases",
  },
  {
    id: "payments",
    number: "03",
    title: "الدفعات",
    description: "متابعة الإيجارات والشيكات والاستحقاقات والحالات المالية.",
    visual: "payments",
  },
  {
    id: "financials",
    number: "04",
    title: "العمليات المالية",
    description: "إدارة المصاريف والإيرادات والإيصالات والتقارير المالية.",
    visual: "financial-operations",
  },
  {
    id: "documents",
    number: "05",
    title: "المستندات",
    description: "تنظيم مستندات العقارات والوحدات والعقود في مكان واحد.",
    visual: "documents",
  },
  {
    id: "notifications",
    number: "06",
    title: "الإشعارات",
    description: "تنبيهات مرتبطة بالعمليات والاستحقاقات المهمة.",
    visual: "notifications",
  },
  {
    id: "security",
    number: "07",
    title: "الصلاحيات والأمان",
    description: "تحكم كامل بالأدوار والصلاحيات والوصول إلى البيانات.",
    visual: "documents",
  },
  {
    id: "arabic",
    number: "08",
    title: "واجهة عربية بالكامل",
    description: "تجربة RTL مصممة لتناسب طريقة العمل في السوق الأردني.",
    visual: "buildings",
  },
];

export function LandingCoreFeatures() {
  const [activeFeatureId, setActiveFeatureId] = useState<CoreFeatureId>("properties");
  const activeFeature = CORE_FEATURES.find((feature) => feature.id === activeFeatureId) ?? CORE_FEATURES[0];

  const selectFeatureAt = (index: number) => {
    const feature = CORE_FEATURES[index];
    if (!feature) return;

    setActiveFeatureId(feature.id);
    window.requestAnimationFrame(() => {
      document.getElementById(`core-feature-tab-${feature.id}`)?.focus();
    });
  };

  return (
    <section id="features" className="landing-core-features" aria-labelledby="landing-core-features-title">
      <div className="landing-core-features__container">
        <header className="landing-core-features__header">
          <p className="landing-core-features__eyebrow">إمكانيات AqariOS</p>
          <h2 id="landing-core-features-title">
            كل عمليات إدارة العقار،
            <span>مترابطة في نظام واحد.</span>
          </h2>
          <p className="landing-core-features__intro">
            من إدارة العقارات والعقود إلى الدفعات والعمليات المالية والمستندات، AqariOS يجمع سير العمل اليومي في مكان واحد.
          </p>
        </header>

        <div className="landing-core-features__showcase">
          <div
            className="landing-core-features__selector"
            role="tablist"
            aria-label="إمكانيات AqariOS الأساسية"
            aria-orientation="vertical"
          >
            {CORE_FEATURES.map((feature, index) => {
              const isActive = feature.id === activeFeature.id;

              return (
                <button
                  key={feature.id}
                  id={`core-feature-tab-${feature.id}`}
                  type="button"
                  role="tab"
                  aria-selected={isActive}
                  aria-controls="core-feature-product-panel"
                  tabIndex={isActive ? 0 : -1}
                  className="landing-core-features__selector-item"
                  onClick={() => setActiveFeatureId(feature.id)}
                  onKeyDown={(event) => {
                    if (event.key === "ArrowDown") {
                      event.preventDefault();
                      selectFeatureAt((index + 1) % CORE_FEATURES.length);
                    } else if (event.key === "ArrowUp") {
                      event.preventDefault();
                      selectFeatureAt((index - 1 + CORE_FEATURES.length) % CORE_FEATURES.length);
                    } else if (event.key === "Home") {
                      event.preventDefault();
                      selectFeatureAt(0);
                    } else if (event.key === "End") {
                      event.preventDefault();
                      selectFeatureAt(CORE_FEATURES.length - 1);
                    }
                  }}
                >
                  <span className="landing-core-features__number">{feature.number}</span>
                  <span className="landing-core-features__copy">
                    <strong>{feature.title}</strong>
                    <span>{feature.description}</span>
                  </span>
                  <ArrowLeft className="landing-core-features__arrow" aria-hidden="true" />
                </button>
              );
            })}
          </div>

          <div className="landing-core-features__product">
            <div className="landing-core-features__product-heading" aria-live="polite">
              <div>
                <span>واجهة AqariOS</span>
                <h3>{activeFeature.title}</h3>
              </div>
              <p>{activeFeature.description}</p>
            </div>

            <div
              id="core-feature-product-panel"
              role="tabpanel"
              aria-labelledby={`core-feature-tab-${activeFeature.id}`}
              className="landing-core-features__product-frame"
            >
              <div key={activeFeature.id} className="landing-core-features__visual-stage">
                <div className="landing-core-features__visual-canvas">
                  <DemoModulePage moduleKey={activeFeature.visual} />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
