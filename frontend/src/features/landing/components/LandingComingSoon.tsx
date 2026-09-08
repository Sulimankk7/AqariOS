import { useEffect, useRef, useState, type CSSProperties } from "react";
import "./LandingComingSoon.css";

interface FutureItem {
  index: string;
  title: string;
  statement: string;
  description: string;
  flow: readonly string[];
}

const FUTURE_ITEMS: readonly FutureItem[] = [
  {
    index: "01",
    title: "Marketplace",
    statement: "حوّل الوحدات الشاغرة إلى فرص إيجار.",
    description:
      "اعرض العقار مع صوره وتفاصيله وسعر الإيجار، واستقبل طلبات المهتمين لمعاينة الوحدة، مع إدارة الطلبات من داخل AqariOS.",
    flow: [
      "وحدة شاغرة",
      "عرض العقار",
      "التفاصيل والصور",
      "طلب معاينة",
      "إدارة الطلب داخل AqariOS",
    ],
  },
  {
    index: "02",
    title: "Maintenance",
    statement: "من بلاغ الصيانة إلى إغلاق الطلب.",
    description:
      "سجّل طلبات الصيانة المرتبطة بالعقار أو الوحدة، أرفق الصور والملاحظات، تابع حالة الطلب وتحديثاته، وحافظ على سجل واضح للعملية.",
    flow: [
      "بلاغ صيانة",
      "تفاصيل وصور وملاحظات",
      "متابعة حالة الطلب",
      "تحديثات وتعليقات",
      "إغلاق الطلب",
    ],
  },
];

export function LandingComingSoon() {
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
      { threshold: 0.16 },
    );

    observer.observe(section);

    return () => observer.disconnect();
  }, []);

  return (
    <section
      ref={sectionRef}
      className={`landing-coming-soon${isVisible ? " landing-coming-soon--visible" : ""}`}
      aria-labelledby="landing-coming-soon-title"
    >
      <div className="landing-coming-soon__container">
        <header className="landing-coming-soon__header">
          <p className="landing-coming-soon__eyebrow">قريبًا</p>
          <h2 id="landing-coming-soon-title">وAqariOS لا يتوقف هنا.</h2>
          <p className="landing-coming-soon__lead">
            نبني المزيد من الأدوات التي تكمل دورة إدارة العقار، من تأجير الوحدة إلى متابعة ما يحدث داخلها.
          </p>
        </header>

        <div className="landing-coming-soon__continuity" aria-label="امتداد تجربة AqariOS الحالية">
          <span>AqariOS اليوم</span>
          <i aria-hidden="true" />
          <span>مسارات مستقبلية</span>
        </div>

        <ol className="landing-coming-soon__roadmap" aria-label="الميزات القادمة في AqariOS">
          {FUTURE_ITEMS.map((item, itemIndex) => (
            <li
              className="landing-coming-soon__item"
              key={item.index}
              style={{ "--future-item-index": itemIndex } as CSSProperties}
            >
              <article>
                <div className="landing-coming-soon__marker" aria-hidden="true">
                  <span>{item.index}</span>
                </div>
                <div className="landing-coming-soon__item-copy">
                  <h3 dir="ltr">{item.title}</h3>
                  <p className="landing-coming-soon__statement">{item.statement}</p>
                  <p className="landing-coming-soon__description">{item.description}</p>
                  <p className="landing-coming-soon__status" dir="ltr">COMING SOON</p>
                  <ol className="landing-coming-soon__flow" aria-label={`مسار ${item.title}`}>
                    {item.flow.map((step) => (
                      <li key={step}>{step}</li>
                    ))}
                  </ol>
                </div>
              </article>
            </li>
          ))}
        </ol>

        <p className="landing-coming-soon__closing">
          <span aria-hidden="true" />
          المزيد قادم.
          <span aria-hidden="true" />
        </p>
      </div>
    </section>
  );
}
