import { ApiError } from "@/shared/lib/http";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";

const codeKeys: Record<string, string> = {
  CURRENT_SUBSCRIPTION_ALREADY_EXISTS: "subscriptions.errors.currentSubscriptionExists",
  PENDING_PLAN_CHANGE_ALREADY_EXISTS: "subscriptions.errors.pendingRequestExists",
  PLAN_CHANGE_INVALID_LIFECYCLE_TRANSITION: "subscriptions.errors.invalidTransition",
  CONCURRENCY_CONFLICT: "subscriptions.errors.concurrency",
  PLAN_CODE_ALREADY_EXISTS: "subscriptions.planForm.codeExists",
};

export function getSubscriptionError(error: unknown, t: (key: string) => string) {
  if (error instanceof ApiError) {
    if (error.code && codeKeys[error.code]) return t(codeKeys[error.code]);
  }
  return extractUserFriendlyError(error, t("subscriptions.errors.unexpected"));
}
