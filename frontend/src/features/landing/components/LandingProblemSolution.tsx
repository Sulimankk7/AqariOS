import { useEffect, useRef, useState } from "react";
import { Check, Minus } from "lucide-react";
import "./LandingProblemSolution.css";

const BEFORE_AQARIOS = [
  "Excel لإدارة الوحدات والبيانات",
  "WhatsApp للمتابعة والتواصل",
  "ملفات وعقود موزعة",
  "تذكيرات يدوية للدفعات",
  "صعوبة في رؤية الصورة المالية كاملة",
] as const;

const WITH_AQARIOS = [
  "بيانات العقارات والوحدات في مكان واحد",
  "إدارة العقود والمستأجرين",
  "متابعة الدفعات والشيكات",
  "تنظيم العمليات المالية",
  "مستندات وتنبيهات مرتبطة بسير العمل",
] as const;

interface ComparisonColumnProps {
  title: string;
  items: readonly string[];
  variant: "before" | "with";
}

function ComparisonColumn({ title, items, variant }: ComparisonColumnProps) {
  const isWithAqariOS = variant === "with";
  const Indicator = isWithAqariOS ? Check : Minus;

  return (
    <div className={`landing-problem__comparison landing-problem__comparison--${variant}`}>
      <h3>{title}</h3>
      <ul>
        {items.map((item) => (
          <li key={item}>
            <span className="landing-problem__indicator" aria-hidden="true">
              <Indicator />
            </span>
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function LandingProblemSolution() {
  const sectionRef = useRef<HTMLElement>(null);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const section = sectionRef.current;
    if (!section || typeof IntersectionObserver === "undefined") {
      setIsVisible(true);
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      { threshold: 0.18, rootMargin: "0px 0px -8%" },
    );

    observer.observe(section);
    return () => observer.disconnect();
  }, []);

  return (
    <section
      ref={sectionRef}
      className={`landing-problem${isVisible ? " landing-problem--visible" : ""}`}
      aria-labelledby="landing-problem-title"
    >
      <div className="landing-problem__container">
        <header className="landing-problem__header">
          <p className="landing-problem__eyebrow">المشكلة</p>
          <h2 id="landing-problem-title">عندما تكون بيانات العقار في كل مكان.</h2>
          <p className="landing-problem__description">
            المشكلة ليست في نقص الأدوات. المشكلة أن العمليات لا تتحدث مع بعضها.
          </p>
        </header>

        <div className="landing-problem__comparisons">
          <ComparisonColumn title="قبل AqariOS" items={BEFORE_AQARIOS} variant="before" />
          <ComparisonColumn title="مع AqariOS" items={WITH_AQARIOS} variant="with" />
        </div>
      </div>
    </section>
  );
}
