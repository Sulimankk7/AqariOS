export const dictionary = {
  en: {
    // Page & Section Headers
    apartments: 'Apartments',
    addApartment: 'Add Apartment',
    editApartment: 'Edit Apartment',
    apartmentDetails: 'Apartment Details',
    building: 'Building',
    floor: 'Floor',
    pageDescription: 'Manage apartment units across property floors.',
    createPageDescription: 'Register a new apartment unit under a specific floor.',
    editPageDescription: 'Update base rent and monetary terms for this unit.',
    backToApartments: 'Back to Apartments',
    
    // Form Sections
    unitInfo: 'Unit Information',
    ownershipInfo: 'Ownership Details',
    financialTerms: 'Financial Terms',
    
    // Fields
    unitNumber: 'Unit Number',
    areaSqm: 'Area (m²)',
    ownershipStatusTitle: 'Ownership Model',
    externalOwnerName: 'External Owner Name',
    externalOwnerPhone: 'External Owner Phone',
    bedrooms: 'Bedrooms',
    bathrooms: 'Bathrooms',
    baseRentAmount: 'Base Rent Amount',
    baseRentCurrency: 'Currency',
    occupancyStatusTitle: 'Occupancy Status',
    floorId: 'Floor',
    buildingId: 'Building',
    
    // Form Placeholders
    unitNumberPlaceholder: 'e.g. APT-101',
    areaSqmPlaceholder: 'e.g. 120.5',
    selectOwnershipPlaceholder: 'Select ownership model',
    externalOwnerNamePlaceholder: 'e.g. Ahmad Al-Mansoor',
    externalOwnerPhonePlaceholder: 'e.g. 0791234567',
    bedroomsPlaceholder: 'e.g. 2',
    bathroomsPlaceholder: 'e.g. 2',
    baseRentAmountPlaceholder: 'e.g. 450.00',
    floorIdPlaceholder: 'Select a floor',

    // Table Columns
    tableUnit: 'Unit',
    tableArea: 'Area',
    tableBedrooms: 'Bedrooms',
    tableBathrooms: 'Bathrooms',
    tableBaseRent: 'Base Rent',
    tableOwnership: 'Ownership',
    tableOccupancy: 'Status',
    searchPlaceholder: 'Search apartments by unit number, building name, or status...',
    totalApartments: 'Total Apartments',
    occupiedApartments: 'Occupied',
    vacantApartments: 'Vacant',
    unassignedBuilding: 'Unassigned Building',
    expandAll: 'Expand All',
    collapseAll: 'Collapse All',

    // Actions & Buttons
    save: 'Save Apartment',
    saving: 'Saving...',
    cancel: 'Cancel',
    view: 'View',
    edit: 'Edit',
    delete: 'Delete',
    deleting: 'Deleting...',
    viewApartment: 'View apartment details',
    editApartmentTitle: 'Edit apartment',
    deleteApartmentTitle: 'Delete apartment',
    
    // Status & Empty States
    active: 'Active',
    inactive: 'Archived',
    loadingData: 'Loading apartment data...',
    notFound: 'Apartment not found.',
    noApartments: 'No apartments found.',
    
    // Dialogs & Toasts
    createSuccess: 'Apartment created successfully',
    updateSuccess: 'Apartment updated successfully',
    deleteSuccess: 'Apartment archived successfully',
    deleteConfirmTitle: 'Archive Apartment',
    deleteConfirmMessage: 'Are you sure you want to archive this apartment unit? This action will mark the unit inactive.',
    loadError: 'Failed to load apartments list.',
    floorRequiredMessage: 'Select a floor to create an apartment unit.',

    // Enum Labels
    ownershipStatus: {
      companyOwned: 'Company Owned',
      thirdPartyOwned: 'Third Party Owned',
    },
    occupancyStatus: {
      vacant: 'Vacant',
      occupied: 'Occupied',
      underMaintenance: 'Under Maintenance',
      listed: 'Listed',
    },
  },
  ar: {
    // Page & Section Headers
    apartments: 'الشقق والوحدات',
    addApartment: 'إضافة شقة',
    editApartment: 'تعديل الشقة',
    apartmentDetails: 'تفاصيل الشقة',
    building: 'العمارة',
    floor: 'الطابق',
    pageDescription: 'إدارة الشقق والوحدات العقارية حسب الطوابق.',
    createPageDescription: 'تسجيل وحدة شقة جديدة تحت طابق محدد.',
    editPageDescription: 'تحديث قيمة الإيجار الشخطي والشروط المالية للوحدة.',
    backToApartments: 'الرجوع إلى الشقق',
    
    // Form Sections
    unitInfo: 'بيانات الوحدة السكنية',
    ownershipInfo: 'تفاصيل الملكية',
    financialTerms: 'الشروط المالية والإيجار',
    
    // Fields
    unitNumber: 'رقم الشقة / الوحدة',
    areaSqm: 'المساحة (م²)',
    ownershipStatusTitle: 'نموذج الملكية',
    externalOwnerName: 'اسم المالِك الخارجي',
    externalOwnerPhone: 'رقم هاتف المالِك الخارجي',
    bedrooms: 'عدد غرف النوم',
    bathrooms: 'عدد دورات المياه',
    baseRentAmount: 'قيمة الإيجار الأساسي',
    baseRentCurrency: 'العملة',
    occupancyStatusTitle: 'حالة الإشغال',
    floorId: 'الطابق',
    buildingId: 'العمارة',
    
    // Form Placeholders
    unitNumberPlaceholder: 'مثال: شقة 101',
    areaSqmPlaceholder: 'مثال: 120.5',
    selectOwnershipPlaceholder: 'اختر نموذج الملكية',
    externalOwnerNamePlaceholder: 'مثال: أحمد المنصور',
    externalOwnerPhonePlaceholder: 'مثال: 0791234567',
    bedroomsPlaceholder: 'مثال: 2',
    bathroomsPlaceholder: 'مثال: 2',
    baseRentAmountPlaceholder: 'مثال: 450.00',
    floorIdPlaceholder: 'اختر الطابق',

    // Table Columns
    tableUnit: 'الوحدة',
    tableArea: 'المساحة',
    tableBedrooms: 'غرف النوم',
    tableBathrooms: 'الحمامات',
    tableBaseRent: 'الإيجار الأساسي',
    tableOwnership: 'الملكية',
    tableOccupancy: 'الحالة',
    searchPlaceholder: 'ابحث برقم الشقة، اسم العمارة، أو حالة الإشغال...',
    totalApartments: 'إجمالي الشقق',
    occupiedApartments: 'مشغولة',
    vacantApartments: 'شاغرة',
    unassignedBuilding: 'عمارة غير محددة',
    expandAll: 'توسيع الكل',
    collapseAll: 'طي الكل',

    // Actions & Buttons
    save: 'حفظ الشقة',
    saving: 'جارٍ الحفظ...',
    cancel: 'إلغاء',
    view: 'عرض',
    edit: 'تعديل',
    delete: 'أرشفة',
    deleting: 'جارٍ الأرشفة...',
    viewApartment: 'عرض تفاصيل الشقة',
    editApartmentTitle: 'تعديل الشقة',
    deleteApartmentTitle: 'أرشفة الشقة',
    
    // Status & Empty States
    active: 'نشط',
    inactive: 'مؤرشف',
    loadingData: 'جارٍ تحميل بيانات الشقة...',
    notFound: 'الشقة غير موجودة.',
    noApartments: 'لا توجد شقق مسجلة.',
    
    // Dialogs & Toasts
    createSuccess: 'تم إضافة الشقة بنجاح',
    updateSuccess: 'تم تحديث بيانات الشقة بنجاح',
    deleteSuccess: 'تم أرشفة الشقة بنجاح',
    deleteConfirmTitle: 'أرشفة الشقة',
    deleteConfirmMessage: 'هل أنت تأكد من رغبتك في أرشفة هذه الشقة؟ سيتم تغيير حالتها إلى غير نشط.',
    loadError: 'فشل في تحميل قائمة الشقق.',
    floorRequiredMessage: 'اختر الطابق لإضافة شقة جديدة.',

    // Enum Labels
    ownershipStatus: {
      companyOwned: 'ملكية الشركة',
      thirdPartyOwned: 'ملكية طرف ثالث',
    },
    occupancyStatus: {
      vacant: 'شاغرة',
      occupied: 'مشغولة',
      underMaintenance: 'قيد الصيانة',
      listed: 'معروضة للإيجار',
    },
  }
};

export function getApartmentTranslation(key: string, lang: 'en' | 'ar' = 'en'): string {
  const dict = dictionary[lang] || dictionary.en;
  const parts = key.split('.');
  let current: any = dict;
  for (const part of parts) {
    if (current && typeof current === 'object' && part in current) {
      current = current[part];
    } else {
      return key;
    }
  }
  return typeof current === 'string' ? current : key;
}
