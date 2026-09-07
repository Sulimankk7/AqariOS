import { toast } from "sonner";
import { DashboardHeader } from "@/features/dashboard/components/DashboardHeader";
import { KpiGrid } from "@/features/dashboard/components/KpiGrid";
import { QuickActions } from "@/features/dashboard/components/QuickActions";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { DEMO_DASHBOARD_SUMMARY } from "../data/demoData";

export interface DemoDashboardContentProps {
  preview?: boolean;
}

export function DemoDashboardContent({ preview = false }: DemoDashboardContentProps) {
  const explainReadOnly = () => {
    if (!preview) {
      toast.info("وضع التجربة للقراءة فقط. هذه العملية متاحة في النسخة الكاملة.");
    }
  };

  return (
    <div data-demo-module="dashboard" className={preview ? "demo-dashboard-content demo-dashboard-content--preview" : "demo-dashboard-content"}>
      <PageContainer>
        <DashboardHeader dataUpdatedAt={Date.UTC(2026, 8, 7, 11, 30)} />
        <QuickActions
          excludedActionIds={["report-maintenance"]}
          onAction={explainReadOnly}
        />
        <KpiGrid data={DEMO_DASHBOARD_SUMMARY} routePrefix="/demo" />
      </PageContainer>
    </div>
  );
}
