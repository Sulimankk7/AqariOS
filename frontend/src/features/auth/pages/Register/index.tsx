/**
 * RegisterPage component.
 * Reads language context from AuthLayout and renders RegisterForm.
 */

import { useOutletContext } from "react-router";
import { RegisterForm } from "./components/RegisterForm";

interface AuthOutletContext {
  lang: "en" | "ar";
  setFeedbackMessage: (msg: string | null) => void;
}

export default function RegisterPage() {
  const context = useOutletContext<AuthOutletContext>();
  const lang = context?.lang ?? "en";

  const handleFeedback = (msg: string) => {
    if (context?.setFeedbackMessage) {
      context.setFeedbackMessage(msg);
      setTimeout(() => context.setFeedbackMessage(null), 4000);
    }
  };

  return <RegisterForm lang={lang} onFeedbackMessage={handleFeedback} />;
}
