import { useCallback, useEffect, useRef, useState, type MouseEvent } from "react";
import { Button } from "@/shared/ui/button";
import { AqariosLogoDrawAnimation } from "./AqariosLogoDrawAnimation";
import "./LandingFinalCta.css";

type AnimationState = "idle" | "animating" | "complete";

const ANIMATION_FALLBACK_MS = 2700;
const COMPLETION_PAUSE_MS = 180;

function scrollToContact(behavior: ScrollBehavior) {
  const contact = document.getElementById("contact");
  contact?.scrollIntoView({ behavior, block: "start" });
  contact?.focus({ preventScroll: true });
}

export function LandingFinalCta() {
  const [animationState, setAnimationState] = useState<AnimationState>("idle");
  const [prefersReducedMotion, setPrefersReducedMotion] = useState(false);
  const completionHandledRef = useRef(false);
  const fallbackTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const transitionTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const mediaQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    const updateMotionPreference = () => setPrefersReducedMotion(mediaQuery.matches);

    updateMotionPreference();
    mediaQuery.addEventListener("change", updateMotionPreference);

    return () => mediaQuery.removeEventListener("change", updateMotionPreference);
  }, []);

  useEffect(
    () => () => {
      if (fallbackTimerRef.current) clearTimeout(fallbackTimerRef.current);
      if (transitionTimerRef.current) clearTimeout(transitionTimerRef.current);
    },
    [],
  );

  const handleLogoAnimationComplete = useCallback(() => {
    if (completionHandledRef.current) return;

    completionHandledRef.current = true;
    if (fallbackTimerRef.current) clearTimeout(fallbackTimerRef.current);
    setAnimationState("complete");
    transitionTimerRef.current = setTimeout(
      () => scrollToContact(prefersReducedMotion ? "auto" : "smooth"),
      COMPLETION_PAUSE_MS,
    );
  }, [prefersReducedMotion]);

  const handlePrimaryAction = useCallback(() => {
    if (animationState === "animating") return;

    if (animationState === "complete") {
      scrollToContact(prefersReducedMotion ? "auto" : "smooth");
      return;
    }

    completionHandledRef.current = false;
    setAnimationState("animating");
    fallbackTimerRef.current = setTimeout(
      handleLogoAnimationComplete,
      prefersReducedMotion ? 0 : ANIMATION_FALLBACK_MS,
    );
  }, [animationState, handleLogoAnimationComplete, prefersReducedMotion]);

  const handleContactLink = useCallback(
    (event: MouseEvent<HTMLAnchorElement>) => {
      event.preventDefault();
      scrollToContact(prefersReducedMotion ? "auto" : "smooth");
    },
    [prefersReducedMotion],
  );

  return (
    <section className="landing-final-cta" aria-labelledby="landing-final-cta-title">
      <div className="landing-final-cta__container">
        <div className="landing-final-cta__logo-stage">
          <AqariosLogoDrawAnimation
            isAnimating={animationState === "animating"}
            prefersReducedMotion={prefersReducedMotion}
            onComplete={handleLogoAnimationComplete}
          />
        </div>

        <header className="landing-final-cta__header">
          <p className="landing-final-cta__eyebrow">جاهز تبسّط إدارة عقاراتك؟</p>
          <h2 id="landing-final-cta-title">ابدأ إدارة عقاراتك بطريقة مختلفة.</h2>
          <p className="landing-final-cta__lead">
            جرّب AqariOS مجانًا لمدة <strong>30 يومًا</strong>، واكتشف كيف يمكن أن تجمع إدارة العقارات والعقود
            والمستأجرين والدفعات والعمليات اليومية في مكان واحد.
          </p>
        </header>

        <div className="landing-final-cta__actions">
          <Button
            type="button"
            size="lg"
            className="landing-final-cta__primary"
            disabled={animationState === "animating"}
            aria-busy={animationState === "animating"}
            aria-describedby="landing-final-cta-support"
            onClick={handlePrimaryAction}
          >
            {animationState === "animating"
              ? "نجهّز تجربتك..."
              : "ابدأ تجربتك المجانية لمدة 30 يومًا"}
          </Button>
          <a className="landing-final-cta__secondary" href="#contact" onClick={handleContactLink}>
            تواصل معنا
          </a>
          <p id="landing-final-cta-support">بدون التزام. سنتواصل معك لمساعدتك في بدء تجربتك.</p>
        </div>
      </div>

    </section>
  );
}
