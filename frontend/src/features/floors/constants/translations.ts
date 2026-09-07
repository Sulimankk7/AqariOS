import { translateLegacy } from '@/shared/i18n';

export const dictionary = {
  en: {
    unknown: 'Unknown',
    // Headers & Labels
    floors: 'Floors',
    addFloor: 'Add Floor',
    editFloor: 'Edit Floor',
    floorDetails: 'Floor Details',
    building: 'Building',
    createPageDescription: 'Register a new floor under this property building.',
    editPageDescription: 'Update floor designation label and floor type.',
    backToBuilding: 'Back to Building',
    backToFloor: 'Back to Floor Details',
    
    // Entity Fields
    floorNumber: 'Floor Number',
    floorLabel: 'Floor Label / Name',
    floorType: 'Floor Type',
    apartmentsCount: 'Apartments Units Count',
    
    // Placeholders
    numberPlaceholder: 'e.g. 1',
    labelPlaceholder: 'e.g. Ground Floor / Floor 1',
    selectTypePlaceholder: 'Select floor type',
    
    // Table Columns & Actions
    tableNumber: 'Floor #',
    tableLabel: 'Designation / Label',
    tableType: 'Floor Type',
    tableApartments: 'Units Count',
    searchPlaceholder: 'Search floors by label or number...',
    
    // Buttons & Dialogs
    save: 'Save Floor',
    saving: 'Saving...',
    cancel: 'Cancel',
    view: 'View',
    edit: 'Edit',
    delete: 'Archive',
    deleting: 'Archiving...',
    viewFloor: 'View floor details',
    editFloorTitle: 'Edit floor details',
    deleteFloorTitle: 'Archive floor',
    
    // Empty & Loading States
    loadingData: 'Loading floor data...',
    notFound: 'Floor not found.',
    noFloors: 'This building does not have any floors registered yet.',
    noApartmentsOnFloor: 'This floor does not contain any apartment units yet.',
    addFirstApartment: 'Add First Apartment',
    addFirstFloor: 'Add First Floor',
    
    // Toasts & Confirmations
    createSuccess: 'Floor created successfully',
    updateSuccess: 'Floor updated successfully',
    deleteSuccess: 'Floor archived successfully',
    numberExists: 'Floor number {number} already exists in this building.',
    deleteConfirmTitle: 'Archive Floor',
    deleteConfirmMessage: 'Are you sure you want to archive this floor? This action will mark the floor inactive and affect associated units.',

    // Enum Labels
    floorTypes: {
      standard: 'Standard Floor',
      mezzanine: 'Mezzanine',
      basement: 'Basement Level',
      penthouse: 'Penthouse',
      service: 'Service Floor',
    },
  },
  ar: {
    unknown: 'غير معروف',
    // Headers & Labels
    floors: 'الطوابق',
    addFloor: 'إضافة طابق',
    editFloor: 'تعديل الطابق',
    floorDetails: 'تفاصيل الطابق',
    building: 'المبنى',
    createPageDescription: 'تسجيل طابق جديد تحت هذا المبنى العقاري.',
    editPageDescription: 'تحديث المسمى والتصنيف الخاص بالطابق.',
    backToBuilding: 'الرجوع إلى المبنى',
    backToFloor: 'الرجوع إلى تفاصيل الطابق',
    
    // Entity Fields
    floorNumber: 'رقم الطابق',
    floorLabel: 'اسم / مسمى الطابق',
    floorType: 'نوع / تصنيف الطابق',
    apartmentsCount: 'عدد الشقق والوحدات',
    
    // Placeholders
    numberPlaceholder: 'مثال: 1',
    labelPlaceholder: 'مثال: الطابق الأرضي / الطابق الأول',
    selectTypePlaceholder: 'اختر تصنيف الطابق',
    
    // Table Columns & Actions
    tableNumber: 'رقم الطابق',
    tableLabel: 'المسمى',
    tableType: 'تصنيف الطابق',
    tableApartments: 'عدد الوحدات',
    searchPlaceholder: 'ابحث برقم أو مسمى الطابق...',
    
    // Buttons & Dialogs
    save: 'حفظ الطابق',
    saving: 'جارٍ الحفظ...',
    cancel: 'إلغاء',
    view: 'عرض',
    edit: 'تعديل',
    delete: 'أرشفة',
    deleting: 'جارٍ الأرشفة...',
    viewFloor: 'عرض تفاصيل الطابق',
    editFloorTitle: 'تعديل بيانات الطابق',
    deleteFloorTitle: 'أرشفة الطابق',
    
    // Empty & Loading States
    loadingData: 'جارٍ تحميل بيانات الطابق...',
    notFound: 'الطابق غير موجود.',
    noFloors: 'لا توجد طوابق مسجلة لهذا المبنى بعد.',
    noApartmentsOnFloor: 'لا توجد شقق مضافة في هذا الطابق بعد.',
    addFirstApartment: 'إضافة أول شقة',
    addFirstFloor: 'إضافة أول طابق',
    
    // Toasts & Confirmations
    createSuccess: 'تم إضافة الطابق بنجاح',
    updateSuccess: 'تم تحديث بيانات الطابق بنجاح',
    deleteSuccess: 'تم أرشفة الطابق بنجاح',
    numberExists: 'رقم الطابق {number} موجود مسبقًا في هذا المبنى.',
    deleteConfirmTitle: 'أرشفة الطابق',
    deleteConfirmMessage: 'هل أنت تأكد من رغبتك في أرشفة هذا الطابق؟ سيتم تغيير حالته إلى غير نشط.',

    // Enum Labels
    floorTypes: {
      standard: 'طابق متكرر / عادي',
      mezzanine: 'ميزانين',
      basement: 'طابق تسوية / قبو',
      penthouse: 'بنتهاوس',
      service: 'طابق خدمات',
    },
  }
};

export function getFloorTranslation(key: string, lang: 'en' | 'ar' = 'ar'): string {
  return translateLegacy('floors', dictionary, lang, key);
}
