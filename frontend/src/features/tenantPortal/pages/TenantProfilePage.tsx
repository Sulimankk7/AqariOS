import React from "react";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useQuery } from "@tanstack/react-query";
import { tenantPortalApi } from "../api/tenantPortal.api";


export function TenantProfilePage() {
  const { t } = useTranslation();

  const { data: profile, isLoading, isError } = useQuery({
    queryKey: ["tenant", "profile"],
    queryFn: () => tenantPortalApi.getProfile(),
    staleTime: 5 * 60 * 1000,
  });

  return (
    <PageContainer
      title={t("tenant.profile.title", "My Profile")}
      description={t("tenant.profile.subtitle", "Manage your personal information")}
    >
      <div className="rounded-xl border bg-card text-card-foreground shadow-sm">
        <div className="flex flex-col space-y-1.5 p-6">
          <h3 className="font-semibold leading-none tracking-tight">{t("tenant.profile.personalInformation", "Personal Information")}</h3>
        </div>
        <div className="p-6 pt-0">
          {isLoading ? (
            <div className="space-y-4">
              <div className="h-4 w-[250px] animate-pulse rounded bg-muted" />
              <div className="h-4 w-[200px] animate-pulse rounded bg-muted" />
              <div className="h-4 w-[300px] animate-pulse rounded bg-muted" />
            </div>
          ) : isError ? (
            <p className="text-sm text-destructive">{t("common.error", "An error occurred while loading data.")}</p>
          ) : profile ? (
            <div className="space-y-4 text-sm">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <span className="font-semibold">{t("tenant.profile.fullName", "Full Name")}: </span>
                  <span className="text-muted-foreground">{profile.fullName}</span>
                </div>
                {profile.email && (
                  <div>
                    <span className="font-semibold">{t("tenant.profile.email", "Email")}: </span>
                    <span className="text-muted-foreground">{profile.email}</span>
                  </div>
                )}
                {profile.phone && (
                  <div>
                    <span className="font-semibold">{t("tenant.profile.phone", "Phone")}: </span>
                    <span className="text-muted-foreground">{profile.phone}</span>
                  </div>
                )}
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </PageContainer>
  );
}
