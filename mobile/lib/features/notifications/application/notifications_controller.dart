import 'dart:async';

import 'package:flutter/foundation.dart';
import '../data/notifications_repository.dart';
import '../domain/notification_models.dart';

class NotificationsController extends ChangeNotifier {
  NotificationsController(
    this._repository, {
    this.unreadCount = 0,
    this.pollingInterval = const Duration(minutes: 1),
  });
  final NotificationsRepository _repository;
  final Duration pollingInterval;
  final List<InboxNotification> items = [];
  int unreadCount;
  bool loading = false, loadingMore = false, hasMore = true;
  bool inboxLoaded = false, unreadLoaded = false;
  Object? error;
  Timer? _pollTimer;
  Future<void>? _unreadInFlight;

  Future<void> initialize() async {
    startPolling();
    if (!unreadLoaded) {
      await loadUnread();
    }
  }

  void startPolling() {
    _pollTimer ??= Timer.periodic(pollingInterval, (_) => loadUnread());
  }

  Future<void> loadUnread() => _unreadInFlight ??= _loadUnread().whenComplete(
    () => _unreadInFlight = null,
  );

  Future<void> _loadUnread() async {
    try {
      unreadCount = await _repository.unreadCount();
      unreadLoaded = true;
      notifyListeners();
    } catch (_) {
      // The inbox owns visible errors. A failed badge refresh stays silent.
    }
  }

  Future<void> load({bool refresh = false}) async {
    if (loading || (inboxLoaded && !refresh)) return;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final page = await _repository.inbox(const NotificationPageCursor());
      items
        ..clear()
        ..addAll(_unique(page));
      hasMore = page.length == 50;
      inboxLoaded = true;
    } catch (e) {
      error = e;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> refresh() => load(refresh: true);
  Future<void> loadMore() async {
    if (loadingMore || !hasMore || items.isEmpty) return;
    loadingMore = true;
    notifyListeners();
    try {
      final last = items.last;
      final page = await _repository.inbox(
        NotificationPageCursor(createdAt: last.createdAt, id: last.id),
      );
      items.addAll(_unique(page));
      hasMore = page.length == 50;
    } finally {
      loadingMore = false;
      notifyListeners();
    }
  }

  Future<bool> open(InboxNotification item) async {
    if (!item.canMarkRead) return false;
    try {
      await _repository.markRead(item.id);
      final index = items.indexWhere((value) => value.id == item.id);
      if (index >= 0) items[index] = item.markedRead;
      if (unreadCount > 0) unreadCount--;
      notifyListeners();
      return true;
    } catch (_) {
      rethrow;
    }
  }

  Future<int> markAllRead() async {
    final marked = await _repository.markAllRead();
    for (var i = 0; i < items.length; i++) {
      if (items[i].canMarkRead) {
        items[i] = items[i].markedRead;
      }
    }
    unreadCount = 0;
    notifyListeners();
    return marked;
  }

  void reset() {
    _pollTimer?.cancel();
    _pollTimer = null;
    _unreadInFlight = null;
    items.clear();
    unreadCount = 0;
    unreadLoaded = false;
    inboxLoaded = false;
    loading = false;
    loadingMore = false;
    hasMore = true;
    error = null;
    notifyListeners();
  }

  List<InboxNotification> _unique(List<InboxNotification> page) {
    final known = items.map((item) => item.id).toSet();
    return page.where((item) => known.add(item.id)).toList();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }
}
