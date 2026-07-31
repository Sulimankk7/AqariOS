/**
 * Dashboard Summary DTO types.
 * Strictly mirrors ASP.NET Core GET /api/v1/dashboard/summary response payload.
 */

export interface PropertySummaryDto {
  totalBuildings: number;
  totalApartments: number;
  occupiedApartments: number;
  vacantApartments: number;
  occupancyRate: number;
}

export interface LeasingSummaryDto {
  activeLeases: number;
  expiringIn30Days: number;
  newLeasesThisMonth: number;
}

export interface PaymentSummaryDto {
  collectedThisMonth: number;
  outstandingAmount: number;
  overduePayments: number;
}

export interface FinancialSummaryDto {
  expensesThisMonth: number;
}

export interface DashboardSummaryDto {
  property: PropertySummaryDto;
  leasing: LeasingSummaryDto;
  payments: PaymentSummaryDto;
  financials: FinancialSummaryDto;
}
