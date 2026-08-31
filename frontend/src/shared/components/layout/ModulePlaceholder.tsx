import React from "react";
import { LucideIcon } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

interface ModulePlaceholderProps {
  title: string;
  description: string;
  icon: LucideIcon;
}

export function ModulePlaceholder({ title, description, icon: Icon }: ModulePlaceholderProps) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col items-center justify-center h-full min-h-[400px] text-center p-8 bg-card rounded-xl border border-border shadow-sm">
      <div className="w-16 h-16 rounded-2xl bg-secondary flex items-center justify-center mb-6">
        <Icon className="w-8 h-8 text-muted-foreground" />
      </div>
      <h2 className="text-2xl font-bold tracking-tight mb-2 text-foreground">{title}</h2>
      <p className="text-muted-foreground max-w-md">{description}</p>
      
      <div className="mt-8 px-4 py-2 bg-secondary rounded-md border border-border">
        <p className="text-xs font-medium text-muted-foreground">{t("common.moduleInDevelopment")}</p>
      </div>
    </div>
  );
}
