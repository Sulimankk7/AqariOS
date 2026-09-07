import React, { useId, useRef, useState } from "react";
import { FileText, Upload, X } from "lucide-react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  filesApi,
  type UploadFileRequestResponse,
} from "@/shared/services/files.api";
import { useTranslation } from "@/shared/i18n";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Textarea } from "@/shared/ui/textarea";
import { DatePicker } from "@/shared/components/ui/DatePicker";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/shared/ui/dialog";
import {
  Choice,
  Failure,
  Field,
  Loading,
  useMvpContext,
} from "../mvp/primitives";
import {
  documentsApi,
  documentMetadataSchema,
  type BuildingDocument,
} from "./documents.api";

type UploadCheckpoint = {
  file: File;
  capability?: UploadFileRequestResponse;
  uploaded?: boolean;
  fileId?: string;
};

export function DocumentEditor({
  building,
  buildingName,
  document,
  mode,
  onClose,
  onSaved,
}: {
  building: string;
  buildingName: string;
  document?: BuildingDocument;
  mode: "create" | "edit" | "replace";
  onClose: () => void;
  onSaved: (document: BuildingDocument) => void;
}) {
  const { t, direction, formatNumber } = useTranslation();
  const { can, scope } = useMvpContext();
  const [name, setName] = useState(document?.documentName ?? "");
  const [description, setDescription] = useState(document?.description ?? "");
  const [issue, setIssue] = useState<string | null>(
    document?.issueDate ?? null,
  );
  const [expiry, setExpiry] = useState<string | null>(
    document?.expiryDate ?? null,
  );
  const [category, setCategory] = useState("");
  const [file, setFile] = useState<File>();
  const [invalid, setInvalid] = useState("");
  const [stage, setStage] = useState("");
  const [progress, setProgress] = useState(0);
  const fileInput = useRef<HTMLInputElement>(null);
  const fileId = useId();
  const errorId = useId();
  // Memory-only checkpoint: retry a failed attachment request without uploading the same file again.
  const checkpoint = useRef<UploadCheckpoint>();
  const locked = useRef(false);
  const categories = useQuery({
    queryKey: ["document-categories", ...scope],
    queryFn: documentsApi.categories,
    enabled: mode === "create",
    retry: false,
  });
  const metadata = () => ({
    documentName: name,
    description: description || null,
    issueDate: issue,
    expiryDate: expiry,
    // Hidden in the MVP UI; editing must preserve the existing stored value.
    isConfidential: document?.isConfidential ?? false,
  });
  const save = useMutation({
    mutationFn: async () => {
      if (mode === "edit")
        return documentsApi.update(
          document!.id,
          documentMetadataSchema.parse(metadata()),
        );
      const selected = file!;
      if (checkpoint.current?.file !== selected)
        checkpoint.current = { file: selected };
      const upload = checkpoint.current;
      if (!upload.fileId) {
        if (!upload.capability) {
          setStage("requestUpload");
          upload.capability = await filesApi.requestUpload({
            moduleName: "BuildingDocuments",
            entityId: building,
            filename: selected.name,
            mimeType: selected.type || "application/octet-stream",
            sizeBytes: selected.size,
          });
        }
        const capability = upload.capability;
        if (!upload.uploaded) {
          setStage("uploading");
          setProgress(0);
          try {
            await filesApi.uploadBinary(
              capability.uploadUrl,
              selected,
              setProgress,
            );
            upload.uploaded = true;
          } catch (error) {
            upload.capability = undefined;
            throw error;
          }
        }
        setStage("confirmingUpload");
        const confirmed = await filesApi.confirmUpload({
          fileId: capability.fileId,
          storageKey: capability.storageKey,
          originalFilename: selected.name,
          mimeType: selected.type || "application/octet-stream",
          sizeBytes: selected.size,
        });
        upload.fileId = confirmed.id;
      }
      setStage("savingDocument");
      return mode === "replace"
        ? documentsApi.replace(document!.id, upload.fileId)
        : documentsApi.create(building, {
            ...documentMetadataSchema.parse(metadata()),
            categoryId: category,
            fileId: upload.fileId,
          });
    },
    onSuccess: (result) => {
      toast.success(t("mvp.saved"));
      onSaved(result);
    },
    onSettled: () => {
      locked.current = false;
    },
  });
  const title = t(
    `mvp.${mode === "create" ? "addDocument" : mode === "edit" ? "editDocument" : "replaceFile"}`,
  );
  const selectFile = (selected?: File) => {
    setFile(selected);
    checkpoint.current = undefined;
    save.reset();
    setInvalid("");
    if (!selected && fileInput.current) fileInput.current.value = "";
  };
  const today = new Date();
  const todayIso = [
    today.getFullYear(),
    String(today.getMonth() + 1).padStart(2, "0"),
    String(today.getDate()).padStart(2, "0"),
  ].join("-");
  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!can("documents.upload") || locked.current) return;
    if (
      mode !== "replace" &&
      (!documentMetadataSchema.safeParse(metadata()).success ||
        (issue && issue > todayIso))
    ) {
      setInvalid("documentValidation");
      return;
    }
    if (
      mode === "create" &&
      (!category || !categories.data?.some((c) => c.id === category))
    ) {
      setInvalid("chooseCategory");
      return;
    }
    if (mode !== "edit" && (!file || file.size === 0)) {
      setInvalid("fileRequired");
      return;
    }
    setInvalid("");
    setStage("savingDocument");
    locked.current = true;
    save.mutate();
  };
  return (
    <Dialog open onOpenChange={(open) => !open && !locked.current && onClose()}>
      <DialogContent dir={direction} className="sm:max-w-xl text-start">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            {mode === "replace" ? t("mvp.replaceNote") : t("mvp.documents")}
          </DialogDescription>
        </DialogHeader>
        <form
          onSubmit={submit}
          className="min-w-0 space-y-4"
          aria-describedby={invalid ? errorId : undefined}
        >
          <div>
            <p className="text-sm text-muted-foreground">{t("mvp.building")}</p>
            <p className="break-words font-medium [overflow-wrap:anywhere]">
              <bdi>{buildingName || building}</bdi>
            </p>
          </div>
          <fieldset disabled={save.isPending} className="min-w-0 space-y-4">
            {mode === "create" &&
              (categories.isPending ? (
                <Loading />
              ) : categories.error ? (
                <Failure
                  error={categories.error}
                  retry={() => categories.refetch()}
                />
              ) : !categories.data?.length ? (
                <p role="status">{t("mvp.noCategories")}</p>
              ) : (
                <Field label={t("mvp.categories")} required>
                  <Choice
                    disabled={save.isPending}
                    value={category}
                    onChange={setCategory}
                    options={[
                      { value: "", label: t("mvp.chooseCategory") },
                      ...categories.data.map((c) => ({
                        value: c.id,
                        label: c.name,
                      })),
                    ]}
                  />
                </Field>
              ))}
            {mode !== "replace" && (
              <>
                <Field label={t("mvp.search")} required>
                  <Input
                    required
                    aria-invalid={invalid === "documentValidation" || undefined}
                    aria-describedby={
                      invalid === "documentValidation" ? errorId : undefined
                    }
                    maxLength={255}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                  />
                </Field>
                <Field label={t("mvp.description")}>
                  <Textarea
                    rows={2}
                    className="min-h-20 resize-y"
                    maxLength={2000}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                  />
                </Field>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field label={t("mvp.issueDate")}>
                    <DatePicker
                      disabled={save.isPending}
                      ariaLabel={t("mvp.issueDate")}
                      value={issue}
                      onValueChange={setIssue}
                      max={todayIso}
                      ariaInvalid={invalid === "documentValidation"}
                      ariaDescribedBy={
                        invalid === "documentValidation" ? errorId : undefined
                      }
                    />
                  </Field>
                  <Field label={t("mvp.expiryDate")}>
                    <DatePicker
                      disabled={save.isPending}
                      ariaLabel={t("mvp.expiryDate")}
                      value={expiry}
                      onValueChange={setExpiry}
                      ariaInvalid={invalid === "documentValidation"}
                      ariaDescribedBy={
                        invalid === "documentValidation" ? errorId : undefined
                      }
                    />
                  </Field>
                </div>
              </>
            )}
            {mode !== "edit" && (
              <div className="min-w-0 space-y-2">
                <p className="text-sm font-medium">
                  {t("mvp.file")}{" "}
                  <span aria-hidden="true" className="text-destructive">
                    *
                  </span>
                </p>
                <input
                  ref={fileInput}
                  id={fileId}
                  type="file"
                  className="hidden"
                  aria-label={t("mvp.file")}
                  onChange={(e) => {
                    if (e.target.files?.[0]) selectFile(e.target.files[0]);
                  }}
                />
                <div className="min-w-0 rounded-lg border border-outline-variant bg-surface-container-low p-3">
                  {file ? (
                    <div className="flex min-w-0 items-start gap-3">
                      <FileText
                        aria-hidden="true"
                        className="mt-1 size-5 shrink-0 text-primary"
                      />
                      <div className="min-w-0 flex-1 space-y-1">
                        <p className="text-sm font-medium [overflow-wrap:anywhere]">
                          <bdi>{file.name}</bdi>
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {t("mvp.size")}: <bdi>{formatNumber(file.size)}</bdi>
                        </p>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => fileInput.current?.click()}
                        >
                          {t("mvp.changeFile")}
                        </Button>
                      </div>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        aria-label={t("mvp.removeFile")}
                        onClick={() => selectFile()}
                      >
                        <X aria-hidden="true" className="size-4" />
                      </Button>
                    </div>
                  ) : (
                    <Button
                      type="button"
                      variant="ghost"
                      className="h-auto min-h-16 w-full justify-start gap-3 whitespace-normal px-2 py-3 text-start"
                      onClick={() => fileInput.current?.click()}
                      aria-describedby={`${fileId}-help${invalid === "fileRequired" ? ` ${errorId}` : ""}`}
                    >
                      <Upload
                        aria-hidden="true"
                        className="size-5 shrink-0 text-primary"
                      />
                      <span className="min-w-0">
                        <span className="block">{t("mvp.chooseFile")}</span>
                        <span className="block text-xs font-normal text-muted-foreground">
                          {t("mvp.chooseFileHint")}
                        </span>
                      </span>
                    </Button>
                  )}
                </div>
                <p
                  id={`${fileId}-help`}
                  className="text-xs text-muted-foreground"
                >
                  {t("mvp.supportedDocumentFiles")}
                </p>
              </div>
            )}
          </fieldset>
          {invalid && (
            <p id={errorId} role="alert" className="text-sm text-destructive">
              {t(`mvp.${invalid}`)}
            </p>
          )}
          {save.isPending && (
            <div role="status" aria-live="polite" className="space-y-2 text-sm">
              <p>{t(`mvp.${stage}`)}</p>
              {stage === "uploading" && (
                <progress
                  className="w-full"
                  aria-label={t("mvp.uploading")}
                  max={100}
                  value={progress}
                />
              )}
            </div>
          )}
          {save.error && (
            <>
              <Failure error={save.error} />
              {stage === "uploading" && <p>{t("mvp.uploadFailure")}</p>}
            </>
          )}
          <DialogFooter className="sticky -bottom-4 border-t border-outline-variant bg-surface-container-high pt-3 pb-1 sm:-bottom-6">
            <Button
              type="button"
              variant="outline"
              disabled={save.isPending}
              onClick={onClose}
            >
              {t("mvp.cancel")}
            </Button>
            <Button
              type="submit"
              loading={save.isPending}
              disabled={
                !can("documents.upload") ||
                (mode === "create" &&
                  (categories.isPending ||
                    !!categories.error ||
                    !categories.data?.length))
              }
            >
              {t("mvp.save")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
