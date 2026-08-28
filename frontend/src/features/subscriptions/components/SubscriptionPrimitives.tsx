import { X } from "lucide-react";
import { useEffect } from "react";
import { Button } from "@/shared/ui/button";
import { useTranslation } from "@/shared/i18n";

export function SubscriptionStatusBadge({ value }: { value: string }) {
  const { t } = useTranslation();
  const tone = value === "Active" || value === "Approved" ? "bg-emerald-500/10 text-emerald-700" : value === "Pending" || value === "Trialing" || value === "PastDue" ? "bg-amber-500/10 text-amber-700" : value === "Rejected" || value === "Cancelled" || value === "Expired" ? "bg-destructive/10 text-destructive" : "bg-muted text-muted-foreground";
  return <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${tone}`}>{value === "Inactive" ? t("subscriptions.inactive") : t(`subscriptions.status.${value}`)}</span>;
}

export function DetailsDrawer({ open, title, onClose, children, footer }: { open: boolean; title: string; onClose: () => void; children: React.ReactNode; footer?: React.ReactNode }) {
  const { t } = useTranslation();
  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => { if (event.key === "Escape") onClose(); };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [open, onClose]);
  if (!open) return null;
  return <div className="fixed inset-0 z-50 flex justify-end bg-black/40 backdrop-blur-xs" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
    <aside role="dialog" aria-modal="true" className="flex h-full w-full max-w-md flex-col border-s border-border bg-card shadow-2xl" aria-label={title}>
      <header className="flex h-14 items-center justify-between border-b border-border px-5"><h2 className="text-sm font-semibold">{title}</h2><Button variant="ghost" size="icon" onClick={onClose} aria-label={t("common.close")}><X className="h-4 w-4" /></Button></header>
      <div className="flex-1 overflow-y-auto p-5">{children}</div>{footer && <footer className="flex gap-2 border-t border-border p-4">{footer}</footer>}
    </aside>
  </div>;
}

export function DetailList({ items }: { items: Array<{ label: string; value?: React.ReactNode }> }) {
  return <dl className="space-y-4">{items.filter((item) => item.value !== undefined && item.value !== null && item.value !== "").map((item) => <div key={item.label}><dt className="text-xs text-muted-foreground">{item.label}</dt><dd className="mt-1 text-sm font-medium break-words">{item.value}</dd></div>)}</dl>;
}
