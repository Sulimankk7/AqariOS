using System;
using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetBuildingDocumentById;

public record GetBuildingDocumentByIdQuery(Guid Id) : IRequest<BuildingDocumentDto>;
