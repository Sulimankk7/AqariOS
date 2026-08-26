import React from 'react';
import { FileText, Loader2, Paperclip, RefreshCw, Trash2, Upload } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/app/components/ui/button';
import { Skeleton } from '@/app/components/ui/skeleton';
import { useTranslation } from '@/shared/i18n';
import { filesApi } from '@/shared/services/files.api';
import { extractUserFriendlyError } from '@/shared/utils';
import { useMaintenanceAttachments, useAddMaintenanceAttachment, useRemoveMaintenanceAttachment } from '../hooks/useMaintenance';
import type { MaintenanceAttachmentDto } from '../types/maintenance.types';

type UploadState = { file: File; phase: 'request' | 'upload' | 'confirm' | 'attach' | 'failed'; progress: number; error?: string };

export const MaintenanceAttachmentsTab = ({ requestId }: { requestId: string }) => {
  const { t } = useTranslation();
  const { data: attachments, isLoading } = useMaintenanceAttachments(requestId);
  const addAttachment = useAddMaintenanceAttachment(requestId);
  const removeAttachment = useRemoveMaintenanceAttachment(requestId);
  const inputRef = React.useRef<HTMLInputElement>(null);
  const [upload, setUpload] = React.useState<UploadState | null>(null);

  const runUpload = async (file: File) => {
    setUpload({ file, phase: 'request', progress: 0 });
    try {
      const capability = await filesApi.requestUpload({ moduleName: 'Maintenance', entityId: requestId, filename: file.name, mimeType: file.type || 'application/octet-stream', sizeBytes: file.size });
      setUpload({ file, phase: 'upload', progress: 0 });
      await filesApi.uploadBinary(capability.uploadUrl, file, (progress) => setUpload((current) => current ? { ...current, phase: 'upload', progress } : current));
      setUpload({ file, phase: 'confirm', progress: 100 });
      const confirmed = await filesApi.confirmUpload({ fileId: capability.fileId, storageKey: capability.storageKey, originalFilename: file.name, mimeType: file.type || 'application/octet-stream', sizeBytes: file.size });
      setUpload({ file, phase: 'attach', progress: 100 });
      await addAttachment.mutateAsync({ fileId: confirmed.id, description: file.name });
      setUpload(null);
      if (inputRef.current) inputRef.current.value = '';
    } catch (error) {
      const message = extractUserFriendlyError(error, t('maintenance.uploadFailed'));
      setUpload((current) => ({ file, phase: 'failed', progress: current?.progress || 0, error: message }));
      toast.error(message);
    }
  };

  if (isLoading) return <div className="space-y-2 py-3"><Skeleton className="h-14 w-full" /><Skeleton className="h-14 w-full" /></div>;
  const busy = !!upload && upload.phase !== 'failed';

  return <div className="space-y-3 py-3">
    <input ref={inputRef} type="file" className="hidden" onChange={(event) => { const file = event.target.files?.[0]; if (file) void runUpload(file); }} disabled={busy} />
    {upload && <div className="rounded-lg border border-border px-3 py-2.5 text-xs">
      <div className="flex items-center justify-between gap-3"><div className="min-w-0 flex items-center gap-2">
        {upload.phase === 'failed' ? <FileText className="h-4 w-4 text-destructive shrink-0" /> : <Loader2 className="h-4 w-4 animate-spin text-primary shrink-0" />}
        <div className="min-w-0"><bdi dir="ltr" className="block truncate font-medium">{upload.file.name}</bdi><span className={upload.phase === 'failed' ? 'text-destructive' : 'text-muted-foreground'}>{upload.phase === 'failed' ? upload.error : t(`maintenance.uploadPhase.${upload.phase}`)}</span></div>
      </div>{upload.phase === 'failed' && <Button variant="ghost" size="sm" className="h-8 gap-1" onClick={() => void runUpload(upload.file)}><RefreshCw className="h-3.5 w-3.5" />{t('maintenance.retry')}</Button>}</div>
      {upload.phase === 'upload' && <div className="mt-2 h-1 overflow-hidden rounded-full bg-secondary"><div className="h-full bg-primary transition-all" style={{ width: `${upload.progress}%` }} /></div>}
    </div>}
    {!attachments?.length && !upload ? <button type="button" onClick={() => inputRef.current?.click()} className="w-full rounded-lg border border-dashed border-border py-6 text-sm text-muted-foreground hover:border-primary/50 hover:text-foreground"><Paperclip className="mx-auto mb-2 h-5 w-5" />{t('maintenance.noAttachments')}<span className="mt-2 block text-primary">{t('maintenance.uploadFile')}</span></button> : <>
      <div className="space-y-1">{attachments?.map((attachment) => <AttachmentRow key={attachment.id} attachment={attachment} onRemove={() => { if (window.confirm(t('maintenance.removeAttachmentConfirm'))) removeAttachment.mutate(attachment.id); }} removing={removeAttachment.isPending} />)}</div>
      <Button variant="outline" size="sm" className="h-8 gap-1.5" onClick={() => inputRef.current?.click()} disabled={busy}><Upload className="h-3.5 w-3.5" />{t('maintenance.uploadFile')}</Button>
    </>}
  </div>;
};

function AttachmentRow({ attachment, onRemove, removing }: { attachment: MaintenanceAttachmentDto; onRemove: () => void; removing: boolean }) {
  const { t, formatDate } = useTranslation();
  const [file, setFile] = React.useState<{ url: string; mimeType: string; name: string } | null>(null);
  const [loading, setLoading] = React.useState(false);
  React.useEffect(() => {
    if (!attachment.fileId) return;
    let active = true;
    filesApi.getFileDownloadUrl(attachment.fileId, true).then((result) => { if (active) setFile({ url: result.downloadUrl, mimeType: result.mimeType, name: result.originalFilename }); }).catch(() => undefined);
    return () => { active = false; };
  }, [attachment.fileId]);
  const open = async () => {
    if (!attachment.fileId) return;
    setLoading(true);
    try { const result = await filesApi.getFileDownloadUrl(attachment.fileId, true); window.open(result.downloadUrl, '_blank', 'noopener,noreferrer'); }
    catch { toast.error(t('maintenance.fileOpenFailed')); } finally { setLoading(false); }
  };
  return <div className="flex min-w-0 items-center gap-3 rounded-md px-2 py-2 hover:bg-secondary/40">
    <button type="button" onClick={() => void open()} className="h-10 w-10 shrink-0 overflow-hidden rounded-md bg-secondary flex items-center justify-center" aria-label={t('maintenance.viewAttachment')}>{file?.mimeType.startsWith('image/') ? <img src={file.url} alt="" className="h-full w-full object-cover" /> : loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileText className="h-4 w-4 text-muted-foreground" />}</button>
    <button type="button" onClick={() => void open()} className="min-w-0 flex-1 text-start text-xs"><bdi dir="ltr" className="block truncate font-medium text-foreground">{file?.name || attachment.description || t('maintenance.attachment')}</bdi><span className="text-muted-foreground">{formatDate(attachment.createdAt)}</span></button>
    <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-destructive" onClick={onRemove} disabled={removing} aria-label={t('maintenance.removeAttachment')}><Trash2 className="h-3.5 w-3.5" /></Button>
  </div>;
}
