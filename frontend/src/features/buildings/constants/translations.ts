import { translateLegacy } from '@/shared/i18n';

export const dictionary = {
  en: {
    unknown: 'Unknown',
    // Navigation & Headers
    buildings: 'Buildings',
    addBuilding: 'Add Building',
    editBuilding: 'Edit Building',
    buildingDetails: 'Building Details',
    pageDescription: 'Manage your property buildings and locations.',
    createPageDescription: 'Add a new building to your property portfolio.',
    editPageDescription: 'Update building information and coordinates.',
    backToBuildings: 'Back to Buildings',
    
    // Form Sections
    generalInfo: 'General Information',
    locationAndAddress: 'Location & Address',
    gpsAndMapTitle: 'GPS Coordinates & Map Location',
    required: '(required)',
    optional: '(optional)',
    
    // Entity Fields
    name: 'Building Name',
    internalCode: 'Internal Code',
    buildingType: 'Building Type',
    totalFloors: 'Licensed Floors',
    managedFloors: 'Managed Floors',
    licensedFloorsDescription: 'Number of floors according to the building permit or approved plan. This does not limit the floors managed in the system.',
    constructionYear: 'Construction Year',
    governorate: 'Governorate',
    district: 'District',
    area: 'Area',
    streetName: 'Street Name',
    postalCode: 'Postal Code',
    gpsLatitude: 'GPS Latitude',
    gpsLongitude: 'GPS Longitude',

    // Form Placeholders
    namePlaceholder: 'e.g. Al-Noor Tower',
    codePlaceholder: 'e.g. BLD-001',
    selectTypePlaceholder: 'Select building type',
    floorsPlaceholder: 'e.g. 10',
    yearPlaceholder: 'e.g. 2022',
    selectGovernoratePlaceholder: 'Select governorate',
    districtPlaceholder: 'e.g. Abdoun',
    areaPlaceholder: 'e.g. 5th Circle',
    streetPlaceholder: 'e.g. Zahran Street',
    postalCodePlaceholder: 'e.g. 11183',
    gpsAutoFilledPlaceholder: 'Auto-filled from map or manual',
    geocodingLoading: 'Finding a suggested address…',
    geocodingFailure: 'The address could not be determined automatically. You can enter it manually.',
    suggestedAddress: 'Suggested address',
    useSuggestedAddress: 'Use suggested address',
    addressDetailsManual: 'The map could not determine additional structured details. Complete the address fields manually.',
    coordinatesDetails: 'Coordinates',

    // Table Columns
    tableCode: 'Code',
    tableFloors: 'Licensed Floors',
    tableApartments: 'Apartments',
    tableLocation: 'Location',
    searchPlaceholder: 'Search buildings by name or code...',

    // Actions & Buttons
    save: 'Save Building',
    saving: 'Saving...',
    cancel: 'Cancel',
    view: 'View',
    edit: 'Edit',
    delete: 'Delete',
    deleting: 'Deleting...',
    viewBuilding: 'View building details',
    editBuildingTitle: 'Edit building',
    deleteBuildingTitle: 'Delete building',
    
    // Status & Empty States
    active: 'Active',
    inactive: 'Inactive',
    loadingData: 'Loading building data...',
    noAddress: 'No address provided',
    locationMap: 'Location Map',
    noGpsRecorded: 'No GPS coordinates recorded',
    editGpsInstruction: 'Edit this building to select coordinates on map.',
    totalCount: 'Total',
    noApartments: 'No apartments added to this building yet.',
    manageApartments: 'Manage Apartments',
    noLeases: 'No active lease contracts recorded.',
    viewLeases: 'View Leases',
    noFinancialOps: 'No financial operations recorded.',
    viewFinancialOps: 'View Financial Operations',
    noParking: 'No parking spots assigned to this building yet.',
    manageParking: 'Manage Parking',
    
    // Dialogs & Messages
    createSuccess: 'Building created successfully',
    updateSuccess: 'Building updated successfully',
    deleteSuccess: 'Building deleted successfully',
    deleteConfirmTitle: 'Delete Building',
    deleteConfirmMessage: 'Are you sure you want to delete this building? This action cannot be undone and will permanently remove this building along with any associated records.',
    loadError: 'Failed to load buildings list.',
    notFound: 'Building not found.',
    
    // Enums
    buildingTypes: {
      residential: 'Residential',
      commercial: 'Commercial',
      mixedUse: 'Mixed Use',
    },
    governorates: {
      amman: 'Amman',
      zarqa: 'Zarqa',
      irbid: 'Irbid',
      balqa: 'Balqa',
      madaba: 'Madaba',
      karak: 'Karak',
      tafilah: 'Tafilah',
      maan: "Ma'an",
      aqaba: 'Aqaba',
      ajloun: 'Ajloun',
      jerash: 'Jerash',
      mafraq: 'Mafraq',
    },
  },
  ar: {
    unknown: 'غير معروف',
    // Navigation & Headers
    buildings: 'المباني',
    addBuilding: 'إضافة مبنى',
    editBuilding: 'تعديل مبنى',
    buildingDetails: 'تفاصيل المبنى',
    pageDescription: 'إدارة مباني عقاراتك ومواقعها بسهولة.',
    createPageDescription: 'إضافة مبنى جديد إلى النظام.',
    editPageDescription: 'تحديث بيانات المبنى وإحداثيات الموقع.',
    backToBuildings: 'الرجوع إلى المباني',
    
    // Form Sections
    generalInfo: 'المعلومات الأساسية',
    locationAndAddress: 'الموقع والعنوان',
    gpsAndMapTitle: 'إحداثيات الموقع على الخريطة',
    required: '(مطلوب)',
    optional: '(اختياري)',
    
    // Entity Fields
    name: 'اسم المبنى',
    internalCode: 'الرمز الداخلي',
    buildingType: 'نوع المبنى',
    totalFloors: 'طوابق الترخيص',
    managedFloors: 'الطوابق المُدارة',
    licensedFloorsDescription: 'عدد الطوابق حسب الترخيص أو المخطط المعتمد، ولا يحدد عدد الطوابق التي تتم إدارتها فعليًا في النظام.',
    constructionYear: 'سنة الإنشاء',
    governorate: 'المحافظة',
    district: 'المدينة',
    area: 'المنطقة',
    streetName: 'اسم الشارع',
    postalCode: 'الرمز البريدي',
    gpsLatitude: 'خط العرض',
    gpsLongitude: 'خط الطول',

    // Form Placeholders
    namePlaceholder: 'مثال: برج النور',
    codePlaceholder: 'مثال: BLD-001',
    selectTypePlaceholder: 'اختر نوع المبنى',
    floorsPlaceholder: 'مثال: 10',
    yearPlaceholder: 'مثال: 2022',
    selectGovernoratePlaceholder: 'اختر المحافظة',
    districtPlaceholder: 'مثال: عبدون',
    areaPlaceholder: 'مثال: الدوار الخامس',
    streetPlaceholder: 'مثال: شارع زهران',
    postalCodePlaceholder: 'مثال: 11183',
    gpsAutoFilledPlaceholder: 'تحديد تلقائي من الخريطة أو يدوي',
    geocodingLoading: 'جارٍ تحديد العنوان المقترح…',
    geocodingFailure: 'تعذر تحديد العنوان تلقائيًا. يمكنك إدخال العنوان يدويًا.',
    suggestedAddress: 'العنوان المقترح من الخريطة',
    useSuggestedAddress: 'استخدام العنوان المقترح',
    addressDetailsManual: 'تعذر تحديد تفاصيل إضافية موثوقة من الخريطة. يرجى إكمال حقول العنوان يدويًا.',
    coordinatesDetails: 'الإحداثيات',

    // Table Columns
    tableCode: 'الرمز',
    tableFloors: 'طوابق الترخيص',
    tableApartments: 'الشقق',
    tableLocation: 'الموقع',
    searchPlaceholder: 'ابحث باسم المبنى أو الرمز الداخلي...',

    // Actions & Buttons
    save: 'حفظ المبنى',
    saving: 'جارٍ الحفظ...',
    cancel: 'إلغاء',
    view: 'عرض',
    edit: 'تعديل',
    delete: 'حذف',
    deleting: 'جارٍ الحذف...',
    viewBuilding: 'عرض تفاصيل المبنى',
    editBuildingTitle: 'تعديل المبنى',
    deleteBuildingTitle: 'حذف المبنى',
    
    // Status & Empty States
    active: 'نشط',
    inactive: 'غير نشط',
    loadingData: 'جارٍ تحميل بيانات المبنى...',
    noAddress: 'لم يتم إدخال العنوان',
    locationMap: 'خريطة الموقع',
    noGpsRecorded: 'لم يتم تسجيل إحداثيات الموقع',
    editGpsInstruction: 'قم بتعديل المبنى لتحديد الموقع على الخريطة.',
    totalCount: 'الإجمالي',
    noApartments: 'لا توجد شقق مضافة لهذا المبنى بعد.',
    manageApartments: 'إدارة الشقق',
    noLeases: 'لا توجد عقود إيجار نشطة مسجلة.',
    viewLeases: 'عرض العقود',
    noFinancialOps: 'لا توجد عمليات مالية مسجلة.',
    viewFinancialOps: 'عرض العمليات المالية',
    noParking: 'لا توجد مواقف سيارات مخصصة لهذا المبنى بعد.',
    manageParking: 'إدارة المواقف',
    
    // Dialogs & Messages
    createSuccess: 'تم إضافة المبنى بنجاح',
    updateSuccess: 'تم تحديث بيانات المبنى بنجاح',
    deleteSuccess: 'تم حذف المبنى بنجاح',
    deleteConfirmTitle: 'حذف المبنى',
    deleteConfirmMessage: 'هل أنت تأكد من رغبتك في حذف هذا المبنى؟ لا يمكن التراجع عن هذا الإجراء وسيتم حذف جميع البيانات المرتبطة به.',
    loadError: 'فشل في تحميل قائمة المباني.',
    notFound: 'المبنى غير موجود.',
    
    // Enums
    buildingTypes: {
      residential: 'سكني',
      commercial: 'تجاري',
      mixedUse: 'متعدد الاستخدامات',
    },
    governorates: {
      amman: 'عمان',
      zarqa: 'الزرقاء',
      irbid: 'إربد',
      balqa: 'البلقاء',
      madaba: 'مأدبا',
      karak: 'الكرك',
      tafilah: 'الطفيلة',
      maan: 'معان',
      aqaba: 'العقبة',
      ajloun: 'عجلون',
      jerash: 'جرش',
      mafraq: 'المفرق',
    },
  }
};

/**
 * Resolves a translation key (e.g. "buildings", "buildingTypes.residential") for current locale.
 */
export function getBuildingTranslation(key: string, lang: 'en' | 'ar' = 'ar'): string {
  return translateLegacy('buildings', dictionary, lang, key);
}

export const buildingTranslations = dictionary.en;
