import 'dart:async';

import 'package:flutter/foundation.dart';

import 'tenant_utility_models.dart';
import 'tenant_utility_repository.dart';

class TenantUtilityController extends ChangeNotifier {
  TenantUtilityController(
    this.repository, {
    this.pollInterval = const Duration(seconds: 3),
    this.verificationTimeout = const Duration(seconds: 120),
  });
  final TenantUtilityRepository repository;
  final Duration pollInterval, verificationTimeout;
  List<TenantUtilityAccount> accounts = const [];
  List<TenantUtilityBill> bills = const [];
  TenantUtilitySummary? summary;
  TenantUtilityType? filter;
  bool loading = false, loadingMore = false, mutating = false, hasMore = false;
  String? nextCursor;
  Object? accountsError, billsError, summaryError;
  bool _loaded = false;
  Future<void>? _loadInFlight, _summaryInFlight;
  Timer? _pollTimer;
  final Map<String, _Verification> _verifications = {};

  Future<void> load({bool force = false}) {
    if (_loadInFlight != null) return _loadInFlight!;
    if (_loaded && !force) return Future.value();
    final request = _performLoad();
    _loadInFlight = request;
    return request.whenComplete(() {
      if (identical(_loadInFlight, request)) _loadInFlight = null;
    });
  }

  Future<void> _performLoad() async {
    loading = true;
    accountsError = null;
    billsError = null;
    notifyListeners();
    final accountFuture = repository.accounts();
    final billsFuture = repository.bills(type: filter);
    try {
      accounts = await accountFuture;
      _trackTransient(accounts);
    } catch (error) {
      accountsError = error;
    }
    try {
      final page = await billsFuture;
      bills = page.items;
      nextCursor = page.nextCursor;
      hasMore = page.hasMore;
    } catch (error) {
      billsError = error;
    }
    loading = false;
    _loaded = true;
    notifyListeners();
  }

  Future<void> loadSummary({bool force = false}) {
    if (_summaryInFlight != null) return _summaryInFlight!;
    if (summary != null && !force) return Future.value();
    final request = _performSummary();
    _summaryInFlight = request;
    return request.whenComplete(() {
      if (identical(_summaryInFlight, request)) _summaryInFlight = null;
    });
  }

  Future<void> _performSummary() async {
    summaryError = null;
    notifyListeners();
    try {
      summary = await repository.summary();
    } catch (error) {
      summaryError = error;
    }
    notifyListeners();
  }

  Future<void> changeFilter(TenantUtilityType? value) async {
    if (filter == value) return;
    filter = value;
    bills = const [];
    nextCursor = null;
    hasMore = false;
    billsError = null;
    _loaded = false;
    await load(force: true);
  }

  Future<void> loadMore() async {
    if (loadingMore || !hasMore || nextCursor == null) return;
    loadingMore = true;
    notifyListeners();
    try {
      final page = await repository.bills(type: filter, cursor: nextCursor);
      final known = bills.map((item) => item.id).toSet();
      bills = [...bills, ...page.items.where((item) => known.add(item.id))];
      nextCursor = page.nextCursor;
      hasMore = page.hasMore;
    } finally {
      loadingMore = false;
      notifyListeners();
    }
  }

  Future<void> link(
    TenantUtilityType type,
    String number,
    String? meter,
  ) async {
    await _mutation(() => repository.link(type, number, meter), verify: true);
  }

  Future<void> replace(
    TenantUtilityAccount account,
    String number,
    String? meter,
  ) async {
    await _mutation(
      () => repository.replace(account.id, number, meter),
      verify: true,
      baseline: account,
    );
  }

  Future<void> unlink(TenantUtilityAccount account) async {
    mutating = true;
    notifyListeners();
    try {
      await repository.unlink(account.id);
      _verifications.remove(account.id);
      await _refreshAll();
    } finally {
      mutating = false;
      notifyListeners();
    }
  }

  Future<void> sync(TenantUtilityAccount account) async {
    mutating = true;
    notifyListeners();
    try {
      await repository.requestSync(account.id);
      _startVerification(account, account);
      await _refreshAll();
    } finally {
      mutating = false;
      notifyListeners();
    }
  }

  Future<void> _mutation(
    Future<TenantUtilityAccount> Function() action, {
    required bool verify,
    TenantUtilityAccount? baseline,
  }) async {
    mutating = true;
    notifyListeners();
    try {
      final account = await action();
      if (verify) _startVerification(account, baseline ?? account);
      await _refreshAll();
    } finally {
      mutating = false;
      notifyListeners();
    }
  }

  Future<void> _refreshAll() async {
    _loaded = false;
    summary = null;
    await Future.wait([load(force: true), loadSummary(force: true)]);
  }

  bool isVerifying(String id) => _verifications.containsKey(id);
  bool verificationTimedOut(String id) => _verifications[id]?.timedOut == true;

  void _trackTransient(List<TenantUtilityAccount> values) {
    for (final account in values.where(
      (item) => item.isCurrent && item.isTransient,
    )) {
      _verifications.putIfAbsent(
        account.id,
        () => _Verification(account, DateTime.now().add(verificationTimeout)),
      );
    }
    _evaluate(values);
  }

  void _startVerification(
    TenantUtilityAccount account,
    TenantUtilityAccount baseline,
  ) {
    _verifications[account.id] = _Verification(
      baseline,
      DateTime.now().add(verificationTimeout),
    );
    _ensurePolling();
  }

  void _ensurePolling() {
    if (_verifications.values.any((value) => !value.timedOut)) {
      _pollTimer ??= Timer.periodic(pollInterval, (_) => _poll());
    }
  }

  Future<void> _poll() async {
    if (_loadInFlight != null ||
        !_verifications.values.any((value) => !value.timedOut)) {
      return;
    }
    try {
      final latest = await repository.accounts();
      accounts = latest;
      final completedSuccessfully = _evaluate(latest);
      await loadSummary(force: true);
      if (completedSuccessfully) {
        final page = await repository.bills(type: filter);
        bills = page.items;
        nextCursor = page.nextCursor;
        hasMore = page.hasMore;
      }
      notifyListeners();
    } catch (_) {
      // Keep the bounded verification window alive; visible data remains usable.
    }
  }

  bool _evaluate(List<TenantUtilityAccount> latest) {
    var success = false;
    final now = DateTime.now();
    for (final entry in _verifications.entries.toList()) {
      final window = entry.value;
      if (now.isAfter(window.expiresAt)) {
        window.timedOut = true;
        continue;
      }
      final account = latest.where((item) => item.id == entry.key).firstOrNull;
      if (account == null) continue;
      if (account.isTransient) {
        window.sawTransient = true;
        continue;
      }
      final changed =
          window.sawTransient ||
          account.syncStatus != window.baselineStatus ||
          account.lastSuccessfulSyncAt != window.baselineSuccessfulAt;
      if (changed) {
        success = account.syncStatus == 'Synced';
        _verifications.remove(entry.key);
      }
    }
    if (!_verifications.values.any((value) => !value.timedOut)) {
      _pollTimer?.cancel();
      _pollTimer = null;
    }
    _ensurePolling();
    return success;
  }

  void reset() {
    _pollTimer?.cancel();
    _pollTimer = null;
    _verifications.clear();
    accounts = const [];
    bills = const [];
    summary = null;
    filter = null;
    _loaded = false;
    _loadInFlight = null;
    _summaryInFlight = null;
    accountsError = null;
    billsError = null;
    summaryError = null;
    loading = false;
    loadingMore = false;
    mutating = false;
    hasMore = false;
    nextCursor = null;
    notifyListeners();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }
}

class _Verification {
  _Verification(TenantUtilityAccount baseline, this.expiresAt)
    : baselineStatus = baseline.syncStatus,
      baselineSuccessfulAt = baseline.lastSuccessfulSyncAt,
      sawTransient = baseline.isTransient;
  final String baselineStatus;
  final String? baselineSuccessfulAt;
  final DateTime expiresAt;
  bool sawTransient;
  bool timedOut = false;
}
