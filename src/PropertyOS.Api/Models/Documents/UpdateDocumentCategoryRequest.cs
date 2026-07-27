namespace PropertyOS.Api.Models.Documents;

/// <summary>
/// Request model for updating an existing document category.
/// </summary>
public record UpdateDocumentCategoryRequest(
    string Name,
    string? Description = null
);
