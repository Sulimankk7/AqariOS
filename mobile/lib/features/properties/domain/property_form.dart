import 'property_models.dart';

String normalizeNumber(String value) {
  var result = value.trim().replaceAll('٫', '.');
  for (var i = 0; i < 10; i++) {
    result = result
        .replaceAll('٠١٢٣٤٥٦٧٨٩'[i], '$i')
        .replaceAll('۰۱۲۳۴۵۶۷۸۹'[i], '$i');
  }
  return result;
}

class PropertyField {
  const PropertyField(
    this.key,
    this.ar,
    this.en, {
    this.required = false,
    this.maxLength,
    this.min,
    this.max,
    this.integer = false,
    this.ltr = false,
  });
  final String key, ar, en;
  final bool required, integer, ltr;
  final int? maxLength;
  final double? min, max;
  bool get numeric => min != null;
  String? validate(String text, bool arabic) {
    final value = text.trim();
    if (value.isEmpty) {
      return required
          ? (arabic ? 'هذا الحقل مطلوب.' : 'This field is required.')
          : null;
    }
    if (maxLength != null && value.length > maxLength!) {
      return arabic
          ? 'الحد الأقصى $maxLength حرفاً.'
          : 'Maximum $maxLength characters.';
    }
    if (numeric) {
      final number = num.tryParse(normalizeNumber(value));
      if (number == null ||
          !number.isFinite ||
          number < min! ||
          (max != null && number > max!) ||
          (integer && number != number.roundToDouble())) {
        return arabic
            ? 'أدخل ${integer ? 'عدداً صحيحاً' : 'رقماً'} بين $min و$max.'
            : 'Enter ${integer ? 'an integer' : 'a number'} between $min and $max.';
      }
    }
    return null;
  }
}

List<PropertyField> propertyFields(
  PropertyKind kind, {
  required bool editing,
  required int ownership,
}) => switch (kind) {
  PropertyKind.buildings => [
    const PropertyField(
      'name',
      'اسم المبنى',
      'Building name',
      required: true,
      maxLength: 100,
    ),
    const PropertyField(
      'internalCode',
      'الرمز الداخلي',
      'Internal code',
      maxLength: 20,
      ltr: true,
    ),
    if (!editing)
      const PropertyField(
        'totalFloors',
        'عدد الطوابق المعلن',
        'Declared floor count',
        required: true,
        min: 1,
        max: 200,
        integer: true,
      ),
    const PropertyField(
      'addressCity',
      'المدينة',
      'City',
      required: true,
      maxLength: 50,
    ),
    const PropertyField(
      'addressNeighborhood',
      'الحي',
      'Neighborhood',
      required: true,
      maxLength: 50,
    ),
    const PropertyField('addressStreet', 'الشارع', 'Street', maxLength: 100),
    const PropertyField(
      'addressPostalCode',
      'الرمز البريدي',
      'Postal code',
      maxLength: 20,
      ltr: true,
    ),
    const PropertyField(
      'constructionYear',
      'سنة البناء (اختياري)',
      'Construction year (optional)',
      min: 1800,
      max: 32767,
      integer: true,
    ),
    const PropertyField(
      'gpsLatitude',
      'خط العرض (اختياري)',
      'Latitude (optional)',
      min: -90,
      max: 90,
    ),
    const PropertyField(
      'gpsLongitude',
      'خط الطول (اختياري)',
      'Longitude (optional)',
      min: -180,
      max: 180,
    ),
  ],
  PropertyKind.floors => [
    if (!editing)
      const PropertyField(
        'floorNumber',
        'رقم الطابق',
        'Floor number',
        required: true,
        min: -50,
        max: 200,
        integer: true,
      ),
    const PropertyField(
      'floorLabel',
      'اسم الطابق',
      'Floor label',
      required: true,
      maxLength: 50,
    ),
  ],
  PropertyKind.apartments => [
    if (!editing) ...[
      const PropertyField(
        'unitNumber',
        'رقم الوحدة',
        'Unit number',
        required: true,
        maxLength: 20,
        ltr: true,
      ),
      const PropertyField(
        'areaSqm',
        'المساحة (م²)',
        'Area (m²)',
        required: true,
        min: .01,
        max: 10000,
      ),
      const PropertyField(
        'bedrooms',
        'غرف النوم',
        'Bedrooms',
        required: true,
        min: 0,
        max: 100,
        integer: true,
      ),
      const PropertyField(
        'bathrooms',
        'الحمامات',
        'Bathrooms',
        required: true,
        min: 0,
        max: 100,
        integer: true,
      ),
      if (ownership == 1) ...[
        const PropertyField(
          'externalOwnerName',
          'اسم المالك',
          'Owner name',
          required: true,
          maxLength: 255,
        ),
        const PropertyField(
          'externalOwnerPhone',
          'هاتف المالك',
          'Owner phone',
          maxLength: 20,
          ltr: true,
        ),
      ],
    ],
    const PropertyField(
      'baseRentAmount',
      'الإيجار الأساسي (اختياري)',
      'Base rent (optional)',
      min: .001,
      max: 999999999.999,
    ),
  ],
};

Map<String, String> initialPropertyValues(PropertyRecord? record) =>
    switch (record) {
      Building b => {
        'name': b.name,
        'internalCode': b.code ?? '',
        'totalFloors': '${b.totalFloors}',
        'addressCity': b.address?.city ?? '',
        'addressNeighborhood': b.address?.neighborhood ?? '',
        'addressStreet': b.address?.street ?? '',
        'addressPostalCode': b.address?.postalCode ?? '',
        'constructionYear': b.year?.toString() ?? '',
        'gpsLatitude': b.latitude?.toString() ?? '',
        'gpsLongitude': b.longitude?.toString() ?? '',
      },
      Floor f => {'floorNumber': '${f.number}', 'floorLabel': f.label},
      Apartment a => {'baseRentAmount': a.rent?.toString() ?? ''},
      null => {'bedrooms': '0', 'bathrooms': '0'},
    };

String? _optional(Map<String, String> v, String key) =>
    v[key]?.trim().isNotEmpty == true ? v[key]!.trim() : null;
double? _number(Map<String, String> v, String key) =>
    _optional(v, key) == null ? null : double.parse(normalizeNumber(v[key]!));

/// Explicit write DTOs: never serialize a response entity or a company ID.
sealed class PropertyWriteRequest {
  Json toJson();
}

class BuildingWriteRequest extends PropertyWriteRequest {
  BuildingWriteRequest(
    Map<String, String> values, {
    required this.type,
    required this.governorate,
    required bool editing,
  }) : name = values['name']!.trim(),
       code = _optional(values, 'internalCode'),
       totalFloors = editing ? null : _number(values, 'totalFloors')!.toInt(),
       year = _number(values, 'constructionYear')?.toInt(),
       latitude = _number(values, 'gpsLatitude'),
       longitude = _number(values, 'gpsLongitude'),
       city = values['addressCity']!.trim(),
       neighborhood = values['addressNeighborhood']!.trim(),
       street = _optional(values, 'addressStreet'),
       postalCode = _optional(values, 'addressPostalCode');
  final String name, city, neighborhood;
  final String? code, street, postalCode;
  final int type, governorate;
  final int? totalFloors, year;
  final double? latitude, longitude;
  @override
  Json toJson() => {
    'name': name,
    'internalCode': code,
    'buildingType': type,
    if (totalFloors != null) 'totalFloors': totalFloors,
    'constructionYear': year,
    'gpsLatitude': latitude,
    'gpsLongitude': longitude,
    'addressGovernorate': governorate,
    'addressCity': city,
    'addressNeighborhood': neighborhood,
    'addressStreet': street,
    'addressPostalCode': postalCode,
  };
}

class FloorWriteRequest extends PropertyWriteRequest {
  FloorWriteRequest(
    Map<String, String> values, {
    required this.type,
    required bool editing,
  }) : label = values['floorLabel']!.trim(),
       number = editing ? null : _number(values, 'floorNumber')!.toInt();
  final String label;
  final int type;
  final int? number;
  @override
  Json toJson() => {
    'floorLabel': label,
    'floorType': type,
    if (number != null) 'floorNumber': number,
  };
}

class ApartmentWriteRequest extends PropertyWriteRequest {
  ApartmentWriteRequest(
    Map<String, String> values, {
    required this.editing,
    required this.ownership,
    required this.currency,
  }) : number = editing ? null : values['unitNumber']!.trim(),
       area = editing ? null : _number(values, 'areaSqm'),
       bedrooms = editing ? null : _number(values, 'bedrooms')!.toInt(),
       bathrooms = editing ? null : _number(values, 'bathrooms')!.toInt(),
       rent = _number(values, 'baseRentAmount'),
       owner = ownership == 1 ? _optional(values, 'externalOwnerName') : null,
       phone = ownership == 1 ? _optional(values, 'externalOwnerPhone') : null;
  final bool editing;
  final int ownership;
  final String currency;
  final String? number, owner, phone;
  final int? bedrooms, bathrooms;
  final double? area, rent;
  @override
  Json toJson() => {
    if (!editing) ...{
      'unitNumber': number,
      'areaSqm': area,
      'bedrooms': bedrooms,
      'bathrooms': bathrooms,
      'ownershipStatus': ownership,
      'externalOwnerName': owner,
      'externalOwnerPhone': phone,
    },
    'baseRentAmount': rent,
    'baseRentCurrency': currency,
  };
}
