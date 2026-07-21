using System;
using System.Collections.Generic;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Application.Marketplace.Queries.Common;

/// <summary>
/// DTO representing a public browse listing card.
/// </summary>
public record PublicListingSummaryDto(
    Guid Id,
    string ListingTitle,
    string ListingDescription,
    decimal MonthlyRent,
    decimal? SecurityDeposit,
    string Currency,
    DateOnly? PublishedDate,
    bool IsFeatured,
    string ContactPhone,
    string? ContactWhatsapp,
    Guid? CoverImageFileId);

/// <summary>
/// DTO representing a listing in the company staff dashboard list.
/// </summary>
public record CompanyListingSummaryDto(
    Guid Id,
    string ListingTitle,
    string ApartmentUnitNumber,
    string BuildingName,
    decimal MonthlyRent,
    string Currency,
    ListingStatus Status,
    DateOnly? PublishedDate,
    DateOnly? ExpirationDate,
    bool IsFeatured,
    int ImageCount,
    int ViewingRequestCount);

/// <summary>
/// DTO representing full details of a listing.
/// </summary>
public record ListingDetailDto(
    Guid Id,
    Guid CompanyId,
    Guid BuildingId,
    string BuildingName,
    Guid ApartmentId,
    string ApartmentUnitNumber,
    string ListingTitle,
    string ListingDescription,
    decimal MonthlyRent,
    decimal? SecurityDeposit,
    string Currency,
    ListingStatus Status,
    DateOnly? PublishedDate,
    DateOnly? ExpirationDate,
    bool IsFeatured,
    string ContactPhone,
    string? ContactWhatsapp,
    List<ListingImageDto> Images);

/// <summary>
/// DTO representing a single image for a listing.
/// </summary>
public record ListingImageDto(
    Guid Id,
    Guid FileId,
    short DisplayOrder,
    bool IsCover);

/// <summary>
/// DTO representing a viewing request lead.
/// </summary>
public record ViewingRequestSummaryDto(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    string ApplicantName,
    string PhoneNumber,
    string? Email,
    DateOnly? PreferredViewingDate,
    string? Notes,
    ViewingRequestStatus RequestStatus,
    DateTimeOffset SubmittedAt);
