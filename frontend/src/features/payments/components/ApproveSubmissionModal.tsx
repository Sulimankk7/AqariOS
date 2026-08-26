import React from 'react';
import { useTranslation } from '@/shared/i18n';
import { Modal } from '@/shared/components/ui';

interface ApproveSubmissionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  isSubmitting: boolean;
}

export function ApproveSubmissionModal({ isOpen, onClose, onConfirm, isSubmitting }: ApproveSubmissionModalProps) {
  const { t } = useTranslation();

  return (
    <Modal
      isOpen={isOpen}
      onClose={isSubmitting ? () => {} : onClose}
      title={t('payments.confirmApprovalTitle')}
      maxWidth="md"
    >
      <div className="p-6 space-y-4">
        <p className="text-sm text-foreground">
          {t('payments.confirmApprovalDesc')}
        </p>
        <p className="text-sm font-medium text-destructive">
          {t('payments.actionIrreversible')}
        </p>

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
            type="button"
            onClick={onConfirm}
            disabled={isSubmitting}
            className="px-4 py-2 rounded-md text-sm font-medium bg-primary text-primary-foreground hover:bg-primary/90 transition-colors disabled:opacity-50 cursor-pointer"
          >
            {isSubmitting ? t('payments.processing') : t('payments.approvePayment')}
          </button>
        </div>
      </div>
    </Modal>
  );
}
