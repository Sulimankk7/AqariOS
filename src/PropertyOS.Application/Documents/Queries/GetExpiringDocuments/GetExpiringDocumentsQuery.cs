using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetExpiringDocuments;

public record GetExpiringDocumentsQuery(int WithinDays = 30) : IRequest<List<ExpiringDocumentDto>>;
