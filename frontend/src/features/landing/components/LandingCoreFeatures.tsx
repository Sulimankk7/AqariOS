import {
  BarChart3,
  Building2,
  CreditCard,
  FileText,
  ShieldCheck,
  Users,
  type LucideIcon,
} from "lucide-react";
import "./LandingCoreFeatures.css";

interface Capability {
  number: string;
  title: string;
  description: string;
  icon: LucideIcon;
}

const CAPABILITIES: readonly Capability[] = [
  {
    number: "01",
    title: "العقارات والوحدات",
    description:
      "أنشئ مبانيك ووحداتها ونظّم معلومات كل عقار، لتبقى تفاصيل عقاراتك واضحة وسهلة الوصول.",
    icon: Building2,
  },
  {
    number: "02",
    title: "الإيجارات والمستأجرين",
    description:
      "أنشئ عقود الإيجار وتابع المستأجرين وحالة العقود، مع سجل واضح للتغييرات والعمليات المرتبطة بها.",
    icon: Users,
  },
  {
    number: "03",
    title: "الدفعات والشيكات",
    description:
      "تابع الاستحقاقات والدفعات والشيكات، واعرف ما تم تحصيله وما يزال مستحقًا لكل عقد.",
    icon: CreditCard,
  },
  {
    number: "04",
    title: "العمليات المالية",
    description:
      "سجّل المصروفات والإيصالات والعمليات المالية المرتبطة بإدارة العقار، مع رؤية أوضح للحركة المالية.",
    icon: BarChart3,
  },
  {
    number: "05",
    title: "المستندات",
    description:
      "نظّم مستندات المباني والعقود في مكان واحد، مع إمكانية إدارة المستندات الحساسة وفق الصلاحيات.",
    icon: FileText,
  },
  {
    number: "06",
    title: "الصلاحيات والأمان",
    description:
      "حدّد ما يستطيع كل مستخدم الوصول إليه وتنفيذه، مع أدوار وصلاحيات مصممة لبيئة العمل متعددة المستخدمين.",
    icon: ShieldCheck,
  },
];

export function LandingCoreFeatures() {
  return (
    <section
      id="features"
      className="landing-core-features"
      aria-labelledby="landing-core-features-title"
    >
      <div className="landing-core-features__container">
        <h2 id="landing-core-features-title" className="landing-core-features__title">
          إمكانيات AqariOS
        </h2>

        <div className="landing-core-features__intro">
          <div className="landing-core-features__brand" aria-label="AqariOS">
            <img
              src="/branding/landing/aqarios-logo.png"
              alt="شعار AqariOS"
              width="180"
              height="180"
            />
          </div>

          <div className="landing-core-features__summary">
            <h3>نظام واحد لإدارة تفاصيل عقاراتك اليومية.</h3>
            <p>
              AqariOS مصمم ليجمع العمل الذي تحتاجه لإدارة العقارات في مكان واحد.
              من تسجيل المباني والوحدات، وإدارة المستأجرين والعقود، إلى متابعة
              الدفعات والشيكات، وتنظيم العمليات المالية والمستندات والصلاحيات.
            </p>
            <p>
              بدل أن تنتقل بين ملفات وأدوات مختلفة، تحصل على مساحة واحدة ترى فيها
              عقاراتك وعملياتها وتتابع ما يحتاج إلى إجراء.
            </p>
          </div>
        </div>

        <ol className="landing-core-features__timeline">
          {CAPABILITIES.map(({ number, title, description, icon: Icon }) => (
            <li key={number} className="landing-core-features__capability">
              <span className="landing-core-features__number" aria-hidden="true">
                {number}
              </span>
              <span className="landing-core-features__icon" aria-hidden="true">
                <Icon strokeWidth={1.7} />
              </span>
              <div className="landing-core-features__copy">
                <h3>{title}</h3>
                <p>{description}</p>
              </div>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
