import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/app/components/ui/dialog';
import { toast } from 'sonner';
import { extractUserFriendlyError } from '@/shared/utils';
import {
  LeaseContractDetailDto,
  ContractStatus,
  ContractDocumentDto,
} from '../types/leasing.types';
import { getLeasingTranslation } from '../constants/translations';
import {
  contractStatusToLabel,
  contractStatusToBadgeVariant,
  legalRegimeToLabel,
  paymentFrequencyToLabel,
  tenantTypeToLabel,
  contractDocumentTypeToLabel,
  terminationTypeToLabel,
} from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import {
  Edit,
  CheckCircle,
  FilePlus,
  RefreshCw,
  XCircle,
  DollarSign,
  Home,
  FileText,
  Clock,
  ShieldAlert,
  Link as LinkIcon,
} from 'lucide-react';
import { ActivateLeaseDialog } from './ActivateLeaseDialog';
import { AttachDocumentDialog } from './AttachDocumentDialog';
import { ReplaceDocumentDialog } from './ReplaceDocumentDialog';
import { TerminateLeaseDialog } from './TerminateLeaseDialog';
import { RenewLeaseDialog } from './RenewLeaseDialog';
import { leasingApi } from '../api/leasing.api';
import { useDeleteContractDocument } from '../hooks/useLeasing';
import { Eye, Download, Trash2 } from 'lucide-react';


interface LeaseContractDetailsProps {
  contract: LeaseContractDetailDto;
}

export function LeaseContractDetails({ contract }: LeaseContractDetailsProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);

  const [openActivate, setOpenActivate] = useState(false);
  const [openAttach, setOpenAttach] = useState(false);
  const [openTerminate, setOpenTerminate] = useState(false);
  const [openRenew, setOpenRenew] = useState(false);

  const [replacingDoc, setReplacingDoc] = useState<ContractDocumentDto | null>(null);
  const [deletingDoc, setDeletingDoc] = useState<ContractDocumentDto | null>(null);

  const deleteDocMutation = useDeleteContractDocument();

  const handleConfirmDeleteDoc = async () => {
    if (!deletingDoc) return;
    try {
      await deleteDocMutation.mutateAsync({
        contractId: contract.id,
        documentId: deletingDoc.id,
      });
      setDeletingDoc(null);
    } catch (err: any) {
      // Keep dialog open on failure, onError in useDeleteContractDocument displays the toast
    }
  };


  const isDraftOrPending =
    contract.status === ContractStatus.Draft ||
    contract.status === ContractStatus.PendingSignature;
  const isActive = contract.status === ContractStatus.Active;
  const isExpired = contract.status === ContractStatus.Expired;
  const isTerminated = contract.status === ContractStatus.Terminated;

  const currencySymbol = contract.currency || 'JOD';

  const handleDownloadDoc = async (docId: string, inline: boolean) => {
    try {
      const res = await leasingApi.getDownloadUrl(contract.id, docId, inline);
      if (res?.url) {
        window.open(res.url, '_blank');
      }
    } catch (err: any) {
      toast.error(extractUserFriendlyError(err, t('actionFailed')));
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Actions Card */}

      <div className="flex flex-wrap items-center justify-between gap-4 bg-card p-6 rounded-lg border shadow-sm">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">{contract.contractNumber}</h1>
            <Badge variant={contractStatusToBadgeVariant(contract.status)}>
              {contractStatusToLabel(contract.status, t)}
            </Badge>
          </div>
          <p className="text-sm text-muted-foreground mt-1">
            {t('startDate')}: {contract.startDate} — {t('endDate')}: {contract.endDate}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {isDraftOrPending && (
            <>
              <Button variant="outline" onClick={() => navigate(`/leases/${contract.id}/edit`)}>
                <Edit className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
                {t('editDraft')}
              </Button>
              <Button onClick={() => setOpenActivate(true)}>
                <CheckCircle className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
                {t('activate')}
              </Button>
            </>
          )}

          {(isActive || isExpired) && (
            <Button variant="secondary" onClick={() => setOpenRenew(true)}>
              <RefreshCw className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
              {t('renew')}
            </Button>
          )}

          {isActive && (
            <Button variant="destructive" onClick={() => setOpenTerminate(true)}>
              <XCircle className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
              {t('terminate')}
            </Button>
          )}

          {!isTerminated && (
            <Button variant="outline" onClick={() => setOpenAttach(true)}>
              <FilePlus className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
              {t('attachDoc')}
            </Button>
          )}
        </div>
      </div>

      {/* Main Details Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Financial & Terms */}
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <DollarSign className="w-5 h-5 text-primary" />
              {t('financialParameters')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('monthlyRent')}</span>
              <span className="text-base font-semibold">
                {contract.monthlyRentAmount} {currencySymbol}
              </span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('securityDeposit')}</span>
              <span className="text-base font-semibold">
                {contract.securityDepositAmount} {currencySymbol}
              </span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('paymentFrequency')}</span>
              <span className="text-sm font-medium">
                {paymentFrequencyToLabel(contract.paymentFrequency, t)}
              </span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('paymentDueDay')}</span>
              <span className="text-sm font-medium">{t('dayOfMonth').replace('{day}', contract.paymentDueDay.toString())}</span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('legalRegime')}</span>
              <span className="text-sm font-medium">
                {legalRegimeToLabel(contract.legalRegime, t)}
              </span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('tenantType')}</span>
              <span className="text-sm font-medium">
                {tenantTypeToLabel(contract.tenantType, t)}
              </span>
            </div>

            {contract.signedDate && (
              <div className="border p-3 rounded-md sm:col-span-2">
                <span className="text-xs text-muted-foreground block">{t('signedDate')}</span>
                <span className="text-sm font-medium">{contract.signedDate}</span>
              </div>
            )}

            {contract.notes && (
              <div className="border p-3 rounded-md sm:col-span-2">
                <span className="text-xs text-muted-foreground block">{t('notes')}</span>
                <span className="text-sm">{contract.notes}</span>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Related Entities Card (Surfacing priorContractId, Apartment, Tenant) */}
        <Card className="space-y-4">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Home className="w-5 h-5 text-primary" />
              {t('relatedEntities')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="border p-3 rounded-md flex items-center justify-between">
              <div>
                <span className="text-xs text-muted-foreground block">{t('apartment')}</span>
                <span className="text-sm font-medium">{t('unitReference')}</span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate(`/apartments/${contract.apartmentId}`)}
              >
                {t('viewApartment')}
              </Button>
            </div>

            <div className="border p-3 rounded-md flex items-center justify-between">
              <div>
                <span className="text-xs text-muted-foreground block">{t('tenant')}</span>
                <span className="text-sm font-medium">{t('tenantProfile')}</span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate(`/tenants`)}
              >
                {t('viewTenants')}
              </Button>
            </div>

            {/* Surface priorContractId when present */}
            {contract.priorContractId && (
              <div className="border p-3 rounded-md flex items-center justify-between bg-muted/20">
                <div>
                  <span className="text-xs text-muted-foreground block">{t('priorContract')}</span>
                  <span className="text-xs font-mono truncate max-w-[120px] block">
                    {contract.priorContractId}
                  </span>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => navigate(`/leases/${contract.priorContractId}`)}
                >
                  <LinkIcon className="w-3.5 h-3.5 mr-1 rtl:ml-1 rtl:mr-0" />
                  {t('viewPrior')}
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Termination Summary if Terminated */}
      {contract.termination && (
        <Card className="border-destructive/50 bg-destructive/5">
          <CardHeader>
            <CardTitle className="text-lg text-destructive flex items-center gap-2">
              <ShieldAlert className="w-5 h-5" />
              {t('terminationDetails')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <span className="text-xs text-muted-foreground block">{t('terminationType')}</span>
              <span className="text-sm font-medium">
                {terminationTypeToLabel(contract.termination.terminationType, t)}
              </span>
            </div>
            <div>
              <span className="text-xs text-muted-foreground block">{t('terminationDate')}</span>
              <span className="text-sm font-medium">{contract.termination.terminationDate}</span>
            </div>
            <div>
              <span className="text-xs text-muted-foreground block">{t('outstandingBalance')}</span>
              <span className="text-sm font-medium">
                {contract.termination.outstandingBalance} {contract.termination.currency || currencySymbol}
              </span>
            </div>
            <div>
              <span className="text-xs text-muted-foreground block">{t('depositReturned')}</span>
              <span className="text-sm font-medium">
                {contract.termination.depositReturnedAmount} {contract.termination.currency || currencySymbol}
              </span>
            </div>
            <div>
              <span className="text-xs text-muted-foreground block">{t('depositDeduction')}</span>
              <span className="text-sm font-medium">
                {contract.termination.depositDeductionAmount} {contract.termination.currency || currencySymbol}
              </span>
            </div>
            <div>
              <span className="text-xs text-muted-foreground block">{t('utilitySettlement')}</span>
              <Badge variant={contract.termination.finalUtilitySettlementCompleted ? 'default' : 'outline'}>
                {contract.termination.finalUtilitySettlementCompleted ? t('statusCompleted') : t('statusPending')}
              </Badge>
            </div>
            {contract.termination.reason && (
              <div className="sm:col-span-3">
                <span className="text-xs text-muted-foreground block">{t('reason')}</span>
                <span className="text-sm">{contract.termination.reason}</span>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Documents List */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <FileText className="w-5 h-5 text-primary" />
              {t('documents')}
            </CardTitle>
            <CardDescription>{t('attachedDocsCount').replace('{count}', contract.documents.length.toString())}</CardDescription>
          </div>
          {!isTerminated && (
            <Button size="sm" variant="outline" onClick={() => setOpenAttach(true)}>
              <FilePlus className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
              {t('attachDoc')}
            </Button>
          )}
        </CardHeader>
        <CardContent>
          {contract.documents.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('noDocuments')}</p>
          ) : (
            <div className="divide-y border rounded-md">
              {contract.documents.map((doc) => (
                <div key={doc.id} className="p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div className="space-y-1">
                    <div className="font-medium text-sm flex items-center gap-2">
                      <span>{contractDocumentTypeToLabel(doc.documentType, t)}</span>
                      {doc.originalFilename && (
                        <span className="text-xs text-muted-foreground font-normal">
                          ({doc.originalFilename})
                        </span>
                      )}
                      {doc.sizeBytes > 0 && (
                        <Badge variant="outline" className="text-[10px] font-normal">
                          {(doc.sizeBytes / (1024 * 1024)).toFixed(2)} MB
                        </Badge>
                      )}
                    </div>
                    {doc.description && (
                      <div className="text-xs text-muted-foreground">{doc.description}</div>
                    )}
                    <div className="text-xs text-muted-foreground">
                      {t('uploadedOn').replace('{date}', new Date(doc.createdAt).toLocaleDateString())}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-1">
                    <Button
                      size="sm"
                      variant="ghost"
                      title={t('viewDoc')}
                      aria-label={t('viewDoc')}
                      onClick={() => handleDownloadDoc(doc.id, true)}
                      className="h-8 px-2 text-xs flex items-center gap-1"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>{t('viewDoc')}</span>
                    </Button>

                    <Button
                      size="sm"
                      variant="ghost"
                      title={t('downloadDoc')}
                      aria-label={t('downloadDoc')}
                      onClick={() => handleDownloadDoc(doc.id, false)}
                      className="h-8 px-2 text-xs flex items-center gap-1"
                    >
                      <Download className="w-3.5 h-3.5" />
                      <span>{t('downloadDoc')}</span>
                    </Button>

                    {!isTerminated && (
                      <>
                        <Button
                          size="sm"
                          variant="ghost"
                          title={t('replaceDoc')}
                          aria-label={t('replaceDoc')}
                          onClick={() => setReplacingDoc(doc)}
                          className="h-8 px-2 text-xs flex items-center gap-1"
                        >
                          <RefreshCw className="w-3.5 h-3.5" />
                          <span>{t('replaceDoc')}</span>
                        </Button>

                        <Button
                          size="sm"
                          variant="ghost"
                          title={t('deleteDoc')}
                          aria-label={t('deleteDoc')}
                          onClick={() => setDeletingDoc(doc)}
                          className="h-8 px-2 text-xs text-destructive hover:text-destructive flex items-center gap-1"
                        >
                          <Trash2 className="w-3.5 h-3.5 text-destructive" />
                          <span>{t('deleteDoc')}</span>
                        </Button>
                      </>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Status History Timeline */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Clock className="w-5 h-5 text-primary" />
            {t('statusHistory')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {contract.statusHistory.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('noHistory')}</p>
          ) : (
            <div className="space-y-4">
              {contract.statusHistory.map((history) => (
                <div key={history.id} className="flex items-start gap-3 border-l-2 border-primary/30 pl-4 py-1">
                  <div>
                    <div className="flex items-center gap-2">
                      <Badge variant={contractStatusToBadgeVariant(history.toStatus)}>
                        {contractStatusToLabel(history.toStatus, t)}
                      </Badge>
                      <span className="text-xs text-muted-foreground">
                        {new Date(history.changedAt).toLocaleString()}
                      </span>
                    </div>
                    {history.reason && (
                      <p className="text-xs text-muted-foreground mt-1">{history.reason}</p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Modals */}
      <ActivateLeaseDialog
        contractId={contract.id}
        contractNumber={contract.contractNumber}
        open={openActivate}
        onOpenChange={setOpenActivate}
      />

      <AttachDocumentDialog
        contractId={contract.id}
        open={openAttach}
        onOpenChange={setOpenAttach}
      />

      <ReplaceDocumentDialog
        contractId={contract.id}
        document={replacingDoc}
        open={!!replacingDoc}
        onOpenChange={(open) => !open && setReplacingDoc(null)}
      />

      <TerminateLeaseDialog
        contractId={contract.id}
        open={openTerminate}
        onOpenChange={setOpenTerminate}
      />

      <RenewLeaseDialog
        contract={contract}
        open={openRenew}
        onOpenChange={setOpenRenew}
      />

      {/* Delete Confirmation Modal */}
      {deletingDoc && (
        <Dialog open={!!deletingDoc} onOpenChange={(open) => !open && setDeletingDoc(null)}>
          <DialogContent className="sm:max-w-[425px]">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2 text-destructive">
                <Trash2 className="w-5 h-5" />
                {t('confirmDeleteDocTitle')}
              </DialogTitle>
              <DialogDescription>{t('confirmDeleteDocDesc')}</DialogDescription>
            </DialogHeader>
            <DialogFooter className="pt-4">
              <Button variant="outline" onClick={() => setDeletingDoc(null)} disabled={deleteDocMutation.isPending}>
                {t('cancel')}
              </Button>
              <Button
                variant="destructive"
                onClick={handleConfirmDeleteDoc}
                disabled={deleteDocMutation.isPending}
                aria-busy={deleteDocMutation.isPending}
              >
                {deleteDocMutation.isPending ? t('deleting') : t('deleteDoc')}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
