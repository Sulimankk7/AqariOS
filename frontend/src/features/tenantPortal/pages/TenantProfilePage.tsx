import React from "react";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useQuery } from "@tanstack/react-query";
import { tenantPortalApi } from "../api/tenantPortal.api";
import { useAuth } from "@/features/auth/hooks/useAuth";
import {
  User,
  Phone,
  CreditCard,
  Briefcase,
  Building,
  Users,
  PhoneCall,
  Car,
  ShieldCheck,
  AlertCircle,
  RefreshCw,
  Info,
} from "lucide-react";

export function TenantProfilePage() {
  const { t, language } = useTranslation();
  const { user } = useAuth();
  const userId = user?.id;

  const {
    data: profile,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ["tenant", userId, "profile"],
    queryFn: () => tenantPortalApi.getProfile(),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
  });

  return (
    <PageContainer
      title={t("tenant.profile.title", "My Profile")}
      description={t("tenant.profile.subtitle", "Your official profile recorded with property management")}
    >
      <div className="space-y-6">
        {/* Read-Only Status Banner */}
        <div className="p-3.5 rounded-xl border border-primary/20 bg-primary/5 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <ShieldCheck className="w-5 h-5 text-primary shrink-0" />
            <div>
              <p className="text-xs font-semibold text-foreground">
                {t("tenant.profile.readOnlyBadge", "Read-only official record")}
              </p>
              <p className="text-[11.5px] text-muted-foreground">
                {t("tenant.profile.readOnlyNote", "To update these details, please contact property management.")}
              </p>
            </div>
          </div>
        </div>

        {isLoading ? (
          <div className="space-y-6">
            <div className="h-40 rounded-xl border bg-card/60 animate-pulse" />
            <div className="h-32 rounded-xl border bg-card/60 animate-pulse" />
            <div className="h-48 rounded-xl border bg-card/60 animate-pulse" />
          </div>
        ) : isError ? (
          <div className="p-8 rounded-xl border border-destructive/30 bg-destructive/10 text-center space-y-3">
            <AlertCircle className="w-8 h-8 text-destructive mx-auto" />
            <p className="text-sm font-medium text-foreground">
              {t("errors.generic", "An unexpected error occurred. Please try again.")}
            </p>
            <button
              onClick={() => refetch()}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 transition-colors cursor-pointer"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              {t("common.retry", "Retry")}
            </button>
          </div>
        ) : profile ? (
          <>
            {/* 1. Personal Information Card */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center gap-2 border-b border-border/70 pb-3">
                <User className="w-4 h-4 text-primary" />
                <h3 className="font-semibold text-sm">
                  {t("tenant.profile.personalInformation", "Personal Information")}
                </h3>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.fullName", "Full Name")}
                  </span>
                  <span className="font-semibold text-foreground text-sm block truncate">
                    {profile.name}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.nationalId", "National ID / Iqama")}
                  </span>
                  <span className="font-medium text-foreground block font-mono">
                    {profile.nationalId || "—"}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.phone", "Phone Number")}
                  </span>
                  <span className="font-medium text-foreground block font-mono">
                    {profile.phone}
                  </span>
                </div>
              </div>
            </div>

            {/* 2. Employment Information Card */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center gap-2 border-b border-border/70 pb-3">
                <Briefcase className="w-4 h-4 text-primary" />
                <h3 className="font-semibold text-sm">
                  {t("tenant.profile.workInformation", "Employment Information")}
                </h3>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.occupation", "Occupation")}
                  </span>
                  <span className="font-medium text-foreground block">
                    {profile.occupation || (language === "ar" ? "غير محدد" : "Not specified")}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.employer", "Employer / Company")}
                  </span>
                  <span className="font-medium text-foreground block">
                    {profile.employer || (language === "ar" ? "غير محدد" : "Not specified")}
                  </span>
                </div>
              </div>
            </div>

            {/* 3. Family Members Card (Read-Only) */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center justify-between border-b border-border/70 pb-3">
                <div className="flex items-center gap-2">
                  <Users className="w-4 h-4 text-primary" />
                  <h3 className="font-semibold text-sm">
                    {t("tenant.profile.familyMembers", "Family Members")}
                  </h3>
                </div>
                <span className="text-[11px] font-medium bg-secondary text-muted-foreground px-2 py-0.5 rounded-full">
                  {profile.familyMembers.length}
                </span>
              </div>

              {profile.familyMembers.length === 0 ? (
                <div className="py-6 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg">
                  <p>{t("tenant.profile.noFamilyMembers", "No family members recorded")}</p>
                </div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {profile.familyMembers.map((member) => (
                    <div
                      key={member.id}
                      className="p-3 rounded-lg border border-border/60 bg-secondary/20 flex flex-col justify-between text-xs space-y-1"
                    >
                      <span className="font-semibold text-foreground">{member.name}</span>
                      <div className="flex items-center justify-between text-muted-foreground text-[11px]">
                        <span>{member.relationshipType}</span>
                        {member.ageBracket && (
                          <span className="bg-background border border-border px-1.5 py-0.5 rounded">
                            {member.ageBracket}
                          </span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* 4. Emergency Contacts Card (Read-Only) */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center justify-between border-b border-border/70 pb-3">
                <div className="flex items-center gap-2">
                  <PhoneCall className="w-4 h-4 text-primary" />
                  <h3 className="font-semibold text-sm">
                    {t("tenant.profile.emergencyContacts", "Emergency Contacts")}
                  </h3>
                </div>
                <span className="text-[11px] font-medium bg-secondary text-muted-foreground px-2 py-0.5 rounded-full">
                  {profile.emergencyContacts.length}
                </span>
              </div>

              {profile.emergencyContacts.length === 0 ? (
                <div className="py-6 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg">
                  <p>{t("tenant.profile.noEmergencyContacts", "No emergency contacts recorded")}</p>
                </div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {profile.emergencyContacts.map((contact) => (
                    <div
                      key={contact.id}
                      className="p-3 rounded-lg border border-border/60 bg-secondary/20 flex flex-col justify-between text-xs space-y-1"
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-semibold text-foreground">{contact.name}</span>
                        <span className="text-[11px] text-muted-foreground">{contact.relationshipType}</span>
                      </div>
                      <span className="font-mono text-primary font-medium text-[11.5px]">{contact.phone}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* 5. Vehicles Card (Read-Only) */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center justify-between border-b border-border/70 pb-3">
                <div className="flex items-center gap-2">
                  <Car className="w-4 h-4 text-primary" />
                  <h3 className="font-semibold text-sm">
                    {t("tenant.profile.vehicles", "Registered Vehicles")}
                  </h3>
                </div>
                <span className="text-[11px] font-medium bg-secondary text-muted-foreground px-2 py-0.5 rounded-full">
                  {profile.vehicles.length}
                </span>
              </div>

              {profile.vehicles.length === 0 ? (
                <div className="py-6 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg">
                  <p>{t("tenant.profile.noVehicles", "No vehicles recorded")}</p>
                </div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {profile.vehicles.map((vehicle) => (
                    <div
                      key={vehicle.id}
                      className="p-3 rounded-lg border border-border/60 bg-secondary/20 flex flex-col justify-between text-xs space-y-1"
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-foreground font-mono bg-background border px-2 py-0.5 rounded">
                          {vehicle.plateNumber}
                        </span>
                        <span className="text-muted-foreground text-[11px]">{vehicle.color}</span>
                      </div>
                      <span className="font-medium text-foreground text-[11.5px]">{vehicle.makeModel}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </>
        ) : null}
      </div>
    </PageContainer>
  );
}
