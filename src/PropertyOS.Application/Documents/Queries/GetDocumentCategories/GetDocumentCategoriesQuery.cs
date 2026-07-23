using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetDocumentCategories;

public record GetDocumentCategoriesQuery() : IRequest<List<DocumentCategoryDto>>;
