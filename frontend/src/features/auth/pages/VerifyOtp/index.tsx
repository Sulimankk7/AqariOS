/**
 * VerifyOtpPage component.
 * Reads language context from AuthLayout and renders OtpGrid.
 */

import { useOutletContext } from "react-router";
import { OtpGrid } from "./components/OtpGrid";

interface AuthOutletContext {
  lang: "en" | "ar";
  setFeedbackMessage: (msg: string | null) => void;
}

export default function VerifyOtpPage() {
  const context = useOutletContext<AuthOutletContext>();
  const lang = context?.lang ?? "en";

  const handleFeedback = (msg: string) => {
    if (context?.setFeedbackMessage) {
      context.setFeedbackMessage(msg);
      setTimeout(() => context.setFeedbackMessage(null), 4000);
    }
  };

  return <OtpGrid lang={lang} onFeedbackMessage={handleFeedback} />;
}
