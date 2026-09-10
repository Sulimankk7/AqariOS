import { LandingNavbar } from "@/features/landing/components/LandingNavbar";
import { LandingHero } from "@/features/landing/components/LandingHero";
import { LandingCoreFeatures } from "@/features/landing/components/LandingCoreFeatures";
import { LandingComingSoon } from "@/features/landing/components/LandingComingSoon";
import { LandingFinalCta } from "@/features/landing/components/LandingFinalCta";
import { LandingContactForm } from "@/features/landing/components/LandingContactForm";
import { LandingPricing } from "@/features/landing/components/LandingPricing";
import { LandingJordanPropertyShowcase } from "@/features/landing/components/LandingJordanPropertyShowcase";
import { LandingProblemSolution } from "@/features/landing/components/LandingProblemSolution";
import { LandingWhyAqarios } from "@/features/landing/components/LandingWhyAqarios";
import { LandingFooter } from "@/features/landing/components/LandingFooter";
import "./LandingPage.css";

export default function LandingPage() {
  return (
    <div className="landing-page" dir="rtl">
      <LandingNavbar />
      <main>
        <LandingHero />
        <LandingProblemSolution />
        <LandingCoreFeatures />
        
        <LandingWhyAqarios />
        <LandingComingSoon />
        <LandingPricing />
        <LandingFinalCta />
        <LandingContactForm />
      </main>
      <LandingFooter />
    </div>
  );
}
