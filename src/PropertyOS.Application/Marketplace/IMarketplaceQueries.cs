using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Marketplace.Queries.Common;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Application.Marketplace;

/// <summary>
/// Read-side query service for Marketplace operations.
/// All methods return DTO projections directly, fully decoupled from database change tracking.
/// </summary>
public interface IMarketplaceQueries
{
    Task<List<PublicListingSummaryDto>> GetPublicListingsAsync(
        PublicListingsFilterOptions filter,
        CancellationToken cancellationToken = default);

    Task<List<CompanyListingSummaryDto>> GetCompanyListingsAsync(
        CompanyListingsFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ListingDetailDto?> GetListingDetailsAsync(
        Guid id,
        Guid? companyId,
        CancellationToken cancellationToken = default);

    Task<List<ViewingRequestSummaryDto>> GetViewingRequestsAsync(
        ViewingRequestsFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter options for public marketplace browsing. Enforces page capping.
/// </summary>
public record PublicListingsFilterOptions(
    Guid? BuildingId = null,
    Guid? ApartmentId = null,
    string? SearchText = null,
    // Keyset pagination cursor
    bool? LastSeenIsFeatured = null,
    DateOnly? LastSeenPublishedDate = null,
    Guid? LastSeenId = null,
    int PageSize = 10)
{
    public int PageSize { get; init; } = PageSize is > 0 and <= 50 ? PageSize : throw new ArgumentOutOfRangeException(nameof(PageSize), "Page size must be between 1 and 50.");
}

/// <summary>
/// Filter options for company staff dashboard.
/// </summary>
public record CompanyListingsFilterOptions(
    ListingStatus? Status = null,
    Guid? BuildingId = null,
    Guid? ApartmentId = null,
    string? SearchText = null,
    // Keyset pagination cursor
    DateOnly? LastSeenPublishedDate = null,
    Guid? LastSeenId = null,
    int PageSize = 10)
{
    public int PageSize { get; init; } = PageSize is > 0 and <= 50 ? PageSize : throw new ArgumentOutOfRangeException(nameof(PageSize), "Page size must be between 1 and 50.");
}

/// <summary>
/// Filter options for company staff viewing request worklist.
/// </summary>
public record ViewingRequestsFilterOptions(
    Guid? ListingId = null,
    ViewingRequestStatus? Status = null,
    // Keyset pagination cursor
    DateTimeOffset? LastSeenSubmittedAt = null,
    Guid? LastSeenId = null,
    int PageSize = 10)
{
    public int PageSize { get; init; } = PageSize is > 0 and <= 50 ? PageSize : throw new ArgumentOutOfRangeException(nameof(PageSize), "Page size must be between 1 and 50.");
}
