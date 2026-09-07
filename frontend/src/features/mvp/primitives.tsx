import React from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { buildingsApi } from "@/features/buildings/api/buildings.api";
import { useTranslation } from "@/shared/i18n";
import { Button } from "@/shared/ui/button";
import { Label } from "@/shared/ui/label";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/shared/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/shared/ui/dialog";
import { ErrorState } from "@/shared/ui/error-state";
import { Skeleton } from "@/shared/ui/skeleton";
import { ApiError } from "@/shared/lib/http";
import {
  extractUserFriendlyError,
  getSafeBackendDetail,
} from "@/shared/utils/errorHandling";

export function useMvpContext() {
  const { user } = useAuth();
  return {
    user,
    scope: [user?.companyId, user?.id],
    can: (permission: string) =>
      !!user &&
      (user.permissions.includes(permission) ||
        user.permissions.includes("properties.manage")),
  };
}
export function Field({
  label,
  children,
  required = false,
}: {
  label: string;
  children: React.ReactElement;
  required?: boolean;
}) {
  const id = React.useId();
  return (
    <div className="min-w-0 space-y-2">
      <div className="flex items-center gap-1">
        <Label htmlFor={id}>{label}</Label>
        {required && (
          <span aria-hidden="true" className="text-destructive">
            *
          </span>
        )}
      </div>
      {React.cloneElement(children, { id } as object)}
    </div>
  );
}
export function Choice({
  value,
  onChange,
  options,
  id,
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  id?: string;
  disabled?: boolean;
}) {
  const { direction } = useTranslation();
  return (
    <Select
      dir={direction}
      value={value || "__none"}
      onValueChange={(v) => onChange(v === "__none" ? "" : v)}
      disabled={disabled}
    >
      <SelectTrigger id={id} className="w-full min-w-0">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={o.value || "__none"} value={o.value || "__none"}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
export function Failure({
  error,
  retry,
}: {
  error: unknown;
  retry?: () => void;
}) {
  const { t } = useTranslation();
  const denied = error instanceof ApiError && [401, 403].includes(error.status);
  let message =
    error instanceof ApiError && error.code === "DOCUMENT_CATEGORY_IN_USE"
      ? t("mvp.categoryInUse")
      : extractUserFriendlyError(error, t("mvp.error"));
  if (
    error instanceof ApiError &&
    [
      "FILE_VALIDATION_FAILED",
      "DOCUMENT_INVALID_STATE",
      "DOCUMENT_CATEGORY_INVALID_STATE",
    ].includes(error.code ?? "")
  ) {
    message = getSafeBackendDetail(error) ?? t("mvp.documentValidation");
  }
  if (
    error instanceof ApiError &&
    [
      "FILE_STORAGE_KEY_INVALID",
      "FILE_UPLOAD_INCOMPLETE",
      "FILE_SIZE_MISMATCH",
    ].includes(error.code ?? "")
  ) {
    message = t("mvp.uploadFailure");
  }
  return (
    <ErrorState
      title={denied ? t("mvp.denied") : message}
      description={denied ? message : undefined}
      onRetry={denied ? undefined : retry}
    />
  );
}
export function Loading() {
  const { t } = useTranslation();
  return (
    <div role="status" aria-label={t("mvp.loading")} className="space-y-3">
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-10 w-full" />
    </div>
  );
}
export function Empty() {
  const { t } = useTranslation();
  return (
    <p role="status" className="py-12 text-center text-muted-foreground">
      {t("mvp.empty")}
    </p>
  );
}
export function BuildingChoice({
  value,
  onChange,
  emptyLabel,
}: {
  value: string;
  onChange: (v: string, name?: string) => void;
  emptyLabel?: string;
}) {
  const { scope, user, can } = useMvpContext();
  const { t } = useTranslation();
  const query = useQuery({
    queryKey: ["mvp-buildings", ...scope],
    queryFn: () => buildingsApi.getBuildings(),
    enabled: !!user?.companyId && can("properties.read"),
    retry: false,
  });
  if (!can("properties.read")) return <p role="alert">{t("mvp.denied")}</p>;
  if (query.isPending) return <Loading />;
  if (query.error)
    return <Failure error={query.error} retry={() => query.refetch()} />;
  const buildings =
    query.data?.filter((b) => b.companyId === user?.companyId && b.isActive) ??
    [];
  if (!buildings.length && emptyLabel)
    return (
      <p role="status" className="py-6 text-muted-foreground">
        {emptyLabel}
      </p>
    );
  return (
    <div className="max-w-lg">
      <Field label={t("mvp.building")}>
        <Choice
          value={value}
          onChange={(id) =>
            onChange(id, buildings.find((b) => b.id === id)?.name)
          }
          options={[
            { value: "", label: t("mvp.chooseBuilding") },
            ...buildings.map((b) => ({ value: b.id, label: b.name })),
          ]}
        />
      </Field>
    </div>
  );
}
export function Confirm({
  title,
  description,
  pending,
  error,
  onClose,
  onConfirm,
}: {
  title: string;
  description: string;
  pending: boolean;
  error?: unknown;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const { t, direction } = useTranslation();
  return (
    <Dialog open onOpenChange={(open) => !open && !pending && onClose()}>
      <DialogContent dir={direction}>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        {!!error && <Failure error={error} />}
        <DialogFooter>
          <Button variant="outline" disabled={pending} onClick={onClose}>
            {t("mvp.cancel")}
          </Button>
          <Button variant="destructive" loading={pending} onClick={onConfirm}>
            {t("mvp.confirm")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
export function Pager({
  page,
  busy,
  next,
  previous,
  hasNext,
}: {
  page: number;
  busy: boolean;
  hasNext: boolean;
  next: () => void;
  previous: () => void;
}) {
  const { t, formatNumber } = useTranslation();
  return (
    <nav
      aria-label={t("mvp.page")}
      className="flex flex-wrap items-center justify-between gap-3 border-t py-4"
    >
      <Button
        variant="outline"
        disabled={busy || page === 1}
        onClick={previous}
      >
        {t("mvp.previous")}
      </Button>
      <span>
        {t("mvp.page")} {formatNumber(page)}
      </span>
      <Button variant="outline" disabled={busy || !hasNext} onClick={next}>
        {t("mvp.next")}
      </Button>
    </nav>
  );
}
