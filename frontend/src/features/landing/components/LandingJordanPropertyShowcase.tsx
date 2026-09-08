import { useEffect, useRef, useState } from "react";
import { ArrowLeft } from "lucide-react";
import { Link } from "react-router";
import "./LandingJordanPropertyShowcase.css";

interface PropertyScene {
  id: string;
  name: string;
  location: string;
  type: string;
  image: string;
  alt: string;
  objectPosition: string;
  annotations: readonly string[];
}

type StoryKind = "property" | "workflow" | "conclusion";

interface StoryStep {
  id: string;
  kind: StoryKind;
  propertyIndex: number;
  eyebrow: string;
  title: string;
  description: string;
}

const PROPERTY_SCENES: readonly PropertyScene[] = [
  {
    id: "yasmeen",
    name: "مبنى الياسمين",
    location: "عبدون، عمّان",
    type: "مبنى سكني متعدد الوحدات",
    image: "/branding/landing/jordan-property-yasmeen.jpg",
    alt: "مبنى سكني حديث متعدد الوحدات في عمّان ضمن مثال توضيحي",
    objectPosition: "50% 52%",
    annotations: ["العقارات", "الوحدات", "المستأجرون", "العقود"],
  },
  {
    id: "nakheel",
    name: "مبنى النخيل",
    location: "دابوق، عمّان",
    type: "نموذج لعقار سكني حديث",
    image: "/branding/landing/jordan-property-nakheel.jpg",
    alt: "واجهة مبنى سكني أردني حديث في دابوق ضمن مثال توضيحي",
    objectPosition: "50% 50%",
    annotations: ["العقود", "الدفعات", "الشيكات", "المستندات"],
  },
];

const STORY_STEPS: readonly StoryStep[] = [
  {
    id: "property-one",
    kind: "property",
    propertyIndex: 0,
    eyebrow: "العقار الأول",
    title: "تفاصيل المبنى تبدأ من صورة واضحة.",
    description: "من المبنى إلى كل وحدة داخله، تبقى المعلومات الأساسية منظمة وقريبة من سير العمل اليومي.",
  },
  {
    id: "workflow",
    kind: "workflow",
    propertyIndex: 0,
    eyebrow: "سير عمل مترابط",
    title: "كل تفصيل يقود إلى الخطوة التالية.",
    description: "بدل أن تعيش بيانات العقار في ملفات منفصلة، ترتبط الوحدة بالمستأجر والعقد والدفعة.",
  },
  {
    id: "property-two",
    kind: "property",
    propertyIndex: 1,
    eyebrow: "العقار الثاني",
    title: "عقار مختلف، وتفاصيل إدارة مختلفة.",
    description: "يبقى نفس السياق الإداري واضحًا مهما اختلف شكل المبنى أو عدد الوحدات أو دورة التحصيل.",
  },
  {
    id: "aqarios",
    kind: "conclusion",
    propertyIndex: 1,
    eyebrow: "AqariOS",
    title: "كل عقار له تفاصيله.",
    description: "لكن طريقة إدارته يمكن أن تكون أبسط.",
  },
];

const WORKFLOW_STEPS = ["العقار", "الوحدات", "المستأجرون", "العقود", "الدفعات"] as const;

export function LandingJordanPropertyShowcase() {
  const introRef = useRef<HTMLDivElement>(null);
  const storyStepRefs = useRef<Array<HTMLElement | null>>([]);
  const [introVisible, setIntroVisible] = useState(false);
  const [activeStepIndex, setActiveStepIndex] = useState(0);

  useEffect(() => {
    if (typeof IntersectionObserver === "undefined") {
      setIntroVisible(true);
      return;
    }

    const introObserver = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        setIntroVisible(true);
        introObserver.disconnect();
      }
    }, { threshold: 0.2, rootMargin: "0px 0px -8%" });

    if (introRef.current) introObserver.observe(introRef.current);

    const storyObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        const nextIndex = Number((entry.target as HTMLElement).dataset.sceneIndex);
        if (Number.isInteger(nextIndex)) setActiveStepIndex(nextIndex);
      });
    }, { threshold: 0, rootMargin: "-36% 0px -48% 0px" });

    storyStepRefs.current.forEach((step) => step && storyObserver.observe(step));

    return () => {
      introObserver.disconnect();
      storyObserver.disconnect();
    };
  }, []);

  const activeStep = STORY_STEPS[activeStepIndex] ?? STORY_STEPS[0];
  const activeProperty = PROPERTY_SCENES[activeStep.propertyIndex] ?? PROPERTY_SCENES[0];
  const isConclusion = activeStep.kind === "conclusion";

  return (
    <section id="jordan" className="landing-jordan-showcase" aria-labelledby="landing-jordan-showcase-title">
      <div
        ref={introRef}
        className={`landing-jordan-showcase__intro${introVisible ? " landing-jordan-showcase__intro--visible" : ""}`}
      >
        <p className="landing-jordan-showcase__eyebrow">مصمم للسوق الأردني</p>
        <h2 id="landing-jordan-showcase-title">إدارة العقارات، كما تعمل في الأردن.</h2>
        <p className="landing-jordan-showcase__lead">
          من المباني والوحدات إلى العقود والدفعات، AqariOS يجمع دورة إدارة العقار في مكان واحد.
        </p>
        <p className="landing-jordan-showcase__prompt">
          استكشف كيف يمكن أن تبدو إدارة العقار عندما تكون كل تفاصيله مترابطة.
        </p>
      </div>

      <div className="landing-jordan-showcase__journey">
        <figure className={`landing-jordan-showcase__visual${isConclusion ? " landing-jordan-showcase__visual--conclusion" : ""}`}>
          <div className="landing-jordan-showcase__media">
            {PROPERTY_SCENES.map((scene, index) => {
              const isActive = index === activeStep.propertyIndex;
              return (
                <img
                  key={scene.id}
                  src={scene.image}
                  alt={scene.alt}
                  loading="lazy"
                  decoding="async"
                  aria-hidden={!isActive}
                  className={`landing-jordan-showcase__image${isActive ? " landing-jordan-showcase__image--active" : ""}`}
                  style={{ objectPosition: scene.objectPosition }}
                />
              );
            })}
          </div>

          <div className="landing-jordan-showcase__visual-shade" aria-hidden="true" />

          <div key={activeProperty.id} className="landing-jordan-showcase__property-meta" aria-live="polite">
            <span>مثال توضيحي</span>
            <strong>{activeProperty.name}</strong>
            <p>{activeProperty.location}</p>
            <small>{activeProperty.type}</small>
          </div>

          <ul className="landing-jordan-showcase__annotations" aria-hidden="true">
            {activeProperty.annotations.map((annotation, index) => (
              <li key={annotation}>
                <span>{String(index + 1).padStart(2, "0")}</span>
                {annotation}
              </li>
            ))}
          </ul>

          <figcaption className="sr-only">
            أمثلة توضيحية لعقارات سكنية أردنية مرتبطة بسير عمل إدارة العقار في AqariOS.
          </figcaption>
        </figure>

        <div className="landing-jordan-showcase__story" aria-label="رحلة إدارة العقار">
          {STORY_STEPS.map((step, index) => {
            const property = PROPERTY_SCENES[step.propertyIndex];
            const isActive = index === activeStepIndex;

            return (
              <article
                key={step.id}
                ref={(element) => { storyStepRefs.current[index] = element; }}
                data-scene-index={index}
                className={`landing-jordan-showcase__step${isActive ? " landing-jordan-showcase__step--active" : ""}`}
                aria-current={isActive ? "step" : undefined}
              >
                <div className="landing-jordan-showcase__step-content">
                  <p className="landing-jordan-showcase__step-eyebrow">{step.eyebrow}</p>
                  <h3>{step.title}</h3>
                  <p className="landing-jordan-showcase__step-description">{step.description}</p>

                  {step.kind === "property" && property && (
                    <dl className="landing-jordan-showcase__details">
                      <div><dt>العقار</dt><dd>{property.name}</dd></div>
                      <div><dt>الموقع</dt><dd>{property.location}</dd></div>
                      <div><dt>السياق</dt><dd>مثال توضيحي</dd></div>
                    </dl>
                  )}

                  {step.kind === "workflow" && (
                    <ol className="landing-jordan-showcase__workflow" aria-label="تسلسل سير عمل العقار">
                      {WORKFLOW_STEPS.map((item, workflowIndex) => (
                        <li key={item}>
                          <span>{String(workflowIndex + 1).padStart(2, "0")}</span>
                          <strong>{item}</strong>
                        </li>
                      ))}
                    </ol>
                  )}

                  {step.kind === "conclusion" && (
                    <div className="landing-jordan-showcase__conclusion">
                      <strong>وAqariOS يجمعها في مكان واحد.</strong>
                      <p>العقارات، الوحدات، المستأجرون، العقود، الدفعات والمستندات ضمن سير عمل واحد.</p>
                      <Link to="/demo" className="landing-jordan-showcase__cta">
                        استكشف AqariOS
                        <ArrowLeft aria-hidden="true" />
                      </Link>
                    </div>
                  )}
                </div>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
