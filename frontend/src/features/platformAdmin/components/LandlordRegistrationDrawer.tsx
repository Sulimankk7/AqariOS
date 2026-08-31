import { useEffect } from "react";
import { X, Building2, User, Mail, Phone, Calendar, Globe } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";
import { ErrorState } from "@/shared/components/ui/Feedback";
import { useEntityLabel, useTranslation } from "@/shared/i18n";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useLandlordRegistration } from "../hooks/useLandlordRegistrations";
import { getPlatformErrorMessage } from "../utils/platformAdminErrors";

interface Props {
  registrationId: string | null;
  open: boolean;
  onClose: () => void;
  onApprove: () => void;
  onReject: () => void;
}

export function LandlordRegistrationDrawer({
  registrationId,
  open,
  onClose,
  onApprove,
  onReject,
}: Props) {
  const { t, formatDate, language } = useTranslation();
  const entityLabel = useEntityLabel();
  const { user } = useAuth();
  const query = useLandlordRegistration(open ? registrationId : null);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape" && open) {
        onClose();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [open, onClose]);

  if (!open || !registrationId) return null;

  const detail = query.data;
  const canApprove = user?.permissions?.includes("platform.landlord_registrations.approve") === true;
  const canReject = user?.permissions?.includes("platform.landlord_registrations.reject") === true;
  const isPending = detail?.status === "Pending";

  const formatCompanyType = (type: string | undefined | null) => {
    if (!type) return "—";
    const lower = type.toLowerCase();
    if (lower.includes("individual") || type === "0") {
      return entityLabel("companyType", "IndividualOwner");
    }
    if (lower.includes("management") || type === "1") {
      return entityLabel("companyType", "PropertyManagementCompany");
    }
    if (lower.includes("investment") || type === "2") {
      return entityLabel("companyType", "InvestmentCompany");
    }
    return type;
  };

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end overflow-hidden bg-black/40 backdrop-blur-xs transition-opacity duration-200"
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <aside
        className="flex h-full w-full flex-col overflow-hidden border-s border-border bg-card shadow-2xl sm:max-w-[480px]"
        aria-label={t("platformAdmin.details.title")}
      >
        {/* Sticky Header */}
        <header className="flex h-14 shrink-0 items-center justify-between border-b border-border px-5">
          <h2 className="text-sm font-semibold text-foreground">
            {t("platformAdmin.details.title")}
          </h2>
          <button
            onClick={onClose}
            className="rounded-md p-1.5 text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors"
            aria-label={t("common.close")}
          >
            <X className="h-4 w-4" />
          </button>
        </header>

        {/* Scrollable Body */}
        <div className="flex-1 overflow-y-auto p-5 space-y-6">
          {query.isLoading && (
            <div className="space-y-4">
              <Skeleton className="h-16 w-full rounded-lg" />
              <div className="grid grid-cols-2 gap-4">
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
                <Skeleton className="h-12 w-full rounded-md" />
              </div>
            </div>
          )}

          {query.isError && (
            <ErrorState
              title={getPlatformErrorMessage(query.error, t)}
              onRetry={() => query.refetch()}
            />
          )}

          {detail && (
            <div className="space-y-6">
              {/* Primary Identity Header */}
              <div className="flex items-start justify-between gap-3 border-b border-border pb-4">
                <div className="min-w-0">
                  <h3 className="text-lg font-bold text-foreground truncate">
                    {detail.userName}
                  </h3>
                  <p className="text-sm text-muted-foreground truncate mt-0.5">
                    {detail.companyName}
                  </p>
                </div>
                <span className="shrink-0 inline-flex items-center rounded-full bg-amber-500/10 px-2.5 py-1 text-xs font-medium text-amber-600 dark:text-amber-400 border border-amber-500/20">
                  {t("platformAdmin.status.pending")}
                </span>
              </div>

              {/* Applicant & Contact Section */}
              <div className="space-y-3">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  {t("platformAdmin.fields.applicant")}
                </h4>
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <InfoField
                    icon={Mail}
                    label={t("platformAdmin.fields.email")}
                    value={detail.email || "—"}
                    ltr
                  />
                  <InfoField
                    icon={Phone}
                    label={t("platformAdmin.fields.phone")}
                    value={detail.phone || "—"}
                    ltr
                  />
                </div>
              </div>

              {/* Company / Business Section */}
              <div className="space-y-3 border-t border-border pt-4">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  {t("platformAdmin.fields.company")}
                </h4>
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <InfoField
                    icon={Building2}
                    label={t("platformAdmin.fields.company")}
                    value={detail.companyName}
                  />
                  {detail.companyDisplayName && detail.companyDisplayName !== detail.companyName && (
                    <InfoField
                      label={t("platformAdmin.fields.companyDisplayName")}
                      value={detail.companyDisplayName}
                    />
                  )}
                  <InfoField
                    label={t("platformAdmin.fields.companyType")}
                    value={formatCompanyType(detail.companyType)}
                  />
                  <InfoField
                    icon={Globe}
                    label={t("platformAdmin.fields.country")}
                    value={detail.countryCode || "—"}
                    ltr
                  />
                </div>
              </div>

              {/* Submission Information */}
              <div className="space-y-3 border-t border-border pt-4">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  {t("platformAdmin.fields.submittedAt")}
                </h4>
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <InfoField
                    icon={Calendar}
                    label={t("platformAdmin.fields.submittedAt")}
                    value={formatDate(detail.registrationDate, {
                      dateStyle: "medium",
                      timeStyle: "short",
                    })}
                  />
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Sticky Action Footer */}
        {isPending && (canApprove || canReject) && (
          <footer className="sticky bottom-0 flex shrink-0 items-center justify-end gap-2.5 border-t border-border bg-card/95 p-4 backdrop-blur-xs">
            {canReject && (
              <Button
                variant="outline"
                className="text-destructive border-destructive/30 hover:bg-destructive/10 hover:border-destructive text-xs font-medium"
                onClick={onReject}
              >
                {t("platformAdmin.actions.reject")}
              </Button>
            )}
            {canApprove && (
              <Button
                className="text-xs font-medium px-4"
                onClick={onApprove}
              >
                {t("platformAdmin.actions.approve")}
              </Button>
            )}
          </footer>
        )}
      </aside>
    </div>
  );
}

function InfoField({
  icon: Icon,
  label,
  value,
  ltr = false,
}: {
  icon?: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
  ltr?: boolean;
}) {
  return (
    <div className="rounded-md border border-border/60 bg-muted/20 p-2.5 min-w-0">
      <div className="flex items-center gap-1 text-[11px] text-muted-foreground">
        {Icon && <Icon className="h-3 w-3 shrink-0" />}
        <span>{label}</span>
      </div>
      <bdi dir={ltr ? "ltr" : undefined} className="mt-1 block truncate text-xs font-medium text-foreground">
        {value}
      </bdi>
    </div>
  );
}
