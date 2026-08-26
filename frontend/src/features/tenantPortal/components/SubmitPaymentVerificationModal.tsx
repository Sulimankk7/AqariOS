import React, { useState, useRef, useEffect } from "react";
import { useTranslation } from "@/shared/i18n";
import { useSubmitPaymentVerification } from "../hooks/useTenantPayments";
import { PaymentMethod } from "../types/tenantPortal.types";
import type { TenantPaymentDto } from "../types/tenantPortal.types";
import { filesApi } from "@/shared/services/files.api";
import { extractUserFriendlyError } from "@/shared/utils";
import {
  CreditCard,
  UploadCloud,
  FileCheck,
  CheckCircle2,
  AlertCircle,
  X,
  Loader2,
  Info,
  Building2,
  Home,
} from "lucide-react";

interface SubmitPaymentVerificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedPayment?: TenantPaymentDto | null;
  defaultRentPaymentId?: string;
}

const MAX_FILE_SIZE_BYTES = 15 * 1024 * 1024; // 15 MB
const ALLOWED_MIME_TYPES = [
  "application/pdf",
  "image/png",
  "image/jpeg",
  "image/jpg",
];

const GUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

/**
 * Tenant-exposable payment methods (strictly 3 methods: Cash, CliQ, Cheque).
 */
const TENANT_PAYMENT_METHODS = [
  { value: PaymentMethod.Cash, key: "Cash" },
  { value: PaymentMethod.CliQ, key: "CliQ" },
  { value: PaymentMethod.Cheque, key: "Cheque" },
] as const;

export function SubmitPaymentVerificationModal({
  isOpen,
  onClose,
  selectedPayment,
  defaultRentPaymentId = "",
}: SubmitPaymentVerificationModalProps) {
  const { t, language } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Internal payment GUID (resolved from selectedPayment object or prop)
  const resolvedPaymentId = selectedPayment?.id || defaultRentPaymentId || "";

  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.Cash);
  const [amount, setAmount] = useState<string>("");
  const [referenceNumber, setReferenceNumber] = useState("");
  
  // Cheque details state
  const [chequeNumber, setChequeNumber] = useState("");
  const [bankName, setBankName] = useState("");
  const [chequeIssueDate, setChequeIssueDate] = useState("");
  const [chequeDueDate, setChequeDueDate] = useState("");

  // Outstanding balance calculation
  const remainingBalance = selectedPayment
    ? Math.max(0, (selectedPayment.amountDue ?? 0) - (selectedPayment.amountPaid ?? 0))
    : 0;
  const paymentCurrency = selectedPayment?.currency || "JOD";

  // File upload state
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadedFileId, setUploadedFileId] = useState<string | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);

  // Form submit state
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const submitMutation = useSubmitPaymentVerification();

  useEffect(() => {
    if (isOpen) {
      setPaymentMethod(PaymentMethod.Cash);
      const initialRemaining = selectedPayment
        ? Math.max(0, (selectedPayment.amountDue ?? 0) - (selectedPayment.amountPaid ?? 0))
        : 0;
      setAmount(initialRemaining > 0 ? initialRemaining.toString() : "");
      setReferenceNumber("");
      setChequeNumber("");
      setBankName("");
      setChequeIssueDate("");
      setChequeDueDate("");
      handleRemoveFile();
      setSubmitError(null);
      setIsSuccess(false);
    }
  }, [isOpen, resolvedPaymentId, selectedPayment]);

  if (!isOpen) return null;

  const handleMethodChange = (newMethod: PaymentMethod) => {
    setPaymentMethod(newMethod);
    setReferenceNumber("");
    setChequeNumber("");
    setBankName("");
    setChequeIssueDate("");
    setChequeDueDate("");
    handleRemoveFile();
    setSubmitError(null);
  };

  const handleRemoveFile = () => {
    setSelectedFile(null);
    setUploadedFileId(null);
    setUploadedFileName(null);
    setUploadProgress(0);
    setUploadError(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate internal payment GUID
    const cleanPaymentId = resolvedPaymentId.trim();
    if (!cleanPaymentId || !GUID_REGEX.test(cleanPaymentId)) {
      setUploadError(
        t(
          "paymentVerification.paymentIdRequiredForUpload",
          "Please select a valid payment from your payment schedule before uploading proof."
        )
      );
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setUploadError(t("paymentVerification.fileTooLarge", "File size exceeds 15 MB limit."));
      return;
    }

    if (!ALLOWED_MIME_TYPES.includes(file.type)) {
      setUploadError(t("paymentVerification.fileInvalidType", "Invalid file type. Only PDF, PNG, and JPG images are allowed."));
      return;
    }

    setUploadError(null);
    setSelectedFile(file);
    setIsUploading(true);
    setUploadProgress(10);

    try {
      // Phase 1: Request Upload using internal RentPayment GUID
      const uploadReq = await filesApi.requestUpload({
        moduleName: "Financials",
        entityId: cleanPaymentId,
        filename: file.name,
        mimeType: file.type || "application/octet-stream",
        sizeBytes: file.size,
      });

      // Phase 2: Binary Upload
      await filesApi.uploadBinary(uploadReq.uploadUrl, file, (percent) => {
        setUploadProgress(10 + Math.round(percent * 0.8));
      });

      // Phase 3: Confirm Upload
      const confirmed = await filesApi.confirmUpload({
        fileId: uploadReq.fileId,
        storageKey: uploadReq.storageKey,
        originalFilename: file.name,
        mimeType: file.type || "application/octet-stream",
        sizeBytes: file.size,
      });

      setUploadedFileId(confirmed.id);
      setUploadedFileName(confirmed.originalFilename);
      setUploadProgress(100);
    } catch (err: any) {
      setUploadError(extractUserFriendlyError(err, t("paymentVerification.uploadFailed", "Failed to upload proof file.")));
      setSelectedFile(null);
    } finally {
      setIsUploading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const cleanPaymentId = resolvedPaymentId.trim();

    if (!cleanPaymentId || !GUID_REGEX.test(cleanPaymentId)) {
      setSubmitError(
        t(
          "paymentVerification.invalidGuidError",
          "Please select a valid payment from your payment schedule."
        )
      );
      return;
    }

    // Validate submitted amount
    const parsedAmount = parseFloat(amount);
    if (isNaN(parsedAmount) || parsedAmount <= 0) {
      setSubmitError(
        t("paymentVerification.invalidAmount", "Please enter a valid payment amount greater than zero.")
      );
      return;
    }

    if (remainingBalance > 0 && parsedAmount > remainingBalance) {
      setSubmitError(
        t(
          "paymentVerification.amountExceedsOutstanding",
          `Submitted payment amount (${parsedAmount} ${paymentCurrency}) cannot exceed the remaining balance (${remainingBalance} ${paymentCurrency}).`
        )
      );
      return;
    }

    // Method-specific validation strictly matching backend validator rules
    if (paymentMethod === PaymentMethod.CliQ) {
      if (!referenceNumber.trim()) {
        setSubmitError(t("paymentVerification.cliQRefRequired", "Please enter your CliQ alias or phone number."));
        return;
      }
      if (!uploadedFileId) {
        setSubmitError(t("paymentVerification.cliQProofRequired", "Please attach your CliQ transfer proof screenshot."));
        return;
      }
    } else if (paymentMethod === PaymentMethod.Cheque) {
      if (!chequeNumber.trim()) {
        setSubmitError(t("paymentVerification.chequeNumberRequired", "Please enter the cheque number (رقم الشيك)."));
        return;
      }
      if (!bankName.trim()) {
        setSubmitError(t("paymentVerification.bankNameRequired", "Please enter the bank name (اسم البنك)."));
        return;
      }
      if (!chequeIssueDate) {
        setSubmitError(t("paymentVerification.issueDateRequired", "Please select the cheque issue date (تاريخ إصدار الشيك)."));
        return;
      }
      if (!chequeDueDate) {
        setSubmitError(t("paymentVerification.dueDateRequired", "Please select the cheque due date (تاريخ استحقاق الشيك)."));
        return;
      }
      if (new Date(chequeDueDate) < new Date(chequeIssueDate)) {
        setSubmitError(t("paymentVerification.dueDateInvalid", "Cheque due date cannot be earlier than the issue date."));
        return;
      }
    }

    setSubmitError(null);

    const payload: any = {
      amount: parsedAmount,
      paymentMethod,
      referenceNumber: paymentMethod === PaymentMethod.Cash
        ? (referenceNumber.trim() || null)
        : paymentMethod === PaymentMethod.Cheque
        ? (chequeNumber.trim() || null)
        : referenceNumber.trim(),
      proofFileId: paymentMethod === PaymentMethod.Cash ? null : (uploadedFileId || null),
    };

    if (paymentMethod === PaymentMethod.Cheque) {
      payload.chequeDetails = {
        chequeNumber: chequeNumber.trim(),
        bankName: bankName.trim(),
        issueDate: chequeIssueDate,
        dueDate: chequeDueDate,
      };
    }

    submitMutation.mutate(
      {
        rentPaymentId: cleanPaymentId,
        payload,
      },
      {
        onSuccess: () => {
          setIsSuccess(true);
        },
        onError: (err: any) => {
          setSubmitError(
            extractUserFriendlyError(
              err,
              t("paymentVerification.submitFailed", "Failed to submit payment verification. Please check details and try again.")
            )
          );
        },
      }
    );
  };

  const handleReset = () => {
    setIsSuccess(false);
    setSubmitError(null);
    handleRemoveFile();
    onClose();
  };

  return (
    <div
      className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4 overflow-y-auto"
      aria-modal="true"
      role="dialog"
      aria-labelledby="modal-title"
    >
      <div className="bg-card border border-border rounded-2xl shadow-xl w-full max-w-lg overflow-hidden animate-in fade-in-50 zoom-in-95 duration-200">
        {/* Modal Header */}
        <div className="h-14 px-6 border-b border-border flex items-center justify-between bg-card shrink-0">
          <div className="flex items-center gap-2.5">
            <CreditCard className="w-5 h-5 text-brand-green-600 dark:text-brand-green-400 shrink-0" />
            <h3 id="modal-title" className="text-sm font-bold text-foreground">
              {t("paymentVerification.modalTitle", "Submit Rent Payment Verification")}
            </h3>
          </div>
          <button
            onClick={handleReset}
            className="w-7 h-7 rounded-lg flex items-center justify-center text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer"
            aria-label="Close"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-6 space-y-5">
          {/* Success Screen */}
          {isSuccess ? (
            <div className="text-center py-6 space-y-4">
              <div className="w-14 h-14 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-8 h-8" />
              </div>
              <div className="space-y-1">
                <h4 className="text-base font-bold text-foreground">
                  {t("paymentVerification.successTitle", "Payment Submitted for Verification")}
                </h4>
                <p className="text-xs text-muted-foreground max-w-xs mx-auto">
                  {t("paymentVerification.successDescription", "Your payment details have been sent to property management for review.")}
                </p>
              </div>

              <div className="p-3.5 rounded-xl bg-secondary/50 border border-border/50 text-[11.5px] text-muted-foreground flex items-center gap-2.5 max-w-xs mx-auto text-start">
                <Info className="w-4 h-4 text-brand-green-600 shrink-0" />
                <span>{t("paymentVerification.submissionNote", "Note: Submissions are reviewed and verified by property management before receipts are generated.")}</span>
              </div>

              <div className="pt-2">
                <button
                  onClick={handleReset}
                  className="px-5 py-2.5 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors cursor-pointer"
                >
                  {t("common.done", "Done")}
                </button>
              </div>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              <p className="text-xs text-muted-foreground leading-relaxed">
                {t("paymentVerification.modalSubtitle", "Select your payment method and submit details for property management review.")}
              </p>

              {/* Selected Payment Human-Readable Summary Banner */}
              {selectedPayment && (
                <div className="p-3.5 rounded-xl bg-secondary/40 border border-border/60 flex flex-col gap-2 text-xs">
                  <div className="flex items-center justify-between gap-3">
                    <div className="space-y-0.5 min-w-0">
                      <span className="text-[11px] text-muted-foreground block">
                        {t("paymentVerification.selectedPaymentLabel", "Selected Rent Payment")}
                      </span>
                      <p className="font-bold text-foreground truncate">
                        {selectedPayment.contractNumber ? `${selectedPayment.contractNumber}` : t("paymentVerification.installment", "Scheduled Installment")}
                        {selectedPayment.dueDate && ` — ${new Date(selectedPayment.dueDate).toLocaleDateString(language)}`}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      {selectedPayment.buildingName && (
                        <span className="hidden sm:inline-flex items-center gap-1 text-[11px] text-muted-foreground">
                          <Building2 className="w-3 h-3" />
                          {selectedPayment.buildingName}
                        </span>
                      )}
                      {selectedPayment.apartmentNumber && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-primary/10 text-primary font-medium text-[11px]">
                          <Home className="w-3 h-3" />
                          {selectedPayment.apartmentNumber}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Financial Breakdown: Amount Due vs Remaining Balance */}
                  <div className="grid grid-cols-2 gap-2 pt-2 border-t border-border/40">
                    <div>
                      <span className="text-[10px] text-muted-foreground block">
                        {t("paymentVerification.totalDue", "Total Installment Due / إجمالي القسط")}
                      </span>
                      <span className="font-medium text-foreground text-xs">
                        {selectedPayment.amountDue.toLocaleString()} {paymentCurrency}
                      </span>
                    </div>
                    <div className="text-right rtl:text-left">
                      <span className="text-[10px] text-muted-foreground block">
                        {t("paymentVerification.remainingBalance", "المبلغ المستحق (Remaining Balance)")}
                      </span>
                      <span className="font-bold text-primary text-xs">
                        {remainingBalance.toLocaleString()} {paymentCurrency}
                      </span>
                    </div>
                  </div>
                </div>
              )}

              {/* Error Alert */}
              {submitError && (
                <div className="p-3 rounded-xl border border-destructive/30 bg-destructive/5 text-destructive text-xs flex items-start gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>{submitError}</span>
                </div>
              )}

              {/* Amount Actually Paid Input */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-semibold text-foreground block">
                    {t("paymentVerification.amountPaidLabel", "المبلغ المدفوع فعلياً (Actually Paid Amount) *")}
                  </label>
                  {remainingBalance > 0 && (
                    <button
                      type="button"
                      onClick={() => setAmount(remainingBalance.toString())}
                      className="text-[11px] text-primary hover:underline font-medium cursor-pointer"
                    >
                      {t("paymentVerification.payFullBalance", "Pay Full Balance / دفع كامل المبلغ")}
                    </button>
                  )}
                </div>
                <div className="relative">
                  <input
                    type="number"
                    step="0.001"
                    min="0.001"
                    max={remainingBalance > 0 ? remainingBalance : undefined}
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    placeholder={remainingBalance > 0 ? remainingBalance.toString() : "0.000"}
                    disabled={submitMutation.isPending}
                    className="w-full h-9 px-3 pe-14 rounded-lg border border-border bg-background text-xs font-mono text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:opacity-50"
                    required
                  />
                  <div className="absolute inset-y-0 end-0 pe-3 flex items-center pointer-events-none text-xs text-muted-foreground font-medium">
                    {paymentCurrency}
                  </div>
                </div>
                <p className="text-[11px] text-muted-foreground">
                  {t("paymentVerification.amountPaidHint", "أدخل المبلغ الذي قمت بتحويله أو دفعه فعلياً (يمكنك دفع كامل المبلغ المستحق أو دفعة جزئية).")}
                </p>
              </div>

              {/* 1. Payment Method Selector (Strictly 3 methods: Cash, CliQ, Cheque) */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-foreground block">
                  {t("paymentVerification.paymentMethodLabel", "Payment Method")}
                </label>
                <select
                  value={paymentMethod}
                  onChange={(e) => handleMethodChange(e.target.value as PaymentMethod)}
                  disabled={submitMutation.isPending}
                  className="w-full h-9 px-3 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 cursor-pointer disabled:opacity-50"
                >
                  {TENANT_PAYMENT_METHODS.map((m) => (
                    <option key={m.value} value={m.value}>
                      {t(`paymentVerification.methods.${m.key}`, m.key)}
                    </option>
                  ))}
                </select>
              </div>

              {/* 2. METHOD-SPECIFIC FORM SECTIONS */}

              {/* CASH METHOD FORM */}
              {paymentMethod === PaymentMethod.Cash && (
                <div className="space-y-4 pt-1">
                  <div className="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/25 text-amber-700 dark:text-amber-400 text-xs flex items-start gap-2.5 leading-relaxed">
                    <Info className="w-4 h-4 shrink-0 mt-0.5" />
                    <span>{t("paymentVerification.cashNotice", "Cash payment declaration will be registered as 'Pending Verification' until cash is physically received and verified by property management.")}</span>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-xs font-semibold text-foreground block">
                      {t("paymentVerification.cashNotesLabel", "Additional Notes (Optional)")}
                    </label>
                    <textarea
                      value={referenceNumber}
                      onChange={(e) => setReferenceNumber(e.target.value)}
                      placeholder={t("paymentVerification.cashNotesPlaceholder", "Add any optional notes about this cash payment...")}
                      rows={2}
                      className="w-full p-2.5 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 resize-none"
                    />
                  </div>
                </div>
              )}

              {/* CLIQ METHOD FORM */}
              {paymentMethod === PaymentMethod.CliQ && (
                <div className="space-y-4 pt-1">
                  <div className="space-y-1.5">
                    <label className="text-xs font-semibold text-foreground block">
                      {t("paymentVerification.cliQRefLabel", "CliQ Alias or Phone Number *")}
                    </label>
                    <input
                      type="text"
                      value={referenceNumber}
                      onChange={(e) => setReferenceNumber(e.target.value)}
                      placeholder={t("paymentVerification.cliQRefPlaceholder", "e.g., CLQ-8823104 or 0791234567...")}
                      className="w-full h-9 px-3 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 font-mono"
                      required
                    />
                    <p className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <Info className="w-3 h-3 text-primary/70 shrink-0" />
                      {t("paymentVerification.cliQRefHint", "Enter the CliQ alias or phone number associated with the account transferred from.")}
                    </p>
                  </div>

                  {/* Proof Attachment (Required for CliQ) */}
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between">
                      <label className="text-xs font-semibold text-foreground block">
                        {t("paymentVerification.cliQProofLabel", "CliQ Transfer Proof Screenshot *")}
                      </label>
                      <span className="text-[10px] font-semibold text-primary">
                        {t("paymentVerification.proofRequiredBadge", "(Required)")}
                      </span>
                    </div>

                    {uploadError && (
                      <p className="text-[11px] text-destructive flex items-center gap-1">
                        <AlertCircle className="w-3 h-3" />
                        {uploadError}
                      </p>
                    )}

                    {uploadedFileId ? (
                      <div className="p-3 rounded-xl border border-emerald-500/30 bg-emerald-500/5 flex items-center justify-between gap-2">
                        <div className="flex items-center gap-2 overflow-hidden">
                          <FileCheck className="w-4 h-4 text-emerald-600 shrink-0" />
                          <span className="text-xs font-medium text-foreground truncate">
                            {uploadedFileName}
                          </span>
                        </div>
                        <button
                          type="button"
                          onClick={handleRemoveFile}
                          className="text-xs text-destructive hover:underline shrink-0"
                        >
                          {t("paymentVerification.removeFile", "Remove")}
                        </button>
                      </div>
                    ) : (
                      <div
                        onClick={() => fileInputRef.current?.click()}
                        className={`p-4 rounded-xl border border-dashed text-center transition-colors cursor-pointer ${
                          isUploading
                            ? "border-primary bg-primary/5"
                            : "border-border hover:border-primary/50 bg-secondary/30"
                        }`}
                      >
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept=".pdf,.png,.jpg,.jpeg"
                          onChange={handleFileSelect}
                          className="hidden"
                        />

                        {isUploading ? (
                          <div className="space-y-2">
                            <Loader2 className="w-6 h-6 text-primary animate-spin mx-auto" />
                            <span className="text-xs text-muted-foreground block">
                              {t("paymentVerification.uploadingFile", "Uploading file...")} ({uploadProgress}%)
                            </span>
                            <div className="w-full h-1.5 bg-secondary rounded-full overflow-hidden">
                              <div
                                className="h-full bg-primary transition-all duration-200"
                                style={{ width: `${uploadProgress}%` }}
                              />
                            </div>
                          </div>
                        ) : (
                          <div className="space-y-1">
                            <UploadCloud className="w-6 h-6 text-muted-foreground mx-auto" />
                            <span className="text-xs font-medium text-foreground block">
                              {t("paymentVerification.uploadHint", "Click to upload proof file (max 15 MB)")}
                            </span>
                            <span className="text-[10px] text-muted-foreground block">
                              PDF, PNG, JPG
                            </span>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* CHEQUE METHOD FORM */}
              {paymentMethod === PaymentMethod.Cheque && (
                <div className="space-y-4 pt-1">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div className="space-y-1.5">
                      <label className="text-xs font-semibold text-foreground block">
                        {t("paymentVerification.chequeNumberLabel", "رقم الشيك / Cheque Number *")}
                      </label>
                      <input
                        type="text"
                        value={chequeNumber}
                        onChange={(e) => setChequeNumber(e.target.value)}
                        placeholder="e.g. 104928"
                        className="w-full h-9 px-3 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 font-mono"
                        required
                      />
                    </div>

                    <div className="space-y-1.5">
                      <label className="text-xs font-semibold text-foreground block">
                        {t("paymentVerification.bankNameLabel", "اسم البنك / Bank Name *")}
                      </label>
                      <input
                        type="text"
                        value={bankName}
                        onChange={(e) => setBankName(e.target.value)}
                        placeholder="e.g. البنك العربي / Arab Bank"
                        className="w-full h-9 px-3 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
                        required
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div className="space-y-1.5">
                      <label className="text-xs font-semibold text-foreground block">
                        {t("paymentVerification.chequeIssueDateLabel", "تاريخ إصدار الشيك / Issue Date *")}
                      </label>
                      <input
                        type="date"
                        value={chequeIssueDate}
                        onChange={(e) => setChequeIssueDate(e.target.value)}
                        className="w-full h-9 px-3 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
                        required
                      />
                    </div>

                    <div className="space-y-1.5">
                      <label className="text-xs font-semibold text-foreground block">
                        {t("paymentVerification.chequeDueDateLabel", "تاريخ استحقاق الشيك / Due Date *")}
                      </label>
                      <input
                        type="date"
                        value={chequeDueDate}
                        onChange={(e) => setChequeDueDate(e.target.value)}
                        className="w-full h-9 px-3 rounded-lg border border-border bg-background text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
                        required
                      />
                    </div>
                  </div>

                  {/* Proof Attachment (Optional for Cheque) */}
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between">
                      <label className="text-xs font-semibold text-foreground block">
                        {t("paymentVerification.chequeProofLabel", "Bank Cheque Photo (Optional)")}
                      </label>
                      <span className="text-[10px] text-muted-foreground">
                        {t("paymentVerification.proofOptionalBadge", "(Optional)")}
                      </span>
                    </div>

                    {uploadError && (
                      <p className="text-[11px] text-destructive flex items-center gap-1">
                        <AlertCircle className="w-3 h-3" />
                        {uploadError}
                      </p>
                    )}

                    {uploadedFileId ? (
                      <div className="p-3 rounded-xl border border-emerald-500/30 bg-emerald-500/5 flex items-center justify-between gap-2">
                        <div className="flex items-center gap-2 overflow-hidden">
                          <FileCheck className="w-4 h-4 text-emerald-600 shrink-0" />
                          <span className="text-xs font-medium text-foreground truncate">
                            {uploadedFileName}
                          </span>
                        </div>
                        <button
                          type="button"
                          onClick={handleRemoveFile}
                          className="text-xs text-destructive hover:underline shrink-0"
                        >
                          {t("paymentVerification.removeFile", "Remove")}
                        </button>
                      </div>
                    ) : (
                      <div
                        onClick={() => fileInputRef.current?.click()}
                        className={`p-4 rounded-xl border border-dashed text-center transition-colors cursor-pointer ${
                          isUploading
                            ? "border-primary bg-primary/5"
                            : "border-border hover:border-primary/50 bg-secondary/30"
                        }`}
                      >
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept=".pdf,.png,.jpg,.jpeg"
                          onChange={handleFileSelect}
                          className="hidden"
                        />

                        {isUploading ? (
                          <div className="space-y-2">
                            <Loader2 className="w-6 h-6 text-primary animate-spin mx-auto" />
                            <span className="text-xs text-muted-foreground block">
                              {t("paymentVerification.uploadingFile", "Uploading file...")} ({uploadProgress}%)
                            </span>
                            <div className="w-full h-1.5 bg-secondary rounded-full overflow-hidden">
                              <div
                                className="h-full bg-primary transition-all duration-200"
                                style={{ width: `${uploadProgress}%` }}
                              />
                            </div>
                          </div>
                        ) : (
                          <div className="space-y-1">
                            <UploadCloud className="w-6 h-6 text-muted-foreground mx-auto" />
                            <span className="text-xs font-medium text-foreground block">
                              {t("paymentVerification.uploadHint", "Click to upload proof file (max 15 MB)")}
                            </span>
                            <span className="text-[10px] text-muted-foreground block">
                              PDF, PNG, JPG
                            </span>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Form Buttons */}
              <div className="pt-3 flex items-center justify-end gap-2.5">
                <button
                  type="button"
                  onClick={handleReset}
                  disabled={submitMutation.isPending || isUploading}
                  className="px-4 py-2 rounded-xl text-xs font-medium text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer disabled:opacity-50"
                >
                  {t("paymentVerification.cancelButton", "Cancel")}
                </button>

                <button
                  type="submit"
                  disabled={submitMutation.isPending || isUploading || !resolvedPaymentId.trim()}
                  className="inline-flex items-center gap-2 px-5 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 transition-colors cursor-pointer disabled:opacity-50 shadow-xs"
                >
                  {submitMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                  {submitMutation.isPending
                    ? t("paymentVerification.submittingButton", "Submitting...")
                    : paymentMethod === PaymentMethod.Cash
                    ? t("paymentVerification.submitCashButton", "Confirm Payment")
                    : t("paymentVerification.submitButton", "Submit for Review")}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
