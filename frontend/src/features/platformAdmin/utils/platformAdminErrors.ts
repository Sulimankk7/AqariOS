import { ApiError } from "@/shared/lib/http";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";

export type PlatformErrorKind = "unauthenticated" | "forbidden" | "notFound" | "conflict" | "validation" | "unexpected";

export function platformErrorKind(error: unknown): PlatformErrorKind {
  if (!(error instanceof ApiError)) return "unexpected";
  if (error.status === 401) return "unauthenticated";
  if (error.status === 403) return "forbidden";
  if (error.status === 404) return "notFound";
  if (error.status === 409) return "conflict";
  if (error.status === 422) return "validation";
  return "unexpected";
}

export function getPlatformErrorMessage(error: unknown, t: (key: string, params?: any) => string): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return t("platformAdmin.errors.unauthenticated");
    if (error.status === 403) return t("platformAdmin.errors.forbidden");
    if (error.status === 404) return t("platformAdmin.errors.notFound");
    if (error.status === 409) return t("platformAdmin.errors.conflict");
    if (error.status === 422) return t("platformAdmin.errors.validation");
  }
  return extractUserFriendlyError(error, t("platformAdmin.errors.unexpected"));
}
