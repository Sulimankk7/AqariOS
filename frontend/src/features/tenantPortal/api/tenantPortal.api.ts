import { http, ApiError } from "@/shared/lib/http";
import { storage, STORAGE_KEYS } from "@/shared/services/storage";
import { apiUrl } from "@/config/api";
import type {
  TenantDetailDto,
  TenantLeaseDto,
  TenantPaymentDto,
  SubmitPaymentVerificationRequestPayload,
} from "@/features/tenantPortal/types/tenantPortal.types";

const PAYMENT_METHOD_NUMERIC_MAP: Record<string, number> = {
  Cash: 0,
  BankTransfer: 1,
  Cheque: 2,
  Efawateercom: 3,
  CliQ: 4,
};

export const tenantPortalApi = {
  /**
   * Gets complete detail profile for currently authenticated Tenant session.
   * GET /api/v1/tenant-portal/me
   */
  getProfile(): Promise<TenantDetailDto> {
    return http.get<TenantDetailDto>("/api/v1/tenant-portal/me");
  },

  /**
   * Gets current active lease for currently authenticated Tenant session.
   * GET /api/v1/tenant-portal/lease
   * Returns null if HTTP 404 is returned (tenant has no active lease).
   */
  async getMyLease(): Promise<TenantLeaseDto | null> {
    try {
      return await http.get<TenantLeaseDto>("/api/v1/tenant-portal/lease");
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Submits payment proof & transaction reference for a rent payment.
   * POST /api/v1/tenant/payments/{id}/submit-verification
   */
  submitPaymentVerification(
    rentPaymentId: string,
    payload: SubmitPaymentVerificationRequestPayload
  ): Promise<string> {
    const cleanId = (rentPaymentId || "").trim();
    const GUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
    if (!cleanId || !GUID_REGEX.test(cleanId)) {
      return Promise.reject(
        new ApiError({
          status: 400,
          title: "Invalid Payment ID",
          detail: "The provided payment ID is not a valid GUID.",
        })
      );
    }

    const numericPaymentMethod =
      typeof payload.paymentMethod === "number"
        ? payload.paymentMethod
        : (PAYMENT_METHOD_NUMERIC_MAP[payload.paymentMethod] ?? 0);

    const backendPayload = {
      ...payload,
      paymentMethod: numericPaymentMethod,
    };

    return http.post<string>(`/api/v1/tenant/payments/${cleanId}/submit-verification`, backendPayload);
  },

  /**
   * Gets all rent payments for the currently authenticated tenant.
   * GET /api/v1/tenant-portal/payments
   * Returns empty array when the tenant has no payment records.
   */
  getMyPayments(): Promise<TenantPaymentDto[]> {
    return http.get<TenantPaymentDto[]>("/api/v1/tenant-portal/payments");
  },

  /**
   * Downloads the final settlement statement PDF for a settled installment.
   * GET /api/v1/tenant-portal/payments/{id}/settlement-statement
   */
  async downloadSettlementStatement(rentPaymentId: string): Promise<Blob> {
    const token = storage.get<string>(STORAGE_KEYS.accessToken);
    const url = apiUrl(`/api/v1/tenant-portal/payments/${rentPaymentId}/settlement-statement`);
    const headers: Record<string, string> = {
      Accept: "application/pdf, application/problem+json",
    };
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(url, {
      method: "GET",
      headers,
      credentials: "include",
    });

    if (!response.ok) {
      throw new Error(`Download failed with status ${response.status}`);
    }

    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("json") || contentType.includes("problem+json")) {
      const errorJson = await response.json();
      throw new Error(errorJson.detail || errorJson.title || "Failed to download settlement statement");
    }

    return await response.blob();
  },
};

