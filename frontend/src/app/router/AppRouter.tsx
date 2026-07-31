/**
 * AppRouter — Application route configuration.
 *
 * Route Architecture:
 * /                     → RootRedirect (auth-aware)
 * /auth/*               → AuthLayout (public)
 *   login, register, otp, verify, forgot-password, reset-password
 * [ProtectedRoute]      → requires isAuthenticated
 *   [AppLayout]         → sidebar + topbar shell
 *     /dashboard
 *     /buildings        /apartments      /parking
 *     /leases           /tenants
 *     /payments         /financial-operations
 *     /maintenance      /marketplace     /documents
 *     /notifications    /settings        /profile   /preferences
 */

import { BrowserRouter, Routes, Route, Navigate } from "react-router";
import { AuthLayout } from "@/app/layouts/AuthLayout";
import { AppLayout } from "@/app/layouts/AppLayout";
import { ProtectedRoute } from "@/app/router/ProtectedRoute";
import { ModulePlaceholder } from "@/shared/components/layout/ModulePlaceholder";
import { ROUTES } from "@/config/routes";
import { useAuth } from "@/features/auth/hooks/useAuth";

// Lucide icons for placeholders
import {
  Building2,
  Home,
  Car,
  FileText,
  Users,
  Wallet,
  Calculator,
  Wrench,
  Store,
  FolderOpen,
  Bell,
  Settings,
  UserCircle,
  SlidersHorizontal,
} from "lucide-react";

// Auth pages
import LoginPage from "@/features/auth/pages/Login";
import RegisterPage from "@/features/auth/pages/Register";
import PhoneOtpPage from "@/features/auth/pages/PhoneOtp";
import VerifyOtpPage from "@/features/auth/pages/VerifyOtp";
import ForgotPasswordPage from "@/features/auth/pages/ForgotPassword";
import ResetPasswordPage from "@/features/auth/pages/ResetPassword";

// Real page implementations
import { DashboardPage } from "@/features/dashboard/pages/DashboardPage";

/** Root redirect — sends authenticated users to /dashboard, others to /auth/login */
function RootRedirect() {
  const { isAuthenticated } = useAuth();
  return isAuthenticated
    ? <Navigate to={ROUTES.dashboard.root} replace />
    : <Navigate to={ROUTES.auth.login} replace />;
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Root: auth-aware redirect */}
        <Route path="/" element={<RootRedirect />} />

        {/* ── Public Auth Routes ──────────────────────────────────────────── */}
        <Route path="/auth" element={<AuthLayout />}>
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
          <Route path="otp" element={<PhoneOtpPage />} />
          <Route path="verify" element={<VerifyOtpPage />} />
          <Route path="forgot-password" element={<ForgotPasswordPage />} />
          <Route path="reset-password" element={<ResetPasswordPage />} />
        </Route>

        {/* ── Protected App Routes ────────────────────────────────────────── */}
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>

            {/* Dashboard */}
            <Route path={ROUTES.dashboard.root} element={<DashboardPage />} />

            {/* Assets — Property */}
            <Route
              path={ROUTES.buildings.root}
              element={
                <ModulePlaceholder
                  title="Buildings"
                  description="Manage your building portfolio, floors, and physical assets."
                  icon={Building2}
                />
              }
            />
            <Route
              path={ROUTES.apartments.root}
              element={
                <ModulePlaceholder
                  title="Apartments"
                  description="View and manage individual apartment units across all buildings."
                  icon={Home}
                />
              }
            />
            <Route
              path={ROUTES.parking.root}
              element={
                <ModulePlaceholder
                  title="Parking"
                  description="Track parking spaces, assignments, and availability."
                  icon={Car}
                />
              }
            />

            {/* Leasing */}
            <Route
              path={ROUTES.leases.root}
              element={
                <ModulePlaceholder
                  title="Leases"
                  description="Manage tenant contracts, renewals, and lease agreements."
                  icon={FileText}
                />
              }
            />
            <Route
              path={ROUTES.tenants.root}
              element={
                <ModulePlaceholder
                  title="Tenants"
                  description="Maintain tenant profiles, communications, and history."
                  icon={Users}
                />
              }
            />

            {/* Finance */}
            <Route
              path={ROUTES.payments.root}
              element={
                <ModulePlaceholder
                  title="Payments"
                  description="Process rent collections, invoicing, and payment tracking."
                  icon={Wallet}
                />
              }
            />
            <Route
              path={ROUTES.financialOperations.root}
              element={
                <ModulePlaceholder
                  title="Financial Operations"
                  description="Accounting, expense management, and financial reporting."
                  icon={Calculator}
                />
              }
            />

            {/* Operations */}
            <Route
              path={ROUTES.maintenance.root}
              element={
                <ModulePlaceholder
                  title="Maintenance"
                  description="Submit and track work orders and facilities requests."
                  icon={Wrench}
                />
              }
            />
            <Route
              path={ROUTES.marketplace.root}
              element={
                <ModulePlaceholder
                  title="Marketplace"
                  description="Discover integrations, add-ons, and third-party services."
                  icon={Store}
                />
              }
            />
            <Route
              path={ROUTES.documents.root}
              element={
                <ModulePlaceholder
                  title="Documents"
                  description="Central repository for contracts, reports, and e-signatures."
                  icon={FolderOpen}
                />
              }
            />

            {/* System */}
            <Route
              path={ROUTES.notifications.root}
              element={
                <ModulePlaceholder
                  title="Notifications"
                  description="View alerts, system messages, and activity updates."
                  icon={Bell}
                />
              }
            />
            <Route
              path={ROUTES.settings.root}
              element={
                <ModulePlaceholder
                  title="Settings"
                  description="Configure system preferences, users, and integrations."
                  icon={Settings}
                />
              }
            />
            <Route
              path={ROUTES.profile.root}
              element={
                <ModulePlaceholder
                  title="Profile"
                  description="Manage your personal information and account details."
                  icon={UserCircle}
                />
              }
            />
            <Route
              path={ROUTES.preferences.root}
              element={
                <ModulePlaceholder
                  title="Preferences"
                  description="Customize your workspace appearance, language, and notifications."
                  icon={SlidersHorizontal}
                />
              }
            />
          </Route>
        </Route>

        {/* Catch-all: redirect to root (which is auth-aware) */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
