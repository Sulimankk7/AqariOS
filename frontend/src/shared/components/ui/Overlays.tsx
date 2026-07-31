/**
 * Overlay Components — Modal, Drawer, ConfirmDialog, and DeleteDialog.
 * Accessible dialog overlays with keyboard navigation (Escape), backdrop blur, and focus management.
 */

import React, { useEffect } from "react";
import { X, AlertTriangle, CheckCircle2 } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: React.ReactNode;
  maxWidth?: "sm" | "md" | "lg" | "xl";
}

const maxWidthMap = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
  xl: "max-w-xl",
};

export function Modal({
  isOpen,
  onClose,
  title,
  description,
  children,
  maxWidth = "md",
}: ModalProps) {
  const { t } = useTranslation();

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen) {
        onClose();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`w-full ${maxWidthMap[maxWidth]} bg-card border border-border rounded-xl shadow-2xl overflow-hidden space-y-0`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <div>
            <h3 className="text-base font-bold text-foreground">{title}</h3>
            {description && <p className="text-xs text-muted-foreground mt-0.5">{description}</p>}
          </div>
          <button
            onClick={onClose}
            aria-label={t("common.close")}
            className="p-1 rounded-md text-muted-foreground hover:bg-secondary hover:text-foreground cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
        <div className="p-6">{children}</div>
      </div>
    </div>
  );
}

export interface DrawerProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  side?: "right" | "left";
}

export function Drawer({
  isOpen,
  onClose,
  title,
  children,
  side = "right",
}: DrawerProps) {
  const { t } = useTranslation();

  if (!isOpen) return null;

  const sideClasses = side === "right" ? "end-0" : "start-0";

  return (
    <div
      className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs flex"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        className={`fixed top-0 bottom-0 ${sideClasses} w-full max-w-md bg-card border-s border-border shadow-2xl flex flex-col animate-in slide-in-from-${side} duration-200`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between p-4 border-b border-border">
          <h3 className="text-base font-bold text-foreground">{title}</h3>
          <button
            onClick={onClose}
            aria-label={t("common.close")}
            className="p-1 rounded-md text-muted-foreground hover:bg-secondary cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
        <div className="flex-1 overflow-y-auto p-4">{children}</div>
      </div>
    </div>
  );
}

export interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  description?: string;
  confirmLabel?: string;
  isLoading?: boolean;
}

export function ConfirmDialog({
  isOpen,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel,
  isLoading,
}: ConfirmDialogProps) {
  const { t } = useTranslation();

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={title || t("dialog.confirmTitle")}
      maxWidth="sm"
    >
      <div className="space-y-4">
        <div className="flex items-start gap-3">
          <div className="p-2 rounded-full bg-info-bg text-info shrink-0">
            <CheckCircle2 className="w-5 h-5" />
          </div>
          <p className="text-xs text-muted-foreground leading-relaxed">
            {description || t("dialog.confirmDescription")}
          </p>
        </div>
        <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
          <button
            onClick={onClose}
            disabled={isLoading}
            className="px-3 py-1.5 rounded-lg border border-border bg-card hover:bg-secondary text-xs font-medium text-foreground cursor-pointer"
          >
            {t("common.cancel")}
          </button>
          <button
            onClick={() => {
              onConfirm();
              onClose();
            }}
            disabled={isLoading}
            className="px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-xs font-semibold hover:opacity-90 cursor-pointer shadow-xs"
          >
            {confirmLabel || t("common.confirm")}
          </button>
        </div>
      </div>
    </Modal>
  );
}

export interface DeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  itemName?: string;
  isLoading?: boolean;
}

export function DeleteDialog({
  isOpen,
  onClose,
  onConfirm,
  title,
  itemName,
  isLoading,
}: DeleteDialogProps) {
  const { t } = useTranslation();

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={title || t("common.delete")}
      maxWidth="sm"
    >
      <div className="space-y-4">
        <div className="flex items-start gap-3">
          <div className="p-2 rounded-full bg-danger-bg text-danger shrink-0">
            <AlertTriangle className="w-5 h-5" />
          </div>
          <p className="text-xs text-muted-foreground leading-relaxed">
            {itemName
              ? `Are you sure you want to delete "${itemName}"? This action cannot be undone.`
              : t("dialog.confirmDescription")}
          </p>
        </div>
        <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
          <button
            onClick={onClose}
            disabled={isLoading}
            className="px-3 py-1.5 rounded-lg border border-border bg-card hover:bg-secondary text-xs font-medium text-foreground cursor-pointer"
          >
            {t("common.cancel")}
          </button>
          <button
            onClick={() => {
              onConfirm();
              onClose();
            }}
            disabled={isLoading}
            className="px-3 py-1.5 rounded-lg bg-danger text-danger-foreground text-xs font-semibold hover:opacity-90 cursor-pointer shadow-xs"
          >
            {t("common.delete")}
          </button>
        </div>
      </div>
    </Modal>
  );
}
