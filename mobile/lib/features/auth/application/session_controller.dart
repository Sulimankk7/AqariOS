import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../core/network/api_problem.dart';
import '../data/auth_repository.dart';
import '../domain/user_profile.dart';

enum SessionStatus {
  initializing,
  bootstrapFailure,
  unauthenticated,
  authenticated,
  unsupportedRole,
}

class SessionController extends ChangeNotifier {
  SessionController(
    this._repository, {
    Duration bootstrapTimeout = const Duration(seconds: 45),
  }) : _bootstrapTimeout = bootstrapTimeout;
  final AuthRepository _repository;
  final Duration _bootstrapTimeout;

  SessionStatus status = SessionStatus.initializing;
  UserProfile? user;
  ApiProblem? error;
  bool submitting = false;

  AuthRepository get repository => _repository;

  Future<bool> acceptAuthentication(Future<UserProfile> operation) async {
    submitting = true;
    error = null;
    notifyListeners();
    try {
      _acceptUser(await operation);
      return status == SessionStatus.authenticated;
    } on ApiProblem catch (problem) {
      error = problem;
      return false;
    } on FormatException {
      error = const ApiProblem(message: 'استجابة المصادقة غير صالحة.');
      return false;
    } finally {
      submitting = false;
      notifyListeners();
    }
  }

  Future<void> bootstrap() async {
    status = SessionStatus.initializing;
    error = null;
    notifyListeners();
    try {
      _acceptUser(await _repository.restore().timeout(_bootstrapTimeout));
    } on TimeoutException {
      user = null;
      error = const ApiProblem(
        message: 'انتهت مهلة استعادة الجلسة. تحقق من الاتصال ثم أعد المحاولة.',
        code: 'TIMEOUT',
        isNetworkFailure: true,
      );
      status = SessionStatus.bootstrapFailure;
      notifyListeners();
    } on ApiProblem catch (problem) {
      user = null;
      if (problem.isUnauthorized) {
        status = SessionStatus.unauthenticated;
      } else {
        error = problem;
        status = SessionStatus.bootstrapFailure;
      }
      notifyListeners();
    } catch (_) {
      error = const ApiProblem(message: 'تعذر استعادة الجلسة المحفوظة.');
      status = SessionStatus.bootstrapFailure;
      notifyListeners();
    }
  }

  Future<bool> login({
    required String identifier,
    required String password,
    required bool rememberMe,
  }) async {
    return acceptAuthentication(
      _repository.login(
        emailOrPhone: identifier,
        password: password,
        rememberMe: rememberMe,
      ),
    );
  }

  Future<void> logout() async {
    submitting = true;
    notifyListeners();
    try {
      await _repository.logout();
    } catch (_) {
      // Local sign-out must complete even if the server is unreachable.
    } finally {
      user = null;
      error = null;
      submitting = false;
      status = SessionStatus.unauthenticated;
      notifyListeners();
    }
  }

  Future<void> logoutAll() async {
    submitting = true;
    error = null;
    notifyListeners();
    try {
      await _repository.logoutAll();
      user = null;
      status = SessionStatus.unauthenticated;
    } on ApiProblem catch (problem) {
      error = problem;
      rethrow;
    } finally {
      submitting = false;
      notifyListeners();
    }
  }

  void invalidate() {
    user = null;
    status = SessionStatus.unauthenticated;
    notifyListeners();
  }

  void _acceptUser(UserProfile profile) {
    user = profile;
    error = null;
    status = profile.role == AqariRole.unsupported
        ? SessionStatus.unsupportedRole
        : SessionStatus.authenticated;
    notifyListeners();
  }
}
