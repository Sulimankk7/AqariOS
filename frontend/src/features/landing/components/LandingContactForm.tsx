import { useState, type FormEvent, type ReactNode } from "react";
import { CheckCircle2 } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Textarea } from "@/shared/ui/textarea";
import { contactRequestsApi } from "../api/contactRequests.api";
import "./LandingContactForm.css";

const initialValues = { name: "", companyName: "", phoneNumber: "", numberOfBuildings: "", notes: "" };

export function LandingContactForm() {
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [submitError, setSubmitError] = useState(false);

  const setField = (field: keyof typeof values, value: string) => {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: "" }));
    setSubmitError(false);
  };

  const validate = () => {
    const next: Record<string, string> = {};
    if (values.name.trim().length < 2) next.name = "يرجى إدخال الاسم الكامل.";
    if (values.companyName.trim().length < 2) next.companyName = "يرجى إدخال اسم الشركة أو المكتب.";
    const phone = values.phoneNumber.replace(/[\s\-()]/g, "");
    if (!/^(?:\+962|00962|0)7[789]\d{7}$/.test(phone)) next.phoneNumber = "يرجى إدخال رقم هاتف أردني صحيح.";
    const buildings = Number(values.numberOfBuildings);
    if (!Number.isInteger(buildings) || buildings < 1 || buildings > 10000) next.numberOfBuildings = "يرجى إدخال عدد صحيح للعمارات.";
    if (values.notes.length > 1000) next.notes = "يجب ألا تتجاوز الملاحظات 1000 حرف.";
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting || submitted || !validate()) return;
    setSubmitting(true);
    setSubmitError(false);
    try {
      await contactRequestsApi.create({
        name: values.name.trim(),
        companyName: values.companyName.trim(),
        phoneNumber: values.phoneNumber.trim(),
        numberOfBuildings: Number(values.numberOfBuildings),
        notes: values.notes.trim() || undefined,
      });
      setValues(initialValues);
      setSubmitted(true);
    } catch {
      setSubmitError(true);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section id="contact" className="landing-contact" tabIndex={-1} aria-labelledby="landing-contact-title">
      <div className="landing-contact__container">
        <header className="landing-contact__intro">
          <p className="landing-contact__eyebrow">تواصل معنا</p>
          <h2 id="landing-contact-title">احجز مكالمة تعريفية مع فريق AqariOS.</h2>
          <p>أرسل بياناتك وسنتواصل معك لفهم احتياجك والإجابة عن أسئلتك.</p>
        </header>

        {submitted ? (
          <div className="landing-contact__success" role="status">
            <CheckCircle2 aria-hidden="true" />
            <h3>تم إرسال طلبك بنجاح.</h3>
            <p>سنتواصل معك قريبًا لتنسيق موعد مكالمة مناسب.</p>
          </div>
        ) : (
          <form className="landing-contact__form" onSubmit={handleSubmit} noValidate>
            <FormField id="contact-name" label="الاسم" error={errors.name}>
              <Input id="contact-name" value={values.name} onChange={(e) => setField("name", e.target.value)} placeholder="الاسم الكامل" maxLength={120} autoComplete="name" aria-invalid={Boolean(errors.name)} />
            </FormField>
            <FormField id="contact-company" label="اسم الشركة / المكتب" error={errors.companyName}>
              <Input id="contact-company" value={values.companyName} onChange={(e) => setField("companyName", e.target.value)} placeholder="اسم الشركة أو المكتب" maxLength={160} autoComplete="organization" aria-invalid={Boolean(errors.companyName)} />
            </FormField>
            <FormField id="contact-phone" label="رقم الهاتف" error={errors.phoneNumber}>
              <Input id="contact-phone" type="tel" inputMode="tel" dir="ltr" value={values.phoneNumber} onChange={(e) => setField("phoneNumber", e.target.value)} placeholder="07XXXXXXXX" maxLength={24} autoComplete="tel" aria-invalid={Boolean(errors.phoneNumber)} />
            </FormField>
            <FormField id="contact-buildings" label="عدد العمارات" error={errors.numberOfBuildings}>
              <Input id="contact-buildings" type="number" inputMode="numeric" min={1} max={10000} value={values.numberOfBuildings} onChange={(e) => setField("numberOfBuildings", e.target.value)} placeholder="مثال: 5" aria-invalid={Boolean(errors.numberOfBuildings)} />
            </FormField>
            <FormField id="contact-notes" label="ملاحظات (اختياري)" error={errors.notes} wide>
              <Textarea id="contact-notes" value={values.notes} onChange={(e) => setField("notes", e.target.value)} placeholder="أي معلومات إضافية..." maxLength={1000} rows={4} aria-invalid={Boolean(errors.notes)} />
            </FormField>
            <div className="landing-contact__submit">
              {submitError && <p role="alert">تعذر إرسال الطلب حاليًا. حاول مرة أخرى.</p>}
              <Button type="submit" size="lg" disabled={submitting} aria-busy={submitting}>
                {submitting ? "جارٍ إرسال الطلب..." : "احجز مكالمتك معنا"}
              </Button>
            </div>
          </form>
        )}
      </div>
    </section>
  );
}

function FormField({ id, label, error, wide, children }: { id: string; label: string; error?: string; wide?: boolean; children: ReactNode }) {
  return <div className={wide ? "landing-contact__field landing-contact__field--wide" : "landing-contact__field"}>
    <label htmlFor={id}>{label}</label>{children}{error && <span role="alert">{error}</span>}
  </div>;
}
