import { useState } from "react";
import { RefreshCw, Eye, Check, X, ShieldAlert } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/shared/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui/table";
import { Skeleton } from "@/shared/ui/skeleton";
import { ErrorState } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { useAuth } from "@/features/auth/hooks/useAuth";
import type { LandlordRegistrationListItemDto } from "../types/platformAdmin.types";
import {
  useApproveLandlordRegistration,
  usePendingLandlordRegistrations,
  useRejectLandlordRegistration,
} from "../hooks/useLandlordRegistrations";
import { getPlatformErrorMessage, platformErrorKind } from "../utils/platformAdminErrors";
import { LandlordRegistrationDrawer } from "../components/LandlordRegistrationDrawer";
import {
  ApproveRegistrationDialog,
  RejectRegistrationDialog,
} from "../components/RegistrationReviewDialogs";

const PAGE_SIZE = 20;

export function LandlordRegistrationsPage() {
  const { t, formatDate, formatNumber } = useTranslation();
  const { user } = useAuth();
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<LandlordRegistrationListItemDto | null>(null);
  const [dialog, setDialog] = useState<"approve" | "reject" | null>(null);
  const [actionItem, setActionItem] = useState<LandlordRegistrationListItemDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const query = usePendingLandlordRegistrations(page, PAGE_SIZE);
  const approve = useApproveLandlordRegistration();
  const reject = useRejectLandlordRegistration();

  const canRead = user?.permissions?.includes("platform.landlord_registrations.read") === true;
  const canApprove = user?.permissions?.includes("platform.landlord_registrations.approve") === true;
  const canReject = user?.permissions?.includes("platform.landlord_registrations.reject") === true;

  const handleOpenApprove = (item: LandlordRegistrationListItemDto, e?: React.MouseEvent) => {
    e?.stopPropagation();
    setActionError(null);
    setActionItem(item);
    setDialog("approve");
  };

  const handleOpenReject = (item: LandlordRegistrationListItemDto, e?: React.MouseEvent) => {
    e?.stopPropagation();
    setActionError(null);
    setActionItem(item);
    setDialog("reject");
  };

  const runApprove = async () => {
    if (!actionItem || approve.isPending) return;
    setActionError(null);
    try {
      await approve.mutateAsync(actionItem.registrationId);
      setDialog(null);
      if (selected?.registrationId === actionItem.registrationId) {
        setSelected(null);
      }
      setActionItem(null);
      toast.success(t("platformAdmin.approve.success"));
    } catch (error) {
      const message = getPlatformErrorMessage(error, t);
      setActionError(message);
      if (platformErrorKind(error) === "conflict") {
        query.refetch();
      }
    }
  };

  const runReject = async (reason: string) => {
    if (!actionItem || reject.isPending || !reason) return;
    setActionError(null);
    try {
      await reject.mutateAsync({ id: actionItem.registrationId, reason });
      setDialog(null);
      if (selected?.registrationId === actionItem.registrationId) {
        setSelected(null);
      }
      setActionItem(null);
      toast.success(t("platformAdmin.reject.success"));
    } catch (error) {
      const message = getPlatformErrorMessage(error, t);
      setActionError(message);
      if (platformErrorKind(error) === "conflict") {
        query.refetch();
      }
    }
  };

  if (!canRead) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-center">
        <ShieldAlert className="h-10 w-10 text-muted-foreground mb-3" />
        <h2 className="text-base font-semibold text-foreground">{t("platformAdmin.errors.forbidden")}</h2>
      </div>
    );
  }

  const totalPages = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / PAGE_SIZE));

  return (
    <section className="space-y-4">
      {/* Header */}
      <header className="flex flex-wrap items-center justify-between gap-3 pb-1 border-b border-border/40">
        <div>
          <h1 className="text-xl font-bold text-foreground tracking-tight">
            {t("platformAdmin.registrations.title")}
          </h1>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("platformAdmin.registrations.subtitle")}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
          className="h-8 gap-1.5 text-xs"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${query.isFetching ? "animate-spin" : ""}`} />
          <span>{t("common.refresh")}</span>
        </Button>
      </header>

      {/* Loading State */}
      {query.isLoading && (
        <div className="space-y-2 rounded-lg border border-border bg-card p-4">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      )}

      {/* Error State */}
      {query.isError && (
        <ErrorState
          title={getPlatformErrorMessage(query.error, t)}
          onRetry={() => query.refetch()}
        />
      )}

      {/* Empty State */}
      {query.data && query.data.items.length === 0 && (
        <div className="rounded-lg border border-border/60 bg-card/40 p-10 text-center">
          <p className="text-sm font-medium text-muted-foreground">
            {t("platformAdmin.registrations.empty")}
          </p>
        </div>
      )}

      {/* Request List Table */}
      {query.data && query.data.items.length > 0 && (
        <div className="space-y-3">
          <div className="overflow-hidden rounded-lg border border-border bg-card shadow-2xs">
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent border-b border-border bg-muted/20">
                  <TableHead className="py-2.5 text-xs font-semibold">
                    {t("platformAdmin.fields.applicant")}
                  </TableHead>
                  <TableHead className="py-2.5 text-xs font-semibold">
                    {t("platformAdmin.fields.company")}
                  </TableHead>
                  <TableHead className="py-2.5 text-xs font-semibold hidden md:table-cell">
                    {t("platformAdmin.fields.email")}
                  </TableHead>
                  <TableHead className="py-2.5 text-xs font-semibold hidden sm:table-cell">
                    {t("platformAdmin.fields.submittedAt")}
                  </TableHead>
                  <TableHead className="py-2.5 text-xs font-semibold text-center w-28">
                    {t("platformAdmin.status.pending")}
                  </TableHead>
                  <TableHead className="py-2.5 text-xs font-semibold text-end w-44">
                    {t("common.actions")}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {query.data.items.map((item) => (
                  <TableRow
                    key={item.registrationId}
                    onClick={() => setSelected(item)}
                    className="cursor-pointer transition-colors hover:bg-muted/40"
                  >
                    {/* Applicant */}
                    <TableCell className="py-3 font-medium text-sm text-foreground">
                      {item.userName}
                    </TableCell>

                    {/* Company */}
                    <TableCell className="py-3 text-sm text-foreground">
                      {item.companyName}
                    </TableCell>

                    {/* Email */}
                    <TableCell className="py-3 hidden md:table-cell text-xs text-muted-foreground">
                      <bdi dir="ltr">{item.email || "—"}</bdi>
                    </TableCell>

                    {/* Submission Date */}
                    <TableCell className="py-3 hidden whitespace-nowrap sm:table-cell text-xs text-muted-foreground">
                      {formatDate(item.registrationDate, {
                        dateStyle: "medium",
                      })}
                    </TableCell>

                    {/* Status Badge */}
                    <TableCell className="py-3 text-center">
                      <span className="inline-flex items-center rounded-full bg-amber-500/10 px-2 py-0.5 text-[11px] font-medium text-amber-600 dark:text-amber-400 border border-amber-500/20">
                        {t("platformAdmin.status.pending")}
                      </span>
                    </TableCell>

                    {/* Actions */}
                    <TableCell className="py-3 text-end" onClick={(e) => e.stopPropagation()}>
                      <div className="inline-flex items-center gap-1.5">
                        {canReject && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 px-2 text-xs text-destructive hover:bg-destructive/10 hover:text-destructive"
                            onClick={(e) => handleOpenReject(item, e)}
                            title={t("platformAdmin.actions.reject")}
                          >
                            <X className="h-3.5 w-3.5 me-1" />
                            <span>{t("platformAdmin.actions.reject")}</span>
                          </Button>
                        )}
                        {canApprove && (
                          <Button
                            size="sm"
                            className="h-7 px-2.5 text-xs font-medium"
                            onClick={(e) => handleOpenApprove(item, e)}
                            title={t("platformAdmin.actions.approve")}
                          >
                            <Check className="h-3.5 w-3.5 me-1" />
                            <span>{t("platformAdmin.actions.approve")}</span>
                          </Button>
                        )}
                        <Button
                          size="sm"
                          variant="ghost"
                          className="h-7 px-2 text-xs text-muted-foreground hover:text-foreground"
                          onClick={() => setSelected(item)}
                          title={t("common.view")}
                        >
                          <Eye className="h-3.5 w-3.5" />
                          <span className="sr-only">{t("common.view")}</span>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Compact Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between text-xs text-muted-foreground pt-1">
              <span>
                {t("platformAdmin.registrations.total", {
                  count: formatNumber(query.data.totalCount),
                })}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 px-2.5 text-xs"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  {t("common.back")}
                </Button>
                <bdi dir="ltr" className="px-1 font-medium">
                  {page} / {totalPages}
                </bdi>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 px-2.5 text-xs"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  {t("common.next")}
                </Button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Details Drawer */}
      <LandlordRegistrationDrawer
        registrationId={selected?.registrationId ?? null}
        open={Boolean(selected)}
        onClose={() => setSelected(null)}
        onApprove={() => {
          if (selected) {
            handleOpenApprove(selected);
          }
        }}
        onReject={() => {
          if (selected) {
            handleOpenReject(selected);
          }
        }}
      />

      {/* Review Dialogs */}
      <ApproveRegistrationDialog
        open={dialog === "approve"}
        name={actionItem?.userName ?? ""}
        pending={approve.isPending}
        error={actionError}
        onClose={() => {
          setDialog(null);
          setActionItem(null);
          setActionError(null);
        }}
        onConfirm={runApprove}
      />

      <RejectRegistrationDialog
        open={dialog === "reject"}
        name={actionItem?.userName ?? ""}
        pending={reject.isPending}
        error={actionError}
        onClose={() => {
          setDialog(null);
          setActionItem(null);
          setActionError(null);
        }}
        onConfirm={runReject}
      />
    </section>
  );
}
