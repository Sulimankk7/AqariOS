/**
 * LoginPage component.
 * Reads language context from AuthLayout and renders LoginForm.
 */

import { useOutletContext } from "react-router";
import { LoginForm } from "./components/LoginForm";

interface AuthOutletContext {
  lang: "en" | "ar";
  setFeedbackMessage: (msg: string | null) => void;
}

export default function LoginPage() {
  const context = useOutletContext<AuthOutletContext>();
  const lang = context?.lang ?? "en";

  const handleFeedback = (msg: string) => {
    if (context?.setFeedbackMessage) {
      context.setFeedbackMessage(msg);
      setTimeout(() => context.setFeedbackMessage(null), 4000);
    }
  };

  return <LoginForm lang={lang} onFeedbackMessage={handleFeedback} />;
}
