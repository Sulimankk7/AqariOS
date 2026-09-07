enum AqariRole { companyAdmin, tenant, systemAdmin, unsupported }

class UserCompanyRole {
  const UserCompanyRole({required this.companyId, required this.roleCode});
  final String companyId;
  final String roleCode;

  factory UserCompanyRole.fromJson(Map<String, dynamic> json) =>
      UserCompanyRole(
        companyId: json['companyId']?.toString() ?? '',
        roleCode: json['roleCode']?.toString() ?? '',
      );
}

class UserProfile {
  const UserProfile({
    required this.id,
    required this.fullName,
    required this.preferredLanguage,
    required this.companyRoles,
    required this.permissions,
    required this.systemRoles,
    this.email,
    this.phone,
    this.activeCompanyId,
  });

  final String id;
  final String fullName;
  final String preferredLanguage;
  final String? email;
  final String? phone;
  final String? activeCompanyId;
  final List<UserCompanyRole> companyRoles;
  final Set<String> permissions;
  final Set<String> systemRoles;

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
    id: json['id']?.toString() ?? '',
    fullName: json['fullName']?.toString() ?? '',
    preferredLanguage: json['preferredLanguage']?.toString() ?? 'ar',
    email: json['email']?.toString(),
    phone: json['phone']?.toString(),
    activeCompanyId: json['activeCompanyId']?.toString(),
    companyRoles: (json['companyRoles'] as List? ?? const [])
        .whereType<Map>()
        .map(
          (item) => UserCompanyRole.fromJson(Map<String, dynamic>.from(item)),
        )
        .toList(growable: false),
    permissions: (json['permissions'] as List? ?? const [])
        .map((item) => item.toString())
        .toSet(),
    systemRoles: (json['systemRoles'] as List? ?? const [])
        .map((item) => item.toString())
        .toSet(),
  );

  AqariRole get role {
    if (systemRoles.contains('SYSTEM_ADMIN')) return AqariRole.systemAdmin;
    bool supported(UserCompanyRole value) =>
        value.roleCode == 'COMPANY_ADMIN' || value.roleCode == 'TENANT';
    UserCompanyRole? selected;
    if (activeCompanyId != null) {
      for (final companyRole in companyRoles) {
        if (companyRole.companyId == activeCompanyId &&
            supported(companyRole)) {
          selected = companyRole;
          break;
        }
      }
    }
    selected ??= companyRoles.where(supported).firstOrNull;
    return switch (selected?.roleCode) {
      'COMPANY_ADMIN' => AqariRole.companyAdmin,
      'TENANT' => AqariRole.tenant,
      _ => AqariRole.unsupported,
    };
  }

  bool hasPermission(String permission) => permissions.contains(permission);
  bool hasAnyPermission(Iterable<String> values) =>
      values.any(permissions.contains);
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
