import React, { useState } from "react";
import { getLocale, getRuntimeLanguage, useEntityLabel, useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useMyPayments } from "../hooks/useTenantPayments";
import type { TenantPaymentDto } from "../types/tenantPortal.types";
import { SubmitPaymentVerificationModal } from "../components/SubmitPaymentVerificationModal";
import { paymentsApi } from "@/features/payments/api/payments.api";
import { tenantPortalApi } from "../api/tenantPortal.api";
import { filesApi } from "@/shared/services/files.api";
import { toast } from "sonner";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import {
  CreditCard,
  Calendar,
  Building2,
  Home,
  CheckCircle2,
  Clock,
  AlertCircle,
  XCircle,
  FileText,
  ChevronDown,
  ChevronUp,
  Wallet,
  Receipt,
  ArrowUpRight,
  Info,
  Download,
  RotateCcw,
} from "lucide-react";

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatDate(dateStr?: string | null, locale = "en-US"): string {
  if (!dateStr) return "—";
  try {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return "—";
    return d.toLocaleDateString(locale === "ar" ? "ar-JO" : "en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  } catch {
    return "—";
  }
}

function formatCurrency(amount: number, currency = "JOD"): string {
  return new Intl.NumberFormat(getLocale(getRuntimeLanguage()), {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

function normalizeDueDateStatus(status: unknown): string {
  if (status === null || status === undefined) return "unknown";

  let val = status;
  if (typeof val === "string" && !isNaN(Number(val)) && val.trim() !== "") {
    val = Number(val);
  }

  if (typeof val === "number") {
    switch (val) {
      case 0:
        return "pending";
      case 1:
        return "pendingverification";
      case 2:
        return "paid";
      case 3:
        return "partiallypaid";
      case 4:
        return "late";
      case 5:
        return "overdueunpaid";
      case 6:
        return "cancelled";
      default:
        return "unknown";
    }
  }

  const str = String(val).trim().toLowerCase();
  switch (str) {
    case "paid":
      return "paid";
    case "pending":
      return "pending";
    case "pendingverification":
      return "pendingverification";
    case "partiallypaid":
      return "partiallypaid";
    case "late":
      return "late";
    case "overdueunpaid":
    case "overdue":
      return "overdueunpaid";
    case "cancelled":
    case "canceled":
      return "cancelled";
    default:
      return "unknown";
  }
}

function normalizeSubmissionStatus(status: unknown): string {
  if (status === null || status === undefined) return "none";
  let val = status;
  if (typeof val === "string" && !isNaN(Number(val)) && val.trim() !== "") {
    val = Number(val);
  }
  if (typeof val === "number") {
    switch (val) {
      case 0:
        return "pending";
      case 1:
        return "approved";
      case 2:
        return "rejected";
      default:
        return "none";
    }
  }
  const str = String(val).trim().toLowerCase();
  if (str === "pending") return "pending";
  if (str === "approved") return "approved";
  if (str === "rejected") return "rejected";
  return "none";
}

function normalizePaymentPurpose(purpose: unknown): string {
  if (purpose === null || purpose === undefined) return "unknown";

  let val = purpose;
  if (typeof val === "string" && !isNaN(Number(val)) && val.trim() !== "") {
    val = Number(val);
  }

  if (typeof val === "number") {
    switch (val) {
      case 0:
        return "scheduledinstallment";
      case 1:
        return "unallocatedreceipt";
      case 2:
        return "adjustmentcredit";
      case 3:
        return "adjustmentdebit";
      default:
        return "unknown";
    }
  }

  const str = String(val).trim();
  const normalizedStr = str.replace(/[^a-zA-Z]/g, "").toLowerCase();

  switch (normalizedStr) {
    case "scheduledinstallment":
    case "installment":
      return "scheduledinstallment";
    case "unallocatedreceipt":
    case "receipt":
      return "unallocatedreceipt";
    case "adjustmentcredit":
      return "adjustmentcredit";
    case "adjustmentdebit":
      return "adjustmentdebit";
    default:
      return "unknown";
  }
}

interface StatusBadgeConfig {
  label: string;
  className: string;
  icon: React.ReactNode;
}

function getStatusBadge(
  status: unknown,
  t: (key: string, fallback?: string) => string
): StatusBadgeConfig {
  const normalized = normalizeDueDateStatus(status);
  switch (normalized) {
    case "paid":
      return {
        label: t("tenant.payments.statusPaid"),
        className:
          "bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 border-emerald-500/25",
        icon: <CheckCircle2 className="w-3.5 h-3.5" />,
      };
    case "pending":
      return {
        label: t("tenant.payments.statusPending"),
        className:
          "bg-amber-500/10 text-amber-700 dark:text-amber-400 border-amber-500/25",
        icon: <Clock className="w-3.5 h-3.5" />,
      };
    case "pendingverification":
      return {
        label: t("tenant.payments.statusPendingVerification"),
        className:
          "bg-blue-500/10 text-blue-700 dark:text-blue-400 border-blue-500/25",
        icon: <Clock className="w-3.5 h-3.5" />,
      };
    case "partiallypaid":
      return {
        label: t("tenant.payments.statusPartiallyPaid"),
        className:
          "bg-sky-500/10 text-sky-700 dark:text-sky-400 border-sky-500/25",
        icon: <Wallet className="w-3.5 h-3.5" />,
      };
    case "late":
      return {
        label: t("tenant.payments.statusLate"),
        className:
          "bg-orange-500/10 text-orange-700 dark:text-orange-400 border-orange-500/25",
        icon: <AlertCircle className="w-3.5 h-3.5" />,
      };
    case "overdueunpaid":
      return {
        label: t("tenant.payments.statusOverdue"),
        className:
          "bg-destructive/10 text-destructive border-destructive/25",
        icon: <AlertCircle className="w-3.5 h-3.5" />,
      };
    case "cancelled":
      return {
        label: t("tenant.payments.statusCancelled"),
        className:
          "bg-secondary text-muted-foreground border-border",
        icon: <XCircle className="w-3.5 h-3.5" />,
      };
    case "unknown":
    default:
      return {
        label: t("tenant.payments.statusUnavailable"),
        className: "bg-secondary text-muted-foreground border-border",
        icon: <AlertCircle className="w-3.5 h-3.5" />,
      };
  }
}

function isSubmittable(status: unknown): boolean {
  const s = normalizeDueDateStatus(status);
  return s === "pending" || s === "late" || s === "overdueunpaid" || s === "partiallypaid";
}

function formatPurpose(purpose: unknown, t: (key: string, fallback?: string) => string): string {
  const normalized = normalizePaymentPurpose(purpose);
  switch (normalized) {
    case "scheduledinstallment":
      return t("tenant.payments.purposeScheduledInstallment");
    case "unallocatedreceipt":
      return t("tenant.payments.purposeUnallocatedReceipt");
    case "adjustmentcredit":
      return t("tenant.payments.purposeAdjustmentCredit");
    case "adjustmentdebit":
      return t("tenant.payments.purposeAdjustmentDebit");
    default:
      return t("common.unknown");
  }
}

// ─── Payment Row Card ─────────────────────────────────────────────────────────

interface PaymentCardProps {
  payment: TenantPaymentDto;
  language: string;
  t: (key: string, fallback?: string) => string;
  onSubmitVerification: (payment: TenantPaymentDto) => void;
}

function PaymentCard({ payment, language, t, onSubmitVerification }: PaymentCardProps) {
  const entityLabel = useEntityLabel();
  const [expanded, setExpanded] = useState(false);
  const [isDownloadingReceipt, setIsDownloadingReceipt] = useState(false);
  const [isDownloadingSettlement, setIsDownloadingSettlement] = useState(false);

  const statusBadge = getStatusBadge(payment.dueDateStatus, t);
  const canSubmit = isSubmittable(payment.dueDateStatus);
  const isPendingVerification = normalizeDueDateStatus(payment.dueDateStatus) === "pendingverification";
  const isRejected = normalizeSubmissionStatus(payment.latestSubmissionStatus) === "rejected" && normalizeDueDateStatus(payment.dueDateStatus) !== "paid";
  const amountRemaining = Math.max(0, payment.amountDue - payment.amountPaid);

  const handleDownloadReceipt = async () => {
    setIsDownloadingReceipt(true);
    try {
      if (payment.receiptFileId) {
        const fileRes = await filesApi.getFileDownloadUrl(payment.receiptFileId, false);
        window.open(fileRes.downloadUrl, '_blank');
        return;
      }
      const receiptRes = await paymentsApi.getReceiptByRentPaymentId(payment.id);
      if (receiptRes?.fileId) {
        const fileRes = await filesApi.getFileDownloadUrl(receiptRes.fileId, false);
        window.open(fileRes.downloadUrl, '_blank');
      } else {
        alert(t("tenant.payments.noReceiptPdf"));
      }
    } catch (err) {
      if (import.meta.env.DEV) console.error('Failed to download tenant receipt:', err);
      toast.error(t("tenant.payments.downloadSettlementError"));
    } finally {
      setIsDownloadingReceipt(false);
    }
  };

  const handleDownloadTxReceipt = async (fileId?: string | null) => {
    if (fileId) {
      try {
        const fileRes = await filesApi.getFileDownloadUrl(fileId, false);
        window.open(fileRes.downloadUrl, '_blank');
        return;
      } catch (err) {
        if (import.meta.env.DEV) console.error('Failed to download transaction receipt:', err);
        toast.error(t("tenant.payments.downloadSettlementError"));
      }
    }
    handleDownloadReceipt();
  };

  const handleDownloadSettlementStatement = async (e?: React.MouseEvent) => {
    e?.preventDefault();
    e?.stopPropagation();
    if (isDownloadingSettlement) return;
    setIsDownloadingSettlement(true);
    try {
      const blob = await tenantPortalApi.downloadSettlementStatement(payment.id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.style.display = "none";
      a.href = url;
      a.download = payment.receiptNumber ? `Settlement_Statement_${payment.receiptNumber}.pdf` : 'Settlement_Statement.pdf';
      document.body.appendChild(a);
      a.click();
      setTimeout(() => {
        if (document.body.contains(a)) {
          document.body.removeChild(a);
        }
        window.URL.revokeObjectURL(url);
      }, 1000);
    } catch (err: any) {
      console.error("Failed to download settlement statement:", err);
      toast.error(extractUserFriendlyError(err, t("tenant.payments.downloadSettlementError")));
    } finally {
      setIsDownloadingSettlement(false);
    }
  };

  const hasTxReceipts = payment.transactionReceipts && payment.transactionReceipts.length > 0;
  const isSettled = payment.settlementSummary?.isAvailable || (normalizeDueDateStatus(payment.dueDateStatus) === "paid" && payment.amountPaid >= payment.amountDue);

  return (
    <div
      className="rounded-xl border border-border bg-card shadow-2xs overflow-hidden transition-all duration-200"
      role="article"
    >
      {/* Card Header */}
      <div className="p-4 sm:p-5 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          {/* Left: Purpose + contract info */}
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-10 h-10 rounded-xl bg-primary/8 text-primary flex items-center justify-center shrink-0 font-bold">
              <CreditCard className="w-5 h-5" />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-bold text-foreground truncate">
                {formatPurpose(payment.paymentPurpose, t)}
              </p>
              <div className="flex items-center gap-2 mt-0.5 text-[11px] text-muted-foreground flex-wrap">
                {payment.contractNumber && (
                  <span className="flex items-center gap-1">
                    <FileText className="w-3 h-3 shrink-0 text-muted-foreground/70" />
                    {payment.contractNumber}
                  </span>
                )}
                {payment.buildingName && (
                  <span className="flex items-center gap-1">
                    <Building2 className="w-3 h-3 shrink-0 text-muted-foreground/70" />
                    {payment.buildingName}
                  </span>
                )}
                {payment.apartmentNumber && (
                  <span className="flex items-center gap-1">
                    <Home className="w-3 h-3 shrink-0 text-muted-foreground/70" />
                    {t("tenant.payments.unit")} {payment.apartmentNumber}
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* Right: Amount + status */}
          <div className="flex items-center justify-between sm:flex-col sm:items-end gap-2">
            <div className="sm:text-right">
              <p className="text-base font-bold text-foreground font-mono">
                {formatCurrency(payment.amountDue, payment.currency)}
              </p>
              {payment.amountPaid > 0 && payment.amountPaid < payment.amountDue && (
                <p className="text-[11px] text-muted-foreground">
                  {t("tenant.payments.remaining")}: {formatCurrency(amountRemaining, payment.currency)}
                </p>
              )}
            </div>
            <span
              className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-semibold border ${statusBadge.className}`}
            >
              {statusBadge.icon}
              {statusBadge.label}
            </span>
          </div>
        </div>

        {/* Pending Verification Notice Banner */}
        {isPendingVerification && (
          <div className="p-3 rounded-xl bg-blue-500/10 border border-blue-500/20 text-blue-700 dark:text-blue-400 text-xs flex items-center gap-2 leading-relaxed">
            <Info className="w-4 h-4 shrink-0" />
            <span>{t("tenant.payments.pendingVerificationNotice")}</span>
          </div>
        )}

        {/* Rejected Submission Notice Banner */}
        {isRejected && (
          <div className="p-3.5 rounded-xl bg-destructive/10 border border-destructive/25 text-foreground text-xs space-y-2">
            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-2 text-destructive font-bold">
                <XCircle className="w-4 h-4 shrink-0" />
                <span>{t("tenant.payments.submissionRejectedTitle")}</span>
              </div>
              {payment.latestSubmissionAmount && (
                <span className="text-[11px] font-mono text-muted-foreground">
                  {t("tenant.payments.submittedAmount")}: {formatCurrency(payment.latestSubmissionAmount, payment.currency)}
                </span>
              )}
            </div>
            {payment.latestSubmissionRejectionReason && (
              <div className="p-2.5 rounded-lg bg-background/60 border border-destructive/20 text-xs space-y-1">
                <p className="text-[11px] text-muted-foreground font-semibold">
                  {t("tenant.payments.rejectionReason")}
                </p>
                <p className="text-foreground leading-relaxed font-medium">
                  {payment.latestSubmissionRejectionReason}
                </p>
              </div>
            )}
          </div>
        )}

        {/* Transaction Receipts List (Hybrid Model) */}
        {hasTxReceipts && (
          <div className="p-3 rounded-xl bg-secondary/30 border border-border/80 space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-foreground flex items-center gap-1.5">
                <Receipt className="w-3.5 h-3.5 text-primary" />
                <span>{t("tenant.payments.paymentTransactions")} ({payment.transactionReceipts!.length})</span>
              </span>
            </div>
            <div className="space-y-1.5">
              {payment.transactionReceipts!.map((tx, idx) => (
                <div
                  key={tx.receiptId || idx}
                  className="p-2 rounded-lg bg-card border border-border/60 text-xs flex items-center justify-between gap-2"
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <span className="w-5 h-5 rounded-full bg-primary/10 text-primary text-[10.5px] font-bold flex items-center justify-center shrink-0">
                      {idx + 1}
                    </span>
                    <div className="min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-bold text-foreground font-mono">
                          {formatCurrency(tx.amount, payment.currency)}
                        </span>
                        <span className="text-[11px] text-muted-foreground font-mono">
                          {tx.receiptNumber}
                        </span>
                      </div>
                      <p className="text-[10px] text-muted-foreground">
                        {formatDate(tx.issuedAt, language)}
                      </p>
                    </div>
                  </div>
                  <button
                    onClick={() => handleDownloadTxReceipt(tx.fileId)}
                    className="inline-flex items-center gap-1 px-2.5 py-1 bg-emerald-600/10 text-emerald-700 dark:text-emerald-400 hover:bg-emerald-600/20 rounded-md text-[11px] font-medium transition-colors cursor-pointer shrink-0"
                  >
                    <Download className="w-3 h-3" />
                    <span>{t("tenant.payments.viewTransactionReceipt")}</span>
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Single Approved Receipt Fallback (if transactionReceipts not populated) */}
        {!hasTxReceipts && payment.receiptNumber && (
          <div className="p-2.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-700 dark:text-emerald-400 text-xs flex items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <Receipt className="w-4 h-4 shrink-0" />
              <span className="font-semibold font-mono">
                {t("tenant.payments.receiptBadgeLabel", `Receipt No: ${payment.receiptNumber}`).replace("{{number}}", payment.receiptNumber)}
              </span>
            </div>
            <button
              onClick={handleDownloadReceipt}
              disabled={isDownloadingReceipt}
              className="inline-flex items-center gap-1 px-2.5 py-1 bg-emerald-600 text-white hover:bg-emerald-700 rounded-lg text-xs font-medium transition-colors shadow-2xs cursor-pointer disabled:opacity-50"
            >
              <Download className="w-3.5 h-3.5" />
              <span>{isDownloadingReceipt ? t("common.loading") : t("tenant.payments.downloadReceipt")}</span>
            </button>
          </div>
        )}

        {/* Final Settlement Statement Banner (Available strictly upon full settlement) */}
        {isSettled && (
          <div className="p-3 rounded-xl bg-primary/8 border border-primary/25 text-foreground text-xs flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
            <div className="flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4 text-primary shrink-0" />
              <div>
                <span className="font-bold text-primary">
                  {t("tenant.payments.settlementStatement")}
                </span>
                <p className="text-[11px] text-muted-foreground">
                  {t("tenant.payments.settledInFull")} ({formatCurrency(payment.amountDue, payment.currency)})
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={handleDownloadSettlementStatement}
              disabled={isDownloadingSettlement}
              className="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 bg-primary text-primary-foreground hover:bg-primary/90 rounded-lg text-xs font-semibold transition-colors shadow-2xs cursor-pointer disabled:opacity-50 shrink-0"
            >
              <Download className="w-3.5 h-3.5" />
              <span>
                {isDownloadingSettlement
                  ? t("common.loading")
                  : t("tenant.payments.downloadSettlementStatement")}
              </span>
            </button>
          </div>
        )}

        {/* Due date + action row */}
        <div className="flex items-center justify-between pt-3 border-t border-border/60">
          <div className="flex items-center gap-4 text-[11px] text-muted-foreground">
            {payment.dueDate && (
              <span className="flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-primary/70" />
                {t("tenant.payments.due")}: {formatDate(payment.dueDate, language)}
              </span>
            )}
            {payment.billingPeriodStart && payment.billingPeriodEnd && (
              <span className="hidden sm:flex items-center gap-1">
                {formatDate(payment.billingPeriodStart, language)} –{" "}
                {formatDate(payment.billingPeriodEnd, language)}
              </span>
            )}
          </div>

          <div className="flex items-center gap-2">
            {canSubmit && (
              <button
                onClick={() => onSubmitVerification(payment)}
                className={`inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl text-xs font-semibold transition-colors cursor-pointer shadow-xs ${
                  isRejected
                    ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90'
                    : 'bg-primary text-primary-foreground hover:bg-primary/90'
                }`}
              >
                {isRejected ? <RotateCcw className="w-3.5 h-3.5" /> : <ArrowUpRight className="w-3.5 h-3.5" />}
                {isRejected
                  ? t("tenant.payments.resubmitPaymentAction")
                  : t("tenant.payments.submitPaymentAction")}
              </button>
            )}
            <button
              onClick={() => setExpanded((v) => !v)}
              aria-expanded={expanded}
              className="px-2.5 py-1.5 rounded-lg text-[11px] font-medium text-muted-foreground hover:text-foreground hover:bg-secondary transition-colors cursor-pointer flex items-center gap-1"
            >
              <span>{expanded ? t("tenant.payments.collapse") : t("tenant.payments.expand")}</span>
              {expanded ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
            </button>
          </div>
        </div>
      </div>

      {/* Expanded Detail Panel (Human-readable breakdown; zero database IDs) */}
      {expanded && (
        <div className="border-t border-border/60 bg-secondary/20 px-4 sm:px-5 py-4 space-y-3">
          <p className="text-xs font-bold text-foreground">
            {t("tenant.payments.detailsTitle")}
          </p>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-x-6 gap-y-3 text-[11.5px]">
            <div className="space-y-0.5">
              <p className="text-muted-foreground">{t("tenant.payments.amountDue")}</p>
              <p className="font-bold text-foreground font-mono">{formatCurrency(payment.amountDue, payment.currency)}</p>
            </div>
            <div className="space-y-0.5">
              <p className="text-muted-foreground">{t("tenant.payments.amountPaid")}</p>
              <p className="font-bold text-emerald-600 dark:text-emerald-400 font-mono">{formatCurrency(payment.amountPaid, payment.currency)}</p>
            </div>
            <div className="space-y-0.5">
              <p className="text-muted-foreground">{t("tenant.payments.remaining")}</p>
              <p className="font-bold text-foreground font-mono">{formatCurrency(amountRemaining, payment.currency)}</p>
            </div>
            {payment.paymentMethod !== null && payment.paymentMethod !== undefined && (
              <div className="space-y-0.5">
                <p className="text-muted-foreground">{t("tenant.payments.paymentMethod")}</p>
                <p className="font-semibold text-foreground">{entityLabel("paymentMethod", payment.paymentMethod)}</p>
              </div>
            )}
            {payment.receiptNumber && (
              <div className="space-y-0.5">
                <p className="text-muted-foreground">{t("tenant.payments.receiptNumber")}</p>
                <p className="font-bold text-foreground font-mono">{payment.receiptNumber}</p>
              </div>
            )}
            {payment.notes && (
              <div className="col-span-2 sm:col-span-4 space-y-0.5 pt-1">
                <p className="text-muted-foreground">{t("tenant.payments.notes")}</p>
                <p className="text-foreground text-xs leading-relaxed">{payment.notes}</p>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Summary Bar ──────────────────────────────────────────────────────────────

function SummaryBar({
  payments,
  t,
}: {
  payments: TenantPaymentDto[];
  t: (key: string, fallback?: string) => string;
}) {
  const currency = payments[0]?.currency ?? "JOD";
  const totalDue = payments.reduce((sum, p) => sum + p.amountDue, 0);
  const totalPaid = payments.reduce((sum, p) => sum + p.amountPaid, 0);
  const overdueCount = payments.filter((p) => {
    const s = normalizeDueDateStatus(p.dueDateStatus);
    return s === "late" || s === "overdueunpaid";
  }).length;

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div className="p-4 rounded-xl bg-card border border-border shadow-2xs space-y-1">
        <p className="text-[11px] text-muted-foreground uppercase tracking-wide font-semibold">
          {t("tenant.payments.summaryTotal")}
        </p>
        <p className="text-lg font-bold text-foreground font-mono">
          {formatCurrency(totalDue, currency)}
        </p>
      </div>
      <div className="p-4 rounded-xl bg-card border border-border shadow-2xs space-y-1">
        <p className="text-[11px] text-muted-foreground uppercase tracking-wide font-semibold">
          {t("tenant.payments.summaryPaid")}
        </p>
        <p className="text-lg font-bold text-emerald-600 dark:text-emerald-400 font-mono">
          {formatCurrency(totalPaid, currency)}
        </p>
      </div>
      <div className="p-4 rounded-xl bg-card border border-border shadow-2xs space-y-1">
        <p className="text-[11px] text-muted-foreground uppercase tracking-wide font-semibold">
          {t("tenant.payments.summaryOverdue")}
        </p>
        <p className={`text-lg font-bold font-mono ${overdueCount > 0 ? "text-destructive" : "text-foreground"}`}>
          {overdueCount} {overdueCount === 1 ? t("tenant.payments.payment") : t("tenant.payments.payments")}
        </p>
      </div>
    </div>
  );
}

// ─── Page Component ───────────────────────────────────────────────────────────

export function TenantPaymentsPage() {
  const { t, language } = useTranslation();
  const { data: payments = [], isLoading, isError, refetch } = useMyPayments();
  const [selectedPayment, setSelectedPayment] = useState<TenantPaymentDto | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const handleOpenVerification = (payment: TenantPaymentDto) => {
    setSelectedPayment(payment);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setSelectedPayment(null);
  };

  // Sort: unpaid / overdue first, then by due date descending
  const sortedPayments = [...payments].sort((a, b) => {
    const priority = (s: unknown) => {
      const lower = normalizeDueDateStatus(s);
      if (lower === "overdueunpaid") return 0;
      if (lower === "late") return 1;
      if (lower === "pending") return 2;
      if (lower === "pendingverification") return 3;
      if (lower === "partiallypaid") return 4;
      if (lower === "paid") return 5;
      return 6;
    };
    const pDiff = priority(a.dueDateStatus) - priority(b.dueDateStatus);
    if (pDiff !== 0) return pDiff;
    const aDate = a.dueDate ? new Date(a.dueDate).getTime() : 0;
    const bDate = b.dueDate ? new Date(b.dueDate).getTime() : 0;
    return bDate - aDate;
  });

  return (
    <PageContainer
      title={t("tenant.payments.title")}
      description={t("tenant.payments.subtitle")}
    >
      <SubmitPaymentVerificationModal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        selectedPayment={selectedPayment}
      />

      <div className="max-w-4xl space-y-4">
        {/* Loading Skeleton */}
        {isLoading && (
          <div className="space-y-4 animate-pulse" aria-busy="true" aria-label={t("common.loading")}>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-20 bg-card rounded-xl border border-border" />
              ))}
            </div>
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-28 bg-card rounded-xl border border-border" />
            ))}
          </div>
        )}

        {/* Error State */}
        {!isLoading && isError && (
          <div className="p-8 rounded-xl border border-destructive/30 bg-destructive/5 text-center space-y-4">
            <AlertCircle className="w-10 h-10 text-destructive mx-auto" />
            <div>
              <h3 className="text-base font-semibold text-foreground">
                {t("tenant.payments.errorTitle")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1">
                {t("tenant.payments.errorDescription")}
              </p>
            </div>
            <button
              onClick={() => refetch()}
              className="px-4 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors shadow-xs cursor-pointer inline-flex items-center gap-2"
            >
              {t("tenant.payments.retry")}
            </button>
          </div>
        )}

        {/* Loaded Content */}
        {!isLoading && !isError && (
          <>
            {/* Top Summary Bar */}
            {payments.length > 0 && <SummaryBar payments={payments} t={t} />}

            {/* Empty State */}
            {sortedPayments.length === 0 ? (
              <div className="p-10 rounded-2xl border border-dashed border-border bg-card text-center space-y-4">
                <div className="w-14 h-14 rounded-full bg-secondary flex items-center justify-center mx-auto text-muted-foreground">
                  <CreditCard className="w-7 h-7" />
                </div>
                <div className="max-w-md mx-auto">
                  <h3 className="text-base font-bold text-foreground">
                    {t("tenant.payments.emptyTitle")}
                  </h3>
                  <p className="text-xs text-muted-foreground mt-1 leading-relaxed">
                    {t("tenant.payments.emptyDescription")}
                  </p>
                </div>
              </div>
            ) : (
              /* Payments List */
              <div className="space-y-4">
                <div className="flex items-center justify-between border-b border-border/70 pb-3">
                  <h2 className="text-sm font-bold text-foreground">
                    {t("tenant.payments.listTitle")}
                  </h2>
                  <span className="text-xs text-muted-foreground font-mono">
                    {sortedPayments.length} {sortedPayments.length === 1 ? t("tenant.payments.payment") : t("tenant.payments.payments")}
                  </span>
                </div>

                <div className="space-y-3">
                  {sortedPayments.map((payment) => (
                    <PaymentCard
                      key={payment.id}
                      payment={payment}
                      language={language}
                      t={t}
                      onSubmitVerification={handleOpenVerification}
                    />
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </PageContainer>
  );
}
