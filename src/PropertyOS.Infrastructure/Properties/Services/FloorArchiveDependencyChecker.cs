using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Properties.Floors.Services;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Maintenance.Enums;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Properties.Services;

/// <summary>
/// Counts every category of active dependency that must be resolved before a Floor can be archived.
/// All queries use the DbContext's global query filters, so only non-deleted (active) records are counted.
/// Returns dependencies in a fixed business order with stable machine-readable codes and guidance examples.
/// </summary>
public sealed class FloorArchiveDependencyChecker : IFloorArchiveDependencyChecker
{
    private readonly PropertyOsDbContext _db;

    public FloorArchiveDependencyChecker(PropertyOsDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<ArchiveDependency>> GetActiveDependenciesAsync(
        Guid floorId,
        CancellationToken cancellationToken = default)
    {
        var nonTerminalLeaseStatuses = new[]
        {
            ContractStatus.Draft,
            ContractStatus.PendingSignature,
            ContractStatus.Active
        };

        var terminalMaintenanceStatuses = new[]
        {
            MaintenanceStatus.Closed,
            MaintenanceStatus.Cancelled
        };

        var activeListingStatuses = new[]
        {
            ListingStatus.Published,
            ListingStatus.Rented
        };

        // 2. Apartments
        int apartments = await _db.Apartments
            .Where(a => a.FloorId == floorId)
            .CountAsync(cancellationToken);

        List<string>? apartmentExamples = null;
        if (apartments > 0)
        {
            apartmentExamples = await _db.Apartments
                .Where(a => a.FloorId == floorId)
                .OrderBy(a => a.UnitNumber)
                .Select(a => a.UnitNumber)
                .Take(3)
                .ToListAsync(cancellationToken);
        }

        var floorApartmentIds = _db.Apartments
            .Where(a => a.FloorId == floorId)
            .Select(a => a.Id);

        // 3. Lease Contracts
        int leases = await _db.LeaseContracts
            .Where(lc => floorApartmentIds.Contains(lc.ApartmentId)
                         && nonTerminalLeaseStatuses.Contains(lc.Status))
            .CountAsync(cancellationToken);

        List<string>? leaseExamples = null;
        if (leases > 0)
        {
            leaseExamples = await _db.LeaseContracts
                .Where(lc => floorApartmentIds.Contains(lc.ApartmentId)
                             && nonTerminalLeaseStatuses.Contains(lc.Status))
                .OrderByDescending(lc => lc.CreatedAt)
                .Select(lc => lc.ContractNumber)
                .Take(3)
                .ToListAsync(cancellationToken);
        }

        // 4. Maintenance Requests
        int maintenance = await _db.MaintenanceRequests
            .Where(mr => mr.ApartmentId.HasValue
                         && floorApartmentIds.Contains(mr.ApartmentId.Value)
                         && !terminalMaintenanceStatuses.Contains(mr.Status))
            .CountAsync(cancellationToken);

        List<string>? maintenanceExamples = null;
        if (maintenance > 0)
        {
            maintenanceExamples = await _db.MaintenanceRequests
                .Where(mr => mr.ApartmentId.HasValue
                             && floorApartmentIds.Contains(mr.ApartmentId.Value)
                             && !terminalMaintenanceStatuses.Contains(mr.Status))
                .OrderByDescending(mr => mr.CreatedAt)
                .Select(mr => mr.Title)
                .Take(3)
                .ToListAsync(cancellationToken);
        }

        // 5. Marketplace Listings
        int listings = await _db.MarketplaceListings
            .Where(ml => floorApartmentIds.Contains(ml.ApartmentId)
                         && activeListingStatuses.Contains(ml.Status))
            .CountAsync(cancellationToken);

        List<string>? listingExamples = null;
        if (listings > 0)
        {
            listingExamples = await _db.MarketplaceListings
                .Where(ml => floorApartmentIds.Contains(ml.ApartmentId)
                             && activeListingStatuses.Contains(ml.Status))
                .OrderByDescending(ml => ml.CreatedAt)
                .Select(ml => ml.ListingTitle)
                .Take(3)
                .ToListAsync(cancellationToken);
        }

        // Assemble in fixed business order
        var dependencies = new List<ArchiveDependency>(4);

        if (apartments > 0)  dependencies.Add(new ArchiveDependency("APARTMENTS", "Apartments", apartments, apartmentExamples));
        if (leases > 0)      dependencies.Add(new ArchiveDependency("LEASE_CONTRACTS", "Lease Contracts", leases, leaseExamples));
        if (maintenance > 0) dependencies.Add(new ArchiveDependency("MAINTENANCE_REQUESTS", "Maintenance Requests", maintenance, maintenanceExamples));
        if (listings > 0)    dependencies.Add(new ArchiveDependency("MARKETPLACE_LISTINGS", "Marketplace Listings", listings, listingExamples));

        return dependencies.AsReadOnly();
    }
}
