import * as React from "react";
import { AlertCircle, RefreshCw } from "lucide-react";
import { cn } from "./utils";
import { Button } from "./button";

export interface ErrorStateProps extends React.ComponentProps<"div"> {
  title: string;
  description?: string;
  retryLabel?: string;
  onRetry?: () => void;
}

export function ErrorState({ title, description, retryLabel = "Retry", onRetry, className, ...props }: ErrorStateProps) {
  return (
    <div role="alert" className={cn("flex min-h-48 w-full flex-col items-center justify-center gap-4 rounded-md bg-error-container/55 px-6 py-10 text-center", className)} {...props}>
      <div className="flex size-12 items-center justify-center rounded-full bg-error-container text-destructive"><AlertCircle className="size-6" /></div>
      <div className="max-w-md space-y-1"><h3 className="type-title-medium text-destructive">{title}</h3>{description && <p className="type-body-medium text-on-surface-variant">{description}</p>}</div>
      {onRetry && <Button variant="outlined" size="sm" onClick={onRetry}><RefreshCw />{retryLabel}</Button>}
    </div>
  );
}
