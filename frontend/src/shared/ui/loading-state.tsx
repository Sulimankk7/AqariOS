import * as React from "react";
import { LoaderCircle } from "lucide-react";
import { cn } from "./utils";

export function LoadingState({ label = "Loading", className, children, ...props }: React.ComponentProps<"div"> & { label?: string }) {
  return (
    <div role="status" aria-live="polite" aria-label={label} className={cn("flex min-h-40 w-full flex-col items-center justify-center gap-3 p-8 text-center text-on-surface-variant", className)} {...props}>
      <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-primary" />
      <span className="type-body-medium">{children ?? label}</span>
    </div>
  );
}
