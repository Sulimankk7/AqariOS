import React, { useState } from "react";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useMyPayments } from "../hooks/useTenantPayments";
import type { TenantPaymentDto } from "../types/tenantPortal.types";
import { SubmitPaymentVerificationModal } from "../components/SubmitPaymentVerificationModal";
import { paymentsApi } from "@/features/payments/api/payments.api";
import { filesApi } from "@/shared/services/files.api";
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
} from "lucide-react";

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatDate(dateStr?: string | null, locale = "en-US"): string {
  if (!dateStr) return "—";
  try {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    return d.toLocaleDateString(locale === "ar" ? "ar-JO" : "en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  } catch {
    return dateStr;
  }
}

function formatCurrency(amount: number, currency = "JOD"): string {
  return `${amount.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} ${currency}`;
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

  const str = String(val).trim();
  const normalizedStr = str.replace(/[^a-zA-Z]/g, "").toLowerCase();

  switch (normalizedStr) {
    case "pending":
      return "pending";
    case "pendingverification":
      return "pendingverification";
    case "paid":
      return "paid";
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

function normalizePaymentMethod(method: unknown): string {
  if (typeof method === "number") {
    switch (method) {
      case 0:
        return "Cash";
      case 1:
        return "BankTransfer";
      case 2:
        return "Cheque";
      case 3:
        return "Efawateercom";
      case 4:
        return "CliQ";
      default:
        return String(method);
    }
  }
  return String(method ?? "");
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
        label: t("tenant.payments.statusPaid", "Paid"),
        className:
          "bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 border-emerald-500/25",
        icon: <CheckCircle2 className="w-3.5 h-3.5" />,
      };
    case "pending":
      return {
        label: t("tenant.payments.statusPending", "Due"),
        className:
          "bg-amber-500/10 text-amber-700 dark:text-amber-400 border-amber-500/25",
        icon: <Clock className="w-3.5 h-3.5" />,
      };
    case "pendingverification":
      return {
        label: t("tenant.payments.statusPendingVerification", "Under Review"),
        className:
          "bg-blue-500/10 text-blue-700 dark:text-blue-400 border-blue-500/25",
        icon: <Clock className="w-3.5 h-3.5" />,
      };
    case "partiallypaid":
      return {
        label: t("tenant.payments.statusPartiallyPaid", "Partially Paid"),
        className:
          "bg-sky-500/10 text-sky-700 dark:text-sky-400 border-sky-500/25",
        icon: <Wallet className="w-3.5 h-3.5" />,
      };
    case "late":
      return {
        label: t("tenant.payments.statusLate", "Late"),
        className:
          "bg-orange-500/10 text-orange-700 dark:text-orange-400 border-orange-500/25",
        icon: <AlertCircle className="w-3.5 h-3.5" />,
      };
    case "overdueunpaid":
      return {
        label: t("tenant.payments.statusOverdue", "Overdue & Unpaid"),
        className:
          "bg-destructive/10 text-destructive border-destructive/25",
        icon: <AlertCircle className="w-3.5 h-3.5" />,
      };
    case "cancelled":
      return {
        label: t("tenant.payments.statusCancelled", "Cancelled"),
        className:
          "bg-secondary text-muted-foreground border-border",
        icon: <XCircle className="w-3.5 h-3.5" />,
      };
    case "unknown":
    default:
      return {
        label: t("tenant.payments.statusUnavailable", "Payment status unavailable"),
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
      return t("tenant.payments.purposeScheduledInstallment", "Rent Installment");
    case "unallocatedreceipt":
      return t("tenant.payments.purposeUnallocatedReceipt", "Received Payment");
    case "adjustmentcredit":
      return t("tenant.payments.purposeAdjustmentCredit", "Credit Adjustment");
    case "adjustmentdebit":
      return t("tenant.payments.purposeAdjustmentDebit", "Debit Adjustment");
    default:
      return String(purpose ?? "");
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
  const [expanded, setExpanded] = useState(false);
  const [isDownloadingReceipt, setIsDownloadingReceipt] = useState(false);

  const statusBadge = getStatusBadge(payment.dueDateStatus, t);
  const canSubmit = isSubmittable(payment.dueDateStatus);
  const isPendingVerification = normalizeDueDateStatus(payment.dueDateStatus) === "pendingverification";
  const amountRemaining = Math.max(0, payment.amountDue - payment.amountPaid);

  const handleDownloadReceipt = async () => {
    setIsDownloadingReceipt(true);
    try {
      const receiptRes = await paymentsApi.getReceiptByRentPaymentId(payment.id);
      if (receiptRes?.fileId) {
        const fileRes = await filesApi.getFileDownloadUrl(receiptRes.fileId, false);
        window.open(fileRes.downloadUrl, '_blank');
      } else {
        alert(t("tenant.payments.noReceiptPdf", "Receipt PDF is not available for download yet."));
      }
    } catch (err) {
      console.error('Failed to download tenant receipt:', err);
    } finally {
      setIsDownloadingReceipt(false);
    }
  };

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
                    {t("tenant.payments.unit", "Unit")} {payment.apartmentNumber}
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
                  {t("tenant.payments.remaining", "Remaining")}: {formatCurrency(amountRemaining, payment.currency)}
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
            <span>{t("tenant.payments.pendingVerificationNotice", "Payment verification submitted and pending property management review.")}</span>
          </div>
        )}

        {/* Approved Receipt Badge (Only shown when receiptNumber is provided by backend) */}
        {payment.receiptNumber && (
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
              <span>{isDownloadingReceipt ? t("common.loading", "Loading...") : t("tenant.payments.downloadReceipt", "Download Receipt (سند قبض)")}</span>
            </button>
          </div>
        )}

        {/* Due date + action row */}
        <div className="flex items-center justify-between pt-3 border-t border-border/60">
          <div className="flex items-center gap-4 text-[11px] text-muted-foreground">
            {payment.dueDate && (
              <span className="flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-primary/70" />
                {t("tenant.payments.due", "Due Date")}: {formatDate(payment.dueDate, language)}
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
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors cursor-pointer shadow-xs"
              >
                <ArrowUpRight className="w-3.5 h-3.5" />
                {t("tenant.payments.submitPaymentAction", "Submit Payment")}
              </button>
            )}
            <button
              onClick={() => setExpanded((v) => !v)}
              aria-expanded={expanded}
              className="px-2.5 py-1.5 rounded-lg text-[11px] font-medium text-muted-foreground hover:text-foreground hover:bg-secondary transition-colors cursor-pointer flex items-center gap-1"
            >
              <span>{expanded ? t("tenant.payments.collapse", "Hide Details") : t("tenant.payments.expand", "View Details")}</span>
              {expanded ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
            </button>
          </div>
        </div>
      </div>

      {/* Expanded Detail Panel (Human-readable breakdown; zero database IDs) */}
      {expanded && (
        <div className="border-t border-border/60 bg-secondary/20 px-4 sm:px-5 py-4 space-y-3">
          <p className="text-xs font-bold text-foreground">
            {t("tenant.payments.detailsTitle", "Payment Breakdown")}
          </p>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-x-6 gap-y-3 text-[11.5px]">
            <div className="space-y-0.5">
              <p className="text-muted-foreground">{t("tenant.payments.amountDue", "Amount Due")}</p>
              <p className="font-bold text-foreground font-mono">{formatCurrency(payment.amountDue, payment.currency)}</p>
            </div>
            <div className="space-y-0.5">
              <p className="text-muted-foreground">{t("tenant.payments.amountPaid", "Amount Paid")}</p>
              <p className="font-bold text-emerald-600 dark:text-emerald-400 font-mono">{formatCurrency(payment.amountPaid, payment.currency)}</p>
            </div>
            <div className="space-y-0.5">
              <p className="text-muted-foreground">{t("tenant.payments.remaining", "Remaining Amount")}</p>
              <p className="font-bold text-foreground font-mono">{formatCurrency(amountRemaining, payment.currency)}</p>
            </div>
            {payment.paymentMethod !== null && payment.paymentMethod !== undefined && (
              <div className="space-y-0.5">
                <p className="text-muted-foreground">{t("tenant.payments.paymentMethod", "Payment Method")}</p>
                <p className="font-semibold text-foreground">{normalizePaymentMethod(payment.paymentMethod)}</p>
              </div>
            )}
            {payment.receiptNumber && (
              <div className="space-y-0.5">
                <p className="text-muted-foreground">{t("tenant.payments.receiptNumber", "Receipt Number")}</p>
                <p className="font-bold text-foreground font-mono">{payment.receiptNumber}</p>
              </div>
            )}
            {payment.notes && (
              <div className="col-span-2 sm:col-span-4 space-y-0.5 pt-1">
                <p className="text-muted-foreground">{t("tenant.payments.notes", "Notes")}</p>
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
          {t("tenant.payments.summaryTotal", "Total Invoiced")}
        </p>
        <p className="text-lg font-bold text-foreground font-mono">
          {formatCurrency(totalDue, currency)}
        </p>
      </div>
      <div className="p-4 rounded-xl bg-card border border-border shadow-2xs space-y-1">
        <p className="text-[11px] text-muted-foreground uppercase tracking-wide font-semibold">
          {t("tenant.payments.summaryPaid", "Total Paid")}
        </p>
        <p className="text-lg font-bold text-emerald-600 dark:text-emerald-400 font-mono">
          {formatCurrency(totalPaid, currency)}
        </p>
      </div>
      <div className="p-4 rounded-xl bg-card border border-border shadow-2xs space-y-1">
        <p className="text-[11px] text-muted-foreground uppercase tracking-wide font-semibold">
          {t("tenant.payments.summaryOverdue", "Overdue Payments")}
        </p>
        <p className={`text-lg font-bold font-mono ${overdueCount > 0 ? "text-destructive" : "text-foreground"}`}>
          {overdueCount} {overdueCount === 1 ? t("tenant.payments.payment", "payment") : t("tenant.payments.payments", "payments")}
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
      title={t("tenant.payments.title", "My Payments")}
      description={t("tenant.payments.subtitle", "View your rent payment schedule and submit payment proofs for verification.")}
    >
      <SubmitPaymentVerificationModal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        selectedPayment={selectedPayment}
      />

      <div className="max-w-4xl space-y-4">
        {/* Loading Skeleton */}
        {isLoading && (
          <div className="space-y-4 animate-pulse" aria-busy="true" aria-label={t("common.loading", "Loading...")}>
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
                {t("tenant.payments.errorTitle", "Failed to load payments")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1">
                {t("tenant.payments.errorDescription", "An error occurred while connecting to the server. Please try again.")}
              </p>
            </div>
            <button
              onClick={() => refetch()}
              className="px-4 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors shadow-xs cursor-pointer inline-flex items-center gap-2"
            >
              {t("tenant.payments.retry", "Retry")}
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
                    {t("tenant.payments.emptyTitle", "No payment records found")}
                  </h3>
                  <p className="text-xs text-muted-foreground mt-1 leading-relaxed">
                    {t("tenant.payments.emptyDescription", "Your rent payment schedule will appear here once your lease contract is activated by property management.")}
                  </p>
                </div>
              </div>
            ) : (
              /* Payments List */
              <div className="space-y-4">
                <div className="flex items-center justify-between border-b border-border/70 pb-3">
                  <h2 className="text-sm font-bold text-foreground">
                    {t("tenant.payments.listTitle", "Rent Payment Schedule")}
                  </h2>
                  <span className="text-xs text-muted-foreground font-mono">
                    {sortedPayments.length} {sortedPayments.length === 1 ? t("tenant.payments.payment", "payment") : t("tenant.payments.payments", "payments")}
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
