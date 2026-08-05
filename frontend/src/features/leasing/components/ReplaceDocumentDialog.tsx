import React, { useState, useRef } from 'react';
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
import { Progress } from '@/app/components/ui/progress';
import { ContractDocumentDto } from '../types/leasing.types';
import { useReplaceContractDocument } from '../hooks/useLeasing';
import { filesApi } from '@/shared/services/files.api';
import { getLeasingTranslation } from '../constants/translations';
import { contractDocumentTypeToLabel } from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';
import { UploadCloud, File, AlertCircle, RefreshCw, X } from 'lucide-react';

const MAX_FILE_SIZE_BYTES = 25 * 1024 * 1024; // 25 MB

interface ReplaceDocumentDialogProps {
  contractId: string;
  document: ContractDocumentDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export const ReplaceDocumentDialog: React.FC<ReplaceDocumentDialogProps> = ({
  contractId,
  document,
  open,
  onOpenChange,
}) => {
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);
  const replaceMutation = useReplaceContractDocument();

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [description, setDescription] = useState<string>('');

  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [uploadStatusText, setUploadStatusText] = useState('');

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setFileError(t('fileTooLarge'));
      setSelectedFile(null);
      return;
    }

    setFileError(null);
    setSelectedFile(file);
  };

  const handleRemoveFile = () => {
    setSelectedFile(null);
    setFileError(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleClose = () => {
    if (isUploading) return;
    setSelectedFile(null);
    setFileError(null);
    setDescription('');
    setIsUploading(false);
    setUploadProgress(0);
    onOpenChange(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!document || !selectedFile) return;

    try {
      setIsUploading(true);
      setUploadProgress(10);
      setUploadStatusText(`${t('uploading')}...`);

      // STEP 1: Request presigned upload URL from backend
      const initRes = await filesApi.requestUpload({
        moduleName: 'leasing',
        entityId: contractId,
        filename: selectedFile.name,
        mimeType: selectedFile.type || 'application/octet-stream',
        sizeBytes: selectedFile.size,
      });

      // STEP 2: Direct binary upload to presigned URL
      setUploadStatusText(`${t('uploading')}...`);
      await filesApi.uploadBinary(initRes.uploadUrl, selectedFile, (percent) => {
        setUploadProgress(10 + Math.round(percent * 0.7));
        setUploadStatusText(`${t('uploading')} ${percent}%`);
      });

      // STEP 3: Confirm file upload
      setUploadStatusText(`${t('uploading')}...`);
      setUploadProgress(85);
      const confirmRes = await filesApi.confirmUpload({
        fileId: initRes.fileId,
        storageKey: initRes.storageKey,
        originalFilename: selectedFile.name,
        mimeType: selectedFile.type || 'application/octet-stream',
        sizeBytes: selectedFile.size,
      });

      const newFileId = confirmRes.id || initRes.fileId;

      // STEP 4: Invoke Replace Contract Document command
      setUploadStatusText(`${t('uploading')}...`);
      setUploadProgress(95);

      await replaceMutation.mutateAsync({
        contractId,
        documentId: document.id,
        data: {
          newFileId,
          description: description.trim() || document.description || undefined,
        },
      });

      setUploadProgress(100);
      handleClose();
    } catch (err: any) {
      setFileError(extractUserFriendlyError(err, t('actionFailed')));
    } finally {
      setIsUploading(false);
    }
  };

  if (!document) return null;

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <RefreshCw className="w-5 h-5 text-primary" />
            {t('replaceDocDialogTitle')}
          </DialogTitle>
          <DialogDescription>
            {contractDocumentTypeToLabel(document.documentType, t)}
            {document.originalFilename && ` (${document.originalFilename})`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          {/* Selected File Area */}
          <div className="space-y-2">
            <Label>{t('selectNewFile')}</Label>
            {!selectedFile ? (
              <div
                tabIndex={0}
                role="button"
                aria-label={t('clickToBrowse')}
                onClick={() => fileInputRef.current?.click()}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    fileInputRef.current?.click();
                  }
                }}
                className="border-2 border-dashed rounded-lg p-6 flex flex-col items-center justify-center cursor-pointer hover:border-primary transition-colors bg-muted/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              >
                <UploadCloud className="w-8 h-8 text-muted-foreground mb-2" />
                <p className="text-sm font-medium text-center">{t('clickToBrowse')}</p>
                <p className="text-xs text-muted-foreground text-center mt-1">
                  {t('supportsText')}
                </p>
                <input
                  ref={fileInputRef}
                  type="file"
                  onChange={handleFileChange}
                  className="hidden"
                  accept=".pdf,.png,.jpg,.jpeg,.doc,.docx"
                  aria-label={t('selectNewFile')}
                />
              </div>
            ) : (
              <div className="border rounded-lg p-3 flex items-center justify-between bg-muted/10">
                <div className="flex items-center gap-3 overflow-hidden">
                  <File className="w-6 h-6 text-primary flex-shrink-0" />
                  <div className="truncate">
                    <p className="text-sm font-medium truncate">{selectedFile.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {(selectedFile.size / (1024 * 1024)).toFixed(2)} MB
                    </p>
                  </div>
                </div>
                {!isUploading && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-label="Remove selected file"
                    onClick={handleRemoveFile}
                    className="h-8 w-8 p-0"
                  >
                    <X className="w-4 h-4" />
                  </Button>
                )}
              </div>
            )}
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="replace-description">{t('description')}</Label>
            <Input
              id="replace-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={document.description || t('descPlaceholder')}
              disabled={isUploading}
            />
          </div>

          {/* Error Alert */}
          {fileError && (
            <div className="p-3 border border-destructive/30 bg-destructive/10 rounded-md flex items-center gap-2 text-sm text-destructive" role="alert">
              <AlertCircle className="w-4 h-4 flex-shrink-0" />
              <span>{fileError}</span>
            </div>
          )}

          {/* Upload Progress */}
          {isUploading && (
            <div className="space-y-2 pt-2" role="status" aria-live="polite">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>{uploadStatusText}</span>
                <span>{uploadProgress}%</span>
              </div>
              <Progress value={uploadProgress} className="h-2" />
            </div>
          )}

          <DialogFooter className="pt-4">
            <Button type="button" variant="outline" onClick={handleClose} disabled={isUploading}>
              {t('cancel')}
            </Button>
            <Button type="submit" disabled={!selectedFile || isUploading}>
              {isUploading ? (
                <>
                  <RefreshCw className="w-4 h-4 mr-2 animate-spin" />
                  {t('uploading')}
                </>
              ) : (
                t('replaceDoc')
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};
