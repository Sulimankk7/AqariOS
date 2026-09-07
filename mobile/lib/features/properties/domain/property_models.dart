typedef Json = Map<String, dynamic>;

int enumValue(Object? value, List<String> names) {
  if (value is int) return value;
  final text = value.toString().replaceAll('_', '').toLowerCase();
  return names.indexWhere((name) => name.toLowerCase() == text);
}

sealed class PropertyRecord {
  const PropertyRecord(this.id);
  final String id;
  String get title;
  String get searchText;
}

class BuildingAddress {
  BuildingAddress.fromJson(Json j)
    : governorate = enumValue(j['governorate'], governorateNames),
      city = j['district'] as String,
      neighborhood = j['area'] as String?,
      street = j['streetName'] as String?,
      postalCode = j['postalCode'] as String?;
  final int governorate;
  final String city;
  final String? neighborhood, street, postalCode;
  String get display => [
    city,
    neighborhood,
    street,
  ].whereType<String>().where((s) => s.isNotEmpty).join('، ');
}

class Building extends PropertyRecord {
  Building.fromJson(Json j)
    : name = j['name'] as String,
      code = j['internalCode'] as String?,
      type = enumValue(j['buildingType'], [
        'Residential',
        'Commercial',
        'MixedUse',
      ]),
      totalFloors = (j['totalFloors'] as num).toInt(),
      units = (j['totalApartmentsCount'] as num).toInt(),
      active = j['isActive'] as bool,
      year = (j['constructionYear'] as num?)?.toInt(),
      latitude = (j['gpsLatitude'] as num?)?.toDouble(),
      longitude = (j['gpsLongitude'] as num?)?.toDouble(),
      address = j['address'] == null
          ? null
          : BuildingAddress.fromJson(j['address'] as Json),
      super(j['id'] as String);
  final String name;
  final String? code;
  final int type, totalFloors, units;
  final int? year;
  final double? latitude, longitude;
  final bool active;
  final BuildingAddress? address;
  @override
  String get title => name;
  @override
  String get searchText => '$name ${code ?? ''} ${address?.display ?? ''}';
}

class Floor extends PropertyRecord {
  Floor.fromJson(Json j)
    : buildingId = j['buildingId'] as String,
      label = j['floorLabel'] as String,
      number = (j['floorNumber'] as num).toInt(),
      units = (j['apartmentsCount'] as num).toInt(),
      type = enumValue(j['floorType'], [
        'Basement',
        'Ground',
        'Regular',
        'Roof',
      ]),
      super(j['id'] as String);
  final String buildingId, label;
  final int number, units, type;
  @override
  String get title => label;
  @override
  String get searchText => '$label $number';
}

class Apartment extends PropertyRecord {
  Apartment.fromJson(Json j)
    : buildingId = j['buildingId'] as String,
      floorId = j['floorId'] as String,
      number = j['unitNumber'] as String,
      occupancy = enumValue(j['occupancyStatus'], [
        'Vacant',
        'Occupied',
        'UnderMaintenance',
        'Listed',
      ]),
      ownership = enumValue(j['ownershipStatus'], [
        'CompanyOwned',
        'ThirdPartyOwned',
      ]),
      owner = j['externalOwnerName'] as String?,
      ownerPhone = j['externalOwnerPhone'] as String?,
      area = (j['areaSqm'] as num).toDouble(),
      bedrooms = (j['bedrooms'] as num).toInt(),
      bathrooms = (j['bathrooms'] as num).toInt(),
      rent = (j['baseRentAmount'] as num?)?.toDouble(),
      currency = j['baseRentCurrency'] as String,
      active = j['isActive'] as bool,
      super(j['id'] as String);
  final String buildingId, floorId, number, currency;
  final String? owner, ownerPhone;
  final int occupancy, ownership, bedrooms, bathrooms;
  final double area;
  final double? rent;
  final bool active;
  @override
  String get title => number;
  @override
  String get searchText => number;
}

const governorateNames = [
  'Amman',
  'Zarqa',
  'Irbid',
  'Balqa',
  'Madaba',
  'Karak',
  'Tafilah',
  'Maan',
  'Aqaba',
  'Ajloun',
  'Jerash',
  'Mafraq',
];

enum PropertyKind { buildings, floors, apartments }
