typedef LeaseJson = Map<String, dynamic>;

int leaseEnum(Object? value, List<String> names) => value is num
    ? value.toInt()
    : names.indexWhere((name) => name.toLowerCase() == '$value'.toLowerCase());
const leaseStatuses = [
  'Draft',
  'PendingSignature',
  'Active',
  'Expired',
  'Renewed',
  'Terminated',
  'Cancelled',
  'Superseded',
];
const documentTypes = [
  'SignedContract',
  'NationalIdCopy',
  'Passport',
  'IncomeProof',
  'Other',
];
const frequencies = ['Monthly', 'Quarterly', 'SemiAnnual', 'Annual'];
const legalRegimes = ['Standard', 'OldRentLaw'];
const tenantTypes = ['Personal', 'Corporate'];
const terminationTypes = [
  'NormalExpiration',
  'EarlyTermination',
  'MutualAgreement',
  'TenantRequest',
  'OwnerRequest',
  'LegalEviction',
];

class Lease {
  Lease.fromJson(LeaseJson json)
    : raw = Map.unmodifiable(json),
      id = json['id'] as String,
      number = json['contractNumber'] as String,
      companyId = json['companyId'] as String,
      buildingId = json['buildingId'] as String,
      apartmentId = json['apartmentId'] as String,
      tenantId = json['tenantId'] as String,
      priorId = json['priorContractId'] as String?,
      start = json['startDate'] as String,
      end = json['endDate'] as String,
      signed = json['signedDate'] as String?,
      rent = (json['monthlyRentAmount'] as num).toDouble(),
      deposit = (json['securityDepositAmount'] as num).toDouble(),
      currency = json['currency'] as String? ?? 'JOD',
      dueDay = (json['paymentDueDay'] as num).toInt(),
      frequency = leaseEnum(json['paymentFrequency'], frequencies),
      regime = leaseEnum(json['legalRegime'], legalRegimes),
      tenantType = leaseEnum(json['tenantType'], tenantTypes),
      status = leaseEnum(json['status'], leaseStatuses),
      notes = json['notes'] as String?,
      documents = (json['documents'] as List? ?? [])
          .map((v) => LeaseDocument.fromJson(v as LeaseJson))
          .toList(growable: false),
      history = (json['statusHistory'] as List? ?? [])
          .map((v) => LeaseStatusEvent.fromJson(v as LeaseJson))
          .toList(growable: false),
      termination = json['termination'] == null
          ? null
          : LeaseSettlement.fromJson(json['termination'] as LeaseJson);
  final LeaseJson raw;
  final String id,
      number,
      companyId,
      buildingId,
      apartmentId,
      tenantId,
      start,
      end,
      currency;
  final String? priorId, signed, notes;
  final double rent, deposit;
  final int dueDay, frequency, regime, tenantType, status;
  final List<LeaseDocument> documents;
  final List<LeaseStatusEvent> history;
  final LeaseSettlement? termination;
  // Web DataTable searches every scalar value in the returned row.
  String get searchText => raw.values
      .map(
        (v) => v == null
            ? ''
            : v is num && v == v.roundToDouble()
            ? '${v.toInt()}'
            : '$v',
      )
      .join(' ')
      .toLowerCase();
}

class LeaseDocument {
  LeaseDocument.fromJson(LeaseJson j)
    : id = j['id'] as String,
      fileId = j['fileId'] as String,
      type = leaseEnum(j['documentType'], documentTypes),
      description = j['description'] as String?,
      filename = j['originalFilename'] as String?,
      mimeType = j['mimeType'] as String?,
      size = (j['sizeBytes'] as num).toInt(),
      created = j['createdAt'] as String;
  final String id, fileId, created;
  final String? description, filename, mimeType;
  final int type, size;
}

class LeaseStatusEvent {
  LeaseStatusEvent.fromJson(LeaseJson j)
    : id = j['id'] as String,
      to = leaseEnum(j['toStatus'] ?? j['newStatus'], leaseStatuses),
      from = leaseEnum(j['fromStatus'] ?? j['previousStatus'], leaseStatuses),
      at = j['changedAt'] as String,
      reason = j['reason'] as String?;
  final String id, at;
  final String? reason;
  final int from, to;
}

class LeaseSettlement {
  LeaseSettlement.fromJson(LeaseJson j)
    : type = leaseEnum(j['terminationType'], terminationTypes),
      date = j['terminationDate'] as String,
      balance = (j['outstandingBalance'] as num).toDouble(),
      returned = (j['depositReturnedAmount'] as num).toDouble(),
      deducted = (j['depositDeductionAmount'] as num).toDouble(),
      currency = j['currency'] as String,
      settled = j['finalUtilitySettlementCompleted'] as bool,
      reason = j['reason'] as String?,
      deductionReason = j['depositDeductionReason'] as String?,
      notes = j['notes'] as String?;
  final int type;
  final String date, currency;
  final double balance, returned, deducted;
  final bool settled;
  final String? reason, deductionReason, notes;
}

class LeaseTenant {
  LeaseTenant.fromJson(LeaseJson j)
    : id = j['id'] as String,
      name = j['name'] as String,
      phone = j['phone'] as String? ?? '',
      nationalId = j['nationalId'] as String? ?? '';
  final String id, name, phone, nationalId;
  String get label => '$name (${phone.isNotEmpty ? phone : nationalId})';
}

class TenantRecord {
  TenantRecord.fromJson(LeaseJson json)
    : raw = Map.unmodifiable(json),
      id = json['id']?.toString() ?? '',
      name = json['name']?.toString() ?? '',
      nationalId = json['nationalId']?.toString() ?? '',
      phone = json['phone']?.toString() ?? '',
      email = json['email']?.toString(),
      occupation = json['occupation']?.toString(),
      employer = json['employer']?.toString(),
      userId = json['userId']?.toString(),
      familyMembers = (json['familyMembers'] as List? ?? const [])
          .whereType<Map>()
          .map(
            (value) =>
                TenantFamilyMember.fromJson(Map<String, dynamic>.from(value)),
          )
          .toList(growable: false),
      emergencyContacts = (json['emergencyContacts'] as List? ?? const [])
          .whereType<Map>()
          .map(
            (value) => TenantEmergencyContact.fromJson(
              Map<String, dynamic>.from(value),
            ),
          )
          .toList(growable: false),
      vehicles = (json['vehicles'] as List? ?? const [])
          .whereType<Map>()
          .map(
            (value) => TenantVehicle.fromJson(Map<String, dynamic>.from(value)),
          )
          .toList(growable: false);

  final LeaseJson raw;
  final String id, name, nationalId, phone;
  final String? email, occupation, employer, userId;
  final List<TenantFamilyMember> familyMembers;
  final List<TenantEmergencyContact> emergencyContacts;
  final List<TenantVehicle> vehicles;
}

class TenantFamilyMember {
  TenantFamilyMember.fromJson(LeaseJson json)
    : id = json['id']?.toString() ?? '',
      name = json['name']?.toString() ?? '',
      relationshipType = json['relationshipType']?.toString() ?? '',
      ageBracket = json['ageBracket']?.toString();
  final String id, name, relationshipType;
  final String? ageBracket;
}

class TenantEmergencyContact {
  TenantEmergencyContact.fromJson(LeaseJson json)
    : id = json['id']?.toString() ?? '',
      name = json['name']?.toString() ?? '',
      relationshipType = json['relationshipType']?.toString() ?? '',
      phone = json['phone']?.toString() ?? '';
  final String id, name, relationshipType, phone;
}

class TenantVehicle {
  TenantVehicle.fromJson(LeaseJson json)
    : id = json['id']?.toString() ?? '',
      plateNumber = json['plateNumber']?.toString() ?? '',
      makeModel = json['makeModel']?.toString() ?? '',
      color = json['color']?.toString() ?? '';
  final String id, plateNumber, makeModel, color;
}

class LeaseParkingSpot {
  LeaseParkingSpot.fromJson(LeaseJson json)
    : code = json['parkingSpotCode']?.toString() ?? '',
      type = json['parkingType']?.toString() ?? '';

  final String code;
  final String type;
}

class UtilityAccount {
  UtilityAccount.fromJson(LeaseJson j)
    : raw = Map.unmodifiable(j),
      id = j['id'] as String,
      type = leaseEnum(j['utilityType'], ['Electricity', 'Water']),
      number = j['accountNumber'] as String,
      meter = j['meterNumber'] as String?,
      active = j['isActive'] as bool? ?? j['unlinkedAt'] == null,
      leaseId = j['leaseContractId'] as String?,
      leaseNumber = j['leaseContractNumber'] as String?,
      tenantName = j['tenantName'] as String?,
      unitNumber = j['unitNumber'] as String?,
      buildingName = j['buildingName'] as String?,
      lastSuccessfulSyncAt = j['lastSuccessfulSyncAt'] as String?,
      unlinkedAt = j['unlinkedAt'] as String?,
      syncStatus = leaseEnum(j['syncStatus'], [
        'NeverSynced',
        'Syncing',
        'Synced',
        'ProviderError',
        'RateLimited',
        'Timeout',
        'Suspended',
        'InvalidAccount',
      ]);
  final LeaseJson raw;
  final String id, number;
  final String? meter,
      leaseId,
      leaseNumber,
      tenantName,
      unitNumber,
      buildingName,
      lastSuccessfulSyncAt,
      unlinkedAt;
  final bool active;
  final int type, syncStatus;
}

class UtilityBill {
  UtilityBill.fromJson(LeaseJson json)
    : id = json['id'] as String,
      billDate = json['billDate'] as String,
      dueDate = json['dueDate'] as String?,
      amount = (json['amount'] as num).toDouble(),
      currency = json['currency'] as String? ?? 'JOD',
      paid = json['isPaid'] as bool? ?? false,
      paymentStatus = leaseEnum(json['paymentStatus'], [
        'Unpaid',
        'Paid',
        'Unknown',
      ]);

  final String id, billDate, currency;
  final String? dueDate;
  final double amount;
  final bool paid;
  final int paymentStatus;
}

class UtilityBillPage {
  UtilityBillPage.fromJson(LeaseJson json)
    : items = (json['items'] as List? ?? const [])
          .map((value) => UtilityBill.fromJson(value as LeaseJson))
          .toList(growable: false),
      nextCursor = json['nextCursor'] as String?,
      hasMore = json['hasMore'] as bool? ?? false;

  final List<UtilityBill> items;
  final String? nextCursor;
  final bool hasMore;
}

class UtilityPage {
  UtilityPage.fromJson(LeaseJson j)
    : items = (j['items'] as List)
          .map((v) => UtilityAccount.fromJson(v as LeaseJson))
          .toList(growable: false),
      nextCursor = j['nextCursor'] as String?,
      hasMore = j['hasMore'] as bool;
  final List<UtilityAccount> items;
  final String? nextCursor;
  final bool hasMore;
}
