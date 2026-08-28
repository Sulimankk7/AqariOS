import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { LoaderCircle } from "lucide-react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "./utils";

const buttonVariants = cva(
  "inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-full type-label-large transition-[background-color,color,box-shadow,border-color] duration-150 disabled:pointer-events-none disabled:cursor-not-allowed disabled:border-transparent disabled:bg-disabled-container disabled:text-disabled-foreground disabled:shadow-none [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background aria-invalid:ring-2 aria-invalid:ring-destructive",
  {
    variants: {
      variant: {
        filled: "bg-primary text-primary-foreground shadow-e0 hover:shadow-e1 hover:bg-primary/92",
        default: "bg-primary text-primary-foreground shadow-e0 hover:shadow-e1 hover:bg-primary/92",
        tonal: "bg-primary-container text-on-primary-container hover:bg-primary-container/82",
        elevated: "bg-surface-container-low text-primary shadow-e1 hover:bg-surface-container-lowest hover:shadow-e2",
        outlined: "border border-outline bg-transparent text-primary hover:bg-primary/8",
        text: "bg-transparent text-primary hover:bg-primary/8",
        destructive:
          "bg-destructive text-destructive-foreground hover:bg-destructive/90 focus-visible:ring-destructive",
        outline:
          "border border-outline bg-transparent text-primary hover:bg-primary/8",
        secondary:
          "bg-primary-container text-on-primary-container hover:bg-primary-container/82",
        ghost:
          "bg-transparent text-primary hover:bg-primary/8",
        link: "bg-transparent text-primary underline-offset-4 hover:underline",
      },
      size: {
        default: "h-10 px-6 has-[>svg]:px-5",
        sm: "h-8 gap-1.5 px-4 type-label-medium has-[>svg]:px-3",
        lg: "h-12 px-8 has-[>svg]:px-6",
        icon: "size-10 p-0",
        "icon-sm": "size-8 p-0",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

function Button({
  className,
  variant,
  size,
  asChild = false,
  loading = false,
  loadingText,
  children,
  disabled,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
    loading?: boolean;
    loadingText?: React.ReactNode;
  }) {
  const Comp = asChild ? Slot : "button";

  return (
    <Comp
      data-slot="button"
      data-variant={variant ?? "default"}
      data-loading={loading || undefined}
      className={cn(buttonVariants({ variant, size, className }))}
      aria-busy={loading || undefined}
      disabled={asChild ? undefined : disabled || loading}
      {...props}
    >
      {loading && <LoaderCircle aria-hidden="true" className="animate-spin" />}
      {loading && loadingText ? loadingText : children}
    </Comp>
  );
}

export { Button, buttonVariants };
