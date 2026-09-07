import '../../../core/network/api_client.dart';
import '../../../core/network/api_problem.dart';
import '../domain/tenant_portal_models.dart';

class TenantPortalRepository {
  TenantPortalRepository(this.api, {Duration cacheDuration = _defaultCache})
    : _cacheDuration = cacheDuration;

  static const _defaultCache = Duration(minutes: 5);

  final ApiClient api;
  final Duration _cacheDuration;

  int _scope = 0;
  TenantProfile? _profileCache;
  DateTime? _profileCachedAt;
  TenantLease? _leaseCache;
  DateTime? _leaseCachedAt;
  bool _leaseWasLoaded = false;
  Future<TenantProfile>? _profileInFlight;
  Future<TenantLease?>? _leaseInFlight;

  void clear() {
    _scope++;
    _profileCache = null;
    _profileCachedAt = null;
    _leaseCache = null;
    _leaseCachedAt = null;
    _leaseWasLoaded = false;
    _profileInFlight = null;
    _leaseInFlight = null;
  }

  Future<TenantProfile> profile({bool force = false}) {
    if (!force && _isFresh(_profileCachedAt) && _profileCache != null) {
      return Future.value(_profileCache!);
    }
    final existing = _profileInFlight;
    if (existing != null) return existing;

    final scope = _scope;
    final request = _loadProfile(scope);
    _profileInFlight = request;
    return request.whenComplete(() {
      if (identical(_profileInFlight, request)) _profileInFlight = null;
    });
  }

  Future<TenantLease?> currentLease({bool force = false}) {
    if (!force && _leaseWasLoaded && _isFresh(_leaseCachedAt)) {
      return Future.value(_leaseCache);
    }
    final existing = _leaseInFlight;
    if (existing != null) return existing;

    final scope = _scope;
    final request = _loadLease(scope);
    _leaseInFlight = request;
    return request.whenComplete(() {
      if (identical(_leaseInFlight, request)) _leaseInFlight = null;
    });
  }

  Future<TenantProfile> _loadProfile(int scope) async {
    try {
      final value = TenantProfile.fromJson(
        await api.getJson('/api/v1/tenant-portal/me'),
      );
      _ensureCurrent(scope);
      _profileCache = value;
      _profileCachedAt = DateTime.now();
      return value;
    } on ApiProblem {
      rethrow;
    } on FormatException {
      throw const ApiProblem(
        message: 'Invalid tenant profile response',
        code: 'INVALID_RESPONSE',
      );
    } on TypeError {
      throw const ApiProblem(
        message: 'Invalid tenant profile response',
        code: 'INVALID_RESPONSE',
      );
    }
  }

  Future<TenantLease?> _loadLease(int scope) async {
    try {
      final value = TenantLease.fromJson(
        await api.getJson('/api/v1/tenant-portal/lease'),
      );
      _ensureCurrent(scope);
      _leaseCache = value;
      _leaseWasLoaded = true;
      _leaseCachedAt = DateTime.now();
      return value;
    } on ApiProblem catch (problem) {
      if (problem.statusCode != 404) rethrow;
      _ensureCurrent(scope);
      _leaseCache = null;
      _leaseWasLoaded = true;
      _leaseCachedAt = DateTime.now();
      return null;
    } on FormatException {
      throw const ApiProblem(
        message: 'Invalid tenant lease response',
        code: 'INVALID_RESPONSE',
      );
    } on TypeError {
      throw const ApiProblem(
        message: 'Invalid tenant lease response',
        code: 'INVALID_RESPONSE',
      );
    }
  }

  bool _isFresh(DateTime? value) =>
      value != null && DateTime.now().difference(value) < _cacheDuration;

  void _ensureCurrent(int scope) {
    if (scope != _scope) {
      throw const ApiProblem(
        message: 'Session changed',
        code: 'SESSION_CHANGED',
      );
    }
  }
}
