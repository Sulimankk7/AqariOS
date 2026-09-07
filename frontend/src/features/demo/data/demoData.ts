import type { ApartmentDto } from "@/features/apartments/types/apartments.types";
import type { BuildingDto } from "@/features/buildings/types/buildings.types";
import type { DashboardSummaryDto } from "@/features/dashboard/types/dashboard.types";
import type { BuildingDocument } from "@/features/documents/documents.api";
import type { ChequeDetailDto, ExpenseDto, RentPaymentDto } from "@/features/financials/types/financials.types";
import type { LeaseContractDto } from "@/features/leasing/types/leasing.types";
import type { NotificationDto } from "@/features/notifications/types/notifications.types";
import type { TenantDto } from "@/features/tenants/types/tenants.types";

export type DemoModuleKey =
  | "buildings"
  | "apartments"
  | "leases"
  | "tenants"
  | "payments"
  | "cheques"
  | "financial-operations"
  | "documents"
  | "notifications";

export interface DemoTableRow extends Record<string, string> {
  id: string;
  primary: string;
  secondary: string;
  relation: string;
  status: string;
  value: string;
}

export interface DemoModuleDefinition {
  title: string;
  description: string;
  actionLabel?: string;
  columns: {
    primary: string;
    secondary: string;
    relation: string;
    status: string;
    value: string;
  };
  rows: DemoTableRow[];
}

export const DEMO_DASHBOARD_SUMMARY: DashboardSummaryDto = {
  property: {
    totalBuildings: 2,
    totalApartments: 12,
    occupiedApartments: 9,
    vacantApartments: 3,
    occupancyRate: 75,
  },
  leasing: {
    activeLeases: 9,
    expiringIn30Days: 2,
    newLeasesThisMonth: 1,
  },
  payments: {
    collectedThisMonth: 6350,
    outstandingAmount: 1250,
    overduePayments: 2,
  },
  financials: {
    expensesThisMonth: 780,
  },
};

type DemoBuilding = Pick<BuildingDto, "id" | "name" | "internalCode" | "totalFloors" | "totalApartmentsCount"> & { location: string };
type DemoApartment = Pick<ApartmentDto, "id" | "unitNumber" | "areaSqm" | "bedrooms" | "bathrooms" | "baseRentAmount" | "baseRentCurrency"> & { buildingName: string; occupancyLabel: string };
type DemoLease = Pick<LeaseContractDto, "id" | "contractNumber" | "startDate" | "endDate" | "monthlyRentAmount" | "currency"> & { tenantName: string; unitLabel: string; statusLabel: string };
type DemoTenant = Pick<TenantDto, "id" | "name" | "phone" | "email" | "occupation"> & { activeLease: string };
type DemoPayment = Pick<RentPaymentDto, "id" | "tenantName" | "buildingName" | "apartmentNumber" | "amountDue" | "amountPaid" | "currency" | "dueDate"> & { statusLabel: string };
type DemoCheque = Pick<ChequeDetailDto, "id" | "chequeNumber" | "bankName" | "dueDate" | "amount" | "currency"> & { tenantName: string; statusLabel: string };
type DemoExpense = Pick<ExpenseDto, "id" | "description" | "expenseDate" | "vendorName" | "amount" | "currency"> & { buildingName: string; statusLabel: string };
type DemoDocument = Pick<BuildingDocument, "id" | "documentName" | "categoryName" | "expiryDate" | "originalFilename"> & { buildingName: string; statusLabel: string };
type DemoNotification = Pick<NotificationDto, "id" | "subject" | "body" | "createdAt" | "readAt"> & { statusLabel: string };

const buildings: DemoBuilding[] = [
  { id: "bld-yasmeen", name: "مبنى الياسمين", internalCode: "BLD-001", totalFloors: 4, totalApartmentsCount: 7, location: "عمّان — الجبيهة" },
  { id: "bld-nakheel", name: "مبنى النخيل", internalCode: "BLD-002", totalFloors: 3, totalApartmentsCount: 5, location: "عمّان — خلدا" },
];

const apartments: DemoApartment[] = [
  { id: "apt-101", unitNumber: "شقة 101", areaSqm: 118, bedrooms: 3, bathrooms: 2, baseRentAmount: 420, baseRentCurrency: "د.أ", buildingName: "مبنى الياسمين", occupancyLabel: "مشغولة" },
  { id: "apt-102", unitNumber: "شقة 102", areaSqm: 105, bedrooms: 2, bathrooms: 2, baseRentAmount: 370, baseRentCurrency: "د.أ", buildingName: "مبنى الياسمين", occupancyLabel: "متاحة" },
  { id: "apt-201", unitNumber: "شقة 201", areaSqm: 132, bedrooms: 3, bathrooms: 3, baseRentAmount: 480, baseRentCurrency: "د.أ", buildingName: "مبنى الياسمين", occupancyLabel: "مشغولة" },
  { id: "apt-n101", unitNumber: "شقة 101", areaSqm: 96, bedrooms: 2, bathrooms: 2, baseRentAmount: 350, baseRentCurrency: "د.أ", buildingName: "مبنى النخيل", occupancyLabel: "مشغولة" },
  { id: "apt-n202", unitNumber: "شقة 202", areaSqm: 124, bedrooms: 3, bathrooms: 2, baseRentAmount: 440, baseRentCurrency: "د.أ", buildingName: "مبنى النخيل", occupancyLabel: "متاحة" },
];

const leases: DemoLease[] = [
  { id: "lease-1001", contractNumber: "L-2026-001", startDate: "2026-01-01", endDate: "2026-12-31", monthlyRentAmount: 420, currency: "د.أ", tenantName: "أحمد الخطيب", unitLabel: "الياسمين — شقة 101", statusLabel: "نشط" },
  { id: "lease-1002", contractNumber: "L-2026-002", startDate: "2026-02-01", endDate: "2027-01-31", monthlyRentAmount: 480, currency: "د.أ", tenantName: "سارة العواملة", unitLabel: "الياسمين — شقة 201", statusLabel: "نشط" },
  { id: "lease-1003", contractNumber: "L-2026-003", startDate: "2026-03-15", endDate: "2027-03-14", monthlyRentAmount: 350, currency: "د.أ", tenantName: "محمد الزعبي", unitLabel: "النخيل — شقة 101", statusLabel: "نشط" },
  { id: "lease-1004", contractNumber: "L-2025-014", startDate: "2025-10-01", endDate: "2026-09-30", monthlyRentAmount: 390, currency: "د.أ", tenantName: "ليان حداد", unitLabel: "النخيل — شقة 201", statusLabel: "ينتهي قريبًا" },
];

const tenants: DemoTenant[] = [
  { id: "tenant-1", name: "أحمد الخطيب", phone: "+962 7 9000 1122", email: "ahmad@example.com", occupation: "مهندس مدني", activeLease: "L-2026-001" },
  { id: "tenant-2", name: "سارة العواملة", phone: "+962 7 9111 2233", email: "sara@example.com", occupation: "صيدلانية", activeLease: "L-2026-002" },
  { id: "tenant-3", name: "محمد الزعبي", phone: "+962 7 9222 3344", email: "mohammad@example.com", occupation: "محاسب", activeLease: "L-2026-003" },
  { id: "tenant-4", name: "ليان حداد", phone: "+962 7 9333 4455", email: "layan@example.com", occupation: "مصممة", activeLease: "L-2025-014" },
];

const payments: DemoPayment[] = [
  { id: "pay-1", tenantName: "أحمد الخطيب", buildingName: "مبنى الياسمين", apartmentNumber: "101", amountDue: 420, amountPaid: 420, currency: "د.أ", dueDate: "2026-09-01", statusLabel: "مدفوعة" },
  { id: "pay-2", tenantName: "سارة العواملة", buildingName: "مبنى الياسمين", apartmentNumber: "201", amountDue: 480, amountPaid: 0, currency: "د.أ", dueDate: "2026-09-10", statusLabel: "مستحقة" },
  { id: "pay-3", tenantName: "محمد الزعبي", buildingName: "مبنى النخيل", apartmentNumber: "101", amountDue: 350, amountPaid: 150, currency: "د.أ", dueDate: "2026-08-15", statusLabel: "متأخرة" },
  { id: "pay-4", tenantName: "ليان حداد", buildingName: "مبنى النخيل", apartmentNumber: "201", amountDue: 390, amountPaid: 390, currency: "د.أ", dueDate: "2026-09-05", statusLabel: "مدفوعة" },
];

const cheques: DemoCheque[] = [
  { id: "chq-1", chequeNumber: "004821", bankName: "البنك العربي", dueDate: "2026-09-15", amount: 480, currency: "د.أ", tenantName: "سارة العواملة", statusLabel: "قيد التحصيل" },
  { id: "chq-2", chequeNumber: "008934", bankName: "بنك الاتحاد", dueDate: "2026-10-01", amount: 420, currency: "د.أ", tenantName: "أحمد الخطيب", statusLabel: "مستحق لاحقًا" },
  { id: "chq-3", chequeNumber: "003117", bankName: "البنك الأهلي الأردني", dueDate: "2026-08-15", amount: 350, currency: "د.أ", tenantName: "محمد الزعبي", statusLabel: "بحاجة للمتابعة" },
];

const expenses: DemoExpense[] = [
  { id: "exp-1", description: "فاتورة مياه المناطق المشتركة", expenseDate: "2026-09-03", vendorName: "مياهنا", amount: 96, currency: "د.أ", buildingName: "مبنى الياسمين", statusLabel: "مصروف" },
  { id: "exp-2", description: "خدمة المصعد الشهرية", expenseDate: "2026-09-01", vendorName: "شركة المصاعد الأردنية", amount: 185, currency: "د.أ", buildingName: "مبنى الياسمين", statusLabel: "مصروف" },
  { id: "exp-3", description: "دخل إيجار شهر أيلول", expenseDate: "2026-09-01", vendorName: "أحمد الخطيب", amount: 420, currency: "د.أ", buildingName: "مبنى الياسمين", statusLabel: "دخل" },
  { id: "exp-4", description: "تنظيف المرافق المشتركة", expenseDate: "2026-09-02", vendorName: "خدمات النخبة", amount: 75, currency: "د.أ", buildingName: "مبنى النخيل", statusLabel: "مصروف" },
];

const documents: DemoDocument[] = [
  { id: "doc-1", documentName: "عقد إيجار شقة 101", categoryName: "عقود الإيجار", expiryDate: "2026-12-31", originalFilename: "lease-L-2026-001.pdf", buildingName: "مبنى الياسمين", statusLabel: "ساري" },
  { id: "doc-2", documentName: "رخصة المهن", categoryName: "وثائق المبنى", expiryDate: "2027-01-20", originalFilename: "trade-license.pdf", buildingName: "مبنى الياسمين", statusLabel: "ساري" },
  { id: "doc-3", documentName: "تأمين المبنى", categoryName: "تأمين", expiryDate: "2026-10-12", originalFilename: "insurance-2026.pdf", buildingName: "مبنى النخيل", statusLabel: "ينتهي قريبًا" },
  { id: "doc-4", documentName: "عقد إيجار شقة 201", categoryName: "عقود الإيجار", expiryDate: "2026-09-30", originalFilename: "lease-L-2025-014.pdf", buildingName: "مبنى النخيل", statusLabel: "بحاجة للتجديد" },
];

const notifications: DemoNotification[] = [
  { id: "not-1", subject: "دفعة إيجار مستحقة", body: "دفعة شهر أيلول للعقد L-2026-002 تستحق خلال 3 أيام.", createdAt: "2026-09-07T08:30:00Z", readAt: null, statusLabel: "جديد" },
  { id: "not-2", subject: "عقد ينتهي قريبًا", body: "العقد L-2025-014 ينتهي خلال 23 يومًا.", createdAt: "2026-09-06T11:10:00Z", readAt: null, statusLabel: "جديد" },
  { id: "not-3", subject: "تم تسجيل دفعة", body: "تم تسجيل دفعة بقيمة 420 د.أ للعقد L-2026-001.", createdAt: "2026-09-05T09:45:00Z", readAt: "2026-09-05T10:00:00Z", statusLabel: "مقروء" },
  { id: "not-4", subject: "مستند يقترب من الانتهاء", body: "وثيقة تأمين مبنى النخيل تنتهي خلال 35 يومًا.", createdAt: "2026-09-04T14:20:00Z", readAt: "2026-09-04T15:00:00Z", statusLabel: "مقروء" },
];

export const DEMO_MODULES: Record<DemoModuleKey, DemoModuleDefinition> = {
  buildings: {
    title: "المباني والعقارات",
    description: "إدارة محفظة المباني ومواقعها ووحداتها.",
    actionLabel: "إضافة مبنى",
    columns: { primary: "المبنى", secondary: "الكود", relation: "الموقع", status: "الطوابق", value: "الوحدات" },
    rows: buildings.map((item) => ({ id: item.id, primary: item.name, secondary: item.internalCode ?? "—", relation: item.location, status: `${item.totalFloors} طوابق`, value: `${item.totalApartmentsCount} وحدات` })),
  },
  apartments: {
    title: "الشقق والوحدات",
    description: "حالة كل وحدة وبياناتها الأساسية والعقار المرتبط بها.",
    actionLabel: "إضافة وحدة",
    columns: { primary: "الوحدة", secondary: "المبنى", relation: "التفاصيل", status: "الإشغال", value: "الإيجار" },
    rows: apartments.map((item) => ({ id: item.id, primary: item.unitNumber, secondary: item.buildingName, relation: `${item.areaSqm}م² · ${item.bedrooms} غرف · ${item.bathrooms} حمام`, status: item.occupancyLabel, value: `${item.baseRentAmount} ${item.baseRentCurrency}` })),
  },
  leases: {
    title: "عقود الإيجار",
    description: "العقود النشطة والقريبة من الانتهاء وربطها بالمستأجر والوحدة.",
    actionLabel: "إنشاء عقد",
    columns: { primary: "رقم العقد", secondary: "المستأجر", relation: "الوحدة", status: "الحالة", value: "الإيجار الشهري" },
    rows: leases.map((item) => ({ id: item.id, primary: item.contractNumber, secondary: item.tenantName, relation: item.unitLabel, status: item.statusLabel, value: `${item.monthlyRentAmount} ${item.currency}` })),
  },
  tenants: {
    title: "المستأجرون",
    description: "بيانات المستأجرين والعقود النشطة المرتبطة بهم.",
    actionLabel: "إضافة مستأجر",
    columns: { primary: "المستأجر", secondary: "الهاتف", relation: "البريد الإلكتروني", status: "المهنة", value: "العقد النشط" },
    rows: tenants.map((item) => ({ id: item.id, primary: item.name, secondary: item.phone, relation: item.email ?? "—", status: item.occupation ?? "—", value: item.activeLease })),
  },
  payments: {
    title: "الدفعات والتحصيل",
    description: "متابعة الدفعات المستحقة والمدفوعة والمتأخرة.",
    actionLabel: "تسجيل دفعة",
    columns: { primary: "المستأجر", secondary: "العقار", relation: "تاريخ الاستحقاق", status: "الحالة", value: "المبلغ" },
    rows: payments.map((item) => ({ id: item.id, primary: item.tenantName ?? "—", secondary: `${item.buildingName} · ${item.apartmentNumber}`, relation: item.dueDate ?? "—", status: item.statusLabel, value: `${item.amountPaid}/${item.amountDue} ${item.currency}` })),
  },
  cheques: {
    title: "الشيكات",
    description: "سجل الشيكات ومواعيد استحقاقها وحالة التحصيل.",
    columns: { primary: "رقم الشيك", secondary: "المستأجر", relation: "البنك", status: "الحالة", value: "القيمة" },
    rows: cheques.map((item) => ({ id: item.id, primary: item.chequeNumber, secondary: item.tenantName, relation: `${item.bankName} · ${item.dueDate}`, status: item.statusLabel, value: `${item.amount} ${item.currency}` })),
  },
  "financial-operations": {
    title: "العمليات المالية",
    description: "الدخل والمصروفات المرتبطة بالعقارات في سجل موحد.",
    actionLabel: "تسجيل عملية",
    columns: { primary: "العملية", secondary: "العقار", relation: "الجهة", status: "النوع", value: "القيمة" },
    rows: expenses.map((item) => ({ id: item.id, primary: item.description, secondary: item.buildingName, relation: `${item.vendorName ?? "—"} · ${item.expenseDate}`, status: item.statusLabel, value: `${item.amount} ${item.currency}` })),
  },
  documents: {
    title: "المستندات",
    description: "المستندات المرتبطة بالمباني والعقود وتواريخ صلاحيتها.",
    actionLabel: "رفع مستند",
    columns: { primary: "المستند", secondary: "التصنيف", relation: "العقار", status: "الحالة", value: "تاريخ الانتهاء" },
    rows: documents.map((item) => ({ id: item.id, primary: item.documentName, secondary: item.categoryName, relation: item.buildingName, status: item.statusLabel, value: item.expiryDate ?? "—" })),
  },
  notifications: {
    title: "الإشعارات",
    description: "التنبيهات المرتبطة بالدفعات والعقود والمستندات.",
    columns: { primary: "الإشعار", secondary: "التفاصيل", relation: "التاريخ", status: "الحالة", value: "" },
    rows: notifications.map((item) => ({ id: item.id, primary: item.subject, secondary: item.body, relation: item.createdAt.slice(0, 10), status: item.statusLabel, value: "" })),
  },
};
