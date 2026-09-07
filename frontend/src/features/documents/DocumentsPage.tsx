import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useTranslation } from "@/shared/i18n";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { ChevronRight, FileText, Plus, RefreshCw } from "lucide-react";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/shared/ui/tabs";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/shared/ui/dialog";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from "@/shared/ui/sheet";
import {
  BuildingChoice,
  Choice,
  Confirm,
  Empty,
  Failure,
  Field,
  Loading,
  Pager,
  useMvpContext,
} from "../mvp/primitives";
import { categorySchema, documentsApi, type Category } from "./documents.api";
import { DocumentEditor } from "./DocumentEditor";
export default function DocumentsPage() {
  const { scope } = useMvpContext();
  return <DocumentsWorkspace key={scope.join(":")} />;
}
function DocumentsWorkspace() {
  const { t, direction } = useTranslation();
  const [building, setBuilding] = useState("");
  const [buildingName, setBuildingName] = useState("");
  const buildingControl = (
    <BuildingChoice
      value={building}
      emptyLabel={t("mvp.noBuildings")}
      onChange={(id, name) => {
        setBuilding(id);
        setBuildingName(name ?? "");
      }}
    />
  );
  return (
    <PageContainer
      showBreadcrumbs={false}
      title={t("mvp.documents")}
      description={t("mvp.documentsNote")}
    >
      <Tabs
        defaultValue="documents"
        dir={direction}
        className="min-w-0 text-start"
      >
        <TabsList className="max-w-full">
          <TabsTrigger value="documents">{t("mvp.documents")}</TabsTrigger>
          <TabsTrigger value="categories">{t("mvp.categories")}</TabsTrigger>
        </TabsList>
        <TabsContent value="documents" className="space-y-4">
          {!building && buildingControl}
          {building && (
            <DocumentList
              key={building}
              building={building}
              buildingName={buildingName}
              buildingControl={buildingControl}
            />
          )}
        </TabsContent>
        <TabsContent value="categories">
          <Categories />
        </TabsContent>
      </Tabs>
    </PageContainer>
  );
}
function DocumentList({
  building,
  buildingName,
  buildingControl,
}: {
  building: string;
  buildingName: string;
  buildingControl: React.ReactNode;
}) {
  const { t, formatDate } = useTranslation();
  const { scope, can } = useMvpContext();
  const cache = useQueryClient();
  const [adding, setAdding] = useState(false);
  const refresh = () =>
    cache.invalidateQueries({ queryKey: ["documents", ...scope, building] });
  const [search, setSearch] = useState("");
  const [term, setTerm] = useState("");
  const [category, setCategory] = useState("");
  const [page, setPage] = useState(1);
  const [detail, setDetail] = useState<string | null>(null);
  const categories = useQuery({
    queryKey: ["document-categories", ...scope],
    queryFn: documentsApi.categories,
    retry: false,
  });
  const list = useQuery({
    queryKey: ["documents", ...scope, building, term, category, page],
    queryFn: () =>
      documentsApi.list(building, {
        searchTerm: term || undefined,
        categoryId: category || undefined,
        pageNumber: page,
        pageSize: 50,
      }),
    retry: false,
  });
  return (
    <section className="min-w-0 space-y-4">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          setTerm(search.trim());
          setPage(1);
        }}
        className="grid min-w-0 items-end gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)_auto]"
      >
        {buildingControl}
        <Field label={t("mvp.search")}>
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </Field>
        <Field label={t("mvp.categories")}>
          <Choice
            value={category}
            disabled={categories.isPending || !!categories.error}
            onChange={(v) => {
              setCategory(v);
              setPage(1);
            }}
            options={[
              { value: "", label: t("mvp.all") },
              ...(categories.data ?? []).map((c) => ({
                value: c.id,
                label: c.name,
              })),
            ]}
          />
        </Field>
        <div className="flex flex-wrap gap-2">
          <Button type="submit" variant="outline">
            {t("mvp.apply")}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => list.refetch()}
            disabled={list.isFetching}
          >
            <RefreshCw aria-hidden="true" className="size-4" />
            {t("mvp.refresh")}
          </Button>
        </div>
      </form>
      {can("documents.upload") && (
        <div className="flex justify-end border-t border-outline-variant pt-4">
          <Button onClick={() => setAdding(true)}>
            <Plus aria-hidden="true" className="size-4" />
            {t("mvp.addDocument")}
          </Button>
        </div>
      )}
      {categories.isPending && <Loading />}
      {categories.error && (
        <Failure error={categories.error} retry={() => categories.refetch()} />
      )}
      {list.isPending ? (
        <Loading />
      ) : list.error ? (
        <Failure error={list.error} retry={() => list.refetch()} />
      ) : (
        <>
          {!list.data?.items.length ? (
            <p
              role="status"
              className="py-12 text-center text-muted-foreground"
            >
              {t(
                term || category
                  ? "mvp.noMatchingDocuments"
                  : "mvp.noDocuments",
              )}
            </p>
          ) : (
            <ul className="divide-y">
              {list.data.items.map((doc) => (
                <li key={doc.id}>
                  <button
                    onClick={() => setDetail(doc.id)}
                    className="flex min-h-16 w-full items-center gap-3 rounded-sm px-2 py-4 text-start hover:bg-muted focus-visible:outline-2 focus-visible:outline-primary"
                  >
                    <FileText
                      aria-hidden="true"
                      className="size-5 shrink-0 text-on-surface-variant"
                    />
                    <div className="min-w-0 flex-1 space-y-1">
                      <div className="font-medium [overflow-wrap:anywhere]">
                        <bdi>{doc.documentName}</bdi>
                      </div>
                      <div className="text-sm text-muted-foreground [overflow-wrap:anywhere]">
                        <bdi>{doc.categoryName}</bdi> ·{" "}
                        <bdi>{formatDate(doc.createdAt)}</bdi>
                      </div>
                      <div className="flex flex-wrap gap-x-6 gap-y-1 text-sm text-muted-foreground">
                        <span>
                          {t("mvp.issueDate")}:{" "}
                          <bdi>
                            {doc.issueDate ? formatDate(doc.issueDate) : "—"}
                          </bdi>
                        </span>
                        <span>
                          {t("mvp.expiryDate")}:{" "}
                          <bdi>
                            {doc.expiryDate ? formatDate(doc.expiryDate) : "—"}
                          </bdi>
                        </span>
                      </div>
                    </div>
                    <ChevronRight
                      aria-hidden="true"
                      className="size-4 shrink-0 text-muted-foreground rtl:rotate-180"
                    />
                  </button>
                </li>
              ))}
            </ul>
          )}
          <Pager
            page={page}
            busy={list.isFetching}
            hasNext={!!list.data?.hasNextPage}
            next={() => setPage((p) => p + 1)}
            previous={() => setPage((p) => p - 1)}
          />
        </>
      )}
      {adding && (
        <DocumentEditor
          building={building}
          buildingName={buildingName}
          mode="create"
          onClose={() => setAdding(false)}
          onSaved={() => {
            setAdding(false);
            refresh();
          }}
        />
      )}
      {detail && (
        <DocumentDetail
          key={detail}
          id={detail}
          buildingName={buildingName}
          onClose={() => setDetail(null)}
          onChanged={refresh}
          onReplaced={setDetail}
        />
      )}
    </section>
  );
}
function DocumentDetail({
  id,
  buildingName,
  onClose,
  onChanged,
  onReplaced,
}: {
  id: string;
  buildingName: string;
  onClose: () => void;
  onChanged: () => void;
  onReplaced: (id: string) => void;
}) {
  const { scope, can } = useMvpContext();
  const cache = useQueryClient();
  const [editing, setEditing] = useState<"edit" | "replace" | null>(null);
  const [deleting, setDeleting] = useState(false);
  const remove = useMutation({
    mutationFn: () => documentsApi.remove(id),
    onSuccess: () => {
      cache.removeQueries({ queryKey: ["document", ...scope, id] });
      onChanged();
      onClose();
      toast.success(t("mvp.saved"));
    },
  });
  const { t, direction, formatDate, formatNumber } = useTranslation();
  const detail = useQuery({
    queryKey: ["document", ...scope, id],
    queryFn: () => documentsApi.detail(id),
    retry: false,
  });
  // Only the document-specific endpoint is permitted. Never fall back to a generic file capability.
  const download = useMutation({
    mutationFn: () => documentsApi.download(id),
    onSuccess: (url) => {
      window.open(url, "_blank", "noopener,noreferrer");
    },
  });
  const doc = detail.data;
  const facts = doc
    ? [
        ["categories", doc.categoryName],
        ["building", buildingName || doc.buildingId],
        ["issueDate", doc.issueDate ? formatDate(doc.issueDate) : "—"],
        ["expiryDate", doc.expiryDate ? formatDate(doc.expiryDate) : "—"],
        ["filename", doc.originalFilename],
        ["mime", doc.mimeType],
        ["size", formatNumber(doc.sizeBytes ?? 0)],
        ["description", doc.description],
        ["uploadedBy", doc.uploadedBy],
        ["createdAt", formatDate(doc.createdAt)],
      ]
    : [];
  return (
    <Sheet
      open
      onOpenChange={(open) =>
        !open && !remove.isPending && !editing && onClose()
      }
    >
      <SheetContent
        side={direction === "rtl" ? "left" : "right"}
        dir={direction}
        className="w-full max-w-[calc(100%-2rem)] overflow-y-auto text-start sm:max-w-md"
      >
        <SheetHeader className="pe-14">
          <SheetTitle className="[overflow-wrap:anywhere]">
            <bdi>{doc?.documentName ?? t("mvp.detail")}</bdi>
          </SheetTitle>
          <SheetDescription>{t("mvp.documents")}</SheetDescription>
        </SheetHeader>
        <div className="space-y-4 px-4 pb-6">
          {detail.isPending ? (
            <Loading />
          ) : detail.error ? (
            <Failure error={detail.error} retry={() => detail.refetch()} />
          ) : (
            <>
              <dl className="divide-y">
                {facts.map(([label, value]) => (
                  <div
                    key={label}
                    className="grid min-w-0 grid-cols-[minmax(0,1fr)_minmax(0,2fr)] items-baseline gap-3 py-3"
                  >
                    <dt className="text-sm text-muted-foreground">
                      {t(`mvp.${label}`)}
                    </dt>
                    <dd className="min-w-0 whitespace-pre-wrap text-sm [overflow-wrap:anywhere]">
                      <bdi>{value || "—"}</bdi>
                    </dd>
                  </div>
                ))}
              </dl>
              <Button
                onClick={() => download.mutate()}
                loading={download.isPending}
              >
                {t("mvp.download")}
              </Button>
              {download.error && <Failure error={download.error} />}
              {download.data && !download.error && (
                <div className="space-y-2">
                  <p className="text-sm text-muted-foreground">
                    {t("mvp.openFileNote")}
                  </p>
                  <a
                    className={buttonVariants({ variant: "outline" })}
                    href={download.data}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {t("mvp.openFile")}
                  </a>
                </div>
              )}
              {can("documents.upload") && (
                <div className="flex flex-wrap gap-2 border-t pt-4">
                  <Button variant="outline" onClick={() => setEditing("edit")}>
                    {t("mvp.editDocument")}
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => setEditing("replace")}
                  >
                    {t("mvp.replaceFile")}
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={() => {
                      remove.reset();
                      setDeleting(true);
                    }}
                  >
                    {t("mvp.deleteDocument")}
                  </Button>
                </div>
              )}
            </>
          )}
        </div>
        {editing && doc && (
          <DocumentEditor
            building={doc.buildingId}
            buildingName={buildingName}
            document={doc}
            mode={editing}
            onClose={() => setEditing(null)}
            onSaved={(updated) => {
              setEditing(null);
              onChanged();
              cache.removeQueries({ queryKey: ["document", ...scope, id] });
              onReplaced(updated.id);
              if (updated.id === id) detail.refetch();
            }}
          />
        )}
        {deleting && (
          <Confirm
            title={t("mvp.deleteDocument")}
            description={t("mvp.deleteDocumentNote")}
            pending={remove.isPending}
            error={remove.error}
            onClose={() => setDeleting(false)}
            onConfirm={() => {
              if (can("documents.upload") && !remove.isPending) remove.mutate();
            }}
          />
        )}
      </SheetContent>
    </Sheet>
  );
}
function Categories() {
  const { scope, can } = useMvpContext();
  const { t } = useTranslation();
  const cache = useQueryClient();
  const key = ["document-categories", ...scope];
  const query = useQuery({
    queryKey: key,
    queryFn: documentsApi.categories,
    retry: false,
  });
  const [editing, setEditing] = useState<Category | "new" | null>(null);
  const [deleting, setDeleting] = useState<Category | null>(null);
  const remove = useMutation({
    mutationFn: documentsApi.deleteCategory,
    onSuccess: () => {
      setDeleting(null);
      cache.invalidateQueries({ queryKey: key });
      toast.success(t("mvp.saved"));
    },
  });
  return (
    <section className="space-y-4">
      <div className="flex gap-2">
        <Button
          variant="outline"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
        >
          {t("mvp.refresh")}
        </Button>
        {can("documents.manage_categories") && (
          <Button onClick={() => setEditing("new")}>{t("mvp.create")}</Button>
        )}
      </div>
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <Failure error={query.error} retry={() => query.refetch()} />
      ) : !query.data?.length ? (
        <Empty />
      ) : (
        <ul className="divide-y">
          {query.data.map((c) => (
            <li
              key={c.id}
              className="flex flex-wrap items-center justify-between gap-3 py-4 [overflow-wrap:anywhere]"
            >
              <div className="min-w-0 flex-1 break-words">
                <p className="font-medium">{c.name}</p>
                <p className="text-muted-foreground">{c.description}</p>
              </div>
              {can("documents.manage_categories") && (
                <div className="flex gap-2">
                  <Button variant="outline" onClick={() => setEditing(c)}>
                    {t("mvp.edit")}
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={() => {
                      remove.reset();
                      setDeleting(c);
                    }}
                  >
                    {t("mvp.delete")}
                  </Button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
      {editing && (
        <CategoryForm
          category={editing === "new" ? undefined : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            cache.invalidateQueries({ queryKey: key });
          }}
        />
      )}
      {deleting && (
        <Confirm
          title={`${t("mvp.deleteConfirm")} ${deleting.name}`}
          description={t("mvp.deleteNote")}
          pending={remove.isPending}
          error={remove.error}
          onClose={() => setDeleting(null)}
          onConfirm={() =>
            can("documents.manage_categories") && remove.mutate(deleting.id)
          }
        />
      )}
    </section>
  );
}
function CategoryForm({
  category,
  onClose,
  onSaved,
}: {
  category?: Category;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t, direction } = useTranslation();
  const { can } = useMvpContext();
  const [name, setName] = useState(category?.name ?? "");
  const [description, setDescription] = useState(category?.description ?? "");
  const [invalid, setInvalid] = useState(false);
  const mutation = useMutation({
    mutationFn: () => {
      const body = categorySchema.parse({
        name,
        description: description || null,
      });
      return category
        ? documentsApi.updateCategory(category.id, body)
        : documentsApi.createCategory(body);
    },
    onSuccess: () => {
      toast.success(t("mvp.saved"));
      onSaved();
    },
  });
  return (
    <Dialog
      open
      onOpenChange={(open) => !open && !mutation.isPending && onClose()}
    >
      <DialogContent dir={direction}>
        <DialogHeader>
          <DialogTitle>{t(category ? "mvp.edit" : "mvp.create")}</DialogTitle>
          <DialogDescription>{t("mvp.categories")}</DialogDescription>
        </DialogHeader>
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault();
            if (!can("documents.manage_categories") || mutation.isPending)
              return;
            const valid = categorySchema.safeParse({
              name,
              description: description || null,
            }).success;
            setInvalid(!valid);
            if (valid) mutation.mutate();
          }}
        >
          <Field label={t("mvp.name")}>
            <Input
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              maxLength={100}
            />
          </Field>
          <Field label={t("mvp.description")}>
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={255}
            />
          </Field>
          {invalid && <p role="alert">{t("mvp.required")}</p>}
          {mutation.error && <Failure error={mutation.error} />}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={mutation.isPending}
              onClick={onClose}
            >
              {t("mvp.cancel")}
            </Button>
            <Button
              type="submit"
              loading={mutation.isPending}
              disabled={!can("documents.manage_categories")}
            >
              {t("mvp.save")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
