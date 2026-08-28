/**
 * Auth feature — UI translations.
 * Exact string copy from the Figma-generated design.
 * Do NOT rename any keys — they are used directly across all auth pages.
 */

export type TranslationKey = keyof typeof TRANSLATIONS.en;

export const TRANSLATIONS = {
  en: {
    platformSubtitle: "Property Management Platform",
    copyright: "© 2026 AqariOS. All rights reserved.",
    privacy: "Privacy Policy",
    terms: "Terms of Service",
    security: "Security",
    version: "v1.0.0",

    // Top navigation
    signInTab: "Sign in",
    createAccountTab: "Create account",
    otpTab: "Phone OTP",
    forgotTab: "Reset access",

    // Login
    welcomeBack: "Welcome back",
    loginSubtitle: "Sign in to your enterprise AqariOS workspace",
    passwordLoginTab: "Password",
    otpLoginTab: "Phone OTP",
    emailLabel: "Email or phone number",
    emailPlaceholder: "Email or phone number",
    passwordLabel: "Password",
    passwordPlaceholder: "Enter your password",
    rememberMe: "Remember me for 30 days",
    forgotPassword: "Forgot password?",
    signInBtn: "Sign in to Workspace",
    orDivider: "or continue with",
    googleSSO: "Sign in with Google SSO",
    noAccount: "Don't have an enterprise account?",
    signUpLink: "Create account",
    phoneOtpAlt: "Prefer passwordless? Sign in with Phone OTP",

    // Registration
    createAccountTitle: "Register company account",
    createAccountSubtitle: "Set up your AqariOS organization workspace",
    fullNameLabel: "Full name",
    fullNamePlaceholder: "Sarah Al-Mansoor",
    companyNameLabel: "Company name",
    companyNamePlaceholder: "Emaar Properties PJSC",
    displayNameLabel: "Display name",
    displayNamePlaceholder: "Emaar Management",
    displayNameOptional: "(optional)",
    companyTypeLabel: "Company type",
    companyTypeSelect: "Select organization type",
    phoneLabel: "Phone number",
    phonePlaceholder: "50 123 4567",
    confirmPasswordLabel: "Confirm password",
    confirmPasswordPlaceholder: "Re-enter your password",
    prefLangLabel: "Preferred system language",
    agreeTermsPrefix: "I agree to the ",
    agreeTermsAnd: " and ",
    createAccountBtn: "Complete Registration",
    alreadyHaveAccount: "Already registered?",
    signInLink: "Sign in",
    passwordMismatch: "Passwords do not match",

    // Phone OTP Login
    phoneOtpTitle: "Sign in with Phone OTP",
    phoneOtpSubtitle:
      "We'll send a 6-digit SMS verification code to your registered mobile number",
    sendCodeBtn: "Send Verification Code",
    backToPasswordLogin: "Back to password sign in",

    // OTP Verify
    otpVerifyTitle: "Verify mobile number",
    otpVerifySubtitle: "Enter the 6-digit code sent via SMS to",
    changePhone: "Change number",
    didNotReceive: "Didn't receive code?",
    resendIn: "Resend code in",
    resendNow: "Resend verification code",
    verifyCodeBtn: "Verify & Continue",

    // Forgot Password
    forgotTitle: "Reset your password",
    forgotSubtitle:
      "Enter your registered email or phone number to receive recovery instructions",
    methodEmail: "Email recovery",
    methodPhone: "SMS recovery",
    sendResetBtn: "Send Recovery Link",
    resetInstructionsSent: "Recovery instructions sent! Check your inbox or phone.",

    // Reset Password
    resetTitle: "Set new password",
    resetSubtitle: "Create a strong password for your AqariOS administrator account",
    newPasswordLabel: "New password",
    newPasswordPlaceholder: "At least 8 characters",
    saveNewPasswordBtn: "Update Password & Sign In",
    passwordResetSuccess: "Password updated successfully. Redirecting to workspace...",

    // Form feedback
    signingIn: "Authenticating...",
    creatingAccount: "Creating workspace...",
    sendingCode: "Sending SMS code...",
    verifying: "Verifying code...",
    updating: "Updating password...",
    
    // Errors
    pendingApproval: "Your account is pending approval. You will be able to access the platform once your registration is approved by the system administrator.",
    fixErrors: "Please correct the following errors:",
  },

  ar: {
    platformSubtitle: "منصة إدارة العقارات المؤسسية",
    copyright: "© ٢٠٢٦ عقاري أو إس. جميع الحقوق محفوظة.",
    privacy: "سياسة الخصوصية",
    terms: "شروط الخدمة",
    security: "الأمان",
    version: "الإصدار ١,٠,٠",

    // Top navigation
    signInTab: "تسجيل الدخول",
    createAccountTab: "إنشاء حساب",
    otpTab: "رمز الهاتف",
    forgotTab: "استعادة الدخول",

    // Login
    welcomeBack: "مرحباً بك مجدداً",
    loginSubtitle: "سجّل الدخول إلى مساحة عمل عقاري أو إس المؤسسية",
    passwordLoginTab: "كلمة المرور",
    otpLoginTab: "رمز الهاتف (OTP)",
    emailLabel: "البريد الإلكتروني أو رقم الهاتف",
    emailPlaceholder: "البريد الإلكتروني أو رقم الهاتف",
    passwordLabel: "كلمة المرور",
    passwordPlaceholder: "أدخل كلمة المرور الخاصة بك",
    rememberMe: "تذكرني لمدة ٣٠ يوماً",
    forgotPassword: "نسيت كلمة المرور؟",
    signInBtn: "تسجيل الدخول لمساحة العمل",
    orDivider: "أو المتابعة عبر",
    googleSSO: "المتابعة باستخدام Google SSO",
    noAccount: "ليس لديك حساب مؤسسي؟",
    signUpLink: "أنشئ حساباً جديداً",
    phoneOtpAlt: "تفضل الدخول بدون كلمة مرور؟ استخدم رمز SMS",

    // Registration
    createAccountTitle: "تسجيل حساب شركة جديد",
    createAccountSubtitle: "قم بإعداد مساحة عمل مؤسستك على عقاري أو إس",
    fullNameLabel: "الاسم الكامل",
    fullNamePlaceholder: "سارة المنصور",
    companyNameLabel: "اسم الشركة",
    companyNamePlaceholder: "شركة إعمار العقارية",
    displayNameLabel: "الاسم المعروض",
    displayNamePlaceholder: "إدارة إعمار",
    displayNameOptional: "(اختياري)",
    companyTypeLabel: "نوع الشركة",
    companyTypeSelect: "اختر نوع المنشأة",
    phoneLabel: "رقم الهاتف",
    phonePlaceholder: "50 123 4567",
    confirmPasswordLabel: "تأكيد كلمة المرور",
    confirmPasswordPlaceholder: "أعد إدخال كلمة المرور",
    prefLangLabel: "لغة النظام المفضلة",
    agreeTermsPrefix: "أوافق على ",
    agreeTermsAnd: " و ",
    createAccountBtn: "إتمام التسجيل",
    alreadyHaveAccount: "لديك حساب بالفعل؟",
    signInLink: "تسجيل الدخول",
    passwordMismatch: "كلمتا المرور غير متطابقتين",

    // Phone OTP Login
    phoneOtpTitle: "الدخول برمز الهاتف SMS",
    phoneOtpSubtitle: "سنرسل رمز تحقق مكوناً من ٦ أرقام إلى هاتفك المحمول المسجل",
    sendCodeBtn: "إرسال رمز التحقق",
    backToPasswordLogin: "العودة لتسجيل الدخول بكلمة المرور",

    // OTP Verify
    otpVerifyTitle: "تأكيد رقم الهاتف المحمول",
    otpVerifySubtitle: "أدخل رمز التحقق المكون من ٦ أرقام المرسل عبر SMS إلى",
    changePhone: "تغيير الرقم",
    didNotReceive: "لم تصلك الرسالة؟",
    resendIn: "إعادة الإرسال خلال",
    resendNow: "إعادة إرسال رمز التحقق",
    verifyCodeBtn: "التحقق والمتابعة",

    // Forgot Password
    forgotTitle: "إعادة ضبط كلمة المرور",
    forgotSubtitle: "أدخل بريدك الإلكتروني أو رقم هاتفك المسجل لاستلام تعليمات الاستعادة",
    methodEmail: "البريد الإلكتروني",
    methodPhone: "رسالة SMS",
    sendResetBtn: "إرسال رابط الاستعادة",
    resetInstructionsSent: "تم إرسال تعليمات الاستعادة! تفقد بريدك أو هاتفك.",

    // Reset Password
    resetTitle: "تعيين كلمة مرور جديدة",
    resetSubtitle: "أنشئ كلمة مرور قوية لحساب المسؤول الخاص بك",
    newPasswordLabel: "كلمة المرور الجديدة",
    newPasswordPlaceholder: "٨ أحرف على الأقل",
    saveNewPasswordBtn: "حفظ كلمة المرور وتسجيل الدخول",
    passwordResetSuccess: "تم تحديث كلمة المرور بنجاح. جاري التوجيه...",

    // Form feedback
    signingIn: "جاري المصادقة...",
    creatingAccount: "جاري إنشاء مساحة العمل...",
    sendingCode: "جاري إرسال الرمز...",
    verifying: "جاري التحقق...",
    updating: "جاري التحديث...",
    
    // Errors
    pendingApproval: "حسابك قيد المراجعة ولم تتم الموافقة عليه بعد. ستتمكن من الدخول إلى المنصة بعد موافقة إدارة النظام.",
    fixErrors: "يرجى تصحيح الأخطاء التالية:"
  },
} as const;

export type SupportedTranslationLang = keyof typeof TRANSLATIONS;
export type Translations = (typeof TRANSLATIONS)[SupportedTranslationLang];
