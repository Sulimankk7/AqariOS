/**
 * AuthLoadingScreen Component.
 * Full-screen clean loading screen displayed during startup authentication initialization.
 * Prevents UI flickering and protects authenticated routes.
 */

import React from "react";

export function AuthLoadingScreen() {
  return (
    <div 
      className="fixed inset-0 z-50 bg-background text-foreground flex flex-col items-center justify-center gap-4 select-none"
      role="status"
      aria-label="Initializing authentication session"
    >
      <div className="flex items-center gap-3">
        <div className="w-9 h-9 rounded-lg bg-brand-green-900 text-white flex items-center justify-center font-bold font-mono text-xs shadow-md">
          AQ
        </div>
        <span className="font-bold tracking-tight text-lg">AqariOS</span>
      </div>

      <div className="flex items-center gap-2 text-xs text-muted-foreground font-medium">
        <div className="w-4 h-4 border-2 border-primary border-t-transparent rounded-full animate-spin" />
        <span>Validating session...</span>
      </div>
    </div>
  );
}
