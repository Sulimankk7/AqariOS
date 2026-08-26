import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { Clock, Droplets, Pencil, RefreshCw, Trash2, Zap } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/app/components/ui/button";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { EmptyState, ErrorState, Skeleton } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { TenantLinkUtilityAccountModal } from "../components/TenantLinkUtilityAccountModal";
import { TenantUnlinkUtilityAccountDialog } from "../components/TenantUnlinkUtilityAccountDialog";
import { TenantPaymentBadge, TenantSyncBadge } from "../components/TenantUtilityBadges";
import { useMyUtilityAccounts, useMyUtilityBills, useRequestMyUtilitySync } from "../hooks/useUtilityBills";
import type { TenantUtilityAccountDto, TenantUtilityBillDto, TenantUtilityTypeName } from "../types/utilityBills.types";
import { groupTenantUtilityBills, tenantSyncActionName, tenantSyncStatusName, tenantUtilityTypeName, utilityBillsErrorMessage } from "../utils/utilityBills";
import { useUtilityVerificationStates } from "../utils/utilityVerification";

type Filter = "All" | TenantUtilityTypeName;

export function TenantBillsPage() {
  const { t, formatCurrency, formatDate } = useTranslation();
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const selected = searchParams.get("utilityType");
  const filter: Filter = selected === "Electricity" || selected === "Water" ? selected : "All";
  const [editing, setEditing] = useState<TenantUtilityAccountDto | null>(null);
  const [linkingType, setLinkingType] = useState<TenantUtilityTypeName | null>(null);
  const [unlinking, setUnlinking] = useState<TenantUtilityAccountDto | null>(null);
  const accountsQuery = useMyUtilityAccounts();
  const billsQuery = useMyUtilityBills(filter === "All" ? undefined : filter);
  const syncMutation = useRequestMyUtilitySync();
  const verificationStates = useUtilityVerificationStates(user?.id);
  const accounts = accountsQuery.data ?? [];
  const bills = useMemo(() => billsQuery.data?.pages.flatMap((page) => page.items) ?? [], [billsQuery.data]);
  const current = accounts.filter((account) => !account.unlinkedAt);
  const syncFingerprint = current.map((account) => `${account.id}:${tenantSyncStatusName(account.syncStatus)}`).join("|");
  const previousSyncFingerprint = useRef(syncFingerprint);

  useEffect(() => {
    if (previousSyncFingerprint.current !== syncFingerprint
        && current.some((account) => tenantSyncStatusName(account.syncStatus) === "Synced")) {
      void billsQuery.refetch();
    }
    previousSyncFingerprint.current = syncFingerprint;
  }, [syncFingerprint]);

  const selectFilter = (value: Filter) => value === "All"
    ? setSearchParams({})
    : setSearchParams({ utilityType: value });

  const requestSync = async (account: TenantUtilityAccountDto) => {
    try {
      await syncMutation.mutateAsync(account.id);
      toast.success(t("tenant.utilityBills.syncRequest.success"));
    } catch (error) {
      toast.error(utilityBillsErrorMessage(error, t));
    }
  };

  return (
    <PageContainer title={t("tenant.utilityBills.page.title")} description={t("tenant.utilityBills.page.description")}>
      <div className="max-w-5xl space-y-5">
        <div className="flex flex-wrap gap-2" role="tablist">
          {(["All", "Electricity", "Water"] as Filter[]).map((value) => (
            <Button key={value} variant={filter === value ? "default" : "outline"} size="sm" onClick={() => selectFilter(value)}>
              {value === "All" ? t("tenant.utilityBills.all") : t(`tenant.utilityBills.${value.toLowerCase()}`)}
            </Button>
          ))}
        </div>

        {accountsQuery.isLoading ? <Skeleton className="h-40 rounded-xl" /> : accountsQuery.isError ? (
          <ErrorState title={t("tenant.utilityBills.errors.loadAccountsTitle")} message={utilityBillsErrorMessage(accountsQuery.error, t)} onRetry={() => accountsQuery.refetch()} />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {(["Electricity", "Water"] as TenantUtilityTypeName[]).filter((type) => filter === "All" || filter === type).map((type) => {
              const account = current.find((item) => tenantUtilityTypeName(item.utilityType) === type);
              const Icon = type === "Electricity" ? Zap : Droplets;
              const verification = account ? verificationStates.get(account.id) : undefined;
              return <UtilityAccountCard
                key={type}
                type={type}
                Icon={Icon}
                account={account}
                verificationPhase={verification?.phase}
                isSyncPending={Boolean(account && syncMutation.isPending && syncMutation.variables === account.id)}
                onLink={() => setLinkingType(type)}
                onEdit={() => account && setEditing(account)}
                onSync={() => account && requestSync(account)}
                onUnlink={() => account && setUnlinking(account)}
                formatCurrency={formatCurrency}
                formatDate={formatDate}
              />;
            })}
          </div>
        )}

        <section className="space-y-3">
          <div className="flex items-center gap-2"><Clock className="h-4 w-4 text-primary" /><h2 className="font-semibold">{t("tenant.utilityBills.history.title")}</h2></div>
          {billsQuery.isLoading ? <Skeleton className="h-48 rounded-xl" /> : billsQuery.isError ? <ErrorState title={t("tenant.utilityBills.errors.loadBillsTitle")} message={utilityBillsErrorMessage(billsQuery.error, t)} onRetry={() => billsQuery.refetch()} /> : bills.length === 0 ? <EmptyState title={t("tenant.utilityBills.states.noBills")} description={t("tenant.utilityBills.states.emptyHistory")} /> : <BillHistory accounts={accounts} bills={bills} formatCurrency={formatCurrency} formatDate={formatDate} />}
          {billsQuery.hasNextPage && <div className="text-center"><Button variant="outline" size="sm" onClick={() => billsQuery.fetchNextPage()} disabled={billsQuery.isFetchingNextPage}>{billsQuery.isFetchingNextPage ? t("tenant.utilityBills.states.loadingMore") : t("tenant.utilityBills.actions.loadMore")}</Button></div>}
        </section>
      </div>

      <TenantLinkUtilityAccountModal open={Boolean(linkingType || editing)} initialUtilityType={linkingType ?? (editing ? tenantUtilityTypeName(editing.utilityType) : "Electricity")} account={editing} onClose={() => { setLinkingType(null); setEditing(null); }} />
      <TenantUnlinkUtilityAccountDialog account={unlinking} onClose={() => setUnlinking(null)} />
    </PageContainer>
  );
}

function UtilityAccountCard({
  type,
  Icon,
  account,
  verificationPhase,
  isSyncPending,
  onLink,
  onEdit,
  onSync,
  onUnlink,
  formatCurrency,
  formatDate,
}: {
  type: TenantUtilityTypeName;
  Icon: typeof Zap;
  account?: TenantUtilityAccountDto;
  verificationPhase?: "polling" | "timedOut";
  isSyncPending: boolean;
  onLink: () => void;
  onEdit: () => void;
  onSync: () => void;
  onUnlink: () => void;
  formatCurrency: (amount: number, options?: { currency?: string }) => string;
  formatDate: (value: string) => string;
}) {
  const { t } = useTranslation();
  const status = account ? tenantSyncStatusName(account.syncStatus) : null;
  const actionName = account ? tenantSyncActionName(account.syncStatus) : "sync";
  const verificationInProgress = verificationPhase === "polling" || status === "Syncing";

  return <section className="flex min-h-72 flex-col rounded-xl border border-border bg-card p-4 shadow-2xs">
    <div className="flex items-start justify-between gap-3">
      <div className="flex items-center gap-2">
        <span className={`flex h-9 w-9 items-center justify-center rounded-xl ${type === "Electricity" ? "bg-amber-500/10 text-amber-600" : "bg-sky-500/10 text-sky-600"}`}>
          <Icon className="h-5 w-5" />
        </span>
        <h2 className="font-semibold">{t(`tenant.utilityBills.${type.toLowerCase()}`)}</h2>
      </div>
      {account && <TenantSyncBadge value={account.syncStatus} />}
    </div>

    {!account ? <div className="flex flex-1 flex-col justify-center gap-4 py-6 text-center">
      <div className="space-y-1">
        <p className="font-semibold">{t(`tenant.utilityBills.noAccount.${type}.title`)}</p>
        <p className="text-xs text-muted-foreground">{t(`tenant.utilityBills.noAccount.${type}.description`)}</p>
      </div>
      <Button size="sm" className="self-center" onClick={onLink}>{t(`tenant.utilityBills.noAccount.${type}.action`)}</Button>
    </div> : <>
      <div className="mt-5">
        <p className="text-xs text-muted-foreground">{t("tenant.utilityBills.fields.accountNumber")}</p>
        <p className="mt-1 font-mono text-base font-semibold" dir="ltr">{account.accountNumber}</p>
      </div>

      <dl className="mt-4 grid grid-cols-2 gap-4 border-y border-border py-4 text-xs">
        <div className="min-w-0">
          <dt className="text-muted-foreground">{t("tenant.utilityBills.fields.totalOutstanding")}</dt>
          <dd className="mt-1 text-lg font-bold">
            {account.totalOutstandingBalance == null
              ? "—"
              : formatCurrency(account.totalOutstandingBalance)}
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-muted-foreground">{t("tenant.utilityBills.fields.latestBillAmount")}</dt>
          {account.latestBill ? <dd className="mt-1">
            <p className="text-lg font-bold">{formatCurrency(account.latestBill.amount, { currency: account.latestBill.currency })}</p>
            <p className="mt-0.5 text-[11px] text-muted-foreground">{formatDate(account.latestBill.billDate)}</p>
          </dd> : <dd className="mt-1 text-lg font-bold">—</dd>}
        </div>
      </dl>

      {verificationPhase === "polling" ? <p className="mt-3 rounded-lg bg-info-bg/50 p-2.5 text-xs text-info" role="status">{t("tenant.utilityBills.stateMessage.verificationRequested")}</p>
        : verificationPhase === "timedOut" ? <p className="mt-3 rounded-lg bg-warning-bg/50 p-2.5 text-xs text-warning" role="status">{t("tenant.utilityBills.stateMessage.pollingTimedOut")}</p>
        : status && <SyncMessage status={status} />}

      <div className="mt-auto flex flex-wrap gap-2 border-t border-border pt-4">
        <Button variant="outline" size="sm" onClick={onEdit}><Pencil className="h-3.5 w-3.5" />{t("tenant.utilityBills.actions.edit")}</Button>
        <Button variant="outline" size="sm" onClick={onSync} disabled={isSyncPending || verificationInProgress}>
          <RefreshCw className={`h-3.5 w-3.5 ${verificationInProgress ? "animate-spin" : ""}`} />
          {t(`tenant.utilityBills.actions.${actionName}`)}
        </Button>
        <Button variant="destructive" size="sm" onClick={onUnlink} disabled={status === "Syncing"}><Trash2 className="h-3.5 w-3.5" />{t("tenant.utilityBills.actions.unlink")}</Button>
      </div>
    </>}
  </section>;
}

function SyncMessage({ status }: { status: string }) {
  const { t } = useTranslation();
  if (status === "Synced") return null;
  return <p className="mt-3 rounded-lg bg-secondary/50 p-2.5 text-xs text-muted-foreground" role="status">{t(`tenant.utilityBills.stateMessage.${status}`)}</p>;
}

function BillHistory({ accounts, bills, formatCurrency, formatDate }: { accounts: TenantUtilityAccountDto[]; bills: TenantUtilityBillDto[]; formatCurrency: (amount: number, options?: { currency?: string }) => string; formatDate: (value: string) => string }) {
  const { t } = useTranslation();
  const groups = useMemo(() => groupTenantUtilityBills(accounts, bills), [accounts, bills]);
  return <div className="space-y-4">{groups.map(({ accountId, bills: accountBills, isCurrent, accountNumber, utilityType, unlinkedAt }) => {
    return <div key={accountId} className="overflow-hidden rounded-xl border border-border bg-card">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border bg-secondary/30 px-4 py-3 text-xs">
        <span className="flex flex-wrap items-center gap-2 font-semibold">
          {utilityType && <span className="rounded-full border border-border bg-card px-2 py-0.5">{t(`tenant.utilityBills.${utilityType.toLowerCase()}`)}</span>}
          <span>{isCurrent ? t("tenant.utilityBills.history.currentAccount") : t("tenant.utilityBills.history.previousAccount")}: <span className="font-mono" dir="ltr">{accountNumber}</span></span>
        </span>
        {!isCurrent && <span className="text-muted-foreground">{t("tenant.utilityBills.history.unlinkedAt")}: {unlinkedAt ? formatDate(unlinkedAt) : "—"}</span>}
      </div>
      <div className="divide-y divide-border">{accountBills.map((bill) => <article key={bill.id} className="grid grid-cols-2 gap-3 p-4 text-xs sm:grid-cols-4 sm:items-center">
        <div><p className="text-muted-foreground">{t("tenant.utilityBills.billDate")}</p><p className="font-medium">{formatDate(bill.billDate)}</p></div>
        <div><p className="text-muted-foreground">{t("tenant.utilityBills.dueDate")}</p><p>{bill.dueDate ? formatDate(bill.dueDate) : "—"}</p></div>
        <div><p className="text-muted-foreground">{t("tenant.utilityBills.amount")}</p><p className="font-semibold">{formatCurrency(bill.amount, { currency: bill.currency })}</p></div>
        <div className="sm:text-end"><TenantPaymentBadge value={bill.paymentStatus} /></div>
      </article>)}</div>
    </div>;
  })}</div>;
}
