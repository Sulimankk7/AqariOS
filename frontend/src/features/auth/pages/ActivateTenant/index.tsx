import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router';
import { KeyRound, Lock, Eye, EyeOff, CheckCircle2, AlertCircle, Clock, ShieldAlert, ArrowRight } from 'lucide-react';
import { FieldLabel, inputClass } from '@/features/auth/components/FieldLabel';
import { ArchitecturalButton } from '@/features/auth/components/ArchitecturalButton';
import { authApi } from '@/features/auth/api/auth.api';
import { useAuth } from '@/features/auth/hooks/useAuth';
import { extractUserFriendlyError } from '@/shared/utils';
import type { TenantActivationStatusDto } from '@/features/auth/types/auth.types';

export default function ActivateTenantPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const navigate = useNavigate();
  const { login } = useAuth();

  const [isValidating, setIsValidating] = useState(true);
  const [statusData, setStatusData] = useState<TenantActivationStatusDto | null>(null);

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const isMismatch = confirmPassword.length > 0 && password !== confirmPassword;
  const isTooShort = password.length > 0 && password.length < 8;

  useEffect(() => {
    let isMounted = true;

    async function validateToken() {
      if (!token) {
        if (isMounted) {
          setStatusData({
            status: 'INVALID',
            message: 'رابط التفعيل غير صالح أو مفقود.',
          });
          setIsValidating(false);
        }
        return;
      }

      setIsValidating(true);
      try {
        const result = await authApi.getTenantActivationStatus(token.trim());
        if (isMounted) {
          setStatusData(result);
        }
      } catch (err: any) {
        if (isMounted) {
          setStatusData({
            status: 'INVALID',
            message: 'تعذر التحقق من رابط التفعيل. يرجى المحاولة لاحقاً أو التواصل مع إدارة العقار.',
          });
        }
      } finally {
        if (isMounted) {
          setIsValidating(false);
        }
      }
    }

    validateToken();

    return () => {
      isMounted = false;
    };
  }, [token]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (!token) {
      setErrorMessage('رابط التفعيل غير صالح أو مفقود.');
      return;
    }

    if (password.length < 8) {
      setErrorMessage('يجب أن تتكون كلمة المرور من 8 خانات على الأقل.');
      return;
    }

    if (password !== confirmPassword) {
      setErrorMessage('كلمتا المرور غير متطابقتين.');
      return;
    }

    setIsLoading(true);

    try {
      const response = await authApi.activateTenant({
        activationToken: token.trim(),
        password,
      });

      // Authenticate session & save profile atomically via useAuth login helper
      await login(response.accessToken, response.user);

      // Redirect tenant directly to Tenant Portal Dashboard
      navigate('/tenant/dashboard', { replace: true });
    } catch (err: any) {
      const fallback = 'فشل تفعيل الحساب. قد يكون رابط التفعيل منتهياً أو تم استخدامه مسبقاً.';
      setErrorMessage(extractUserFriendlyError(err, fallback));
    } finally {
      setIsLoading(false);
    }
  };

  // 1. Loading Preflight State
  if (isValidating) {
    return (
      <div className="w-full max-w-md mx-auto space-y-6 text-center py-12">
        <div className="inline-flex items-center justify-center w-12 h-12 rounded-full bg-primary/10 border border-primary/20 text-primary animate-pulse mb-3">
          <KeyRound className="w-6 h-6" />
        </div>
        <h2 className="text-lg font-semibold text-white">جارٍ التحقق من رابط التفعيل...</h2>
        <p className="text-xs text-gray-400">يرجى الانتظار لحظات ريثما يتم تأكيد صلاحية الرابط.</p>
      </div>
    );
  }

  // 2. Expired Token State
  if (statusData?.status === 'EXPIRED') {
    return (
      <div className="w-full max-w-md mx-auto space-y-6">
        <div className="p-6 rounded-2xl bg-[#161B22] border border-amber-500/30 text-right space-y-4 shadow-xl">
          <div className="w-12 h-12 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 flex items-center justify-center">
            <Clock className="w-6 h-6" />
          </div>

          <div className="space-y-1.5">
            <h2 className="text-xl font-bold text-white">انتهت صلاحية رابط التفعيل</h2>
            <p className="text-xs text-amber-200/90 leading-relaxed">
              انتهت صلاحية رابط التفعيل هذا (صلاحية الرابط 48 ساعة من وقت إصداره).
            </p>
            <p className="text-xs text-gray-400 leading-relaxed pt-1">
              يرجى التواصل مع إدارة العقار لطلب إرسال رابط تفعيل جديد لحسابك.
            </p>
          </div>

          <div className="pt-2">
            <ArchitecturalButton
              type="button"
              onClick={() => navigate('/auth/login')}
              isDark={true}
              className="w-full"
            >
              العودة إلى تسجيل الدخول
            </ArchitecturalButton>
          </div>
        </div>
      </div>
    );
  }

  // 3. Already Used / Invalid / Not Found State
  if (statusData?.status === 'ALREADY_USED' || statusData?.status === 'INVALID' || statusData?.status === 'NOT_FOUND' || !token) {
    const isUsed = statusData?.status === 'ALREADY_USED';

    return (
      <div className="w-full max-w-md mx-auto space-y-6">
        <div className="p-6 rounded-2xl bg-[#161B22] border border-white/10 text-right space-y-4 shadow-xl">
          <div className="w-12 h-12 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 flex items-center justify-center">
            <ShieldAlert className="w-6 h-6" />
          </div>

          <div className="space-y-1.5">
            <h2 className="text-xl font-bold text-white">
              {isUsed ? 'تم استخدام رابط التفعيل هذا مسبقًا' : 'رابط التفعيل غير صالح'}
            </h2>
            <p className="text-xs text-gray-300 leading-relaxed">
              {isUsed
                ? 'تم استخدام هذا الرابط لإنشاء كلمة المرور وتفعيل الحساب مسبقاً. لا يمكن استخدام الرابط لمرة ثانية.'
                : statusData?.message || 'رابط التفعيل غير صالح أو مفقود. يرجى التأكد من الرابط أو طلب رابط جديد.'}
            </p>
            {isUsed && (
              <p className="text-xs text-emerald-400/90 pt-1">
                إذا كنت قد فعّلت حسابك بالفعل، يمكنك تسجيل الدخول مباشرة باستخدام بريدك الإلكتروني أو رقم هاتفك وكلمة المرور.
              </p>
            )}
          </div>

          <div className="pt-2">
            <ArchitecturalButton
              type="button"
              onClick={() => navigate('/auth/login')}
              isDark={true}
              className="w-full"
            >
              الانتقال إلى تسجيل الدخول
            </ArchitecturalButton>
          </div>
        </div>
      </div>
    );
  }

  // 4. Valid Token State -> Render Password Creation Form
  return (
    <div className="w-full max-w-md mx-auto space-y-6">
      <div className="text-right">
        <h1 className="text-2xl font-semibold tracking-tight text-white mb-1">
          {statusData?.tenantName ? `مرحباً بك، ${statusData.tenantName}` : 'مرحباً بك في عقاري'}
        </h1>
        <p className="text-[13.5px] text-gray-400">
          تفعيل حساب بوابة المستأجر — أنشئ كلمة المرور الخاصة بك للبدء باستخدام البوابة.
        </p>
      </div>

      <div className="p-3.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 text-xs flex items-center gap-2.5 text-right">
        <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-400" />
        <span>رابط التفعيل صالح ويمكنك الآن إنشاء كلمة المرور.</span>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        {errorMessage && (
          <div className="p-3.5 rounded-xl bg-destructive/15 border border-destructive/30 text-red-300 text-xs flex items-center gap-2 text-right">
            <AlertCircle className="w-4 h-4 shrink-0 text-red-400" />
            <span>{errorMessage}</span>
          </div>
        )}

        <div>
          <FieldLabel isDark={true}>كلمة المرور الجديدة</FieldLabel>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none text-gray-500">
              <KeyRound size={15} />
            </span>
            <input
              type={showPassword ? 'text' : 'password'}
              required
              placeholder="أدخل كلمة مرور قوية (8 خانات على الأقل)"
              value={password}
              onChange={(e) => {
                setPassword(e.target.value);
                if (errorMessage) setErrorMessage(null);
              }}
              className={`${inputClass} pl-9 pr-10`}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer text-gray-500 hover:text-gray-300"
            >
              {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          {isTooShort && (
            <span className="text-[11.5px] text-amber-400 mt-1 block text-right">
              يجب أن تكون كلمة المرور 8 خانات على الأقل.
            </span>
          )}
        </div>

        <div>
          <FieldLabel isDark={true}>تأكيد كلمة المرور</FieldLabel>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none text-gray-500">
              <Lock size={15} />
            </span>
            <input
              type="password"
              required
              placeholder="أعد إدخال كلمة المرور للتأكيد"
              value={confirmPassword}
              onChange={(e) => {
                setConfirmPassword(e.target.value);
                if (errorMessage) setErrorMessage(null);
              }}
              className={`${inputClass} pl-9 ${isMismatch ? 'border-red-400 focus:border-red-500' : ''}`}
            />
          </div>
          {isMismatch && (
            <span className="text-[11.5px] text-red-400 mt-1 block text-right">
              كلمتا المرور غير متطابقتين.
            </span>
          )}
        </div>

        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText="جارٍ تفعيل الحساب..."
          disabled={isLoading || isMismatch || isTooShort || !password}
          isDark={true}
        >
          تفعيل الحساب
        </ArchitecturalButton>
      </form>
    </div>
  );
}
