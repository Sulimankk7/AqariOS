import { useEffect, useState } from "react";
import { AlertCircle, CheckCircle2, XCircle } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Textarea } from "@/shared/ui/textarea";
import { useTranslation } from "@/shared/i18n";

interface BaseProps {
  open: boolean;
  name: string;
  pending: boolean;
  error?: string | null;
  onClose: () => void;
}

export function ApproveRegistrationDialog(props: BaseProps & { onConfirm: () => void }) {
  const { t } = useTranslation();

  return (
    <Dialog open={props.open} onOpenChange={(open) => !open && !props.pending && props.onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary">
              <CheckCircle2 className="h-5 w-5" />
            </div>
            <div>
              <DialogTitle className="text-base font-semibold">{t("platformAdmin.approve.title")}</DialogTitle>
            </div>
          </div>
          <DialogDescription className="pt-2 text-sm text-muted-foreground leading-relaxed">
            {t("platformAdmin.approve.description", { name: props.name })}
          </DialogDescription>
        </DialogHeader>

        {props.error && (
          <div role="alert" className="flex items-start gap-2 rounded-md bg-destructive/10 p-3 text-xs font-medium text-destructive">
            <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
            <span>{props.error}</span>
          </div>
        )}

        <DialogFooter className="gap-2 sm:gap-0 pt-2">
          <Button variant="outline" onClick={props.onClose} disabled={props.pending}>
            {t("common.cancel")}
          </Button>
          <Button onClick={props.onConfirm} disabled={props.pending}>
            {props.pending ? t("common.processing") : t("platformAdmin.actions.approve")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function RejectRegistrationDialog(props: BaseProps & { onConfirm: (reason: string) => void }) {
  const { t } = useTranslation();
  const [reason, setReason] = useState("");
  const normalized = reason.trim();
  const isValid = normalized.length >= 5;

  useEffect(() => {
    if (!props.open) setReason("");
  }, [props.open]);

  return (
    <Dialog open={props.open} onOpenChange={(open) => !open && !props.pending && props.onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-destructive/10 text-destructive">
              <XCircle className="h-5 w-5" />
            </div>
            <div>
              <DialogTitle className="text-base font-semibold">{t("platformAdmin.reject.title")}</DialogTitle>
            </div>
          </div>
          <DialogDescription className="pt-2 text-sm text-muted-foreground leading-relaxed">
            {t("platformAdmin.reject.description", { name: props.name })}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 py-1">
          <label htmlFor="rejection-reason" className="block text-xs font-medium text-foreground">
            {t("platformAdmin.reject.reason")}
          </label>
          <Textarea
            id="rejection-reason"
            value={reason}
            maxLength={500}
            rows={3}
            placeholder={t("platformAdmin.reject.reason")}
            onChange={(e) => setReason(e.target.value)}
            disabled={props.pending}
            className="resize-none text-sm"
          />
          <div className="flex justify-between text-xs text-muted-foreground">
            <span>
              {normalized.length > 0 && normalized.length < 5
                ? t("platformAdmin.reject.required")
                : ""}
            </span>
            <bdi dir="ltr">{reason.length}/500</bdi>
          </div>
        </div>

        {props.error && (
          <div role="alert" className="flex items-start gap-2 rounded-md bg-destructive/10 p-3 text-xs font-medium text-destructive">
            <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
            <span>{props.error}</span>
          </div>
        )}

        <DialogFooter className="gap-2 sm:gap-0 pt-2">
          <Button variant="outline" onClick={props.onClose} disabled={props.pending}>
            {t("common.cancel")}
          </Button>
          <Button
            variant="destructive"
            onClick={() => props.onConfirm(normalized)}
            disabled={props.pending || !isValid}
          >
            {props.pending ? t("common.processing") : t("platformAdmin.actions.reject")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
