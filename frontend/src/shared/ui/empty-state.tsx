import * as React from "react";
import { Inbox } from "lucide-react";
import { cn } from "./utils";

export interface EmptyStateProps extends React.ComponentProps<"div"> {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ComponentType<{ className?: string }>;
}

export function EmptyState({ title, description, action, icon: Icon = Inbox, className, ...props }: EmptyStateProps) {
  return (
    <div className={cn("flex min-h-48 w-full flex-col items-center justify-center gap-4 px-6 py-10 text-center", className)} {...props}>
      <div className="flex size-12 items-center justify-center rounded-full bg-surface-container-high text-on-surface-variant"><Icon className="size-6" /></div>
      <div className="max-w-md space-y-1"><h3 className="type-title-medium text-foreground">{title}</h3>{description && <p className="type-body-medium text-on-surface-variant">{description}</p>}</div>
      {action}
    </div>
  );
}
