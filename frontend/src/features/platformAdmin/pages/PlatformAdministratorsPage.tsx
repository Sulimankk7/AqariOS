import { useState } from "react";
import { Plus, RefreshCw, ShieldCheck } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui/table";
import { ErrorState } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { ApiError } from "@/shared/lib/http";
import { PlatformAdministratorFormDialog } from "../components/PlatformAdministratorFormDialog";
import { useCreatePlatformAdministrator, usePlatformAdministrators } from "../hooks/usePlatformAdministrators";
import type { CreatePlatformAdministratorRequest } from "../types/platformAdmin.types";
import { getPlatformErrorMessage } from "../utils/platformAdminErrors";

export function PlatformAdministratorsPage() {
  const { t, formatDate } = useTranslation();
  const [formOpen, setFormOpen] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const administrators = usePlatformAdministrators();
  const createAdministrator = useCreatePlatformAdministrator();

  const create = async (request: CreatePlatformAdministratorRequest) => {
    setFormError(null);
    try {
      await createAdministrator.mutateAsync(request);
      await administrators.refetch();
      setFormOpen(false);
      toast.success(t("platformAdmin.administrators.success"));
    } catch (error) {
      if (error instanceof ApiError && error.status === 409 && error.code === "EMAIL_ALREADY_EXISTS") {
        setFormError(t("platformAdmin.administrators.duplicateEmail"));
      } else {
        setFormError(getPlatformErrorMessage(error, t));
      }
    }
  };

  return (
    <section className="space-y-4">
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border/40 pb-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">{t("platformAdmin.administrators.title")}</h1>
          <p className="mt-0.5 text-xs text-muted-foreground">{t("platformAdmin.administrators.subtitle")}</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => administrators.refetch()} disabled={administrators.isFetching}>
            <RefreshCw className={administrators.isFetching ? "animate-spin" : ""} />
            {t("common.refresh")}
          </Button>
          <Button size="sm" onClick={() => { setFormError(null); setFormOpen(true); }}>
            <Plus />
            {t("platformAdmin.administrators.add")}
          </Button>
        </div>
      </header>

      {administrators.isLoading && (
        <div className="space-y-2 rounded-lg border border-border bg-card p-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      )}

      {administrators.isError && (
        <ErrorState title={getPlatformErrorMessage(administrators.error, t)} onRetry={() => administrators.refetch()} />
      )}

      {administrators.data && administrators.data.length === 0 && (
        <div className="rounded-lg border border-border/60 bg-card/40 p-10 text-center">
          <ShieldCheck className="mx-auto mb-3 h-9 w-9 text-muted-foreground" />
          <p className="text-sm font-medium text-muted-foreground">{t("platformAdmin.administrators.empty")}</p>
        </div>
      )}

      {administrators.data && administrators.data.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border bg-card shadow-2xs">
          <Table>
            <TableHeader>
              <TableRow className="border-b border-border bg-muted/20 hover:bg-muted/20">
                <TableHead>{t("platformAdmin.administrators.fullName")}</TableHead>
                <TableHead>{t("platformAdmin.administrators.email")}</TableHead>
                <TableHead>{t("platformAdmin.administrators.status")}</TableHead>
                <TableHead>{t("platformAdmin.administrators.createdAt")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {administrators.data.map((administrator) => (
                <TableRow key={administrator.id}>
                  <TableCell className="font-medium">{administrator.fullName}</TableCell>
                  <TableCell><bdi dir="ltr">{administrator.email ?? "—"}</bdi></TableCell>
                  <TableCell>
                    <span className={`inline-flex rounded-full border px-2 py-0.5 text-[11px] font-medium ${administrator.isActive ? "border-emerald-500/20 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400" : "border-border bg-muted text-muted-foreground"}`}>
                      {administrator.isActive ? t("platformAdmin.administrators.active") : t("platformAdmin.administrators.inactive")}
                    </span>
                  </TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                    {formatDate(administrator.createdAt, { dateStyle: "medium" })}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <PlatformAdministratorFormDialog
        open={formOpen}
        pending={createAdministrator.isPending}
        error={formError}
        onClose={() => { setFormOpen(false); setFormError(null); }}
        onSubmit={create}
      />
    </section>
  );
}
