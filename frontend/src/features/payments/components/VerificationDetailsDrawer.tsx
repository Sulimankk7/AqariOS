import React, { useState, useEffect } from 'react';
import { useTranslation } from '@/shared/i18n';
import { PaymentVerificationQueueItem, PaymentSubmissionDto, RentPaymentReceiptDto } from '../types/payments.types';
import { usePaymentDetails, useApproveSubmission, useRejectSubmission } from '../hooks/usePaymentVerifications';
import { paymentsApi } from '../api/payments.api';
import { filesApi, FileDownloadUrlResponse } from '@/shared/services/files.api';
import { RejectSubmissionModal } from './RejectSubmissionModal';
import { ApproveSubmissionModal } from './ApproveSubmissionModal';
import { X, CheckCircle2, XCircle, AlertCircle, Clock, ExternalLink, Download, FileText, Image as ImageIcon } from 'lucide-react';

interface VerificationDetailsDrawerProps {
  item: PaymentVerificationQueueItem | null;
  onClose: () => void;
}

export function VerificationDetailsDrawer({ item, onClose }: VerificationDetailsDrawerProps) {
  const { t, language, direction } = useTranslation();
  const isRtl = direction === 'rtl';
  
  const { data: details, isLoading, isError } = usePaymentDetails(item?.rentPaymentId || null);
  const approveMutation = useApproveSubmission();
  const rejectMutation = useRejectSubmission();
  
  const [isRejectModalOpen, setIsRejectModalOpen] = useState(false);
  const [isApproveModalOpen, setIsApproveModalOpen] = useState(false);

  const [proofFile, setProofFile] = useState<FileDownloadUrlResponse | null>(null);
  const [isProofLoading, setIsProofLoading] = useState(false);
  const [proofError, setProofError] = useState<string | null>(null);

  const [receipt, setReceipt] = useState<RentPaymentReceiptDto | null>(null);
  const [isReceiptLoading, setIsReceiptLoading] = useState(false);
  const [receiptPdfUrl, setReceiptPdfUrl] = useState<string | null>(null);

  const formatPaymentMethod = (method: any) => {
    if (method === null || method === undefined) return '-';
    if (typeof method === 'number' || (!isNaN(Number(method)) && String(method).trim() !== '')) {
      const num = Number(method);
      switch (num) {
        case 0: return t('Cash');
        case 1: return t('Bank Transfer');
        case 2: return t('Cheque');
        case 3: return t('eFAWATEERCOM');
        case 4: return t('CliQ');
        default: return String(method);
      }
    }
    const str = String(method).trim();
    const lower = str.toLowerCase().replace(/[^a-z]/g, '');
    if (lower === 'cash') return t('Cash');
    if (lower === 'banktransfer') return t('Bank Transfer');
    if (lower === 'cheque') return t('Cheque');
    if (lower === 'efawateercom') return t('eFAWATEERCOM');
    if (lower === 'cliq' || lower === 'cli_q') return t('CliQ');
    return t(str);
  };

  const currentSubmission = details?.submissions.find(s => s.id === item?.paymentSubmissionId);
  const targetProofId = currentSubmission?.proofFileId || item?.proofFileId;

  useEffect(() => {
    if (!targetProofId) {
      setProofFile(null);
      return;
    }
    setIsProofLoading(true);
    setProofError(null);
    filesApi.getFileDownloadUrl(targetProofId, true)
      .then(res => setProofFile(res))
      .catch(err => {
        console.error('Failed to load proof download URL:', err);
        setProofError(t('Unable to load proof file preview.'));
      })
      .finally(() => setIsProofLoading(false));
  }, [targetProofId]);

  useEffect(() => {
    if (!item?.rentPaymentId) {
      setReceipt(null);
      setReceiptPdfUrl(null);
      return;
    }
    setIsReceiptLoading(true);
    paymentsApi.getReceiptByRentPaymentId(item.rentPaymentId)
      .then(res => {
        setReceipt(res);
        if (res?.fileId) {
          filesApi.getFileDownloadUrl(res.fileId, false)
            .then(fileRes => setReceiptPdfUrl(fileRes.downloadUrl))
            .catch(() => setReceiptPdfUrl(null));
        }
      })
      .catch(err => {
        console.error('Failed to load receipt details:', err);
        setReceipt(null);
      })
      .finally(() => setIsReceiptLoading(false));
  }, [item?.rentPaymentId]);

  if (!item) return null;

  const handleApprove = () => {
    setIsApproveModalOpen(true);
  };

  return (
    <>
      <div 
        className="fixed inset-0 bg-black/60 backdrop-blur-xs z-40 transition-opacity"
        onClick={onClose}
      />
      
      <div className={`fixed inset-y-0 ${isRtl ? 'left-0' : 'right-0'} max-w-lg w-full bg-card border-${isRtl ? 'r' : 'l'} border-border shadow-xl z-50 overflow-y-auto p-6 flex flex-col justify-between`}>
        <div className="space-y-6">
          <div className="flex items-center justify-between pb-4 border-b border-border">
            <div>
              <h2 className="text-lg font-semibold text-foreground">{t('Payment Verification Details')}</h2>
              <p className="text-sm text-muted-foreground">{item.tenantName || t('Unknown Tenant')}</p>
            </div>
            <button 
              onClick={onClose}
              className="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-full transition-colors"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {isLoading ? (
            <div className="py-12 text-center text-muted-foreground animate-pulse">
              {t('Loading payment details...')}
            </div>
          ) : isError || !details ? (
            <div className="py-12 text-center text-destructive flex flex-col items-center space-y-2">
              <AlertCircle className="h-8 w-8" />
              <p>{t('Failed to load payment verification details.')}</p>
            </div>
          ) : (
            <div className="space-y-6">
              <section className="space-y-3">
                <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('Summary')}</h3>
                <div className="bg-muted/30 rounded-xl p-4 space-y-2 text-sm border border-border/50">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('Contract')}</span>
                    <span className="font-medium">{item.contractNumber || '-'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('Amount')}</span>
                    <span className="font-medium text-lg text-primary">{details.amountDue.toLocaleString()} {details.currency}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('Payment Method')}</span>
                    <span className="font-medium">{formatPaymentMethod(currentSubmission?.paymentMethod ?? item.paymentMethod)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('Reference Number')}</span>
                    <span className="font-medium">{currentSubmission?.referenceNumber || '-'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('Submitted At')}</span>
                    <span className="font-medium">{new Date(item.submittedAt).toLocaleString(language)}</span>
                  </div>
                </div>
              </section>

              {/* Cheque Details Section (Shown only for Cheque method) */}
              {(currentSubmission?.paymentMethod === 'Cheque' || (currentSubmission?.paymentMethod as any) === 2 || item.paymentMethod === 'Cheque' || (item.paymentMethod as any) === 2) && (
                <section className="space-y-3">
                  <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('تفاصيل الشيك / Cheque Details')}</h3>
                  <div className="bg-secondary/40 rounded-xl p-4 space-y-2.5 text-sm border border-border/60">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">{t('رقم الشيك / Cheque Number')}</span>
                      <span className="font-bold text-foreground font-mono">{currentSubmission?.chequeNumber || item.chequeNumber || currentSubmission?.referenceNumber || item.referenceNumber || '-'}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">{t('البنك / Bank Name')}</span>
                      <span className="font-medium text-foreground">{currentSubmission?.bankName || item.bankName || '-'}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">{t('تاريخ الإصدار / Issue Date')}</span>
                      <span className="font-medium text-foreground">{currentSubmission?.chequeIssueDate || item.chequeIssueDate || '-'}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">{t('تاريخ الاستحقاق / Due Date')}</span>
                      <span className="font-medium text-foreground">{currentSubmission?.chequeDueDate || item.chequeDueDate || '-'}</span>
                    </div>
                  </div>
                </section>
              )}

              {receipt && (
                <section className="space-y-3">
                  <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('Official Receipt (سند قبض)')}</h3>
                  <div className="bg-primary/5 border border-primary/20 rounded-xl p-4 space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center">
                        <CheckCircle2 className="h-5 w-5 text-green-600 mr-2 rtl:ml-2 rtl:mr-0 shrink-0" />
                        <div>
                          <div className="font-semibold text-foreground text-sm">{receipt.receiptNumber}</div>
                          <div className="text-xs text-muted-foreground">{t('Issued Date')}: {receipt.issueDate}</div>
                        </div>
                      </div>
                      <div className="text-right font-medium text-primary text-sm">
                        {receipt.amount.toLocaleString()} {receipt.currency}
                      </div>
                    </div>
                    {receiptPdfUrl && (
                      <a
                        href={receiptPdfUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="w-full inline-flex items-center justify-center px-4 py-2 bg-primary text-primary-foreground hover:bg-primary/90 rounded-md text-sm font-medium transition-colors shadow-xs"
                      >
                        <Download className="h-4 w-4 mr-2 rtl:ml-2 rtl:mr-0" />
                        {t('Download Official Receipt PDF (سند قبض)')}
                      </a>
                    )}
                  </div>
                </section>
              )}

              <section className="space-y-4">
                <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('Payment Proof')}</h3>
                {!targetProofId ? (
                  <div className="bg-muted/30 rounded-xl p-6 text-center text-sm text-muted-foreground border border-border/50">
                    {t('No proof file attached to this submission.')}
                  </div>
                ) : isProofLoading ? (
                  <div className="bg-muted/30 rounded-xl p-8 border border-border text-center text-sm text-muted-foreground animate-pulse flex flex-col items-center space-y-2">
                    <Clock className="h-6 w-6 animate-spin text-primary" />
                    <span>{t('Loading proof file preview...')}</span>
                  </div>
                ) : proofError || !proofFile ? (
                  <div className="bg-muted/30 rounded-xl p-6 border border-border text-center space-y-2">
                    <AlertCircle className="h-6 w-6 text-destructive mx-auto" />
                    <p className="text-sm text-muted-foreground">{proofError || t('Proof file could not be retrieved.')}</p>
                  </div>
                ) : proofFile.mimeType.startsWith('image/') ? (
                  <div className="bg-muted/30 rounded-xl p-4 border border-border flex flex-col items-center space-y-3">
                    <img 
                      src={proofFile.downloadUrl} 
                      alt={proofFile.originalFilename}
                      className="max-h-72 object-contain rounded-lg border border-border shadow-xs" 
                    />
                    <div className="flex items-center gap-2">
                      <a
                        href={proofFile.downloadUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center text-xs text-primary hover:underline font-medium"
                      >
                        <ExternalLink className="h-3.5 w-3.5 mr-1" />
                        {t('Open Full Image')}
                      </a>
                    </div>
                  </div>
                ) : (
                  <div className="bg-muted/30 rounded-xl p-6 border border-border flex flex-col items-center text-center space-y-3">
                    <FileText className="h-10 w-10 text-primary" />
                    <div>
                      <p className="text-sm font-medium text-foreground">{proofFile.originalFilename}</p>
                      <p className="text-xs text-muted-foreground">{proofFile.mimeType}</p>
                    </div>
                    <a
                      href={proofFile.downloadUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center px-3 py-1.5 bg-primary text-primary-foreground rounded-md text-xs font-medium hover:bg-primary/90 transition-colors"
                    >
                      <Download className="h-3.5 w-3.5 mr-1.5" />
                      {t('Download / View Document')}
                    </a>
                  </div>
                )}
              </section>

              <section className="space-y-4">
                <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('Submission History')}</h3>
                <div className="space-y-3">
                  {details.submissions.map((sub: PaymentSubmissionDto) => (
                    <div key={sub.id} className="p-3 border border-border rounded-lg bg-card text-sm space-y-2">
                      <div className="flex items-center justify-between">
                        <span className="font-medium">{new Date(sub.submittedAt).toLocaleString(language)}</span>
                        {sub.status === 'Pending' && <span className="text-yellow-600 bg-yellow-100 dark:bg-yellow-900/30 dark:text-yellow-400 px-2 py-0.5 rounded text-xs">{t('Pending')}</span>}
                        {sub.status === 'Approved' && <span className="text-green-600 bg-green-100 dark:bg-green-900/30 dark:text-green-400 px-2 py-0.5 rounded text-xs">{t('Approved')}</span>}
                        {sub.status === 'Rejected' && <span className="text-red-600 bg-red-100 dark:bg-red-900/30 dark:text-red-400 px-2 py-0.5 rounded text-xs">{t('Rejected')}</span>}
                      </div>
                      <div className="text-muted-foreground">
                        {t('Method')}: {formatPaymentMethod(sub.paymentMethod)} | {t('Ref')}: {sub.referenceNumber || '-'}
                      </div>
                      {sub.rejectionReason && (
                        <div className="text-destructive bg-destructive/10 p-2 rounded mt-2">
                          <span className="font-semibold">{t('Rejection Reason')}:</span> {sub.rejectionReason}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </section>
            </div>
          )}
        </div>

        <div className="p-4 md:p-6 border-t border-border bg-card">
          <div className="flex gap-3">
            <button
              onClick={() => setIsRejectModalOpen(true)}
              disabled={isLoading || approveMutation.isPending || !details}
              className="flex-1 py-2.5 rounded-md font-medium text-destructive border border-destructive hover:bg-destructive/10 transition-colors disabled:opacity-50"
            >
              {t('Reject Payment')}
            </button>
            <button
              onClick={handleApprove}
              disabled={isLoading || approveMutation.isPending || !details}
              className="flex-1 py-2.5 rounded-md font-medium bg-primary text-primary-foreground hover:bg-primary/90 transition-colors disabled:opacity-50"
            >
              {t('Approve Payment')}
            </button>
          </div>
        </div>
      </div>

      <RejectSubmissionModal
        isOpen={isRejectModalOpen}
        onClose={() => setIsRejectModalOpen(false)}
        isSubmitting={rejectMutation.isPending}
        onConfirm={(reason) => {
          if (item) {
            rejectMutation.mutate(
              { paymentId: item.rentPaymentId, submissionId: item.paymentSubmissionId, reason },
              {
                onSuccess: () => {
                  setIsRejectModalOpen(false);
                  onClose();
                }
              }
            );
          }
        }}
      />

      <ApproveSubmissionModal
        isOpen={isApproveModalOpen}
        onClose={() => setIsApproveModalOpen(false)}
        isSubmitting={approveMutation.isPending}
        onConfirm={() => {
          if (item) {
            approveMutation.mutate(
              { paymentId: item.rentPaymentId, submissionId: item.paymentSubmissionId },
              {
                onSuccess: () => {
                  setIsApproveModalOpen(false);
                  onClose();
                }
              }
            );
          }
        }}
      />
    </>
  );
}
