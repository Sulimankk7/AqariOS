import { useEffect, useState } from "react";
import { X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/shared/ui/select";
import { useTranslation } from "@/shared/i18n";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useContactRequest, useUpdateContactRequestStatus } from "../hooks/useContactRequests";
import type { ContactRequestStatus } from "../types/contactRequests.types";

const statuses: ContactRequestStatus[] = ["New", "Contacted", "TrialStarted", "Converted", "Rejected"];
export function ContactRequestDrawer({ id, onClose }: { id: string | null; onClose: () => void }) {
  const { t, formatDate, formatNumber } = useTranslation(); const { user } = useAuth();
  const query = useContactRequest(id); const update = useUpdateContactRequestStatus(); const [status, setStatus] = useState<ContactRequestStatus>("New");
  useEffect(() => { if (query.data) setStatus(query.data.status); }, [query.data]);
  useEffect(() => { const close = (e: KeyboardEvent) => e.key === "Escape" && onClose(); window.addEventListener("keydown", close); return () => window.removeEventListener("keydown", close); }, [onClose]);
  if (!id) return null;
  const detail = query.data; const canManage = user?.permissions?.includes("platform.contact_requests.manage") === true;
  const save = async () => { if (!detail || status === detail.status) return; try { await update.mutateAsync({ id, status }); toast.success(t("platformAdmin.contactRequests.updated")); } catch { toast.error(t("platformAdmin.contactRequests.updateError")); } };
  return <div className="fixed inset-0 z-50 flex justify-end bg-black/40" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
    <aside className="flex h-full w-full flex-col border-s border-border bg-card shadow-2xl sm:max-w-[480px]" aria-label={t("platformAdmin.contactRequests.detailsTitle")}>
      <header className="flex h-14 items-center justify-between border-b border-border px-5"><h2 className="text-sm font-semibold">{t("platformAdmin.contactRequests.detailsTitle")}</h2><button onClick={onClose} className="rounded-md p-1.5 text-muted-foreground hover:bg-secondary" aria-label={t("common.close")}><X className="size-4" /></button></header>
      <div className="flex-1 overflow-y-auto p-5">{query.isLoading && <div className="space-y-3"><Skeleton className="h-16 w-full" /><Skeleton className="h-32 w-full" /></div>}{query.isError && <p className="text-sm text-destructive">{t("platformAdmin.contactRequests.loadError")}</p>}{detail && <div className="space-y-5">
        <div><h3 className="text-lg font-bold">{detail.name}</h3><p className="text-sm text-muted-foreground">{detail.companyName}</p></div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2"><Info label={t("platformAdmin.contactRequests.phone")} value={detail.phoneNumber} ltr /><Info label={t("platformAdmin.contactRequests.buildings")} value={formatNumber(detail.numberOfBuildings)} /><Info label={t("platformAdmin.contactRequests.createdAt")} value={formatDate(detail.createdAt, { dateStyle: "medium", timeStyle: "short" })} /><Info label={t("platformAdmin.contactRequests.statusLabel")} value={t(`platformAdmin.contactRequests.status.${detail.status}`)} /></div>
        <div className="rounded-md border border-border/60 bg-muted/20 p-3"><p className="text-xs text-muted-foreground">{t("platformAdmin.contactRequests.notes")}</p><p className="mt-1 whitespace-pre-wrap text-sm">{detail.notes || "—"}</p></div>
        {canManage && <div className="space-y-2 border-t border-border pt-4"><label className="text-xs font-semibold">{t("platformAdmin.contactRequests.changeStatus")}</label><Select value={status} onValueChange={(value) => setStatus(value as ContactRequestStatus)}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent>{statuses.map((item) => <SelectItem key={item} value={item}>{t(`platformAdmin.contactRequests.status.${item}`)}</SelectItem>)}</SelectContent></Select><Button className="w-full" disabled={status === detail.status || update.isPending} onClick={save}>{t("common.save")}</Button></div>}
      </div>}</div>
    </aside></div>;
}
function Info({ label, value, ltr }: { label: string; value: string; ltr?: boolean }) { return <div className="rounded-md border border-border/60 bg-muted/20 p-3"><p className="text-[11px] text-muted-foreground">{label}</p><bdi dir={ltr ? "ltr" : undefined} className="mt-1 block text-sm font-medium">{value}</bdi></div>; }
