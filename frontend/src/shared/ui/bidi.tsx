import * as React from "react";
import { cn } from "./utils";

export type BidiContentType = "auto" | "email" | "phone" | "id" | "code" | "date" | "number";

export function BidiText({ type = "auto", className, ...props }: React.ComponentProps<"bdi"> & { type?: BidiContentType }) {
  const forceLtr = type !== "auto";
  return <bdi dir={forceLtr ? "ltr" : "auto"} data-bidi-isolate data-bidi-type={type} className={cn(forceLtr && "bidi-ltr inline-block", className)} {...props} />;
}
