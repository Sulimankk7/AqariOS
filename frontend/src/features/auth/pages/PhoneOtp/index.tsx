/**
 * PhoneOtpPage component.
 * Reads language context from AuthLayout and renders PhoneOtpForm.
 */

import { useOutletContext } from "react-router";
import { PhoneOtpForm } from "./components/PhoneOtpForm";

interface AuthOutletContext {
  lang: "en" | "ar";
  setFeedbackMessage: (msg: string | null) => void;
}

export default function PhoneOtpPage() {
  const context = useOutletContext<AuthOutletContext>();
  const lang = context?.lang ?? "en";

  const handleFeedback = (msg: string) => {
    if (context?.setFeedbackMessage) {
      context.setFeedbackMessage(msg);
      setTimeout(() => context.setFeedbackMessage(null), 4000);
    }
  };

  return <PhoneOtpForm lang={lang} onFeedbackMessage={handleFeedback} />;
}
