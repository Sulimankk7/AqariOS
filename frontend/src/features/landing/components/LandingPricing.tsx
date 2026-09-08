import { Check } from "lucide-react";
import { buttonVariants } from "@/shared/ui/button";
import { cn } from "@/shared/ui/utils";
import "./LandingPricing.css";

const features = [
  "إدارة العقارات والوحدات",
  "عقود الإيجار والمستأجرون",
  "الدفعات والشيكات",
  "العمليات المالية",
  "المستندات والتنبيهات",
];

export function LandingPricing() {
  return (
    <section id="pricing" className="landing-pricing" aria-labelledby="landing-pricing-title">
      <div className="landing-pricing__container">
        <header className="landing-pricing__header">
          <p className="landing-pricing__eyebrow">الأسعار</p>
          <h2 id="landing-pricing-title">أسعار بسيطة، تنمو معك.</h2>
          <p>ادفع حسب عدد مستأجريك، واختر طريقة الدفع المناسبة لك.</p>
        </header>

        <div className="landing-pricing__plans">
          <PricingPlan
            period="شهريًا"
            price="10 د.أ ثابتة + 3 د.أ لكل مستأجر"
            description="ادفع شهريًا حسب عدد مستأجريك."
          />
          <PricingPlan
            period="سنويًا"
            price="10 د.أ ثابتة + 2 د.أ لكل مستأجر"
            description="نفس المزايا، بسعر أقل."
            savings="وفّر 1 د.أ لكل مستأجر شهريًا"
            savingsDetail="يعني 12 د.أ توفير لكل مستأجر سنويًا"
            badge="الأفضل قيمة"
            emphasized
          />
        </div>
      </div>
    </section>
  );
}

function PricingPlan({
  period,
  price,
  description,
  savings,
  savingsDetail,
  badge,
  emphasized = false,
}: {
  period: string;
  price: string;
  description: string;
  savings?: string;
  savingsDetail?: string;
  badge?: string;
  emphasized?: boolean;
}) {
  return (
    <article className={`landing-pricing__plan${emphasized ? " landing-pricing__plan--emphasized" : ""}`}>
      <div className="landing-pricing__plan-heading">
        <div>
          <h3 className="landing-pricing__plan-name">الدفع حسب الاستخدام</h3>
          <p className="landing-pricing__period">{period}</p>
        </div>
        {badge && <span className="landing-pricing__badge">{badge}</span>}
      </div>

      <p className="landing-pricing__formula">{price}</p>
      {savings && (
        <div className="landing-pricing__savings">
          <strong>{savings}</strong>
          {savingsDetail && <span>{savingsDetail}</span>}
        </div>
      )}
      <p className="landing-pricing__description">{description}</p>

      <ul className="landing-pricing__features">
        {features.map((feature) => (
          <li key={feature}>
            <Check aria-hidden="true" />
            <span>{feature}</span>
          </li>
        ))}
      </ul>

      <a
        href="#contact"
        className={cn(
          buttonVariants({ size: "lg", variant: emphasized ? "default" : "outlined" }),
          "landing-pricing__cta",
        )}
      >
        تواصل معنا
      </a>
    </article>
  );
}
