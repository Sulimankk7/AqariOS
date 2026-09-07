import { ArrowLeft } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { HeroArchitectureBackground } from "./HeroArchitectureBackground";
import { HeroProductVisual } from "./HeroProductVisual";
import "./LandingHero.css";

function HeroActions() {
  return (
    <div className="landing-hero__actions">
      <Button
        className="rounded-sm"
        size="lg"
        type="button"
        onClick={() => {
          window.location.hash = "demo";
        }}
      >
        احجز Demo الآن
      </Button>
      <Button
        className="rounded-sm"
        variant="outlined"
        size="lg"
        type="button"
        onClick={() => {
          window.location.hash = "interactive-experience";
        }}
      >
        جرّب AqariOS تفاعليًا
        <ArrowLeft aria-hidden="true" />
      </Button>
    </div>
  );
}

function HeroContent() {
  return (
    <div className="landing-hero__content">
      <p className="landing-hero__eyebrow" lang="en">
        AqariOS
      </p>
      <h1 id="landing-hero-title " className="landing-hero__title" lang="ar" >
        إدارة عقاراتك من مكان واحد
        
      </h1>
      <p className="landing-hero__description">
        AqariOS منصة متكاملة لإدارة العقارات والإيجارات والدفعات والعمليات اليومية، مصممة للسوق الأردني.
      </p>
      <HeroActions />
    </div>
  );
}

export function LandingHero() {
  return (
    <section className="landing-hero" aria-labelledby="landing-hero-title">
      <HeroArchitectureBackground />
      <div className="landing-hero__container">
        <HeroContent />
        <div className="landing-hero__visual">
          <HeroProductVisual />
        </div>
      </div>
    </section>
  );
}
