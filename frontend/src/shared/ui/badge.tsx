import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "./utils";

const badgeVariants = cva(
  "inline-flex w-fit shrink-0 items-center justify-center gap-1 overflow-hidden whitespace-nowrap rounded-full border px-2.5 py-1 type-label-medium transition-[color,background-color,border-color,box-shadow] [&>svg]:size-3 [&>svg]:pointer-events-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
  {
    variants: {
      variant: {
        default:
          "border-transparent bg-primary text-primary-foreground [a&]:hover:bg-primary/90",
        tonal: "border-transparent bg-primary-container text-on-primary-container [a&]:hover:bg-primary-container/80",
        secondary:
          "border-transparent bg-secondary-container text-on-secondary-container [a&]:hover:bg-secondary-container/80",
        tertiary: "border-transparent bg-tertiary-container text-foreground",
        destructive:
          "border-transparent bg-error-container text-destructive [a&]:hover:bg-error-container/80",
        outline:
          "border-outline bg-transparent text-on-surface-variant [a&]:hover:bg-surface-container",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

function Badge({
  className,
  variant,
  asChild = false,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot : "span";

  return (
    <Comp
      data-slot="badge"
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  );
}

export { Badge, badgeVariants };
