import "./LandingWhyAqarios.css";

const VALUE_POINTS = [
  {
    number: "01",
    title: "وضوح في الإدارة",
    description:
      "تعرف حالة عقاراتك وعقودك ودفعاتك بدون البحث بين ملفات ومحادثات متفرقة.",
  },
  {
    number: "02",
    title: "متابعة بدون نسيان",
    description:
      "الاستحقاقات والدفعات والعمليات المهمة تبقى ضمن سير عمل واضح بدل الاعتماد على التذكيرات الشخصية.",
  },
  {
    number: "03",
    title: "تحكم عندما يكبر العمل",
    description:
      "عندما يعمل أكثر من شخص على الإدارة، تبقى البيانات والصلاحيات والعمليات منظمة وواضحة.",
  },
] as const;

export function LandingWhyAqarios() {
  return (
    <section
      id="why-aqarios"
      className="landing-why-aqarios"
      aria-labelledby="landing-why-aqarios-title"
    >
      <div className="landing-why-aqarios__container">
        <div className="landing-why-aqarios__layout">
          <header className="landing-why-aqarios__header">
            <p className="landing-why-aqarios__eyebrow">لماذا AqariOS؟</p>
            <h2 id="landing-why-aqarios-title">
              لأن إدارة العقار لا يجب أن تعتمد على الذاكرة.
            </h2>
            <p className="landing-why-aqarios__intro">
              عندما تكبر عقاراتك، تصبح المتابعة اليدوية أصعب. AqariOS ينظم التفاصيل
              التي تحتاجها لتعرف ما تم، وما يستحق المتابعة، وما يحتاج إلى إجراء.
            </p>
          </header>

          <ol className="landing-why-aqarios__values" aria-label="أسباب اختيار AqariOS">
            {VALUE_POINTS.map((point) => (
              <li key={point.number} className="landing-why-aqarios__value">
                <span className="landing-why-aqarios__number" aria-hidden="true">
                  {point.number}
                </span>
                <div>
                  <h3>{point.title}</h3>
                  <p>{point.description}</p>
                </div>
              </li>
            ))}
          </ol>
        </div>

        <p className="landing-why-aqarios__closing">
          أنت تدير العقار. AqariOS ينظم التفاصيل.
        </p>
      </div>
    </section>
  );
}
