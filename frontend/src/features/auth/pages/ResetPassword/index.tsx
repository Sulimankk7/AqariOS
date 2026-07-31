/**
 * ResetPasswordPage component.
 * Reads language context from AuthLayout and renders ResetPasswordForm.
 */

import { useOutletContext } from "react-router";
import { ResetPasswordForm } from "./components/ResetPasswordForm";

interface AuthOutletContext {
  lang: "en" | "ar";
  setFeedbackMessage: (msg: string | null) => void;
}

export default function ResetPasswordPage() {
  const context = useOutletContext<AuthOutletContext>();
  const lang = context?.lang ?? "en";

  const handleFeedback = (msg: string) => {
    if (context?.setFeedbackMessage) {
      context.setFeedbackMessage(msg);
      setTimeout(() => context.setFeedbackMessage(null), 4000);
    }
  };

  return <ResetPasswordForm lang={lang} onFeedbackMessage={handleFeedback} />;
}
