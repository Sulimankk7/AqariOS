typedef TenantPortalJson = Map<String, dynamic>;

class TenantProfile {
  const TenantProfile({
    required this.id,
    required this.companyId,
    required this.name,
    required this.nationalId,
    required this.phone,
    required this.createdAt,
    required this.updatedAt,
    required this.familyMembers,
    required this.emergencyContacts,
    required this.vehicles,
    this.email,
    this.occupation,
    this.employer,
    this.userId,
    this.createdBy,
    this.updatedBy,
  });

  final String id;
  final String companyId;
  final String name;
  final String nationalId;
  final String phone;
  final String? email;
  final String? occupation;
  final String? employer;
  final String? userId;
  final String createdAt;
  final String updatedAt;
  final String? createdBy;
  final String? updatedBy;
  final List<TenantFamilyMember> familyMembers;
  final List<TenantEmergencyContact> emergencyContacts;
  final List<TenantVehicle> vehicles;

  factory TenantProfile.fromJson(TenantPortalJson json) => TenantProfile(
    id: _requiredText(json, 'id'),
    companyId: _requiredText(json, 'companyId'),
    name: _requiredText(json, 'name'),
    nationalId: _requiredText(json, 'nationalId'),
    phone: _requiredText(json, 'phone'),
    email: _optionalText(json, 'email'),
    occupation: _optionalText(json, 'occupation'),
    employer: _optionalText(json, 'employer'),
    userId: _optionalText(json, 'userId'),
    createdAt: _requiredText(json, 'createdAt'),
    updatedAt: _requiredText(json, 'updatedAt'),
    createdBy: _optionalText(json, 'createdBy'),
    updatedBy: _optionalText(json, 'updatedBy'),
    familyMembers: _objects(
      json,
      'familyMembers',
    ).map(TenantFamilyMember.fromJson).toList(growable: false),
    emergencyContacts: _objects(
      json,
      'emergencyContacts',
    ).map(TenantEmergencyContact.fromJson).toList(growable: false),
    vehicles: _objects(
      json,
      'vehicles',
    ).map(TenantVehicle.fromJson).toList(growable: false),
  );
}

class TenantFamilyMember {
  const TenantFamilyMember({
    required this.id,
    required this.name,
    required this.relationshipType,
    required this.createdAt,
    this.ageBracket,
  });

  final String id;
  final String name;
  final String relationshipType;
  final String? ageBracket;
  final String createdAt;

  factory TenantFamilyMember.fromJson(TenantPortalJson json) =>
      TenantFamilyMember(
        id: _requiredText(json, 'id'),
        name: _requiredText(json, 'name'),
        relationshipType: _requiredText(json, 'relationshipType'),
        ageBracket: _optionalText(json, 'ageBracket'),
        createdAt: _requiredText(json, 'createdAt'),
      );
}

class TenantEmergencyContact {
  const TenantEmergencyContact({
    required this.id,
    required this.name,
    required this.relationshipType,
    required this.phone,
    required this.createdAt,
  });

  final String id;
  final String name;
  final String relationshipType;
  final String phone;
  final String createdAt;

  factory TenantEmergencyContact.fromJson(TenantPortalJson json) =>
      TenantEmergencyContact(
        id: _requiredText(json, 'id'),
        name: _requiredText(json, 'name'),
        relationshipType: _requiredText(json, 'relationshipType'),
        phone: _requiredText(json, 'phone'),
        createdAt: _requiredText(json, 'createdAt'),
      );
}

class TenantVehicle {
  const TenantVehicle({
    required this.id,
    required this.plateNumber,
    required this.makeModel,
    required this.color,
    required this.createdAt,
  });

  final String id;
  final String plateNumber;
  final String makeModel;
  final String color;
  final String createdAt;

  factory TenantVehicle.fromJson(TenantPortalJson json) => TenantVehicle(
    id: _requiredText(json, 'id'),
    plateNumber: _requiredText(json, 'plateNumber'),
    makeModel: _requiredText(json, 'makeModel'),
    color: _requiredText(json, 'color'),
    createdAt: _requiredText(json, 'createdAt'),
  );
}

class TenantLease {
  const TenantLease({
    required this.id,
    required this.contractNumber,
    required this.startDate,
    required this.endDate,
    required this.monthlyRentAmount,
    required this.currency,
    required this.securityDepositAmount,
    required this.paymentFrequency,
    required this.paymentDueDay,
    required this.status,
    required this.legalRegime,
    required this.tenantType,
    required this.apartmentId,
    required this.apartmentUnitNumber,
    required this.apartmentBedrooms,
    required this.apartmentBathrooms,
    required this.apartmentAreaSqm,
    required this.buildingId,
    required this.buildingName,
    this.signedDate,
  });

  final String id;
  final String contractNumber;
  final String startDate;
  final String endDate;
  final String? signedDate;
  final num monthlyRentAmount;
  final String currency;
  final num securityDepositAmount;
  final String paymentFrequency;
  final int paymentDueDay;
  final String status;
  final String legalRegime;
  final String tenantType;
  final String apartmentId;
  final String apartmentUnitNumber;
  final int apartmentBedrooms;
  final int apartmentBathrooms;
  final num apartmentAreaSqm;
  final String buildingId;
  final String buildingName;

  factory TenantLease.fromJson(TenantPortalJson json) => TenantLease(
    id: _requiredText(json, 'id'),
    contractNumber: _requiredText(json, 'contractNumber'),
    startDate: _requiredText(json, 'startDate'),
    endDate: _requiredText(json, 'endDate'),
    signedDate: _optionalText(json, 'signedDate'),
    monthlyRentAmount: _requiredNumber(json, 'monthlyRentAmount'),
    currency: _requiredText(json, 'currency'),
    securityDepositAmount: _requiredNumber(json, 'securityDepositAmount'),
    paymentFrequency: _requiredText(json, 'paymentFrequency'),
    paymentDueDay: _requiredInteger(json, 'paymentDueDay'),
    status: _requiredText(json, 'status'),
    legalRegime: _requiredText(json, 'legalRegime'),
    tenantType: _requiredText(json, 'tenantType'),
    apartmentId: _requiredText(json, 'apartmentId'),
    apartmentUnitNumber: _requiredText(json, 'apartmentUnitNumber'),
    apartmentBedrooms: _requiredInteger(json, 'apartmentBedrooms'),
    apartmentBathrooms: _requiredInteger(json, 'apartmentBathrooms'),
    apartmentAreaSqm: _requiredNumber(json, 'apartmentAreaSqm'),
    buildingId: _requiredText(json, 'buildingId'),
    buildingName: _requiredText(json, 'buildingName'),
  );
}

String _requiredText(TenantPortalJson json, String key) {
  final value = json[key];
  if (value is! String) throw FormatException('Invalid $key');
  return value;
}

String? _optionalText(TenantPortalJson json, String key) {
  final value = json[key];
  if (value == null) return null;
  if (value is! String) throw FormatException('Invalid $key');
  return value;
}

num _requiredNumber(TenantPortalJson json, String key) {
  final value = json[key];
  if (value is! num) throw FormatException('Invalid $key');
  return value;
}

int _requiredInteger(TenantPortalJson json, String key) {
  final value = _requiredNumber(json, key);
  if (value != value.roundToDouble()) throw FormatException('Invalid $key');
  return value.toInt();
}

List<TenantPortalJson> _objects(TenantPortalJson json, String key) {
  final value = json[key];
  if (value is! List) throw FormatException('Invalid $key');
  return value
      .map((item) {
        if (item is! Map) throw FormatException('Invalid $key item');
        return Map<String, dynamic>.from(item);
      })
      .toList(growable: false);
}
