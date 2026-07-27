using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Domain.Leasing;

public class ContractDocument : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    
    public Guid FileId { get; private set; }
    public ContractDocumentType DocumentType { get; private set; }
    public string? Description { get; private set; }
    public Guid? UploadedBy { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private ContractDocument() { }

    public static ContractDocument Create(
        Guid companyId,
        Guid leaseContractId,
        Guid fileId,
        ContractDocumentType documentType,
        string? description = null,
        Guid? uploadedBy = null,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        return new ContractDocument
        {
            Id = id ?? Guid.CreateVersion7(),
            CompanyId = companyId,
            LeaseContractId = leaseContractId,
            FileId = fileId,
            DocumentType = documentType,
            Description = description,
            UploadedBy = uploadedBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
    }
}
