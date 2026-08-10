using System.Collections.Generic;

namespace PropertyOS.Application.Common.Security;

/// <summary>
/// Domain model record representing a permission catalog definition entry.
/// </summary>
public record PermissionDefinition(
    string Key,
    string Module,
    string DescriptionEn,
    string DescriptionAr
);

/// <summary>
/// Authoritative central platform RBAC permission catalog for AqariOS.
/// Single source of truth for platform permission keys and metadata definitions.
/// </summary>
public static class PlatformPermissions
{
    // Module 1 / 3 — System & Company Management
    public const string CompanyManage = "company.manage";

    // Module 4 — Properties Management
    public const string PropertiesRead = "properties.read";
    public const string PropertiesCreate = "properties.create";
    public const string PropertiesUpdate = "properties.update";
    public const string PropertiesDelete = "properties.delete";
    public const string PropertiesManage = "properties.manage";

    // Module 5 — Leasing Management
    public const string ContractsCreate = "contracts.create";
    public const string ContractsApprove = "contracts.approve";

    // Module 6 — Rent Payments Management
    public const string PaymentsApprove = "payments.approve";

    // Module 7 — Financial Operations
    public const string ExpensesCreate = "expenses.create";
    public const string ExpensesApprove = "expenses.approve";
    public const string ReceiptsIssue = "receipts.issue";
    public const string ReportsExport = "reports.export";

    // Module 8 — Maintenance Management
    public const string MaintenanceCreate = "maintenance.create";
    public const string MaintenanceUpdateStatus = "maintenance.update_status";
    public const string MaintenanceComment = "maintenance.comment";

    // Module 9 — Marketplace
    public const string MarketplacePublish = "marketplace.publish";

    // Module 10 — Documents Management
    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsManageCategories = "documents.manage_categories";
    public const string DocumentsViewConfidential = "documents.view_confidential";

    // Module 11 — Notifications Management
    public const string NotificationsSend = "notifications.send";
    public const string NotificationsManageTemplates = "notifications.manage_templates";
    public const string NotificationsViewAll = "notifications.view_all";

    // Module 5 — Tenant Portal Self-Service
    public const string TenantPortalAccess = "tenant.portal.access";

    /// <summary>
    /// Complete catalog of all active platform permission definitions.
    /// </summary>
    public static readonly IReadOnlyList<PermissionDefinition> Catalog = new List<PermissionDefinition>
    {
        new(TenantPortalAccess, "Leasing", "Access tenant portal self-service capabilities", "الوصول إلى خدمات بوابة المستأجر الذاتية"),

        new(CompanyManage, "Identity", "Manage company settings and tenant parameters", "إدارة إعدادات الشركة ومعايير المستأجر"),

        new(PropertiesRead, "Properties", "View property, floor, apartment, and parking inventory", "عرض عقارات وطوابق وشقق ومواقف السيارات"),
        new(PropertiesCreate, "Properties", "Create new properties, floors, apartments, and parking spots", "إنشاء عقارات وطوابق وشقق ومواقف سيارات جديدة"),
        new(PropertiesUpdate, "Properties", "Update property, floor, apartment, and parking spot details", "تحديث تفاصيل العقارات والطوابق والشقق ومواقف السيارات"),
        new(PropertiesDelete, "Properties", "Archive/soft-delete properties, floors, apartments, and parking spots", "أرشفة/حذف العقارات والطوابق والشقق ومواقف السيارات"),
        new(PropertiesManage, "Properties", "Full administrative control over properties portfolio", "التحكم الإداري الكامل في محفظة العقارات"),

        new(ContractsCreate, "Leasing", "Draft and create lease contracts", "إعداد وإنشاء عقود الإيجار"),
        new(ContractsApprove, "Leasing", "Approve and execute lease contracts", "الموافقة على عقود الإيجار وتنفيذها"),

        new(PaymentsApprove, "Financials", "Approve rent payments and cheques", "الموافقة على دفعات الإيجار والشيكات"),

        new(ExpensesCreate, "Financials", "Record operational expenses", "تسجيل المصاريف التشغيلية"),
        new(ExpensesApprove, "Financials", "Approve operational expense payouts", "الموافقة على صرف المصاريف التشغيلية"),
        new(ReceiptsIssue, "Financials", "Issue official rent and expense receipts", "إصدار إيصالات الإيجار والمصاريف الرسمية"),
        new(ReportsExport, "Financials", "Export financial and portfolio reports", "تصدير التقارير المالية والعقارية"),

        new(MaintenanceCreate, "Maintenance", "Submit maintenance requests", "تقديم طلبات الصيانة"),
        new(MaintenanceUpdateStatus, "Maintenance", "Update maintenance ticket status", "تحديث حالة طلب الصيانة"),
        new(MaintenanceComment, "Maintenance", "Add comments to maintenance tickets", "إضافة تعليقات على طلبات الصيانة"),

        new(MarketplacePublish, "Marketplace", "Publish vacant units to public marketplace", "نشر الوحدات الشاغرة في السوق العام"),

        new(DocumentsUpload, "Documents", "Upload property documents", "تحميل مستندات العقارات"),
        new(DocumentsManageCategories, "Documents", "Manage document category catalog", "إدارة تصنيفات المستندات"),
        new(DocumentsViewConfidential, "Documents", "Access confidential building and legal documents", "الوصول إلى المستندات السرية والقانونية"),

        new(NotificationsSend, "Notifications", "Send notifications to staff and tenants", "إرسال الإشعارات للموظفين والمستأجرين"),
        new(NotificationsManageTemplates, "Notifications", "Manage notification templates", "إدارة قوالب الإشعارات"),
        new(NotificationsViewAll, "Notifications", "Administrative view of all tenant notification inboxes", "عرض إداري لجميع صناديق إشعارات المستأجرين")
    };
}
