import "./LandingComingSoon.css";

interface FutureItem {
  index: string;
  title: string;
  statement: string;
  description: string;
}

const FUTURE_ITEMS: readonly FutureItem[] = [
  {
    index: "01",
    title: "Marketplace",
    statement: "حوّل الوحدات الشاغرة إلى فرص إيجار.",
    description:
      "اعرض العقار مع تفاصيله وصوره وسعر الإيجار، واستقبل طلبات المهتمين لمعاينة الوحدة.",
  },
  {
    index: "02",
    title: "Maintenance",
    statement: "من بلاغ الصيانة إلى إغلاق الطلب.",
    description:
      "سجّل طلبات الصيانة المرتبطة بالعقار أو الوحدة، وأرفق الصور والملاحظات وتابع حالة الطلب حتى إغلاقه.",
  },
];

export function LandingComingSoon() {
  return (
    <section className="landing-coming-soon" aria-labelledby="landing-coming-soon-title">
      <div className="landing-coming-soon__container">
        <header className="landing-coming-soon__header">
          <p className="landing-coming-soon__eyebrow">قريبًا</p>
          <h2 id="landing-coming-soon-title">AqariOS لا يتوقف هنا.</h2>
          <p className="landing-coming-soon__lead">
            نبني المزيد من الأدوات التي تكمل دورة إدارة العقار من تأجير الوحدة إلى
            متابعة ما يحدث داخلها.
          </p>
        </header>

        <ol className="landing-coming-soon__roadmap" aria-label="الميزات القادمة في AqariOS">
          {FUTURE_ITEMS.map((item) => (
            <li className="landing-coming-soon__item" key={item.index}>
              <article>
                <div className="landing-coming-soon__marker" aria-hidden="true">
                  <span>{item.index}</span>
                </div>
                <div className="landing-coming-soon__item-copy">
                  <h3 dir="ltr">{item.title}</h3>
                  <p className="landing-coming-soon__statement">{item.statement}</p>
                  <p className="landing-coming-soon__description">{item.description}</p>
                  <p className="landing-coming-soon__status" dir="ltr">COMING SOON</p>
                </div>
              </article>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
