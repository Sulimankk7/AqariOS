using System;
using MediatR;

namespace PropertyOS.Application.Leasing.Queries.GetContractDocumentDownloadUrl;

public record ContractDocumentDownloadUrlDto(string Url, string Filename);

public record GetContractDocumentDownloadUrlQuery(
    Guid LeaseContractId,
    Guid DocumentId,
    bool Inline = false
) : IRequest<ContractDocumentDownloadUrlDto>;
