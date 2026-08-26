using System;
using MediatR;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentSettlementStatementPdf;

public record SettlementStatementFileDto(
    string Filename,
    byte[] Content,
    string MimeType);

public record GetRentPaymentSettlementStatementPdfQuery(
    Guid RentPaymentId,
    Guid? TenantId = null) : IRequest<SettlementStatementFileDto>;
