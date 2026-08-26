import React, { useState } from 'react';
import { useTranslation } from '@/shared/i18n';
import { Modal } from '@/shared/components/ui';

interface RejectSubmissionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => void;
  isSubmitting: boolean;
}

export function RejectSubmissionModal({ isOpen, onClose, onConfirm, isSubmitting }: RejectSubmissionModalProps) {
  const { t } = useTranslation();
  const [reason, setReason] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (reason.trim()) {
      onConfirm(reason.trim());
    }
  };

  // Reset form when opened
  React.useEffect(() => {
    if (isOpen) {
      setReason('');
    }
  }, [isOpen]);

  return (
    <Modal
      isOpen={isOpen}
      onClose={isSubmitting ? () => {} : onClose}
      title={t('payments.confirmRejectionTitle')}
      maxWidth="md"
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-4">
        <div>
          <label htmlFor="rejectionReason" className="block text-sm font-medium text-foreground mb-1">
            {t('payments.rejectionReason')} <span className="text-destructive">*</span>
          </label>
          <textarea
            id="rejectionReason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary disabled:cursor-not-allowed disabled:opacity-50"
            rows={4}
            required
            placeholder={t('payments.rejectionReasonPlaceholder')}
            disabled={isSubmitting}
          />
        </div>

        <div className="flex justify-end space-x-3 rtl:space-x-reverse pt-4 border-t border-border mt-6">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 rounded-md text-sm font-medium border border-border hover:bg-muted transition-colors disabled:opacity-50 cursor-pointer"
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            disabled={!reason.trim() || isSubmitting}
            className="px-4 py-2 rounded-md text-sm font-medium bg-destructive text-destructive-foreground hover:bg-destructive/90 transition-colors disabled:opacity-50 cursor-pointer"
          >
            {isSubmitting ? t('payments.submitting') : t('payments.confirmRejection')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
