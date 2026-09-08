import { useState } from "react";
import { RefreshCw, ShieldAlert } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Badge } from "@/shared/ui/badge";
import { Skeleton } from "@/shared/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui/table";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/shared/ui/select";
import { useTranslation } from "@/shared/i18n";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useContactRequests } from "../hooks/useContactRequests";
import { ContactRequestDrawer } from "../components/ContactRequestDrawer";
const PAGE_SIZE = 20;
export function ContactRequestsPage() {
  const { t, formatDate, formatNumber } = useTranslation(); const { user } = useAuth(); const [page, setPage] = useState(1); const [status, setStatus] = useState(""); const [selected, setSelected] = useState<string | null>(null); const query = useContactRequests(page, PAGE_SIZE, status);
  if (user?.permissions?.includes("platform.contact_requests.read") !== true) return <div className="flex flex-col items-center p-12 text-center"><ShieldAlert className="mb-3 size-10 text-muted-foreground" /><h2>{t("platformAdmin.errors.forbidden")}</h2></div>;
  const pages = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / PAGE_SIZE));
  return <section className="space-y-4"><header className="flex flex-wrap items-end justify-between gap-3 border-b border-border/40 pb-3"><div><h1 className="text-xl font-bold">{t("platformAdmin.contactRequests.title")}</h1><p className="mt-1 text-xs text-muted-foreground">{t("platformAdmin.contactRequests.subtitle")}</p></div><div className="flex gap-2"><Select value={status || "all"} onValueChange={(v) => { setStatus(v === "all" ? "" : v); setPage(1); }}><SelectTrigger className="w-40"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">{t("platformAdmin.contactRequests.allStatuses")}</SelectItem>{["New","Contacted","TrialStarted","Converted","Rejected"].map(s => <SelectItem key={s} value={s}>{t(`platformAdmin.contactRequests.status.${s}`)}</SelectItem>)}</SelectContent></Select><Button variant="outline" size="sm" onClick={() => query.refetch()} disabled={query.isFetching}><RefreshCw className={query.isFetching ? "size-4 animate-spin" : "size-4"} />{t("common.refresh")}</Button></div></header>
    {query.isLoading && <div className="space-y-2"><Skeleton className="h-12 w-full" /><Skeleton className="h-12 w-full" /></div>}
    {query.isError && <div className="rounded-lg border border-destructive/30 p-6 text-center text-sm text-destructive">{t("platformAdmin.contactRequests.loadError")}</div>}
    {query.data?.items.length === 0 && <div className="rounded-lg border border-border bg-card p-10 text-center text-sm text-muted-foreground">{t("platformAdmin.contactRequests.empty")}</div>}
    {query.data && query.data.items.length > 0 && <><div className="overflow-x-auto rounded-lg border border-border bg-card"><Table><TableHeader><TableRow><TableHead>{t("platformAdmin.contactRequests.name")}</TableHead><TableHead>{t("platformAdmin.contactRequests.company")}</TableHead><TableHead>{t("platformAdmin.contactRequests.phone")}</TableHead><TableHead>{t("platformAdmin.contactRequests.buildings")}</TableHead><TableHead>{t("platformAdmin.contactRequests.statusLabel")}</TableHead><TableHead>{t("platformAdmin.contactRequests.createdAt")}</TableHead></TableRow></TableHeader><TableBody>{query.data.items.map(item => <TableRow key={item.id} className="cursor-pointer hover:bg-muted/40" onClick={() => setSelected(item.id)}><TableCell className="font-medium">{item.name}</TableCell><TableCell>{item.companyName}</TableCell><TableCell><bdi dir="ltr">{item.phoneNumber}</bdi></TableCell><TableCell>{formatNumber(item.numberOfBuildings)}</TableCell><TableCell><Badge variant={item.status === "New" ? "tonal" : "outline"}>{t(`platformAdmin.contactRequests.status.${item.status}`)}</Badge></TableCell><TableCell className="whitespace-nowrap text-xs text-muted-foreground">{formatDate(item.createdAt, { dateStyle: "medium" })}</TableCell></TableRow>)}</TableBody></Table></div>{pages > 1 && <div className="flex items-center justify-between"><Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>{t("common.back")}</Button><bdi dir="ltr">{page} / {pages}</bdi><Button variant="outline" size="sm" disabled={page >= pages} onClick={() => setPage(p => p + 1)}>{t("common.next")}</Button></div>}</>}
    <ContactRequestDrawer id={selected} onClose={() => setSelected(null)} />
  </section>;
}
