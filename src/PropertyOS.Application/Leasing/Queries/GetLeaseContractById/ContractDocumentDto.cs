using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class ContractDocumentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeaseContractId { get; set; }
    public Guid FileId { get; set; }
    public ContractDocumentType DocumentType { get; set; }
    public string? Description { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
