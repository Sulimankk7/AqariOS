import React from "react";
import { Navigate, Outlet, useLocation } from "react-router";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ROUTES } from "@/config/routes";

export function ProtectedRoute() {
  const { isAuthenticated, authStatus } = useAuth();
  const location = useLocation();

  if (authStatus === "initializing") {
    return null;
  }

  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`${ROUTES.auth.login}?returnUrl=${returnUrl}`} replace />;
  }

  return <Outlet />;
}
