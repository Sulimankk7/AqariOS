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

import { lazy, Suspense } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router";
import { SeoManager } from "@/shared/seo/SeoManager";
import { ProtectedRoute } from "@/app/router/ProtectedRoute";
import { ModulePlaceholder } from "@/shared/components/layout/ModulePlaceholder";
import { ROUTES } from "@/config/routes";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTranslation } from "@/shared/i18n";

// Lucide icons for placeholders
import {
  Store,
  UserCircle,
  SlidersHorizontal,
} from "lucide-react";

import { AuthLoadingScreen } from "@/features/auth/components/AuthLoadingScreen";

const AuthLayout = lazy(() => import("@/app/layouts/AuthLayout").then((module) => ({ default: module.AuthLayout })));
const AppLayout = lazy(() => import("@/app/layouts/AppLayout").then((module) => ({ default: module.AppLayout })));
const PlatformAdminLayout = lazy(() => import("@/app/layouts/PlatformAdminLayout").then((module) => ({ default: module.PlatformAdminLayout })));
const TenantLayout = lazy(() => import("@/app/layouts/TenantLayout").then((module) => ({ default: module.TenantLayout })));
const LandingPage = lazy(() => import("@/features/landing/pages/LandingPage"));
const DemoExperience = lazy(() => import("@/features/demo/pages/DemoExperience").then((module) => ({ default: module.DemoExperience })));

const LoginPage = lazy(() => import("@/features/auth/pages/Login"));
const RegisterPage = lazy(() => import("@/features/auth/pages/Register"));
const PhoneOtpPage = lazy(() => import("@/features/auth/pages/PhoneOtp"));
const VerifyOtpPage = lazy(() => import("@/features/auth/pages/VerifyOtp"));
const ForgotPasswordPage = lazy(() => import("@/features/auth/pages/ForgotPassword"));
const ResetPasswordPage = lazy(() => import("@/features/auth/pages/ResetPassword"));
const PasswordResetVerifyPage = lazy(() => import("@/features/auth/pages/PasswordResetVerify"));
const ActivateTenantPage = lazy(() => import("@/features/auth/pages/ActivateTenant"));

const DashboardPage = lazy(() => import("@/features/dashboard/pages/DashboardPage"));
const BuildingsPage = lazy(() => import("@/features/buildings/pages/BuildingsPage"));
const CreateBuildingPage = lazy(() => import("@/features/buildings/pages/CreateBuildingPage"));
const EditBuildingPage = lazy(() => import("@/features/buildings/pages/EditBuildingPage"));
const BuildingDetailsPage = lazy(() => import("@/features/buildings/pages/BuildingDetailsPage"));
const CreateFloorPage = lazy(() => import("@/features/floors/pages/CreateFloorPage"));
const FloorDetailsPage = lazy(() => import("@/features/floors/pages/FloorDetailsPage"));
const ApartmentsPage = lazy(() => import("@/features/apartments/pages/ApartmentsPage"));
const CreateApartmentPage = lazy(() => import("@/features/apartments/pages/CreateApartmentPage"));
const EditApartmentPage = lazy(() => import("@/features/apartments/pages/EditApartmentPage"));
const ApartmentDetailsPage = lazy(() => import("@/features/apartments/pages/ApartmentDetailsPage"));
const LeasesPage = lazy(() => import("@/features/leasing/pages/LeasesPage"));
const CreateLeasePage = lazy(() => import("@/features/leasing/pages/CreateLeasePage"));
const EditLeasePage = lazy(() => import("@/features/leasing/pages/EditLeasePage"));
const LeaseDetailsPage = lazy(() => import("@/features/leasing/pages/LeaseDetailsPage"));
const TenantsPage = lazy(() => import("@/features/tenants/pages/TenantsPage"));
const CreateTenantPage = lazy(() => import("@/features/tenants/pages/CreateTenantPage"));
const EditTenantPage = lazy(() => import("@/features/tenants/pages/EditTenantPage"));
const TenantDetailsPage = lazy(() => import("@/features/tenants/pages/TenantDetailsPage"));
const ParkingPage = lazy(() => import("@/features/parking/ParkingPage"));
const DocumentsPage = lazy(() => import("@/features/documents/DocumentsPage"));
const NotificationsPage = lazy(() => import("@/features/notifications/pages/NotificationsPage"));
const SettingsPage = lazy(() => import("@/features/settings/SettingsPage"));
const OwnerPaymentsWorkspace = lazy(() => import("@/features/payments/pages/OwnerPaymentsWorkspace"));
const FinancialOperationsPage = lazy(() => import("@/features/financials/pages/FinancialOperationsPage"));
const UtilityAccountsPage = lazy(() => import("@/features/utilityBills/pages/UtilityAccountsPage").then((module) => ({ default: module.UtilityAccountsPage })));
const UtilityAccountDetailsPage = lazy(() => import("@/features/utilityBills/pages/UtilityAccountDetailsPage").then((module) => ({ default: module.UtilityAccountDetailsPage })));
const MaintenancePage = lazy(() => import("@/features/maintenance/pages/MaintenancePage").then((module) => ({ default: module.MaintenancePage })));
const CompanySubscriptionsPage = lazy(() => import("@/features/subscriptions/pages/CompanySubscriptionsPage").then((module) => ({ default: module.CompanySubscriptionsPage })));

const PlatformAdminDashboardPage = lazy(() => import("@/features/platformAdmin/pages/PlatformAdminDashboardPage").then((module) => ({ default: module.PlatformAdminDashboardPage })));
const PlatformAdministratorsPage = lazy(() => import("@/features/platformAdmin/pages/PlatformAdministratorsPage").then((module) => ({ default: module.PlatformAdministratorsPage })));
const LandlordRegistrationsPage = lazy(() => import("@/features/platformAdmin/pages/LandlordRegistrationsPage").then((module) => ({ default: module.LandlordRegistrationsPage })));
const ContactRequestsPage = lazy(() => import("@/features/platformAdmin/pages/ContactRequestsPage").then((module) => ({ default: module.ContactRequestsPage })));
const PlatformPlansPage = lazy(() => import("@/features/subscriptions/pages/PlatformPlansPage").then((module) => ({ default: module.PlatformPlansPage })));
const PlatformSubscriptionsPage = lazy(() => import("@/features/subscriptions/pages/PlatformSubscriptionsPage").then((module) => ({ default: module.PlatformSubscriptionsPage })));
const PlatformPlanChangeRequestsPage = lazy(() => import("@/features/subscriptions/pages/PlatformPlanChangeRequestsPage").then((module) => ({ default: module.PlatformPlanChangeRequestsPage })));

const TenantDashboardPage = lazy(() => import("@/features/tenantPortal/pages/TenantDashboardPage").then((module) => ({ default: module.TenantDashboardPage })));
const TenantProfilePage = lazy(() => import("@/features/tenantPortal/pages/TenantProfilePage").then((module) => ({ default: module.TenantProfilePage })));
const TenantLeasePage = lazy(() => import("@/features/tenantPortal/pages/TenantLeasePage").then((module) => ({ default: module.TenantLeasePage })));
const TenantPaymentsPage = lazy(() => import("@/features/tenantPortal/pages/TenantPaymentsPage").then((module) => ({ default: module.TenantPaymentsPage })));
const TenantBillsPage = lazy(() => import("@/features/tenantPortal/pages/TenantBillsPage").then((module) => ({ default: module.TenantBillsPage })));

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
      <SeoManager />
      <Suspense fallback={<AuthLoadingScreen />}>
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
      </Suspense>
    </BrowserRouter>
  );
}
