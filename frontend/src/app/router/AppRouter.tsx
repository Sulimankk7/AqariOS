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
 *     /buildings
 *       /buildings/:id
 *       /buildings/:buildingId/floors/new
 *       /buildings/:buildingId/floors/:floorId
 *       /buildings/:buildingId/floors/:floorId/apartments/new
 *     /apartments
 *       /apartments/:id (Canonical Apartment Details)
 */

import { BrowserRouter, Routes, Route, Navigate } from "react-router";
import { AuthLayout } from "@/app/layouts/AuthLayout";
import { AppLayout } from "@/app/layouts/AppLayout";
import { PlatformAdminLayout } from "@/app/layouts/PlatformAdminLayout";
import { ProtectedRoute } from "@/app/router/ProtectedRoute";
import { ModulePlaceholder } from "@/shared/components/layout/ModulePlaceholder";
import ParkingPage from '@/features/parking/ParkingPage';
import DocumentsPage from '@/features/documents/DocumentsPage';
import NotificationsPage from '@/features/notifications/pages/NotificationsPage';
import SettingsPage from '@/features/settings/SettingsPage';
import { ROUTES } from "@/config/routes";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTranslation } from "@/shared/i18n";

import { TenantLayout } from "@/app/layouts/TenantLayout";
import { TenantDashboardPage } from "@/features/tenantPortal/pages/TenantDashboardPage";
import { TenantProfilePage } from "@/features/tenantPortal/pages/TenantProfilePage";
import { TenantLeasePage } from "@/features/tenantPortal/pages/TenantLeasePage";
import { TenantPaymentsPage } from "@/features/tenantPortal/pages/TenantPaymentsPage";
import { TenantBillsPage } from "@/features/tenantPortal/pages/TenantBillsPage";

// Lucide icons for placeholders
import {
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
import PasswordResetVerifyPage from "@/features/auth/pages/PasswordResetVerify";
import ActivateTenantPage from "@/features/auth/pages/ActivateTenant";

// Real page implementations
import { DashboardPage } from "@/features/dashboard/pages/DashboardPage";
import BuildingsPage from "@/features/buildings/pages/BuildingsPage";
import CreateBuildingPage from "@/features/buildings/pages/CreateBuildingPage";
import EditBuildingPage from "@/features/buildings/pages/EditBuildingPage";
import BuildingDetailsPage from "@/features/buildings/pages/BuildingDetailsPage";

import CreateFloorPage from "@/features/floors/pages/CreateFloorPage";
import FloorDetailsPage from "@/features/floors/pages/FloorDetailsPage";

import ApartmentsPage from "@/features/apartments/pages/ApartmentsPage";
import CreateApartmentPage from "@/features/apartments/pages/CreateApartmentPage";
import EditApartmentPage from "@/features/apartments/pages/EditApartmentPage";
import ApartmentDetailsPage from "@/features/apartments/pages/ApartmentDetailsPage";

import LeasesPage from "@/features/leasing/pages/LeasesPage";
import CreateLeasePage from "@/features/leasing/pages/CreateLeasePage";
import EditLeasePage from "@/features/leasing/pages/EditLeasePage";
import LeaseDetailsPage from "@/features/leasing/pages/LeaseDetailsPage";

import TenantsPage from "@/features/tenants/pages/TenantsPage";
import CreateTenantPage from "@/features/tenants/pages/CreateTenantPage";
import EditTenantPage from "@/features/tenants/pages/EditTenantPage";
import TenantDetailsPage from "@/features/tenants/pages/TenantDetailsPage";

import { OwnerPaymentsWorkspace } from "@/features/payments";
import { FinancialOperationsPage } from "@/features/financials";
import { UtilityAccountsPage } from "@/features/utilityBills/pages/UtilityAccountsPage";
import { UtilityAccountDetailsPage } from "@/features/utilityBills/pages/UtilityAccountDetailsPage";
import { MaintenancePage } from "@/features/maintenance/pages/MaintenancePage";

import { AuthLoadingScreen } from "@/features/auth/components/AuthLoadingScreen";
import { PlatformAdminDashboardPage } from "@/features/platformAdmin/pages/PlatformAdminDashboardPage";
import { PlatformAdministratorsPage } from "@/features/platformAdmin/pages/PlatformAdministratorsPage";
import { LandlordRegistrationsPage } from "@/features/platformAdmin/pages/LandlordRegistrationsPage";
import { ContactRequestsPage } from "@/features/platformAdmin/pages/ContactRequestsPage";
import { CompanySubscriptionsPage } from "@/features/subscriptions/pages/CompanySubscriptionsPage";
import { PlatformPlansPage } from "@/features/subscriptions/pages/PlatformPlansPage";
import { PlatformSubscriptionsPage } from "@/features/subscriptions/pages/PlatformSubscriptionsPage";
import { PlatformPlanChangeRequestsPage } from "@/features/subscriptions/pages/PlatformPlanChangeRequestsPage";
import LandingPage from "@/features/landing/pages/LandingPage";
import { DemoExperience } from "@/features/demo/pages/DemoExperience";

/** Root redirect — sends authenticated users to their respective dashboard, others to /auth/login */
function RootRedirect() {
  const { isAuthenticated, authStatus, user } = useAuth();
  
  if (authStatus === "initializing") {
    return <AuthLoadingScreen />;
  }

  if (!isAuthenticated || !user || !user.roleCode || authStatus === "unsupported_role") {
    return <Navigate to={ROUTES.auth.login} replace />;
  }

  if (user.roleCode === "TENANT") {
    return <Navigate to="/tenant/dashboard" replace />;
  }

  if (user.roleCode === "COMPANY_ADMIN") {
    return <Navigate to={ROUTES.dashboard.root} replace />;
  }

  if (user.roleCode === "SYSTEM_ADMIN") {
    return <Navigate to={ROUTES.platform.dashboard} replace />;
  }

  // Unsupported role fallback (fails closed)
  return <Navigate to={ROUTES.auth.login} replace />;
}

export function AppRouter() {
  const { t } = useTranslation();
  return (
    <BrowserRouter>
      <Routes>
        {/* Public landing page — intentionally limited to the navbar for Section 01 */}
        <Route path="/" element={<LandingPage />} />
        <Route path="/demo/*" element={<DemoExperience />} />

        {/* ── Public Auth Routes ──────────────────────────────────────────── */}
        <Route path="/auth" element={<AuthLayout />}>
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
          <Route path="otp" element={<PhoneOtpPage />} />
          <Route path="verify" element={<VerifyOtpPage />} />
          <Route path="forgot-password" element={<ForgotPasswordPage />} />
          <Route path="reset-password" element={<ResetPasswordPage />} />
          <Route path="password-reset/verify" element={<PasswordResetVerifyPage />} />
          <Route path="activate" element={<ActivateTenantPage />} />
        </Route>

        {/* ── Protected Owner Routes ────────────────────────────────────────── */}
        <Route element={<ProtectedRoute allowedRoles={["COMPANY_ADMIN"]} />}>
          <Route element={<AppLayout />}>

            {/* Dashboard */}
            <Route path={ROUTES.dashboard.root} element={<DashboardPage />} />

            {/* Assets — Buildings Hierarchy */}
            <Route path={ROUTES.buildings.root}>
              <Route index element={<BuildingsPage />} />
              <Route path="new" element={<CreateBuildingPage />} />
              <Route path=":id" element={<BuildingDetailsPage />} />
              <Route path=":id/edit" element={<EditBuildingPage />} />
            </Route>

            {/* Floors Hierarchy (Nested under Building context) */}
            <Route path="/buildings/:buildingId/floors/new" element={<CreateFloorPage />} />
            <Route path="/buildings/:buildingId/floors/:floorId" element={<FloorDetailsPage />} />

            {/* Apartment Creation under Floor Context */}
            <Route path="/buildings/:buildingId/floors/:floorId/apartments/new" element={<CreateApartmentPage />} />
            <Route path="/floors/:floorId/apartments/new" element={<CreateApartmentPage />} />

            {/* Assets — Apartments (Global Discovery & Canonical Unit Details) */}
            <Route path={ROUTES.apartments.root}>
              <Route index element={<ApartmentsPage />} />
              <Route path="new" element={<CreateApartmentPage />} />
              <Route path=":id" element={<ApartmentDetailsPage />} />
              <Route path=":id/edit" element={<EditApartmentPage />} />
            </Route>

            {/* Assets — Parking */}
            <Route
              path={ROUTES.parking.root}
              element={
                <ParkingPage />
              }
            />

            {/* Leasing */}
            <Route path={ROUTES.leases.root}>
              <Route index element={<LeasesPage />} />
              <Route path="new" element={<CreateLeasePage />} />
              <Route path=":id" element={<LeaseDetailsPage />} />
              <Route path=":id/edit" element={<EditLeasePage />} />
            </Route>
            {/* Tenants */}
            <Route path={ROUTES.tenants.root}>
              <Route index element={<TenantsPage />} />
              <Route path="new" element={<CreateTenantPage />} />
              <Route path=":id" element={<TenantDetailsPage />} />
              <Route path=":id/edit" element={<EditTenantPage />} />
            </Route>

            {/* Payments */}
            <Route path={ROUTES.payments.root} element={<OwnerPaymentsWorkspace />} />

            {/* Finance */}
            <Route path={ROUTES.financialOperations.root} element={<FinancialOperationsPage />} />
            <Route path={ROUTES.utilityBills.root}>
              <Route index element={<UtilityAccountsPage />} />
              <Route path=":id" element={<UtilityAccountDetailsPage />} />
            </Route>
            <Route path={ROUTES.subscriptions.root} element={<CompanySubscriptionsPage />} />

            {/* Operations */}
            <Route
              path={ROUTES.maintenance.root}
              element={<MaintenancePage />}
            />
            <Route
              path={ROUTES.marketplace.root}
              element={
                <ModulePlaceholder
                  title={t("placeholders.marketplace.title")}
                  description={t("placeholders.marketplace.description")}
                  icon={Store}
                />
              }
            />
            <Route
              path={ROUTES.documents.root}
              element={
                <DocumentsPage />
              }
            />

            {/* System */}
            <Route
              path={ROUTES.notifications.root}
              element={
                <NotificationsPage />
              }
            />
            <Route
              path={ROUTES.settings.root}
              element={
                <SettingsPage />
              }
            />
            <Route
              path={ROUTES.profile.root}
              element={
                <ModulePlaceholder
                  title={t("placeholders.profile.title")}
                  description={t("placeholders.profile.description")}
                  icon={UserCircle}
                />
              }
            />
            <Route
              path={ROUTES.preferences.root}
              element={
                <ModulePlaceholder
                  title={t("placeholders.preferences.title")}
                  description={t("placeholders.preferences.description")}
                  icon={SlidersHorizontal}
                />
              }
            />
          </Route>
        </Route>

        {/* ── Protected Platform Administration Routes ───────────────────── */}
        <Route element={<ProtectedRoute allowedRoles={["SYSTEM_ADMIN"]} />}>
          <Route element={<PlatformAdminLayout />}>
            <Route path={ROUTES.platform.root} element={<Navigate to={ROUTES.platform.dashboard} replace />} />
            <Route path={ROUTES.platform.dashboard} element={<PlatformAdminDashboardPage />} />
            <Route path={ROUTES.platform.administrators} element={<PlatformAdministratorsPage />} />
            <Route element={<ProtectedRoute requiredPermissions={["platform.landlord_registrations.read"]} />}>
              <Route path={ROUTES.platform.landlordRegistrations} element={<LandlordRegistrationsPage />} />
            </Route>
            <Route element={<ProtectedRoute requiredPermissions={["platform.contact_requests.read"]} />}>
              <Route path={ROUTES.platform.contactRequests} element={<ContactRequestsPage />} />
            </Route>
            <Route element={<ProtectedRoute requiredPermissions={["platform.plans.read"]} />}>
              <Route path={ROUTES.platform.plans} element={<PlatformPlansPage />} />
            </Route>
            <Route element={<ProtectedRoute requiredPermissions={["platform.subscriptions.read"]} />}>
              <Route path={ROUTES.platform.subscriptions} element={<PlatformSubscriptionsPage />} />
            </Route>
            <Route element={<ProtectedRoute requiredPermissions={["platform.plan_change_requests.read"]} />}>
              <Route path={ROUTES.platform.planChangeRequests} element={<PlatformPlanChangeRequestsPage />} />
            </Route>
          </Route>
        </Route>

        {/* ── Protected Tenant Routes ────────────────────────────────────────── */}
        <Route element={<ProtectedRoute allowedRoles={["TENANT"]} />}>
          <Route element={<TenantLayout />}>
            <Route path="/tenant/dashboard" element={<TenantDashboardPage />} />
            <Route path="/tenant/lease" element={<TenantLeasePage />} />
            <Route path="/tenant/payments" element={<TenantPaymentsPage />} />
            <Route path={ROUTES.tenant.bills} element={<TenantBillsPage />} />
            <Route path="/tenant/profile" element={<TenantProfilePage />} />
          </Route>

        </Route>

        {/* Catch-all: redirect to root (which is auth-aware) */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
