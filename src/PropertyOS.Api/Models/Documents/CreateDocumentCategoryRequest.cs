namespace PropertyOS.Api.Models.Documents;

/// <summary>
/// Request model for creating a new document category.
/// </summary>
public record CreateDocumentCategoryRequest(
    string Name,
    string? Description = null
);
