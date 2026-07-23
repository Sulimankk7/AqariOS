using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetBuildingDocuments;

public record GetBuildingDocumentsQuery(
    Guid? BuildingId,
    Guid? CategoryId,
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<PagedBuildingDocumentsResponse>;

public record PagedBuildingDocumentsResponse(
    List<BuildingDocumentListDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage
);
