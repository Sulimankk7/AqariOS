import 'package:flutter/material.dart';
import '../../../core/design_system/design_system.dart';
import '../application/session_controller.dart';
import 'auth_components.dart';
import 'auth_flows.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({required this.session, super.key});
  final SessionController session;
  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final identifier = TextEditingController(),
      password = TextEditingController();
  bool rememberMe = true;
  String? identifierError, passwordError;
  @override
  void initState() {
    super.initState();
    widget.session.addListener(refresh);
  }

  @override
  void dispose() {
    widget.session.removeListener(refresh);
    identifier.dispose();
    password.dispose();
    super.dispose();
  }

  void refresh() {
    if (mounted) setState(() {});
  }

  void open(AuthFlow flow) => Navigator.push(
    context,
    MaterialPageRoute(
      builder: (_) => AuthFlowPage(session: widget.session, flow: flow),
    ),
  );
  Future<void> submit() async {
    setState(() {
      identifierError = identifier.text.trim().isEmpty
          ? (context.isArabic
                ? 'أدخل البريد أو رقم الهاتف'
                : 'Enter email or phone')
          : null;
      passwordError = password.text.isEmpty
          ? (context.isArabic ? 'أدخل كلمة المرور' : 'Enter password')
          : null;
    });
    if (identifierError != null || passwordError != null) return;
    await widget.session.login(
      identifier: identifier.text.trim(),
      password: password.text,
      rememberMe: rememberMe,
    );
  }

  @override
  Widget build(BuildContext context) => AuthScaffold(
    centered: true,
    title: context.isArabic ? 'مرحباً بك مجدداً' : 'Welcome back',
    subtitle: context.isArabic
        ? 'سجّل الدخول إلى مساحة عمل نظام عقاري'
        : 'Sign in to your AqariOS workspace',
    child: AutofillGroup(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AuthSegmentedControl(
            selected: 0,
            items: [
              (
                context.isArabic ? 'كلمة المرور' : 'Password',
                Icons.lock_outline_rounded,
              ),
              (
                context.isArabic ? 'رمز الهاتف' : 'Phone SMS',
                Icons.phone_android_rounded,
              ),
            ],
            onChanged: (value) {
              if (value == 1) open(AuthFlow.otp);
            },
          ),
          const SizedBox(height: AqariSpacing.x5),
          AuthTextField(
            controller: identifier,
            label: context.isArabic
                ? 'البريد الإلكتروني أو رقم الهاتف'
                : 'Email or phone number',
            hint: context.isArabic ? 'البريد أو رقم الهاتف' : 'Email or phone',
            icon: Icons.alternate_email_rounded,
            errorText: identifierError,
            enabled: !widget.session.submitting,
            autofillHints: const [AutofillHints.username],
            onChanged: (_) {
              if (identifierError != null) {
                setState(() => identifierError = null);
              }
            },
          ),
          const SizedBox(height: AqariSpacing.x4),
          AuthPasswordField(
            controller: password,
            label: context.isArabic ? 'كلمة المرور' : 'Password',
            errorText: passwordError,
            enabled: !widget.session.submitting,
            onChanged: (_) {
              if (passwordError != null) setState(() => passwordError = null);
            },
          ),
          Wrap(
            alignment: WrapAlignment.spaceBetween,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 210,
                child: AqariCheckbox(
                  label: context.isArabic
                      ? 'تذكرني لمدة 30 يوماً'
                      : 'Remember me for 30 days',
                  value: rememberMe,
                  onChanged: widget.session.submitting
                      ? null
                      : (value) => setState(() => rememberMe = value ?? false),
                ),
              ),
              AuthLink(
                label: context.isArabic
                    ? 'نسيت كلمة المرور؟'
                    : 'Forgot password?',
                onPressed: widget.session.submitting
                    ? null
                    : () => open(AuthFlow.forgotPassword),
              ),
            ],
          ),
          if (widget.session.error != null) ...[
            const SizedBox(height: AqariSpacing.x2),
            AuthBanner(message: widget.session.error!.message),
          ],
          const SizedBox(height: AqariSpacing.x4),
          AqariButton(
            label: context.isArabic ? 'تسجيل الدخول' : 'Sign in',
            expanded: true,
            loading: widget.session.submitting,
            onPressed: submit,
          ),
          const SizedBox(height: AqariSpacing.x2),
          Wrap(
            alignment: WrapAlignment.center,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text(context.isArabic ? 'ليس لديك حساب؟' : 'No account?'),
              AuthLink(
                label: context.isArabic
                    ? 'أنشئ حساباً جديداً'
                    : 'Create account',
                onPressed: () => open(AuthFlow.register),
              ),
            ],
          ),
        ],
      ),
    ),
  );
}
