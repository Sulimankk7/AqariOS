/**
 * ForgotPasswordPage component.
 * Reads language context from AuthLayout and renders ForgotPasswordForm.
 */

import { useOutletContext } from "react-router";
import { ForgotPasswordForm } from "./components/ForgotPasswordForm";

interface AuthOutletContext {
  lang: "en" | "ar";
  setFeedbackMessage: (msg: string | null) => void;
}

export default function ForgotPasswordPage() {
  const context = useOutletContext<AuthOutletContext>();
  const lang = context?.lang ?? "en";

  const handleFeedback = (msg: string) => {
    if (context?.setFeedbackMessage) {
      context.setFeedbackMessage(msg);
      setTimeout(() => context.setFeedbackMessage(null), 4000);
    }
  };

  return <ForgotPasswordForm lang={lang} onFeedbackMessage={handleFeedback} />;
}
