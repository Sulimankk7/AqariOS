export const tenantTranslations = {
  en: {
    pageTitle: 'Tenants',
    pageDescription: 'Manage tenant profiles, national identification, contact information, and lease history.',
    newTenant: 'New Tenant',
    editTenant: 'Edit Tenant',
    tenantDetails: 'Tenant Profile Details',
    name: 'Full Name',
    nationalId: 'National ID / Passport No.',
    phone: 'Phone Number',
    occupation: 'Occupation',
    employer: 'Employer / Company',
    familyMembers: 'Family Members',
    emergencyContacts: 'Emergency Contacts',
    vehicles: 'Vehicles',
    leaseHistory: 'Lease Contract History',
    noFamilyMembers: 'No family members recorded.',
    noEmergencyContacts: 'No emergency contacts recorded.',
    noVehicles: 'No registered vehicles.',
    noLeases: 'No lease contracts found for this tenant.',
    relationship: 'Relationship',
    ageBracket: 'Age Bracket',
    plateNumber: 'Plate Number',
    makeModel: 'Make & Model',
    color: 'Color',
    searchPlaceholder: 'Search tenants by name, national ID, or phone...',
    
    // Actions & Buttons
    viewDetails: 'View Details',
    edit: 'Edit Tenant',
    delete: 'Delete Tenant',
    cancel: 'Cancel',
    save: 'Save Changes',
    submit: 'Confirm',
    retry: 'Retry',
    loading: 'Loading tenant data...',
    loadError: 'Failed to load tenant information.',
    
    // Success Messages
    createSuccess: 'Tenant profile created successfully.',
    updateSuccess: 'Tenant profile updated successfully.',
    deleteSuccess: 'Tenant profile deleted successfully.',
    
    // Delete Confirmation
    confirmDeleteTitle: 'Delete Tenant Record',
    confirmDeleteDesc: 'Are you sure you want to delete tenant "{name}"? This record will be soft-deleted. Deletion will be rejected if the tenant has active or pending lease contracts.',
    deleteFailedCannotDeleteActiveLease: 'Cannot delete tenant with active or pending lease contracts.',
    
    // Page Subtitles
    createTenantDesc: 'Enter personal and identification details to create a new tenant profile.',
    editTenantDesc: 'Update tenant identification and contact parameters.',
    notFound: 'Tenant not found.',
  },

  ar: {
    pageTitle: 'المستأجرين',
    pageDescription: 'إدارة ملفات المستأجرين، الهويات الوطنية، أرقام التواصل، وسجل العقود.',
    newTenant: 'إضافة مستأجر جديد',
    editTenant: 'تعديل بيانات المستأجر',
    tenantDetails: 'تفاصيل ملف المستأجر',
    name: 'الاسم الكامل',
    nationalId: 'الرقم الوطني / رقم الشخصي',
    phone: 'رقم الهاتف',
    occupation: 'المهنة / الوظيفة',
    employer: 'جهة العمل',
    familyMembers: 'أفراد العائلة المترددين',
    emergencyContacts: 'جهات الاتصال عند الطوارئ',
    vehicles: 'المركبات المسجلة',
    leaseHistory: 'سجل عقود الإيجار',
    noFamilyMembers: 'لا يوجد أفراد عائلة مسجلين.',
    noEmergencyContacts: 'لا توجد جهات اتصال طوارئ مسجلة.',
    noVehicles: 'لا توجد مركبات مسجلة.',
    noLeases: 'لا توجد عقود إيجار مسجلة لهذا المستأجر.',
    relationship: 'صلة القرابة',
    ageBracket: 'الفئة العمرية',
    plateNumber: 'رقم اللوحة',
    makeModel: 'النوع والموديل',
    color: 'اللون',
    searchPlaceholder: 'ابحث عن مستأجر بالاسم، الرقم الوطني، أو رقم الهاتف...',
    
    // Actions & Buttons
    viewDetails: 'عرض التفاصيل',
    edit: 'تعديل',
    delete: 'حذف',
    cancel: 'إلغاء',
    save: 'حفظ التغييرات',
    submit: 'تأكيد',
    retry: 'إعادة المحاولة',
    loading: 'جاري تحميل بيانات المستأجر...',
    loadError: 'فشل في تحميل بيانات المستأجر.',
    
    // Success Messages
    createSuccess: 'تم إنشاء ملف المستأجر بنجاح.',
    updateSuccess: 'تم تحديث بيانات المستأجر بنجاح.',
    deleteSuccess: 'تم حذف ملف المستأجر بنجاح.',
    
    // Delete Confirmation
    confirmDeleteTitle: 'حذف سجل المستأجر',
    confirmDeleteDesc: 'هل أنت تأكد من رغبتك في حذف المستأجر "{name}"؟ سيتم تعطيل/أرشفة السجل. سيتم رفض الحذف في حال وجود عقود إيجار نشطة أو مسودة مرتبطة بالمستأجر.',
    deleteFailedCannotDeleteActiveLease: 'لا يمكن حذف المستأجر لاقترانه بعقود إيجار نشطة أو سارية.',
    
    // Page Subtitles
    createTenantDesc: 'أدخل البيانات الشخصية والهوية لإنشاء ملف مستأجر جديد.',
    editTenantDesc: 'تحديث البيانات الشخصية ومعلومات الاتصال للمستأجر.',
    notFound: 'لم يتم العثور على المستأجر.',
  },
};

export function getTenantTranslation(key: string, language: 'en' | 'ar' = 'en'): string {
  const langDict = tenantTranslations[language] || tenantTranslations.en;
  return langDict[key as keyof typeof langDict] || tenantTranslations.en[key as keyof typeof tenantTranslations.en] || key;
}
