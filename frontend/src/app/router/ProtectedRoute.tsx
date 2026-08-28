import React from "react";
import { Navigate, Outlet, useLocation } from "react-router";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ROUTES } from "@/config/routes";
import { AppRole } from "@/features/auth/providers/AuthProvider";
import { AuthLoadingScreen } from "@/features/auth/components/AuthLoadingScreen";
import { ErrorState } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";

interface ProtectedRouteProps {
  allowedRoles?: AppRole[];
  requiredPermissions?: string[];
}

export function ProtectedRoute({ allowedRoles, requiredPermissions }: ProtectedRouteProps) {
  const { isAuthenticated, authStatus, user } = useAuth();
  const { t } = useTranslation();
  const location = useLocation();

  if (authStatus === "initializing") {
    return <AuthLoadingScreen />;
  }

  if (!isAuthenticated || !user || !user.roleCode || authStatus === "unsupported_role") {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`${ROUTES.auth.login}?returnUrl=${returnUrl}`} replace />;
  }

  // Strict role check: Never allow fallthrough if role mismatch occurs
  if (allowedRoles && allowedRoles.length > 0) {
    if (!allowedRoles.includes(user.roleCode)) {
      if (user.roleCode === "TENANT") {
        return <Navigate to="/tenant/dashboard" replace />;
      }
      if (user.roleCode === "COMPANY_ADMIN") {
        return <Navigate to={ROUTES.dashboard.root} replace />;
      }
      if (user.roleCode === "SYSTEM_ADMIN") {
        return <Navigate to={ROUTES.platform.dashboard} replace />;
      }
      return <Navigate to={ROUTES.auth.login} replace />;
    }
  }

  if (requiredPermissions?.some((permission) => !user.permissions?.includes(permission))) {
    return <main className="mx-auto max-w-2xl p-8"><ErrorState title={t("platformAdmin.errors.forbidden")} /></main>;
  }

  return <Outlet />;
}
