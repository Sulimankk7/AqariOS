/**
 * SessionExpiredModal Component.
 * Rendered when a session expires unexpectedly.
 * Displays a 3-second countdown dialog providing a smooth transition instead of an abrupt redirect.
 */

import React, { useEffect, useState } from "react";
import { AlertTriangle } from "lucide-react";
import { Button } from "@/app/components/ui/button";

interface SessionExpiredModalProps {
  isOpen: boolean;
  onConfirm: () => void;
  autoRedirectSeconds?: number;
}

export function SessionExpiredModal({
  isOpen,
  onConfirm,
  autoRedirectSeconds = 3,
}: SessionExpiredModalProps) {
  const [secondsLeft, setSecondsLeft] = useState(autoRedirectSeconds);

  useEffect(() => {
    if (!isOpen) return;

    setSecondsLeft(autoRedirectSeconds);

    const interval = setInterval(() => {
      setSecondsLeft((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          onConfirm();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [isOpen, autoRedirectSeconds, onConfirm]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-[100] bg-black/60 backdrop-blur-sm flex items-center justify-center p-4 animate-in fade-in duration-200"
      role="dialog"
      aria-modal="true"
      aria-labelledby="session-expired-title"
    >
      <div className="bg-card border border-border rounded-xl shadow-2xl max-w-md w-full p-6 space-y-5 text-center transform scale-100 transition-all">
        {/* Warning Icon */}
        <div className="w-12 h-12 rounded-full bg-amber-500/10 text-amber-500 flex items-center justify-center mx-auto">
          <AlertTriangle className="w-6 h-6" />
        </div>

        {/* Title & Message */}
        <div className="space-y-2">
          <h2
            id="session-expired-title"
            className="text-xl font-bold tracking-tight text-foreground"
          >
            Session expired
          </h2>
          <p className="text-sm text-muted-foreground leading-relaxed">
            Your session has expired. Please sign in again.
          </p>
        </div>

        {/* Auto-redirect indicator */}
        <div className="text-xs text-muted-foreground/80 font-mono bg-secondary/60 py-1.5 px-3 rounded-md inline-block">
          Redirecting in {secondsLeft} second{secondsLeft === 1 ? "" : "s"}...
        </div>

        {/* Action Button */}
        <div className="pt-2">
          <Button
            onClick={onConfirm}
            className="w-full bg-primary text-primary-foreground font-semibold h-10 hover:bg-primary/90 transition-colors"
          >
            Sign in
          </Button>
        </div>
      </div>
    </div>
  );
}
