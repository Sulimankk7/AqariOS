using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Application.Documents.Services;

public class DocumentCategorySeeder : IDocumentCategorySeeder
{
    private readonly IDocumentCategoryRepository _categoryRepository;

    private static readonly (string Name, string Description)[] StandardCategories = new[]
    {
        ("Ownership Documents", "Property deeds, title registry, proof of ownership"),
        ("Building License", "Official building permits, construction licenses"),
        ("Occupancy Permit", "Certificates of occupancy, safety clearance permits"),
        ("Municipality Documents", "Municipal taxes, zoning compliance, civil defense certificates"),
        ("Utility Documents", "Electricity, water, gas meter filings and utility agreements"),
        ("Insurance", "Building insurance policies, liability coverage records"),
        ("Contracts", "Vendor contracts, service level agreements"),
        ("Maintenance", "Elevator inspection reports, HVAC service logs, structural audits"),
        ("Financial", "Property tax records, building financial audits"),
        ("Other", "General miscellaneous building documents")
    };

    public DocumentCategorySeeder(IDocumentCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task SeedDefaultCategoriesAsync(Guid companyId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (existing.Count > 0)
            return; // Already seeded or customized

        var categoriesToSeed = new List<DocumentCategory>();
        foreach (var (name, desc) in StandardCategories)
        {
            categoriesToSeed.Add(DocumentCategory.Create(
                companyId: companyId,
                name: name,
                description: desc,
                now: now,
                createdBy: null // System-seeded
            ));
        }

        await _categoryRepository.AddRangeAsync(categoriesToSeed, cancellationToken);
    }
}
