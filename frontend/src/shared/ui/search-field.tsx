import * as React from "react";
import { Search, X } from "lucide-react";
import { cn } from "./utils";

export interface SearchFieldProps extends Omit<React.ComponentProps<"input">, "type" | "onChange"> {
  onChange?: React.ChangeEventHandler<HTMLInputElement>;
  onValueChange?: (value: string) => void;
  onClear?: () => void;
  clearLabel?: string;
}

const SearchField = React.forwardRef<HTMLInputElement, SearchFieldProps>(
  ({ className, value, onChange, onValueChange, onClear, clearLabel = "Clear search", disabled, ...props }, ref) => {
    const hasValue = value !== undefined && String(value).length > 0;
    const handleClear = () => {
      onClear?.();
      onValueChange?.("");
    };

    return (
      <div data-slot="search-field" className={cn("relative w-full", className)}>
        <Search aria-hidden="true" className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-on-surface-variant" />
        <input
          ref={ref}
          type="search"
          value={value}
          disabled={disabled}
          onChange={(event) => {
            onChange?.(event);
            onValueChange?.(event.target.value);
          }}
          className="h-10 w-full rounded-full border border-outline bg-input-background ps-10 pe-10 type-body-medium text-foreground outline-none transition-[border-color,box-shadow,background-color] placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/20 aria-invalid:border-destructive aria-invalid:ring-2 aria-invalid:ring-destructive/20 disabled:cursor-not-allowed disabled:border-outline-variant disabled:bg-disabled-container disabled:text-disabled-foreground [&::-webkit-search-cancel-button]:hidden"
          {...props}
        />
        {hasValue && !disabled && (onClear || onValueChange) && (
          <button type="button" onClick={handleClear} aria-label={clearLabel} className="absolute end-1 top-1/2 flex size-8 -translate-y-1/2 items-center justify-center rounded-full text-on-surface-variant hover:bg-surface-container-high">
            <X aria-hidden="true" className="size-4" />
          </button>
        )}
      </div>
    );
  },
);
SearchField.displayName = "SearchField";

export { SearchField };
