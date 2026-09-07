import { ArrowLeft, ArrowRight, Check } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";

export interface DemoTourStep {
  title: string;
  message: string;
  path: string;
}

export type DemoTourPhase = "intro" | "active" | "complete" | "idle";

export interface DemoTourProps {
  phase: DemoTourPhase;
  stepIndex: number;
  steps: readonly DemoTourStep[];
  onStart: () => void;
  onSkip: () => void;
  onPrevious: () => void;
  onNext: () => void;
  onExplore: () => void;
  onBookDemo: () => void;
}

export function DemoTour({ phase, stepIndex, steps, onStart, onSkip, onPrevious, onNext, onExplore, onBookDemo }: DemoTourProps) {
  if (phase === "idle") return null;

  if (phase === "intro") {
    return (
      <Dialog open onOpenChange={(open) => !open && onSkip()}>
        <DialogContent className="demo-tour__modal">
          <span className="demo-tour__mark" aria-hidden="true"><Check /></span>
          <DialogHeader className="items-center pe-0 text-center">
            <DialogTitle>مرحبًا بك في AqariOS</DialogTitle>
            <DialogDescription>خلينا نأخذ جولة سريعة على أهم أجزاء النظام.</DialogDescription>
          </DialogHeader>
          <DialogFooter className="demo-tour__actions">
            <Button type="button" onClick={onStart}>ابدأ الجولة</Button>
            <Button type="button" variant="text" onClick={onSkip}>تخطي الجولة</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  if (phase === "complete") {
    return (
      <Dialog open onOpenChange={(open) => !open && onExplore()}>
        <DialogContent className="demo-tour__modal">
          <span className="demo-tour__mark" aria-hidden="true"><Check /></span>
          <DialogHeader className="items-center pe-0 text-center">
            <DialogTitle>هذه مجرد جولة سريعة.</DialogTitle>
            <DialogDescription>اكتشف AqariOS بنفسك.</DialogDescription>
          </DialogHeader>
          <DialogFooter className="demo-tour__actions">
            <Button type="button" onClick={onExplore}>استكشف النظام</Button>
            <Button type="button" variant="outlined" onClick={onBookDemo}>احجز Demo كامل</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  const step = steps[stepIndex];
  return (
    <>
      <div className="demo-tour__spotlight" aria-hidden="true" />
      <aside className="demo-tour__panel" aria-live="polite" aria-label={`الخطوة ${stepIndex + 1} من ${steps.length}`}>
        <div className="demo-tour__progress"><span style={{ width: `${((stepIndex + 1) / steps.length) * 100}%` }} /></div>
        <p className="demo-tour__count">الخطوة {stepIndex + 1} من {steps.length}</p>
        <h2>{step.title}</h2>
        <p>{step.message}</p>
        <div className="demo-tour__panel-actions">
          <Button type="button" variant="text" onClick={onSkip}>تخطي</Button>
          <div>
            <Button type="button" variant="outlined" size="sm" onClick={onPrevious} disabled={stepIndex === 0} aria-label="الخطوة السابقة"><ArrowRight /></Button>
            <Button type="button" size="sm" onClick={onNext}>
              {stepIndex === steps.length - 1 ? "إنهاء" : "التالي"}
              <ArrowLeft />
            </Button>
          </div>
        </div>
      </aside>
    </>
  );
}
