import React from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useTenantLease } from "../hooks/useTenantLease";
import {
  FileText,
  Building2,
  Home,
  Bed,
  Bath,
  Maximize2,
  DollarSign,
  Calendar,
  ShieldCheck,
  CreditCard,
  CheckCircle2,
  AlertCircle,
  RefreshCw,
  FileCheck,
  Scale,
  UserCheck,
} from "lucide-react";

function formatStatus(status: unknown, t: (key: string, fallback?: string) => string) {
  const normalized = String(status ?? "").toLowerCase();
  switch (normalized) {
    case "active":
      return {
        label: t("tenant.lease.statusActive"),
        className: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20",
      };
    case "draft":
      return {
        label: t("tenant.lease.statusDraft"),
        className: "bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20",
      };
    case "terminated":
      return {
        label: t("tenant.lease.statusTerminated"),
        className: "bg-destructive/10 text-destructive border-destructive/20",
      };
    case "expired":
      return {
        label: t("tenant.lease.statusExpired"),
        className: "bg-muted text-muted-foreground border-border",
      };
    default:
      return {
        label: status,
        className: "bg-secondary text-foreground border-border",
      };
  }
}

function formatCurrency(amount: number, currency: string) {
  const code = currency || "JOD";
  return new Intl.NumberFormat("en-US", {
    style: "decimal",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount) + ` ${code}`;
}

function formatDate(dateStr?: string | null, language?: string) {
  if (!dateStr) return "";
  try {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat(language === "ar" ? "ar-JO" : "en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    }).format(date);
  } catch {
    return dateStr;
  }
}

export function TenantLeasePage() {
  const { t, language } = useTranslation();
  const navigate = useNavigate();
  const { data: lease, isLoading, isError, refetch } = useTenantLease();

  return (
    <PageContainer
      title={t("tenant.lease.title")}
      description={t("tenant.lease.subtitle")}
    >
      <div className="max-w-4xl space-y-4">
        {/* Loading Skeleton State */}
        {isLoading && (
          <div className="space-y-6 animate-pulse" aria-busy="true">
            <div className="h-32 bg-card rounded-xl border border-border p-6" />
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="h-56 bg-card rounded-xl border border-border p-6" />
              <div className="h-56 bg-card rounded-xl border border-border p-6" />
            </div>
            <div className="h-44 bg-card rounded-xl border border-border p-6" />
          </div>
        )}

        {/* Error State */}
        {!isLoading && isError && (
          <div className="p-6 rounded-xl border border-destructive/30 bg-destructive/5 text-center space-y-4">
            <AlertCircle className="w-10 h-10 text-destructive mx-auto" />
            <div>
              <h3 className="text-base font-semibold text-foreground">
                {t("tenant.lease.errorTitle")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1">
                {t("tenant.lease.errorDescription")}
              </p>
            </div>
            <button
              onClick={() => refetch()}
              className="px-4 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors shadow-xs cursor-pointer inline-flex items-center gap-2"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              {t("common.retry")}
            </button>
          </div>
        )}

        {/* Empty State (404 / No Active Lease) */}
        {!isLoading && !isError && lease === null && (
          <div className="p-10 rounded-2xl border border-dashed border-border bg-card text-center space-y-4">
            <div className="w-14 h-14 rounded-full bg-secondary flex items-center justify-center mx-auto text-muted-foreground">
              <FileText className="w-7 h-7" />
            </div>
            <div className="max-w-md mx-auto">
              <h3 className="text-base font-bold text-foreground">
                {t("tenant.lease.emptyTitle")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 leading-relaxed">
                {t("tenant.lease.emptyDescription")}
              </p>
            </div>
          </div>
        )}

        {/* Active Lease Content */}
        {!isLoading && !isError && lease && (
          <>
            {/* 1. CONTRACT SUMMARY HERO CARD */}
            <div className="p-6 rounded-2xl bg-card border border-border shadow-xs space-y-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-border/60 pb-4">
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded-xl bg-brand-green-900/10 text-brand-green-600 dark:text-brand-green-400 flex items-center justify-center shrink-0">
                    <FileCheck className="w-6 h-6" />
                  </div>
                  <div>
                    <span className="text-[11px] font-mono tracking-wider text-muted-foreground uppercase">
                      {t("tenant.lease.contractNumber")}
                    </span>
                    <h2 className="text-lg font-bold font-mono text-foreground tracking-tight">
                      {lease.contractNumber}
                    </h2>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  {(() => {
                    const statusInfo = formatStatus(lease.status, t);
                    return (
                      <span className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border ${statusInfo.className}`}>
                        <span className="w-1.5 h-1.5 rounded-full bg-current" />
                        {statusInfo.label}
                      </span>
                    );
                  })()}

                  <button
                    onClick={() => navigate("/tenant/payments")}
                    className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors shadow-xs cursor-pointer"
                  >
                    <CreditCard className="w-3.5 h-3.5" />
                    {t("paymentVerification.modalTitle")}
                  </button>
                </div>
              </div>

              {/* Term Dates Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
                <div className="p-3.5 rounded-xl bg-secondary/30 border border-border/40 space-y-1">
                  <span className="text-[11px] text-muted-foreground flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5 text-primary" />
                    {t("tenant.lease.startDate")}
                  </span>
                  <p className="text-xs font-semibold text-foreground">
                    {formatDate(lease.startDate, language)}
                  </p>
                </div>

                <div className="p-3.5 rounded-xl bg-secondary/30 border border-border/40 space-y-1">
                  <span className="text-[11px] text-muted-foreground flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5 text-primary" />
                    {t("tenant.lease.endDate")}
                  </span>
                  <p className="text-xs font-semibold text-foreground">
                    {formatDate(lease.endDate, language)}
                  </p>
                </div>

                <div className="p-3.5 rounded-xl bg-secondary/30 border border-border/40 space-y-1">
                  <span className="text-[11px] text-muted-foreground flex items-center gap-1.5">
                    <UserCheck className="w-3.5 h-3.5 text-primary" />
                    {t("tenant.lease.signedDate")}
                  </span>
                  <p className="text-xs font-semibold text-foreground">
                    {lease.signedDate ? formatDate(lease.signedDate, language) : t("tenant.lease.notSignedYet")}
                  </p>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* 2. PROPERTY & UNIT DETAILS CARD */}
              <div className="p-6 rounded-2xl bg-card border border-border shadow-xs space-y-5">
                <div className="flex items-center gap-2.5 border-b border-border/60 pb-3">
                  <Building2 className="w-5 h-5 text-brand-green-600 dark:text-brand-green-400 shrink-0" />
                  <h3 className="text-sm font-bold text-foreground">
                    {t("tenant.lease.propertyUnit")}
                  </h3>
                </div>

                <div className="space-y-4">
                  <div className="flex items-center justify-between p-3.5 rounded-xl bg-secondary/30 border border-border/30">
                    <div className="space-y-0.5">
                      <span className="text-[11px] text-muted-foreground">
                        {t("tenant.lease.buildingName")}
                      </span>
                      <p className="text-xs font-bold text-foreground">
                        {lease.buildingName}
                      </p>
                    </div>
                    <Building2 className="w-5 h-5 text-muted-foreground/60" />
                  </div>

                  <div className="flex items-center justify-between p-3.5 rounded-xl bg-secondary/30 border border-border/30">
                    <div className="space-y-0.5">
                      <span className="text-[11px] text-muted-foreground">
                        {t("tenant.lease.unitNumber")}
                      </span>
                      <p className="text-xs font-bold font-mono text-foreground">
                        {t("tenant.payments.unit")} {lease.apartmentUnitNumber}
                      </p>
                    </div>
                    <Home className="w-5 h-5 text-muted-foreground/60" />
                  </div>

                  <div className="grid grid-cols-3 gap-3 pt-1">
                    <div className="p-3 rounded-xl bg-secondary/20 border border-border/30 text-center space-y-1">
                      <Bed className="w-4 h-4 text-primary mx-auto" />
                      <span className="text-[10px] text-muted-foreground block">
                        {t("tenant.lease.bedrooms")}
                      </span>
                      <span className="text-xs font-bold text-foreground">
                        {lease.apartmentBedrooms}
                      </span>
                    </div>

                    <div className="p-3 rounded-xl bg-secondary/20 border border-border/30 text-center space-y-1">
                      <Bath className="w-4 h-4 text-primary mx-auto" />
                      <span className="text-[10px] text-muted-foreground block">
                        {t("tenant.lease.bathrooms")}
                      </span>
                      <span className="text-xs font-bold text-foreground">
                        {lease.apartmentBathrooms}
                      </span>
                    </div>

                    <div className="p-3 rounded-xl bg-secondary/20 border border-border/30 text-center space-y-1">
                      <Maximize2 className="w-4 h-4 text-primary mx-auto" />
                      <span className="text-[10px] text-muted-foreground block">
                        {t("tenant.lease.areaSqm")}
                      </span>
                      <span className="text-xs font-bold text-foreground">
                        {lease.apartmentAreaSqm} m²
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* 3. FINANCIAL & RENT TERMS CARD */}
              <div className="p-6 rounded-2xl bg-card border border-border shadow-xs space-y-5">
                <div className="flex items-center gap-2.5 border-b border-border/60 pb-3">
                  <CreditCard className="w-5 h-5 text-brand-green-600 dark:text-brand-green-400 shrink-0" />
                  <h3 className="text-sm font-bold text-foreground">
                    {t("tenant.lease.rentDetails")}
                  </h3>
                </div>

                <div className="space-y-4">
                  <div className="p-4 rounded-xl bg-brand-green-900/10 border border-brand-green-800/20 space-y-1">
                    <span className="text-[11px] font-medium text-brand-green-700 dark:text-brand-green-300">
                      {t("tenant.lease.monthlyRent")}
                    </span>
                    <p className="text-lg font-bold font-mono text-foreground">
                      {formatCurrency(lease.monthlyRentAmount, lease.currency)}
                    </p>
                  </div>

                  <div className="flex items-center justify-between py-2 border-b border-border/40 text-xs">
                    <span className="text-muted-foreground flex items-center gap-1.5">
                      <ShieldCheck className="w-4 h-4 text-primary" />
                      {t("tenant.lease.securityDeposit")}
                    </span>
                    <span className="font-semibold font-mono text-foreground">
                      {formatCurrency(lease.securityDepositAmount, lease.currency)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between py-2 border-b border-border/40 text-xs">
                    <span className="text-muted-foreground flex items-center gap-1.5">
                      <CreditCard className="w-4 h-4 text-primary" />
                      {t("tenant.lease.paymentFrequency")}
                    </span>
                    <span className="font-semibold text-foreground">
                      {lease.paymentFrequency}
                    </span>
                  </div>

                  <div className="flex items-center justify-between py-1.5 text-xs">
                    <span className="text-muted-foreground">
                      {t("tenant.lease.paymentDueDay")}
                    </span>
                    <span className="font-semibold text-foreground">
                      {language === "ar"
                        ? `اليوم ${lease.paymentDueDay} من الشهر`
                        : `Day ${lease.paymentDueDay} of month`}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* 4. CONTRACT INFORMATION / CLASSIFICATION */}
            <div className="p-6 rounded-2xl bg-card border border-border shadow-xs space-y-4">
              <div className="flex items-center gap-2.5 border-b border-border/60 pb-3">
                <Scale className="w-5 h-5 text-brand-green-600 dark:text-brand-green-400 shrink-0" />
                <h3 className="text-sm font-bold text-foreground">
                  {t("tenant.lease.contractInfo")}
                </h3>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="flex items-center justify-between p-3 rounded-xl bg-secondary/30 border border-border/30 text-xs">
                  <span className="text-muted-foreground">
                    {t("tenant.lease.legalRegime")}
                  </span>
                  <span className="font-semibold text-foreground">
                    {lease.legalRegime}
                  </span>
                </div>

                <div className="flex items-center justify-between p-3 rounded-xl bg-secondary/30 border border-border/30 text-xs">
                  <span className="text-muted-foreground">
                    {t("tenant.lease.tenantType")}
                  </span>
                  <span className="font-semibold text-foreground">
                    {lease.tenantType}
                  </span>
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </PageContainer>
  );
}
