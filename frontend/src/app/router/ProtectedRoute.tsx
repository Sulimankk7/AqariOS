import React from "react";
import { Navigate, Outlet, useLocation } from "react-router";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ROUTES } from "@/config/routes";

export function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    // Redirect to login but save the attempted url
    return <Navigate to={ROUTES.auth.login} state={{ from: location }} replace />;
  }

  return <Outlet />;
}
