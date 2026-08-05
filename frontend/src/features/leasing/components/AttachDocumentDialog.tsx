import React, { useState, useRef } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/app/components/ui/dialog';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/app/components/ui/select';
import { Progress } from '@/app/components/ui/progress';
import { ContractDocumentType } from '../types/leasing.types';
import { useAttachContractDocument } from '../hooks/useLeasing';
import { filesApi } from '@/shared/services/files.api';
import { getLeasingTranslation } from '../constants/translations';
import { contractDocumentTypeToLabel } from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';
import { UploadCloud, File, AlertCircle, RefreshCw, X } from 'lucide-react';

const MAX_FILE_SIZE_BYTES = 25 * 1024 * 1024; // 25 MB
const ALLOWED_MIME_TYPES = [
  'application/pdf',
  'image/png',
  'image/jpeg',
  'image/jpg',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];

const attachDocFormSchema = z.object({
  documentType: z.nativeEnum(ContractDocumentType),
  description: z.string().optional().nullable(),
});

type AttachDocFormValues = z.infer<typeof attachDocFormSchema>;

interface AttachDocumentDialogProps {
  contractId: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function AttachDocumentDialog({
  contractId,
  open,
  onOpenChange,
}: AttachDocumentDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);
  const attachMutation = useAttachContractDocument();

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<number | null>(null);
  const [uploadStatusText, setUploadStatusText] = useState<string>('');
  const [clientError, setClientError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<AttachDocFormValues>({
    resolver: zodResolver(attachDocFormSchema),
    defaultValues: {
      documentType: ContractDocumentType.SignedContract,
      description: '',
    },
  });

  const selectedDocumentType = watch('documentType');

  const validateAndSetFile = (file: File) => {
    setClientError(null);

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setClientError(t('fileTooLarge'));
      setSelectedFile(null);
      return;
    }

    if (file.type && !ALLOWED_MIME_TYPES.includes(file.type)) {
      setClientError(t('unsupportedFileType'));
      setSelectedFile(null);
      return;
    }

    setSelectedFile(file);
  };

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      validateAndSetFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      validateAndSetFile(e.target.files[0]);
    }
  };

  const executeFullUploadFlow = async (data: AttachDocFormValues) => {
    if (!contractId || !selectedFile) {
      setClientError(t('selectFileRequired'));
      return;
    }

    setIsUploading(true);
    setClientError(null);
    setUploadProgress(0);
    setUploadStatusText(`${t('uploading')} 0%`);

    try {
      // Helper function for 3-step file upload
      const runUploadPipeline = async (): Promise<string> => {
        const startTime = Date.now();

        // STEP 1: Request signed upload URL
        setUploadStatusText(`${t('uploading')}...`);
        const initRes = await filesApi.requestUpload({
          moduleName: 'leasing',
          entityId: contractId,
          filename: selectedFile.name,
          mimeType: selectedFile.type || 'application/octet-stream',
          sizeBytes: selectedFile.size,
        });

        // Check if expiration happened during preparation
        const expirationMs = (initRes.expirationMinutes || 15) * 60 * 1000;

        // STEP 2: Direct binary upload to presigned URL
        setUploadStatusText(`${t('uploading')} 0%`);
        if (Date.now() - startTime >= expirationMs) {
          // Auto restart from Step 1 if presigned URL expired
          return runUploadPipeline();
        }

        await filesApi.uploadBinary(initRes.uploadUrl, selectedFile, (percent) => {
          setUploadProgress(percent);
          setUploadStatusText(`${t('uploading')} ${percent}%`);
        });

        // STEP 3: Confirm upload with backend
        setUploadStatusText(`${t('uploading')}...`);
        const confirmRes = await filesApi.confirmUpload({
          fileId: initRes.fileId,
          storageKey: initRes.storageKey,
          originalFilename: selectedFile.name,
          mimeType: selectedFile.type || 'application/octet-stream',
          sizeBytes: selectedFile.size,
        });

        return confirmRes.id || initRes.fileId;
      };

      const fileId = await runUploadPipeline();

      // STEP 4: Attach confirmed document record to lease contract
      setUploadStatusText(`${t('uploading')}...`);
      await attachMutation.mutateAsync({
        id: contractId,
        data: {
          fileId,
          documentType: data.documentType,
          description: data.description || null,
        },
      });

      // Cleanup and close modal on success
      reset();
      setSelectedFile(null);
      setUploadProgress(null);
      setUploadStatusText('');
      setIsUploading(false);
      onOpenChange(false);
    } catch (err: any) {
      setIsUploading(false);
      setUploadProgress(null);
      setUploadStatusText('');
      setClientError(
        extractUserFriendlyError(err, t('actionFailed'))
      );
    }
  };

  const handleResetDialog = () => {
    setSelectedFile(null);
    setClientError(null);
    setUploadProgress(null);
    setUploadStatusText('');
    setIsUploading(false);
    reset();
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(val) => {
        if (!val) handleResetDialog();
        onOpenChange(val);
      }}
    >
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{t('attachTitle')}</DialogTitle>
          <DialogDescription>{t('attachDesc')}</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(executeFullUploadFlow)} className="space-y-4 py-2">
          {/* Document Type Selector */}
          <div className="space-y-2">
            <Label htmlFor="documentType">{t('documentType')} *</Label>
            <Select
              value={selectedDocumentType?.toString()}
              onValueChange={(val) => setValue('documentType', Number(val) as ContractDocumentType)}
              disabled={isUploading || attachMutation.isPending}
            >
              <SelectTrigger id="documentType">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ContractDocumentType.SignedContract.toString()}>
                  {contractDocumentTypeToLabel(ContractDocumentType.SignedContract, t)}
                </SelectItem>
                <SelectItem value={ContractDocumentType.NationalIdCopy.toString()}>
                  {contractDocumentTypeToLabel(ContractDocumentType.NationalIdCopy, t)}
                </SelectItem>
                <SelectItem value={ContractDocumentType.Passport.toString()}>
                  {contractDocumentTypeToLabel(ContractDocumentType.Passport, t)}
                </SelectItem>
                <SelectItem value={ContractDocumentType.IncomeProof.toString()}>
                  {contractDocumentTypeToLabel(ContractDocumentType.IncomeProof, t)}
                </SelectItem>
                <SelectItem value={ContractDocumentType.Other.toString()}>
                  {contractDocumentTypeToLabel(ContractDocumentType.Other, t)}
                </SelectItem>
              </SelectContent>
            </Select>
            {errors.documentType && (
              <p className="text-xs text-destructive">{errors.documentType.message}</p>
            )}
          </div>

          {/* Description Input */}
          <div className="space-y-2">
            <Label htmlFor="description">{t('description')}</Label>
            <Input
              id="description"
              placeholder={t('descPlaceholder')}
              disabled={isUploading || attachMutation.isPending}
              {...register('description')}
            />
          </div>

          {/* File Upload Zone */}
          <div className="space-y-2">
            <Label>{t('documentFile')} *</Label>
            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileSelect}
              accept=".pdf,.png,.jpg,.jpeg,.doc,.docx"
              className="hidden"
              aria-label={t('documentFile')}
            />

            {!selectedFile ? (
              <div
                tabIndex={0}
                role="button"
                aria-label={t('dragDropText')}
                onDragEnter={handleDrag}
                onDragLeave={handleDrag}
                onDragOver={handleDrag}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    fileInputRef.current?.click();
                  }
                }}
                className={`border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${
                  dragActive ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50'
                }`}
              >
                <UploadCloud className="w-10 h-10 mx-auto text-muted-foreground mb-2" />
                <p className="text-sm font-medium">{t('dragDropText')}</p>
                <p className="text-xs text-muted-foreground mt-1">
                  {t('supportsText')}
                </p>
              </div>
            ) : (
              <div className="border rounded-lg p-3 flex items-center justify-between bg-muted/30">
                <div className="flex items-center gap-3 overflow-hidden">
                  <File className="w-8 h-8 text-primary shrink-0" />
                  <div className="truncate text-sm">
                    <p className="font-medium truncate">{selectedFile.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {(selectedFile.size / (1024 * 1024)).toFixed(2)} MB
                    </p>
                  </div>
                </div>
                {!isUploading && !attachMutation.isPending && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    aria-label="Remove selected file"
                    onClick={() => setSelectedFile(null)}
                  >
                    <X className="w-4 h-4 text-muted-foreground hover:text-foreground" />
                  </Button>
                )}
              </div>
            )}
          </div>

          {/* Progress Indicator */}
          {isUploading && (
            <div className="space-y-2 bg-muted/40 p-3 rounded-md border" role="status" aria-live="polite">
              <div className="flex justify-between text-xs font-medium">
                <span>{uploadStatusText}</span>
                {uploadProgress !== null && <span>{uploadProgress}%</span>}
              </div>
              <Progress value={uploadProgress || 10} className="h-2" />
            </div>
          )}

          {/* Error Message */}
          {clientError && (
            <div className="flex items-start gap-2 p-3 rounded-md bg-destructive/10 text-destructive text-xs" role="alert">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <div className="flex-1">
                <p>{clientError}</p>
              </div>
            </div>
          )}

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isUploading || attachMutation.isPending}
            >
              {t('cancel')}
            </Button>
            <Button
              type="submit"
              disabled={!selectedFile || isUploading || attachMutation.isPending}
            >
              {isUploading || attachMutation.isPending ? (
                <>
                  <RefreshCw className="w-4 h-4 mr-2 animate-spin" />
                  {t('uploading')}
                </>
              ) : (
                t('attachDoc')
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
