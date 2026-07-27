using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Domain.Leasing;

public class LeaseContract : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BuildingId { get; private set; }
    public Guid ApartmentId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? PriorContractId { get; private set; }
    
    public string ContractNumber { get; private set; } = string.Empty;
    public LegalRegime LegalRegime { get; private set; }
    public TenantType TenantType { get; private set; }
    
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public DateOnly? SignedDate { get; private set; }
    
    public decimal MonthlyRentAmount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public decimal SecurityDepositAmount { get; private set; }
    
    public PaymentFrequency PaymentFrequency { get; private set; }
    public short PaymentDueDay { get; private set; }
    
    public ContractStatus Status { get; private set; }
    
    public string? ExternalRegistrationRef { get; private set; }
    public Guid? ContractDocumentId { get; private set; }
    public string? Notes { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public uint xmin { get; private set; }

    private LeaseContract() { }

    public static LeaseContract Create(
        Guid companyId,
        Guid buildingId,
        Guid apartmentId,
        Guid tenantId,
        string contractNumber,
        DateOnly startDate,
        DateOnly endDate,
        decimal monthlyRentAmount,
        PaymentFrequency paymentFrequency,
        short paymentDueDay,
        DateTimeOffset createdAt,
        Guid? createdBy,
        Guid? priorContractId = null,
        LegalRegime legalRegime = LegalRegime.Standard,
        TenantType tenantType = TenantType.Personal,
        decimal securityDepositAmount = 0,
        ContractStatus status = ContractStatus.Draft,
        string? notes = null)
    {
        return new LeaseContract
        {
            // Client-generated UUIDv7 (same pattern as MarketplaceListing): the ID must be
            // known before TransactionBehavior's SaveChanges so that status-history rows can
            // reference it and create endpoints can return it. The column default
            // uuid_generate_v7() remains as a fallback for rows created outside this factory.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            TenantId = tenantId,
            PriorContractId = priorContractId,
            ContractNumber = contractNumber,
            LegalRegime = legalRegime,
            TenantType = tenantType,
            StartDate = startDate,
            EndDate = endDate,
            MonthlyRentAmount = monthlyRentAmount,
            SecurityDepositAmount = securityDepositAmount,
            PaymentFrequency = paymentFrequency,
            PaymentDueDay = paymentDueDay,
            Status = status,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateDraftTerms(
        Guid apartmentId,
        Guid buildingId,
        Guid tenantId,
        DateOnly startDate,
        DateOnly endDate,
        decimal monthlyRentAmount,
        decimal securityDepositAmount,
        PaymentFrequency paymentFrequency,
        short paymentDueDay,
        LegalRegime legalRegime,
        TenantType tenantType,
        string? notes,
        DateTimeOffset updatedAt,
        Guid? updatedBy)
    {
        if (Status != ContractStatus.Draft)
            throw new InvalidOperationException($"Cannot update contract terms when contract is in {Status} status. Only Draft contracts can be edited.");

        if (endDate <= startDate)
            throw new InvalidOperationException("EndDate must be strictly greater than StartDate.");

        if (monthlyRentAmount <= 0)
            throw new InvalidOperationException("MonthlyRentAmount must be greater than zero.");

        if (securityDepositAmount < 0)
            throw new InvalidOperationException("SecurityDepositAmount cannot be negative.");

        if (paymentDueDay < 1 || paymentDueDay > 28)
            throw new InvalidOperationException("PaymentDueDay must be between 1 and 28.");

        ApartmentId = apartmentId;
        BuildingId = buildingId;
        TenantId = tenantId;
        StartDate = startDate;
        EndDate = endDate;
        MonthlyRentAmount = monthlyRentAmount;
        SecurityDepositAmount = securityDepositAmount;
        PaymentFrequency = paymentFrequency;
        PaymentDueDay = paymentDueDay;
        LegalRegime = legalRegime;
        TenantType = tenantType;
        Notes = notes;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SetPriorContractId(Guid priorContractId)
    {
        PriorContractId = priorContractId;
    }

    public void Activate(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingSignature)
            throw new InvalidOperationException($"Cannot activate a contract in {Status} status. Only Draft or PendingSignature are allowed.");

        Status = ContractStatus.Active;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SupersedeForRenewal(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ContractStatus.Active && Status != ContractStatus.Expired)
            throw new InvalidOperationException($"Cannot supersede a contract for renewal in {Status} status.");

        Status = ContractStatus.Superseded;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Terminate(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ContractStatus.Active)
            throw new InvalidOperationException($"Cannot terminate a contract in {Status} status. Only Active contracts can be terminated.");

        Status = ContractStatus.Terminated;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Expire(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (Status != ContractStatus.Active)
            throw new InvalidOperationException($"Cannot expire a contract in {Status} status. Only Active contracts can be expired.");

        Status = ContractStatus.Expired;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
