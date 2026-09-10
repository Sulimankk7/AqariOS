import { useEffect, useRef, useState } from "react";
import "./LandingJordanPropertyShowcase.css";

interface PropertyScene {
  id: string;
  name: string;
  location: string;
  image: string;
  alt: string;
  objectPosition: string;
  width: number;
  height: number;
}

type StoryKind = "property" | "units" | "payments" | "connected";

interface StoryStep {
  id: string;
  kind: StoryKind;
  propertyIndex: number;
  title: string;
}

const PROPERTY_SCENES: readonly PropertyScene[] = [
  {
    id: "yasmeen",
    name: "مبنى الياسمين",
    location: "عبدون، عمّان",
    image: "/branding/landing/jordan-property-yasmeen.jpg",
    alt: "مبنى الياسمين في عبدون، عمّان، ضمن مثال توضيحي",
    objectPosition: "50% 52%",
    width: 2500,
    height: 1669,
  },
  {
    id: "nakheel",
    name: "مبنى النخيل",
    location: "دابوق، عمّان",
    image: "/branding/landing/jordan-property-nakheel.jpg",
    alt: "مبنى النخيل في دابوق، عمّان، ضمن مثال توضيحي",
    objectPosition: "50% 50%",
    width: 835,
    height: 467,
  },
];

const STORY_STEPS: readonly StoryStep[] = [
  {
    id: "property",
    kind: "property",
    propertyIndex: 0,
    title: "كل عقار يبدأ من المبنى.",
  },
  {
    id: "units",
    kind: "units",
    propertyIndex: 0,
    title: "ثم تأتي الوحدات والعقود.",
  },
  {
    id: "payments",
    kind: "payments",
    propertyIndex: 0,
    title: "ثم تأتي الدفعات.",
  },
  {
    id: "connected",
    kind: "connected",
    propertyIndex: 1,
    title: "كلها مترابطة في AqariOS.",
  },
];

const UNIT_DATA = ["12 وحدة", "9 مؤجرة", "3 شاغرة"] as const;
const PAYMENT_FLOW = ["الإيجارات", "الشيكات", "الاستحقاقات"] as const;
const CONNECTED_FLOW = ["العقار", "الوحدات", "المستأجرون", "العقود", "الدفعات"] as const;

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

    const introObserver = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIntroVisible(true);
          introObserver.disconnect();
        }
      },
      { threshold: 0.2, rootMargin: "0px 0px -8%" },
    );

    if (introRef.current) introObserver.observe(introRef.current);

    const storyObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          const nextIndex = Number((entry.target as HTMLElement).dataset.sceneIndex);
          if (Number.isInteger(nextIndex)) setActiveStepIndex(nextIndex);
        });
      },
      { threshold: 0, rootMargin: "-38% 0px -48% 0px" },
    );

    storyStepRefs.current.forEach((step) => step && storyObserver.observe(step));

    return () => {
      introObserver.disconnect();
      storyObserver.disconnect();
    };
  }, []);

  const activeStep = STORY_STEPS[activeStepIndex] ?? STORY_STEPS[0];
  const activeProperty = PROPERTY_SCENES[activeStep.propertyIndex] ?? PROPERTY_SCENES[0];

  return (
    <section
      id="jordan"
      className="landing-jordan-showcase"
      aria-labelledby="landing-jordan-showcase-title"
    >
      <div
        ref={introRef}
        className={`landing-jordan-showcase__intro${introVisible ? " landing-jordan-showcase__intro--visible" : ""}`}
      >
        <p className="landing-jordan-showcase__eyebrow">مصمم للسوق الأردني</p>
        <h2 id="landing-jordan-showcase-title">إدارة العقارات، كما تعمل في الأردن.</h2>
        <p className="landing-jordan-showcase__lead">من العقار إلى الدفعة، كل شيء مرتبط.</p>
      </div>

      <div className="landing-jordan-showcase__journey">
        <figure className="landing-jordan-showcase__visual">
          <div className="landing-jordan-showcase__media">
            {PROPERTY_SCENES.map((scene, index) => {
              const isActive = index === activeStep.propertyIndex;
              return (
                <img
                  key={scene.id}
                  src={scene.image}
                  alt={scene.alt}
                  width={scene.width}
                  height={scene.height}
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
            <strong>{activeProperty.name} — {activeProperty.location}</strong>
          </div>

          <figcaption className="sr-only">
            أمثلة توضيحية لعقارات أردنية مرتبطة بمراحل إدارة العقار في AqariOS.
          </figcaption>
        </figure>

        <div className="landing-jordan-showcase__story" aria-label="رحلة إدارة العقار">
          {STORY_STEPS.map((step, index) => {
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
                  <p className="landing-jordan-showcase__step-number" aria-hidden="true">
                    {String(index + 1).padStart(2, "0")} / 04
                  </p>
                  <h3>{step.title}</h3>

                  {step.kind === "property" && (
                    <p className="landing-jordan-showcase__stage-meta">
                      مبنى الياسمين — عبدون، عمّان
                      <span>مثال توضيحي</span>
                    </p>
                  )}

                  {step.kind === "units" && (
                    <ul className="landing-jordan-showcase__metrics" aria-label="بيانات الوحدات التوضيحية">
                      {UNIT_DATA.map((item) => <li key={item}>{item}</li>)}
                    </ul>
                  )}

                  {step.kind === "payments" && (
                    <ul className="landing-jordan-showcase__labels" aria-label="مسار الدفعات">
                      {PAYMENT_FLOW.map((item) => <li key={item}>{item}</li>)}
                    </ul>
                  )}

                  {step.kind === "connected" && (
                    <div className="landing-jordan-showcase__connected">
                      <ol aria-label="تسلسل إدارة العقار">
                        {CONNECTED_FLOW.map((item) => <li key={item}>{item}</li>)}
                      </ol>
                      <p>AqariOS يجمعها في مكان واحد.</p>
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
