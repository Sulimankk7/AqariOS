import { ApiError } from './http';

export interface ArchiveDependencyDto {
  code: string;
  label: string;
  count: number;
  examples?: string[];
}

export interface ArchiveBlockedPayload {
  code: string;
  title?: string;
  detail?: string;
  dependencies: ArchiveDependencyDto[];
}

/**
 * Checks whether an error is a 409 Conflict with an ARCHIVE_BLOCKED payload code.
 */
export function isArchiveBlockedError(error: unknown): error is ApiError {
  if (error instanceof ApiError && error.status === 409) {
    const raw = error.rawPayload as Record<string, unknown> | undefined;
    return raw?.code === 'ARCHIVE_BLOCKED';
  }
  return false;
}

/**
 * Safely extracts the structured ArchiveBlockedPayload from an ApiError if present.
 */
export function extractArchiveBlockedPayload(error: unknown): ArchiveBlockedPayload | null {
  if (isArchiveBlockedError(error)) {
    const raw = error.rawPayload as Partial<ArchiveBlockedPayload> | undefined;
    return {
      code: raw?.code || 'ARCHIVE_BLOCKED',
      title: raw?.title || error.title || 'Cannot be archived',
      detail: raw?.detail || error.detail || 'Resolve active dependencies before archiving.',
      dependencies: raw?.dependencies || [],
    };
  }
  return null;
}
