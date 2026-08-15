using System;

namespace PropertyOS.Application.Leasing.Queries.GetMyActiveLease;

/// <summary>
/// Minimal, read-only DTO for authenticated Tenant self-service active lease viewing.
/// Exposes only Tenant-facing lease, unit, and building details. Internal audit fields,
/// company/user identifiers, and security fields are excluded.
/// </summary>
public record TenantLeaseDto(
    Guid Id,
    string ContractNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? SignedDate,
    decimal MonthlyRentAmount,
    string Currency,
    decimal SecurityDepositAmount,
    string PaymentFrequency,
    short PaymentDueDay,
    string Status,
    string LegalRegime,
    string TenantType,
    Guid ApartmentId,
    string ApartmentUnitNumber,
    short ApartmentBedrooms,
    short ApartmentBathrooms,
    decimal ApartmentAreaSqm,
    Guid BuildingId,
    string BuildingName
);
