/**
 * useOtpTimer — countdown timer hook for OTP resend functionality.
 * Encapsulates the 60-second countdown from the Figma-generated design.
 */

import { useState, useEffect, useCallback } from "react";

interface UseOtpTimerReturn {
  /** Remaining seconds. 0 means the timer has expired. */
  secondsLeft: number;
  /** Whether the countdown is currently active. */
  isActive: boolean;
  /** Start or restart the countdown from `duration` seconds. */
  start: (duration?: number) => void;
  /** Stop the countdown without resetting. */
  stop: () => void;
  /** Stop and reset to 0. */
  reset: () => void;
}

const DEFAULT_DURATION = 60;

export function useOtpTimer(initialDuration = DEFAULT_DURATION): UseOtpTimerReturn {
  const [secondsLeft, setSecondsLeft] = useState(0);
  const [isActive, setIsActive] = useState(false);

  useEffect(() => {
    if (!isActive || secondsLeft <= 0) {
      if (secondsLeft <= 0 && isActive) {
        setIsActive(false);
      }
      return;
    }

    const interval = setInterval(() => {
      setSecondsLeft((prev) => prev - 1);
    }, 1000);

    return () => clearInterval(interval);
  }, [isActive, secondsLeft]);

  const start = useCallback(
    (duration = initialDuration) => {
      setSecondsLeft(duration);
      setIsActive(true);
    },
    [initialDuration],
  );

  const stop = useCallback(() => {
    setIsActive(false);
  }, []);

  const reset = useCallback(() => {
    setIsActive(false);
    setSecondsLeft(0);
  }, []);

  return { secondsLeft, isActive, start, stop, reset };
}
