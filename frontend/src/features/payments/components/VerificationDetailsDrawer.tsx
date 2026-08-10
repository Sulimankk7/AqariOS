import React, { useState } from 'react';
import { useTranslation } from '@/shared/i18n';
import { PaymentVerificationQueueItem, PaymentSubmissionDto } from '../types/payments.types';
import { usePaymentDetails, useApproveSubmission, useRejectSubmission } from '../hooks/usePaymentVerifications';
import { RejectSubmissionModal } from './RejectSubmissionModal';
import { ApproveSubmissionModal } from './ApproveSubmissionModal';
import { X, CheckCircle2, XCircle, AlertCircle, Clock } from 'lucide-react';

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

  // We are not sliding in this example, just a simple overlay panel for simplicity, 
  // or you could use a shared Drawer component if one existed in Overlays.
  // We'll build a custom drawer overlay here.
  
  if (!item) return null;

  const currentSubmission = details?.submissions.find(s => s.id === item.paymentSubmissionId);

  const handleApprove = () => {
    setIsApproveModalOpen(true);
  };

  return (
    <>
      <div 
        className="fixed inset-0 bg-black/60 backdrop-blur-xs z-40 transition-opacity"
        onClick={onClose}
      />
      
      <div className={`fixed inset-y-0 ${isRtl ? 'left-0' : 'right-0'} w-full md:w-[500px] bg-background shadow-2xl z-50 flex flex-col animate-in ${isRtl ? 'slide-in-from-left' : 'slide-in-from-right'} duration-200`}>
        <div className="flex items-center justify-between p-4 md:p-6 border-b border-border bg-card">
          <h2 className="text-xl font-semibold text-foreground">{t('Payment Verification')}</h2>
          <button 
            onClick={onClose}
            className="p-2 rounded-full hover:bg-muted text-muted-foreground transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 md:p-6 space-y-8">
          {isLoading ? (
            <div className="flex justify-center py-12">
              <span className="text-muted-foreground animate-pulse">{t('Loading details...')}</span>
            </div>
          ) : isError || !details ? (
            <div className="flex flex-col items-center justify-center py-12 text-destructive space-y-4">
              <AlertCircle className="h-8 w-8" />
              <p>{t('Failed to load payment details.')}</p>
              <button 
                onClick={() => window.location.reload()}
                className="px-4 py-2 bg-destructive/10 text-destructive rounded hover:bg-destructive/20 transition-colors text-sm font-medium"
              >
                {t('Retry')}
              </button>
            </div>
          ) : (
            <>
              <section className="space-y-4">
                <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('Payment Information')}</h3>
                <div className="bg-muted/30 rounded-xl p-4 space-y-3 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('Tenant')}</span>
                    <span className="font-medium">{item.tenantName}</span>
                  </div>
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
                    <span className="font-medium">{t(currentSubmission?.paymentMethod || item.paymentMethod)}</span>
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

              <section className="space-y-4">
                <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{t('Payment Proof')}</h3>
                <div className="bg-muted/30 rounded-xl p-8 border border-dashed border-border flex flex-col items-center justify-center text-center space-y-2">
                  <AlertCircle className="h-8 w-8 text-muted-foreground/50" />
                  <p className="text-sm text-muted-foreground max-w-[250px]">
                    {t('Proof file preview requires a backend download URL endpoint (capability missing).')}
                  </p>
                  <span className="text-xs text-muted-foreground font-mono bg-muted px-2 py-1 rounded">
                    ID: {currentSubmission?.proofFileId || item.proofFileId || 'None'}
                  </span>
                </div>
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
                        {t('Method')}: {t(sub.paymentMethod)} | {t('Ref')}: {sub.referenceNumber || '-'}
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
            </>
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
