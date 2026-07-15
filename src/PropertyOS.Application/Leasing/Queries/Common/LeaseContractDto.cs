using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Queries.Common;

public class LeaseContractDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid ApartmentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? PriorContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public LegalRegime LegalRegime { get; set; }
    public TenantType TenantType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? SignedDate { get; set; }
    public decimal MonthlyRentAmount { get; set; }
    public string Currency { get; set; } = "JOD";
    public decimal SecurityDepositAmount { get; set; }
    public PaymentFrequency PaymentFrequency { get; set; }
    public short PaymentDueDay { get; set; }
    public ContractStatus Status { get; set; }
    public string? ExternalRegistrationRef { get; set; }
    public Guid? ContractDocumentId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
