import React from "react";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";


export function TenantDashboardPage() {
  const { t } = useTranslation();

  return (
    <PageContainer
      title={t("tenant.dashboard.title", "Dashboard")}
      description={t("tenant.dashboard.subtitle", "Welcome to your tenant portal")}
    >
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-xl border bg-card text-card-foreground shadow-sm">
          <div className="flex flex-col space-y-1.5 p-6">
            <h3 className="font-semibold leading-none tracking-tight">{t("tenant.dashboard.overview", "Overview")}</h3>
          </div>
          <div className="p-6 pt-0">
            <p className="text-sm text-muted-foreground">
              {t("tenant.dashboard.emptyState", "No recent activity to display.")}
            </p>
          </div>
        </div>
      </div>
    </PageContainer>
  );
}
