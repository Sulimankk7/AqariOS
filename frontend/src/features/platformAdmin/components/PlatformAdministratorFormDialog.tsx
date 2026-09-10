import { FormEvent, useEffect, useState } from "react";
import { AlertCircle } from "lucide-react";
import { Button } from "@/shared/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { useTranslation } from "@/shared/i18n";
import type { CreatePlatformAdministratorRequest } from "../types/platformAdmin.types";

interface Props {
  open: boolean;
  pending: boolean;
  error: string | null;
  onClose: () => void;
  onSubmit: (request: CreatePlatformAdministratorRequest) => Promise<void>;
}

const initialForm: CreatePlatformAdministratorRequest = {
  fullName: "",
  email: "",
  password: "",
};

export function PlatformAdministratorFormDialog({ open, pending, error, onClose, onSubmit }: Props) {
  const { t } = useTranslation();
  const [form, setForm] = useState(initialForm);
  const [attempted, setAttempted] = useState(false);

  useEffect(() => {
    if (!open) {
      setForm(initialForm);
      setAttempted(false);
    }
  }, [open]);

  const fullNameValid = form.fullName.trim().length > 0 && form.fullName.trim().length <= 100;
  const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim()) && form.email.trim().length <= 255;
  const passwordValid = form.password.length >= 8;
  const valid = fullNameValid && emailValid && passwordValid;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setAttempted(true);
    if (!valid || pending) return;
    await onSubmit({
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      password: form.password,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !nextOpen && !pending && onClose()}>
      <DialogContent className="sm:max-w-md">
        <form className="space-y-4" onSubmit={submit}>
          <DialogHeader>
            <DialogTitle>{t("platformAdmin.administrators.add")}</DialogTitle>
            <DialogDescription>{t("platformAdmin.administrators.formDescription")}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <Field
              id="platform-admin-full-name"
              label={t("platformAdmin.administrators.fullName")}
              error={attempted && !fullNameValid ? t("platformAdmin.administrators.nameInvalid") : undefined}
            >
              <Input
                id="platform-admin-full-name"
                value={form.fullName}
                maxLength={100}
                autoComplete="name"
                disabled={pending}
                aria-invalid={attempted && !fullNameValid}
                onChange={(event) => setForm((current) => ({ ...current, fullName: event.target.value }))}
              />
            </Field>

            <Field
              id="platform-admin-email"
              label={t("platformAdmin.administrators.email")}
              error={attempted && !emailValid ? t("platformAdmin.administrators.emailInvalid") : undefined}
            >
              <Input
                id="platform-admin-email"
                type="email"
                dir="ltr"
                value={form.email}
                maxLength={255}
                autoComplete="email"
                disabled={pending}
                aria-invalid={attempted && !emailValid}
                onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
              />
            </Field>

            <Field
              id="platform-admin-password"
              label={t("platformAdmin.administrators.password")}
              error={attempted && !passwordValid ? t("platformAdmin.administrators.passwordInvalid") : undefined}
            >
              <Input
                id="platform-admin-password"
                type="password"
                dir="ltr"
                value={form.password}
                minLength={8}
                autoComplete="new-password"
                disabled={pending}
                aria-invalid={attempted && !passwordValid}
                onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
              />
            </Field>
          </div>

          {error && (
            <div role="alert" className="flex items-start gap-2 rounded-md bg-destructive/10 p-3 text-xs font-medium text-destructive">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" disabled={pending} onClick={onClose}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" loading={pending} loadingText={t("common.processing")}>
              {t("platformAdmin.administrators.create")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Field({ id, label, error, children }: { id: string; label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-xs font-medium text-foreground">{label}</label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
