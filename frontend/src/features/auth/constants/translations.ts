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
    resetRequestGeneric: "If an eligible account exists, recovery instructions have been sent.",
    checkResetEmail: "Check your email for instructions to reset your password.",
    resetCodeTitle: "Verify password reset code",
    resetCodeSubtitle: "Enter the 6-digit password recovery code sent to your phone.",
    resetCodeInvalidAccess: "Start from Forgot Password to request a new recovery code.",
    resetCredentialMissing: "This password reset link or authorization is missing or invalid.",
    passwordPolicy: "Use at least 8 characters with uppercase, lowercase, and a number.",

    // Reset Password
    resetTitle: "Set new password",
    resetSubtitle: "Create a strong password for your AqariOS administrator account",
    newPasswordLabel: "New password",
    newPasswordPlaceholder: "At least 8 characters",
    saveNewPasswordBtn: "Update Password",
    passwordResetSuccess: "Password updated successfully. Please sign in.",

    // Form feedback
    signingIn: "Authenticating...",
    creatingAccount: "Creating workspace...",
    sendingCode: "Sending SMS code...",
    verifying: "Verifying code...",
    updating: "Updating password...",
    loginSuccess: "Authentication successful. Welcome back!",
    registrationSubmitted: "Your registration request was received and is awaiting review.",
    otpResent: "Verification code resent successfully!",
    verificationSuccess: "Verification successful. Welcome!",
    googleUnavailable: "Google sign-in is not available yet.",
    loginError: "Unable to sign in. Check your details and try again.",
    registrationError: "Unable to submit the registration request. Please try again.",
    otpError: "The verification code is invalid or expired. Please try again.",
    togglePasswordVisibility: "Toggle password visibility",
    
    // Errors
    pendingApproval: "Your account is pending approval. You will be able to access the platform once your registration is approved by the system administrator.",
    fixErrors: "Please correct the following errors:",
  },

  ar: {
    platformSubtitle: "منصة إدارة العقارات المؤسسية",
    copyright: "© ٢٠٢٦ عقاري . جميع الحقوق محفوظة.",
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
    loginSubtitle: "سجّل الدخول إلى مساحة عمل نظام عقاري ",
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
    noAccount: "ليس لديك حساب مؤسسي؟",
    signUpLink: "أنشئ حساباً جديداً",
    phoneOtpAlt: "تفضل الدخول بدون كلمة مرور؟ استخدم رمز SMS",

    // Registration
    createAccountTitle: "تسجيل حساب شركة جديد",
    createAccountSubtitle: "قم بإعداد مساحة عمل مؤسستك على عقاري ",
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
    resetRequestGeneric: "إذا كان الحساب مؤهلًا، فقد تم إرسال تعليمات استعادة كلمة المرور.",
    checkResetEmail: "تحقق من بريدك الإلكتروني للاطلاع على تعليمات إعادة تعيين كلمة المرور.",
    resetCodeTitle: "التحقق من رمز إعادة تعيين كلمة المرور",
    resetCodeSubtitle: "أدخل رمز استعادة كلمة المرور المكون من 6 أرقام والمرسل إلى هاتفك.",
    resetCodeInvalidAccess: "ابدأ من صفحة نسيت كلمة المرور لطلب رمز استعادة جديد.",
    resetCredentialMissing: "رابط أو تفويض إعادة تعيين كلمة المرور مفقود أو غير صالح.",
    passwordPolicy: "استخدم 8 أحرف على الأقل، تتضمن حرفًا كبيرًا وحرفًا صغيرًا ورقمًا.",

    // Reset Password
    resetTitle: "تعيين كلمة مرور جديدة",
    resetSubtitle: "أنشئ كلمة مرور قوية لحساب المسؤول الخاص بك",
    newPasswordLabel: "كلمة المرور الجديدة",
    newPasswordPlaceholder: "٨ أحرف على الأقل",
    saveNewPasswordBtn: "حفظ كلمة المرور",
    passwordResetSuccess: "تم تحديث كلمة المرور بنجاح. يرجى تسجيل الدخول.",

    // Form feedback
    signingIn: "جاري المصادقة...",
    creatingAccount: "جاري إنشاء مساحة العمل...",
    sendingCode: "جاري إرسال الرمز...",
    verifying: "جاري التحقق...",
    updating: "جاري التحديث...",
    loginSuccess: "تم تسجيل الدخول بنجاح. أهلاً بعودتك!",
    registrationSubmitted: "تم استلام طلب التسجيل وهو الآن بانتظار المراجعة.",
    otpResent: "تمت إعادة إرسال رمز التحقق بنجاح!",
    verificationSuccess: "تم التحقق بنجاح. أهلاً بك!",
    googleUnavailable: "تسجيل الدخول باستخدام Google غير متاح حالياً.",
    loginError: "تعذر تسجيل الدخول. تحقق من بياناتك وحاول مجدداً.",
    registrationError: "تعذر إرسال طلب التسجيل. حاول مجدداً.",
    otpError: "رمز التحقق غير صالح أو منتهي الصلاحية. حاول مجدداً.",
    togglePasswordVisibility: "إظهار أو إخفاء كلمة المرور",
    
    // Errors
    pendingApproval: "حسابك قيد المراجعة ولم تتم الموافقة عليه بعد. ستتمكن من الدخول إلى المنصة بعد موافقة إدارة النظام.",
    fixErrors: "يرجى تصحيح الأخطاء التالية:"
  },
} as const;

export type SupportedTranslationLang = keyof typeof TRANSLATIONS;
export type Translations = (typeof TRANSLATIONS)[SupportedTranslationLang];
