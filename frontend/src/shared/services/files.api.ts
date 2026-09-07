import { http, ApiError } from '@/shared/lib/http';
import { apiUrl } from '@/config/api';

/** Physical storage returns API-relative capabilities; cloud storage returns absolute URLs. */
export function resolveSignedFileUrl(value: string): string {
  const resolved = /^\/(?!\/)/.test(value) ? apiUrl(value) : value;
  const url = new URL(resolved, window.location.origin);
  if ((!/^https?:\/\//i.test(value) && !/^\/(?!\/)/.test(value)) ||
      !['http:', 'https:'].includes(url.protocol) || url.username || url.password) {
    throw new Error('Invalid file URL');
  }
  return url.href;
}

export interface UploadFileRequestPayload {
  moduleName: string;
  entityId: string;
  filename: string;
  mimeType: string;
  sizeBytes: number;
}

export interface UploadFileRequestResponse {
  fileId: string;
  storageKey: string;
  uploadUrl: string;
  expirationMinutes: number;
}

export interface ConfirmFileUploadPayload {
  fileId: string;
  storageKey: string;
  originalFilename: string;
  mimeType: string;
  sizeBytes: number;
}

export interface FileStorageDto {
  id: string;
  companyId: string;
  moduleName: string;
  entityId: string;
  storageKey: string;
  originalFilename: string;
  mimeType: string;
  sizeBytes: number;
  uploadedBy?: string | null;
  createdAt: string;
}

export interface FileDownloadUrlResponse {
  fileId: string;
  downloadUrl: string;
  expirationMinutes: number;
  mimeType: string;
  originalFilename: string;
}

export const filesApi = {
  /** Phase 1: Metadata upload request */
  requestUpload: (payload: UploadFileRequestPayload): Promise<UploadFileRequestResponse> => {
    return http.post<UploadFileRequestResponse>('/api/v1/files/upload-request', payload);
  },

  /** Get signed download URL by FileStorage ID */
  getFileDownloadUrl: (fileId: string, inline: boolean = true): Promise<FileDownloadUrlResponse> => {
    return http.get<FileDownloadUrlResponse>(`/api/v1/files/${fileId}/download-url?inline=${inline}`);
  },

  /** Phase 2: Binary upload to signed URL with progress tracking */
  uploadBinary: (
    uploadUrl: string,
    file: File,
    onProgress?: (progressPercent: number) => void
  ): Promise<void> => {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.open('PUT', resolveSignedFileUrl(uploadUrl), true);
      xhr.setRequestHeader('Content-Type', file.type || 'application/octet-stream');

      // Azure Blob Storage requires x-ms-blob-type: BlockBlob header for Put Blob operations
      const isAzureBlobUrl = uploadUrl.includes('.blob.core.windows.net') || uploadUrl.includes('sv=');
      if (isAzureBlobUrl) {
        xhr.setRequestHeader('x-ms-blob-type', 'BlockBlob');
      }


      if (xhr.upload && onProgress) {
        xhr.upload.onprogress = (event) => {
          if (event.lengthComputable) {
            const percent = Math.round((event.loaded / event.total) * 100);
            onProgress(percent);
          }
        };
      }

      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          resolve();
        } else if (xhr.status === 411) {
          reject(new ApiError(411, 'File upload rejected: Content-Length required.'));
        } else if (xhr.status === 413) {
          reject(new ApiError(413, 'File too large — maximum allowed size exceeded.'));
        } else {
          reject(new ApiError(xhr.status, 'File upload failed.'));
        }
      };

      xhr.onerror = () => {
        reject(new Error('Network error occurred during file upload.'));
      };

      xhr.ontimeout = () => {
        reject(new Error('Upload request timed out.'));
      };

      xhr.send(file);
    });
  },

  /** Phase 3: Confirm uploaded file */
  confirmUpload: (payload: ConfirmFileUploadPayload): Promise<FileStorageDto> => {
    return http.post<FileStorageDto>('/api/v1/files/confirm', payload);
  },
};
