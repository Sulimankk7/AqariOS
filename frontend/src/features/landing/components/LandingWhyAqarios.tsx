import { useEffect, useRef, useState } from "react";
import "./LandingWhyAqarios.css";

const VALUE_POINTS = [
  {
    number: "01",
    title: "كل شيء في مكان واحد",
    description: "العقارات، الوحدات، المستأجرون، العقود والدفعات ضمن نظام واحد.",
  },
  {
    number: "02",
    title: "رؤية أوضح",
    description: "اعرف حالة عقاراتك والتزاماتك وعملياتك المالية بدون جمع البيانات يدويًا.",
  },
  {
    number: "03",
    title: "عمليات مترابطة",
    description: "العقد والدفعة والمستند والتنبيه جزء من نفس سير العمل.",
  },
  {
    number: "04",
    title: "تحكم وأمان",
    description: "صلاحيات وأدوار وإدارة وصول مناسبة لبيئة إدارة العقارات.",
  },
] as const;

export function LandingWhyAqarios() {
  const sectionRef = useRef<HTMLElement>(null);
  const [isVisible, setIsVisible] = useState(false);

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
    }, { threshold: 0.14, rootMargin: "0px 0px -6%" });

    observer.observe(section);
    return () => observer.disconnect();
  }, []);

  return (
    <section
      id="why-aqarios"
      ref={sectionRef}
      className={`landing-why-aqarios${isVisible ? " landing-why-aqarios--visible" : ""}`}
      aria-labelledby="landing-why-aqarios-title"
    >
      <div className="landing-why-aqarios__container">
        <header className="landing-why-aqarios__header">
          <p className="landing-why-aqarios__eyebrow">لماذا AqariOS؟</p>
          <h2 id="landing-why-aqarios-title">
            <span>لأن إدارة العقار لا يجب أن تكون</span>{" "}
            <span>مجموعة أدوات منفصلة.</span>
          </h2>
          <p className="landing-why-aqarios__intro">
            AqariOS يجمع العمليات اليومية في نظام واحد، بحيث تكون بيانات العقار والعقود والدفعات والعمليات المالية مترابطة بدل ما تكون موزعة بين ملفات وأدوات مختلفة.
          </p>
        </header>

        <ol className="landing-why-aqarios__values" aria-label="أسباب اختيار AqariOS">
          {VALUE_POINTS.map((point) => (
            <li key={point.number} className="landing-why-aqarios__value">
              <article>
                <div className="landing-why-aqarios__value-meta">
                  <span className="landing-why-aqarios__number">{point.number}</span>
                  <span className="landing-why-aqarios__accent" aria-hidden="true" />
                </div>
                <h3>{point.title}</h3>
                <p>{point.description}</p>
              </article>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
