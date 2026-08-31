import React from "react";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTenantProfile } from "../hooks/useTenantProfile";
import {
  User,
  Phone,
  Mail,
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

  const {
    data: profile,
    isLoading,
    isError,
    refetch,
  } = useTenantProfile();

  return (
    <PageContainer
      title={t("tenant.profile.title")}
      description={t("tenant.profile.subtitle")}
    >
      <div className="max-w-4xl space-y-4">
        {/* Read-Only Status Banner */}
        <div className="p-3.5 rounded-xl border border-primary/20 bg-primary/5 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <ShieldCheck className="w-5 h-5 text-primary shrink-0" />
            <div>
              <p className="text-xs font-semibold text-foreground">
                {t("tenant.profile.readOnlyBadge")}
              </p>
              <p className="text-[11.5px] text-muted-foreground">
                {t("tenant.profile.readOnlyNote")}
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
              {t("errors.generic")}
            </p>
            <button
              onClick={() => refetch()}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 transition-colors cursor-pointer"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              {t("common.retry")}
            </button>
          </div>
        ) : profile ? (
          <>
            {/* 1. Personal Information Card */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center gap-2 border-b border-border/70 pb-3">
                <User className="w-4 h-4 text-primary" />
                <h3 className="font-semibold text-sm">
                  {t("tenant.profile.personalInformation")}
                </h3>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 text-xs">
                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.fullName")}
                  </span>
                  <span className="font-semibold text-foreground text-sm block truncate">
                    {profile.name}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.nationalId")}
                  </span>
                  <span className="font-medium text-foreground block font-mono">
                    {profile.nationalId || "—"}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.phone")}
                  </span>
                  <span className="font-medium text-foreground block font-mono" dir="ltr">
                    {profile.phone}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.email")}
                  </span>
                  <span className="font-medium text-foreground block truncate" dir="ltr">
                    {profile.email || "—"}
                  </span>
                </div>
              </div>
            </div>

            {/* 2. Employment Information Card */}
            <div className="rounded-xl border border-border bg-card text-card-foreground shadow-xs p-5 space-y-4">
              <div className="flex items-center gap-2 border-b border-border/70 pb-3">
                <Briefcase className="w-4 h-4 text-primary" />
                <h3 className="font-semibold text-sm">
                  {t("tenant.profile.workInformation")}
                </h3>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.occupation")}
                  </span>
                  <span className="font-medium text-foreground block">
                    {profile.occupation || t("common.notSpecified")}
                  </span>
                </div>

                <div className="space-y-1">
                  <span className="text-muted-foreground block">
                    {t("tenant.profile.employer")}
                  </span>
                  <span className="font-medium text-foreground block">
                    {profile.employer || t("common.notSpecified")}
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
                    {t("tenant.profile.familyMembers")}
                  </h3>
                </div>
                <span className="text-[11px] font-medium bg-secondary text-muted-foreground px-2 py-0.5 rounded-full">
                  {profile.familyMembers.length}
                </span>
              </div>

              {profile.familyMembers.length === 0 ? (
                <div className="py-6 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg">
                  <p>{t("tenant.profile.noFamilyMembers")}</p>
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
                    {t("tenant.profile.emergencyContacts")}
                  </h3>
                </div>
                <span className="text-[11px] font-medium bg-secondary text-muted-foreground px-2 py-0.5 rounded-full">
                  {profile.emergencyContacts.length}
                </span>
              </div>

              {profile.emergencyContacts.length === 0 ? (
                <div className="py-6 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg">
                  <p>{t("tenant.profile.noEmergencyContacts")}</p>
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
                    {t("tenant.profile.vehicles")}
                  </h3>
                </div>
                <span className="text-[11px] font-medium bg-secondary text-muted-foreground px-2 py-0.5 rounded-full">
                  {profile.vehicles.length}
                </span>
              </div>

              {profile.vehicles.length === 0 ? (
                <div className="py-6 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg">
                  <p>{t("tenant.profile.noVehicles")}</p>
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
