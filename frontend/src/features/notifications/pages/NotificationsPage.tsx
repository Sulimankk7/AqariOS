import React, { useState } from "react";
import { toast } from "sonner";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useTranslation } from "@/shared/i18n";
import { Button } from "@/shared/ui/button";
import { Badge } from "@/shared/ui/badge";
import {
  Empty,
  Failure,
  Loading,
  Pager,
  useMvpContext,
} from "../../mvp/primitives";
import {
  useMyNotifications,
  useUnreadNotificationCount,
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
} from "../hooks/useNotifications";
import { canMarkRead, notificationTypeKeys } from "../utils/inbox";
import type { GetMyNotificationsParams } from "../types/notifications.types";
export default function NotificationsPage() {
  const { scope } = useMvpContext();
  return <Inbox key={scope.join(":")} />;
}
function Inbox() {
  const { t, formatDate } = useTranslation();
  const [cursors, setCursors] = useState<GetMyNotificationsParams[]>([{}]);
  const [expanded, setExpanded] = useState<string | null>(null);
  const query = useMyNotifications({
    ...cursors[cursors.length - 1],
    pageSize: 50,
  });
  const count = useUnreadNotificationCount();
  const mark = useMarkNotificationAsRead();
  const all = useMarkAllNotificationsAsRead();
  const data = query.data ?? [];
  const busy = query.isFetching || mark.isPending || all.isPending;
  return (
    <PageContainer
      showBreadcrumbs={false}
      title={t("mvp.notifications")}
      description={
        count.data === undefined
          ? undefined
          : t("mvp.unread", { count: count.data })
      }
      actions={
        <>
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => {
              query.refetch();
              count.refetch();
            }}
          >
            {t("mvp.refresh")}
          </Button>
          <Button
            disabled={busy || !count.data}
            onClick={() =>
              all.mutate(undefined, {
                onSuccess: (r) =>
                  toast.success(t("mvp.marked", { count: r.markedCount })),
              })
            }
          >
            {t("mvp.markAll")}
          </Button>
        </>
      }
    >
      {(mark.error || all.error || count.error) && (
        <Failure error={mark.error || all.error || count.error} />
      )}
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <Failure error={query.error} retry={() => query.refetch()} />
      ) : (
        <>
          {!data.length ? (
            <Empty />
          ) : (
            <ul className="divide-y">
              {data.map((n) => (
                <li key={n.id} className="space-y-2 py-4">
                  <button
                    className="flex min-h-12 w-full items-start justify-between gap-3 text-start focus-visible:outline-2 focus-visible:outline-primary"
                    onClick={() => setExpanded(expanded === n.id ? null : n.id)}
                    aria-expanded={expanded === n.id}
                  >
                    <span className="min-w-0 break-words font-medium">
                      {n.subject}
                    </span>
                    <Badge variant="outline">
                      {t(
                        `mvp.${n.readAt ? "read" : (["pending", "sent", "failed", "cancelled"][n.status] ?? "pending")}`,
                      )}
                    </Badge>
                  </button>
                  <p className="text-sm text-muted-foreground">
                    {t(
                      `mvp.${notificationTypeKeys[n.notificationType] ?? "general"}`,
                    )}{" "}
                    · {formatDate(n.createdAt)}
                  </p>
                  {expanded === n.id && (
                    <p className="whitespace-pre-wrap break-words">{n.body}</p>
                  )}
                  {canMarkRead(n) && (
                    <Button
                      variant="outline"
                      disabled={busy}
                      onClick={() => mark.mutate(n.id)}
                    >
                      {t("mvp.markRead")}
                    </Button>
                  )}
                </li>
              ))}
            </ul>
          )}
          <Pager
            page={cursors.length}
            busy={busy}
            hasNext={data.length === 50}
            next={() => {
              const last = data[data.length - 1];
              if (last) {
                setExpanded(null);
                setCursors((c) => [
                  ...c,
                  { lastSeenCreatedAt: last.createdAt, lastSeenId: last.id },
                ]);
              }
            }}
            previous={() => {
              setExpanded(null);
              setCursors((c) => c.slice(0, -1));
            }}
          />
        </>
      )}
    </PageContainer>
  );
}
