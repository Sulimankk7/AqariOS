"use client";

import { Toaster as Sonner, ToasterProps } from "sonner";
import { useTheme } from "@/shared/theme";

const Toaster = ({ ...props }: ToasterProps) => {
  const { resolvedTheme } = useTheme();

  return (
    <Sonner
      theme={resolvedTheme}
      className="toaster group"
      position="top-center"
      richColors
      toastOptions={{
        classNames: {
          toast: "!rounded-md !border-outline-variant !bg-surface-container-high !text-on-surface !shadow-e2",
          description: "!text-on-surface-variant",
          actionButton: "!rounded-full !bg-primary !text-primary-foreground",
          cancelButton: "!rounded-full !bg-surface-container-highest !text-on-surface",
        },
      }}
      style={
        {
          "--normal-bg": "var(--popover)",
          "--normal-text": "var(--popover-foreground)",
          "--normal-border": "var(--border)",
        } as React.CSSProperties
      }
      {...props}
    />
  );
};

export { Toaster };
