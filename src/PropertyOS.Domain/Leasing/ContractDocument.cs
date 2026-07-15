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
    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
    }
}
