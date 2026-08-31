import { CalendarIcon } from "lucide-react";
import { ar, enUS } from "date-fns/locale";
import { useState } from "react";
import { useTranslation } from "@/shared/i18n";
import { Button } from "@/shared/ui/button";
import { Calendar } from "@/shared/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/shared/ui/popover";
import { cn } from "@/shared/ui/utils";

interface DatePickerProps {
  value?: string | null;
  onValueChange: (value: string | null) => void;
  min?: string;
  max?: string;
  disabled?: boolean;
  id?: string;
  name?: string;
  className?: string;
  ariaLabel?: string;
  ariaInvalid?: boolean;
  ariaDescribedBy?: string;
  required?: boolean;
}

const parseIsoDate = (value?: string | null) => {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return undefined;
  const [year, month, day] = value.split("-").map(Number);
  const parsed = new Date(year, month - 1, day);
  return Number.isNaN(parsed.getTime()) ? undefined : parsed;
};

const toIsoDate = (date: Date) => [date.getFullYear(), String(date.getMonth() + 1).padStart(2, "0"), String(date.getDate()).padStart(2, "0")].join("-");

export function DatePicker({ value, onValueChange, min, max, disabled, id, name, className, ariaLabel, ariaInvalid, ariaDescribedBy, required }: DatePickerProps) {
  const { language, formatDate, t } = useTranslation();
  const [open, setOpen] = useState(false);
  const selected = parseIsoDate(value);
  const minimum = parseIsoDate(min);
  const maximum = parseIsoDate(max);
  const disabledDays = [minimum ? { before: minimum } : undefined, maximum ? { after: maximum } : undefined].filter(
    (matcher): matcher is { before: Date } | { after: Date } => Boolean(matcher),
  );

  const selectDate = (date?: Date) => {
    if (!date) return;
    if ((minimum && date < minimum) || (maximum && date > maximum)) return;
    onValueChange(toIsoDate(date));
    setOpen(false);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      {name && <input type="hidden" name={name} value={value ?? ""} />}
      <PopoverTrigger asChild>
        <Button
          id={id}
          type="button"
          variant="outline"
          disabled={disabled}
          aria-label={ariaLabel ?? t("common.date")}
          aria-invalid={ariaInvalid || undefined}
          aria-describedby={ariaDescribedBy}
          aria-required={required || undefined}
          className={cn("h-10 w-full justify-start gap-2 rounded-sm border-border bg-surface-container-low px-3 text-start font-normal shadow-none hover:bg-surface-container-high", !selected && "text-muted-foreground", className)}
        >
          <CalendarIcon className="size-4 shrink-0 text-primary" />
          <span className="truncate" dir="auto">{selected ? formatDate(selected, { dateStyle: "medium" }) : t("common.date")}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-auto rounded-sm border-border bg-popover p-0 shadow-e3" dir={language === "ar" ? "rtl" : "ltr"}>
        <Calendar
          mode="single"
          selected={selected}
          onSelect={selectDate}
          disabled={disabledDays}
          locale={language === "ar" ? ar : enUS}
          dir={language === "ar" ? "rtl" : "ltr"}
          initialFocus
        />
        <div className="flex items-center justify-between border-t border-border px-3 py-2">
          <Button type="button" variant="ghost" size="sm" className="h-8 px-2" onClick={() => { onValueChange(null); setOpen(false); }} disabled={!selected}>{t("common.clear")}</Button>
          <Button type="button" variant="ghost" size="sm" className="h-8 px-2" onClick={() => selectDate(new Date())}>{t("common.today")}</Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}
