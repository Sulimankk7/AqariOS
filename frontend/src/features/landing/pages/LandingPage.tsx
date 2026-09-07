import { LandingNavbar } from "@/features/landing/components/LandingNavbar";
import { LandingHero } from "@/features/landing/components/LandingHero";
import { LandingInteractiveDemo } from "@/features/landing/components/LandingInteractiveDemo";
import { LandingProblemSolution } from "@/features/landing/components/LandingProblemSolution";
import "./LandingPage.css";

export default function LandingPage() {
  return (
    <div className="landing-page" dir="rtl">
      <LandingNavbar />
      <main>
        <LandingHero />
        <LandingProblemSolution />
        <LandingInteractiveDemo />
      </main>
    </div>
  );
}
