import 'dart:async';

import 'package:flutter/material.dart';

import '../../../core/design_system/design_system.dart';
import '../../../core/network/api_problem.dart';
import '../application/session_controller.dart';
import '../domain/user_profile.dart';
import 'auth_components.dart';

String _phone(String value) {
  final compact = value.replaceAll(RegExp(r'[\s().-]'), '');
  if (compact.startsWith('+') || compact.startsWith('00')) return compact;
  return '+962${compact.replaceFirst(RegExp(r'^0+'), '')}';
}

class AuthFlowPage extends StatefulWidget {
  const AuthFlowPage({
    required this.session,
    required this.flow,
    this.activationToken,
    super.key,
  });
  final SessionController session;
  final AuthFlow flow;
  final String? activationToken;

  @override
  State<AuthFlowPage> createState() => _AuthFlowPageState();
}

enum AuthFlow { register, otp, forgotPassword, activation }

class _AuthFlowPageState extends State<AuthFlowPage> {
  final a = TextEditingController();
  final b = TextEditingController();
  final c = TextEditingController();
  final d = TextEditingController();
  final e = TextEditingController();
  final f = TextEditingController();
  final g = TextEditingController();
  final h = TextEditingController();
  bool busy = false;
  bool agreed = false;
  int companyType = 0;
  String countryCode = 'JO', dialCode = '+962', language = 'ar';
  int recoveryMethod = 0;
  String? error;

  @override
  void dispose() {
    for (final controller in [a, b, c, d, e, f, g, h]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<T?> run<T>(Future<T> Function() action) async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      return await action();
    } on ApiProblem catch (e) {
      if (mounted) setState(() => error = e.message);
    } on FormatException {
      if (mounted) {
        setState(
          () => error = context.isArabic
              ? 'استجابة الخادم غير صالحة.'
              : 'Invalid server response.',
        );
      }
    } catch (_) {
      if (mounted) {
        setState(
          () => error = context.isArabic
              ? 'تعذر إكمال الطلب.'
              : 'Unable to complete the request.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
    return null;
  }

  void open(Widget page) => Navigator.pushReplacement(
    context,
    MaterialPageRoute(builder: (_) => page),
  );

  @override
  Widget build(BuildContext context) => AuthScaffold(
    title: title,
    subtitle: subtitle,
    onBack: () => Navigator.pop(context),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ...fields,
        if (error != null) ...[
          const SizedBox(height: AqariSpacing.x3),
          AuthBanner(message: error!),
        ],
        const SizedBox(height: AqariSpacing.x5),
        AqariButton(
          label: actionLabel,
          expanded: true,
          loading: busy,
          onPressed: canSubmit ? submit : null,
        ),
      ],
    ),
  );

  String get subtitle => switch (widget.flow) {
    AuthFlow.register =>
      context.isArabic
          ? 'قم بإعداد مساحة عمل مؤسستك على عقاري'
          : 'Set up your organization workspace',
    AuthFlow.otp =>
      context.isArabic
          ? 'سنرسل رمز تحقق من 6 أرقام إلى هاتفك المسجل'
          : 'We will send a 6-digit code to your registered phone',
    AuthFlow.forgotPassword =>
      context.isArabic
          ? 'اختر طريقة استلام تعليمات الاستعادة'
          : 'Choose how to receive recovery instructions',
    AuthFlow.activation =>
      context.isArabic
          ? 'تحقق من صلاحية رابط التفعيل للمتابعة'
          : 'Validate your activation link to continue',
  };

  String get title => switch (widget.flow) {
    AuthFlow.register => context.isArabic ? 'إنشاء حساب' : 'Create account',
    AuthFlow.otp =>
      context.isArabic ? 'الدخول برمز التحقق' : 'Sign in with OTP',
    AuthFlow.forgotPassword =>
      context.isArabic ? 'نسيت كلمة المرور' : 'Forgot password',
    AuthFlow.activation =>
      context.isArabic ? 'تفعيل حساب المستأجر' : 'Activate tenant account',
  };

  String get actionLabel => switch (widget.flow) {
    AuthFlow.register =>
      context.isArabic ? 'إرسال طلب التسجيل' : 'Submit registration',
    AuthFlow.otp => context.isArabic ? 'إرسال الرمز' : 'Send code',
    AuthFlow.forgotPassword =>
      context.isArabic
          ? 'إرسال تعليمات الاستعادة'
          : 'Send recovery instructions',
    AuthFlow.activation =>
      context.isArabic ? 'التحقق من رابط التفعيل' : 'Validate activation link',
  };

  bool get canSubmit => !busy && (widget.flow != AuthFlow.register || agreed);

  List<Widget> get fields => switch (widget.flow) {
    AuthFlow.register => [
      field(
        a,
        context.isArabic ? 'الاسم الكامل' : 'Full name',
        Icons.person_outline,
      ),
      gap,
      field(
        b,
        context.isArabic ? 'اسم الشركة' : 'Company name',
        Icons.apartment_outlined,
      ),
      gap,
      AqariSelect<int>(
        label: context.isArabic ? 'نوع الشركة' : 'Company type',
        valueLabel: companyTypeLabel,
        items: const [0, 1, 2],
        itemLabel: companyTypeName,
        onChanged: (v) => setState(() => companyType = v),
        enabled: !busy,
      ),
      gap,
      field(
        c,
        context.isArabic
            ? 'الاسم المعروض (اختياري)'
            : 'Display name (optional)',
        Icons.badge_outlined,
      ),
      gap,
      field(
        d,
        context.isArabic ? 'البريد الإلكتروني' : 'Email',
        Icons.alternate_email,
        type: TextInputType.emailAddress,
      ),
      gap,
      Row(
        children: [
          Expanded(
            child: field(
              e,
              context.isArabic ? 'رقم الهاتف' : 'Phone number',
              Icons.phone_outlined,
              type: TextInputType.phone,
            ),
          ),
          const SizedBox(width: AqariSpacing.x2),
          SizedBox(
            width: 112,
            child: AqariSelect<String>(
              label: context.isArabic ? 'الدولة' : 'Country',
              valueLabel: dialCode,
              items: countryCodes.keys.toList(),
              itemLabel: (v) => '${countryCodes[v]!.$1} ${countryCodes[v]!.$2}',
              onChanged: selectCountry,
              enabled: !busy,
            ),
          ),
        ],
      ),
      gap,
      AuthPasswordField(
        controller: f,
        label: context.isArabic ? 'كلمة المرور' : 'Password',
        enabled: !busy,
      ),
      gap,
      AuthPasswordField(
        controller: g,
        label: context.isArabic ? 'تأكيد كلمة المرور' : 'Confirm password',
        enabled: !busy,
      ),
      gap,
      AqariSelect<String>(
        label: context.isArabic ? 'لغة النظام المفضلة' : 'Preferred language',
        valueLabel: language == 'ar' ? 'العربية' : 'English',
        items: const ['ar', 'en'],
        itemLabel: (v) => v == 'ar' ? 'العربية' : 'English',
        onChanged: (v) => setState(() => language = v),
        enabled: !busy,
      ),
      AqariCheckbox(
        label: context.isArabic
            ? 'أوافق على الشروط وسياسة الخصوصية'
            : 'I agree to the terms and privacy policy',
        value: agreed,
        onChanged: busy ? null : (v) => setState(() => agreed = v ?? false),
      ),
    ],
    AuthFlow.otp => [
      AuthSegmentedControl(
        selected: 1,
        items: [
          (context.isArabic ? 'كلمة المرور' : 'Password', Icons.lock_outline),
          (context.isArabic ? 'رمز الهاتف' : 'Phone SMS', Icons.phone_android),
        ],
        onChanged: (v) {
          if (v == 0) Navigator.pop(context);
        },
      ),
      gap,
      field(
        a,
        context.isArabic ? 'رقم الهاتف' : 'Phone number',
        Icons.phone_outlined,
        type: TextInputType.phone,
      ),
    ],
    AuthFlow.forgotPassword => [
      AuthSegmentedControl(
        selected: recoveryMethod,
        items: [
          (
            context.isArabic ? 'البريد الإلكتروني' : 'Email',
            Icons.email_outlined,
          ),
          (context.isArabic ? 'رسالة SMS' : 'SMS', Icons.sms_outlined),
        ],
        onChanged: (v) => setState(() {
          recoveryMethod = v;
          a.clear();
          error = null;
        }),
      ),
      gap,
      field(
        a,
        context.isArabic
            ? 'البريد الإلكتروني أو رقم الهاتف'
            : 'Email or phone number',
        recoveryMethod == 0 ? Icons.email_outlined : Icons.phone_outlined,
        type: recoveryMethod == 0
            ? TextInputType.emailAddress
            : TextInputType.phone,
      ),
    ],
    AuthFlow.activation => [
      if (widget.activationToken == null)
        const AuthBanner(
          message: 'افتح رابط التفعيل المرسل إليك للمتابعة.',
          kind: AuthBannerKind.info,
        ),
    ],
  };

  Widget get gap => const SizedBox(height: AqariSpacing.x4);
  Widget field(
    TextEditingController controller,
    String label,
    IconData icon, {
    bool password = false,
    TextInputType? type,
  }) => AuthTextField(
    controller: controller,
    label: label,
    icon: icon,
    obscureText: password,
    enabled: !busy,
    keyboardType: type,
  );

  static const countryCodes = <String, (String, String)>{
    'JO': ('🇯🇴', '+962'),
    'AE': ('🇦🇪', '+971'),
    'SA': ('🇸🇦', '+966'),
    'US': ('🇺🇸', '+1'),
    'GB': ('🇬🇧', '+44'),
    'KW': ('🇰🇼', '+965'),
    'QA': ('🇶🇦', '+974'),
    'BH': ('🇧🇭', '+973'),
    'OM': ('🇴🇲', '+968'),
    'EG': ('🇪🇬', '+20'),
  };
  void selectCountry(String value) => setState(() {
    countryCode = value;
    dialCode = countryCodes[value]!.$2;
  });
  String companyTypeName(int value) => switch (value) {
    0 => context.isArabic ? 'مالك فردي' : 'Individual owner',
    1 => context.isArabic ? 'شركة إدارة عقارات' : 'Property management company',
    _ => context.isArabic ? 'شركة استثمار' : 'Investment company',
  };
  String get companyTypeLabel => companyTypeName(companyType);

  Future<void> submit() async {
    if (a.text.trim().isEmpty ||
        (widget.flow == AuthFlow.register &&
            (b.text.trim().isEmpty ||
                d.text.trim().isEmpty ||
                e.text.trim().isEmpty ||
                f.text.length < 8 ||
                f.text != g.text))) {
      setState(
        () => error = context.isArabic
            ? 'أكمل الحقول المطلوبة. كلمة المرور 8 أحرف على الأقل.'
            : 'Complete the required fields. Password must be at least 8 characters.',
      );
      return;
    }
    switch (widget.flow) {
      case AuthFlow.register:
        final ok = await run(
          () => widget.session.repository
              .register(
                fullName: a.text,
                companyName: b.text,
                displayName: c.text,
                companyType: companyType,
                email: d.text,
                phone:
                    '$dialCode${e.text.replaceAll(RegExp(r'\s+'), '').replaceFirst(RegExp(r'^0+'), '')}',
                password: f.text,
                countryCode: countryCode,
                preferredLanguage: language,
              )
              .then((_) => true),
        );
        if (ok == true && mounted) Navigator.pop(context);
      case AuthFlow.otp:
        final phone = _phone(a.text);
        final ok = await run(
          () => widget.session.repository.requestOtp(phone).then((_) => true),
        );
        if (ok == true && mounted) {
          open(OtpVerifyPage(session: widget.session, phone: phone));
        }
      case AuthFlow.forgotPassword:
        final identifier = a.text.trim();
        final isEmail = recoveryMethod == 0;
        final normalized = isEmail ? identifier : _phone(identifier);
        final ok = await run(
          () => widget.session.repository
              .requestPasswordReset(isEmail ? 'Email' : 'Phone', normalized)
              .then((_) => true),
        );
        if (ok == true && mounted) {
          if (isEmail) {
            await showDialog<void>(
              context: context,
              builder: (_) => AlertDialog(
                title: Text(
                  context.isArabic ? 'تحقق من بريدك' : 'Check your email',
                ),
                content: Text(
                  context.isArabic
                      ? 'إذا كان الحساب موجوداً فستصلك تعليمات إعادة التعيين.'
                      : 'If the account exists, reset instructions will be sent.',
                ),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(context),
                    child: Text(context.isArabic ? 'حسناً' : 'OK'),
                  ),
                ],
              ),
            );
            if (mounted) Navigator.pop(context);
          } else {
            open(
              PasswordResetOtpPage(session: widget.session, phone: normalized),
            );
          }
        }
      case AuthFlow.activation:
        final token = widget.activationToken?.trim() ?? '';
        final status = await run(
          () => widget.session.repository.activationStatus(token),
        );
        if (status != null && mounted) {
          if (status['status'] == 'VALID') {
            open(
              PasswordEntryPage(
                session: widget.session,
                credential: token,
                activation: true,
                tenantName: status['tenantName']?.toString(),
              ),
            );
          } else {
            setState(
              () => error =
                  status['message']?.toString() ??
                  (context.isArabic
                      ? 'رابط التفعيل غير صالح.'
                      : 'Invalid activation link.'),
            );
          }
        }
    }
  }
}

class OtpVerifyPage extends StatefulWidget {
  const OtpVerifyPage({required this.session, required this.phone, super.key});
  final SessionController session;
  final String phone;
  @override
  State<OtpVerifyPage> createState() => _OtpVerifyPageState();
}

class _OtpVerifyPageState extends State<OtpVerifyPage> {
  String code = '';
  int seconds = 60;
  Timer? timer;
  String? error;
  bool busy = false;
  @override
  void initState() {
    super.initState();
    start();
  }

  void start() {
    timer?.cancel();
    seconds = 60;
    timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted || seconds <= 1) {
        t.cancel();
        if (mounted) setState(() => seconds = 0);
      } else {
        setState(() => seconds--);
      }
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AuthScaffold(
    centered: true,
    onBack: () => Navigator.pop(context),
    title: context.isArabic ? 'تأكيد رقم الهاتف' : 'Confirm phone number',
    subtitle:
        '${context.isArabic ? 'الرمز المرسل عبر SMS إلى' : 'Code sent by SMS to'} ${maskedPhone(widget.phone)}',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const AuthBanner(
          message: 'تم إرسال الرمز بنجاح!',
          kind: AuthBannerKind.success,
        ),
        const SizedBox(height: AqariSpacing.x5),
        AuthOtpInput(
          enabled: !busy,
          onChanged: (value) => setState(() => code = value),
        ),
        if (error != null) ...[
          const SizedBox(height: AqariSpacing.x3),
          AuthBanner(message: error!),
        ],
        Wrap(
          alignment: WrapAlignment.spaceBetween,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            AuthLink(
              label: context.isArabic ? 'تغيير الرقم' : 'Change number',
              onPressed: busy ? null : () => Navigator.pop(context),
            ),
            Text(
              seconds > 0
                  ? '${context.isArabic ? 'إعادة خلال' : 'Resend in'} ${seconds}s'
                  : '',
            ),
          ],
        ),
        AqariButton(
          label: context.isArabic ? 'التحقق والمتابعة' : 'Verify and continue',
          expanded: true,
          loading: busy,
          onPressed: code.length == 6 && !busy ? verify : null,
        ),
        AuthLink(
          label: context.isArabic
              ? 'لم تصلك الرسالة؟ أعد الإرسال'
              : 'Did not receive it? Resend',
          onPressed: seconds == 0 && !busy ? resend : null,
        ),
      ],
    ),
  );
  String maskedPhone(String value) => value.length < 7
      ? value
      : '${value.substring(0, 4)} ••• ${value.substring(value.length - 3)}';
  Future<void> verify() async {
    setState(() {
      busy = true;
      error = null;
    });
    final ok = await widget.session.acceptAuthentication(
      widget.session.repository.verifyOtp(widget.phone, code),
    );
    if (mounted) {
      setState(() {
        busy = false;
        error = ok ? null : widget.session.error?.message;
      });
      if (ok) Navigator.popUntil(context, (r) => r.isFirst);
    }
  }

  Future<void> resend() async {
    setState(() => busy = true);
    try {
      await widget.session.repository.requestOtp(widget.phone);
      if (mounted) start();
    } on ApiProblem catch (e) {
      if (mounted) {
        error = e.message;
        seconds = e.retryAfterSeconds ?? 0;
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }
}

class PasswordResetOtpPage extends StatelessWidget {
  PasswordResetOtpPage({required this.session, required this.phone, super.key});
  final SessionController session;
  final String phone;
  final code = TextEditingController();
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(context.isArabic ? 'تحقق من الرمز' : 'Verify code'),
    ),
    body: Padding(
      padding: const EdgeInsets.all(AqariSpacing.page),
      child: Column(
        children: [
          AqariTextField(
            controller: code,
            label: context.isArabic ? 'رمز الاستعادة' : 'Reset code',
            keyboardType: TextInputType.number,
            textDirection: TextDirection.ltr,
          ),
          const SizedBox(height: 20),
          AqariButton(
            label: context.isArabic ? 'متابعة' : 'Continue',
            onPressed: () async {
              if (code.text.length != 6) return;
              try {
                final credential = await session.repository
                    .verifyPasswordResetOtp(phone, code.text);
                if (context.mounted) {
                  Navigator.pushReplacement(
                    context,
                    MaterialPageRoute(
                      builder: (_) => PasswordEntryPage(
                        session: session,
                        credential: credential,
                      ),
                    ),
                  );
                }
              } on ApiProblem catch (e) {
                if (context.mounted) AqariSnackbar.show(context, e.message);
              }
            },
          ),
        ],
      ),
    ),
  );
}

class PasswordEntryPage extends StatefulWidget {
  const PasswordEntryPage({
    required this.session,
    required this.credential,
    this.activation = false,
    this.tenantName,
    super.key,
  });
  final SessionController session;
  final String credential;
  final bool activation;
  final String? tenantName;
  @override
  State<PasswordEntryPage> createState() => _PasswordEntryPageState();
}

class _PasswordEntryPageState extends State<PasswordEntryPage> {
  final password = TextEditingController(), confirm = TextEditingController();
  bool busy = false;
  bool agreed = false;
  String? error;
  @override
  void dispose() {
    password.dispose();
    confirm.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AuthScaffold(
    onBack: () => Navigator.pop(context),
    title: widget.activation
        ? (context.isArabic ? 'تفعيل حساب المستأجر' : 'Activate tenant account')
        : (context.isArabic ? 'إعادة تعيين كلمة المرور' : 'Reset password'),
    subtitle: widget.activation && widget.tenantName != null
        ? '${context.isArabic ? 'مرحباً' : 'Welcome'} ${widget.tenantName}'
        : (context.isArabic
              ? 'اختر كلمة مرور قوية للمتابعة'
              : 'Choose a strong password to continue'),
    child: Column(
      children: [
        AuthPasswordField(
          controller: password,
          label: context.isArabic ? 'كلمة المرور الجديدة' : 'New password',
          enabled: !busy,
        ),
        const SizedBox(height: 16),
        AuthPasswordField(
          controller: confirm,
          label: context.isArabic ? 'تأكيد كلمة المرور' : 'Confirm password',
          enabled: !busy,
        ),
        if (widget.activation)
          AqariCheckbox(
            label: context.isArabic
                ? 'أوافق على الشروط وسياسة الخصوصية'
                : 'I agree to the terms and privacy policy',
            value: agreed,
            onChanged: busy ? null : (v) => setState(() => agreed = v ?? false),
          ),
        if (error != null) AuthBanner(message: error!),
        const SizedBox(height: 20),
        AqariButton(
          label: widget.activation
              ? (context.isArabic ? 'تفعيل الحساب' : 'Activate account')
              : (context.isArabic ? 'حفظ كلمة المرور' : 'Save password'),
          loading: busy,
          expanded: true,
          onPressed: busy || (widget.activation && !agreed) ? null : submit,
        ),
      ],
    ),
  );
  Future<void> submit() async {
    if (password.text.length < 8 || password.text != confirm.text) {
      setState(
        () => error = context.isArabic
            ? 'كلمتا المرور غير متطابقتين أو أقصر من 8 أحرف.'
            : 'Passwords do not match or are shorter than 8 characters.',
      );
      return;
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      if (widget.activation) {
        final profile = await widget.session.repository.activateTenant(
          widget.credential,
          password.text,
        );
        if (mounted) {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(
              builder: (_) => ActivationSuccessPage(
                session: widget.session,
                profile: profile,
              ),
            ),
          );
        }
      } else {
        await widget.session.repository.completePasswordReset(
          widget.credential,
          password.text,
        );
        if (mounted) Navigator.popUntil(context, (r) => r.isFirst);
      }
    } on ApiProblem catch (e) {
      if (mounted) setState(() => error = e.message);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }
}

class ActivationSuccessPage extends StatelessWidget {
  const ActivationSuccessPage({
    required this.session,
    required this.profile,
    super.key,
  });
  final SessionController session;
  final UserProfile profile;
  @override
  Widget build(BuildContext context) => AuthScaffold(
    centered: true,
    title: context.isArabic ? 'تم تفعيل حسابك' : 'Your account is active',
    subtitle:
        '${context.isArabic ? 'أهلاً بك' : 'Welcome'} ${profile.fullName}. ${context.isArabic ? 'يمكنك الآن متابعة عقدك ودفعاتك على عقاري.' : 'You can now access your lease and payments.'}',
    child: Column(
      children: [
        Icon(
          Icons.check_circle_rounded,
          size: 64,
          color: context.aqariColors.success,
        ),
        const SizedBox(height: AqariSpacing.x5),
        AqariButton(
          label: context.isArabic ? 'تسجيل الدخول' : 'Continue to sign in',
          expanded: true,
          onPressed: () async {
            await session.acceptAuthentication(Future.value(profile));
            if (context.mounted) Navigator.popUntil(context, (r) => r.isFirst);
          },
        ),
      ],
    ),
  );
}
