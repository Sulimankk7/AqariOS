using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Properties.Repositories;

public class ParkingAssignmentRepository(PropertyOsDbContext context) : IParkingAssignmentRepository
{
    // Explicit scope on every participant also protects reads outside RLS transactions.
    private IQueryable<ParkingAssignment> OwnedAssignments(Guid companyId) =>
        from a in context.ParkingAssignments
        join p in context.ParkingSpots on a.ParkingSpotId equals p.Id
        join l in context.LeaseContracts on a.LeaseContractId equals l.Id
        join t in context.Tenants on l.TenantId equals t.Id
        join b in context.Buildings on p.BuildingId equals b.Id
        join unit in context.Apartments on l.ApartmentId equals unit.Id
        where a.CompanyId == companyId && p.CompanyId == companyId && l.CompanyId == companyId
            && t.CompanyId == companyId && b.CompanyId == companyId && unit.CompanyId == companyId
            && p.BuildingId == l.BuildingId && unit.BuildingId == l.BuildingId
        select a;

    public async Task<ParkingAssignment?> GetByIdForUpdateAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("A command transaction is required.");
        if (!await OwnedAssignments(companyId).AnyAsync(a => a.Id == id, cancellationToken))
            return null;
        return await context.ParkingAssignments.FromSqlInterpolated(
            $"SELECT * FROM parking_assignments WHERE id = {id} AND company_id = {companyId} AND deleted_at IS NULL FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveAssignmentAsync(Guid parkingSpotId, Guid companyId, CancellationToken cancellationToken = default) =>
        context.ParkingAssignments.AnyAsync(a => a.CompanyId == companyId && a.ParkingSpotId == parkingSpotId
            && a.Status == ParkingAssignmentStatus.Active, cancellationToken);

    public async Task<ParkingAssignmentDto?> GetCurrentBySpotAsync(Guid parkingSpotId, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (!await HasActiveAssignmentAsync(parkingSpotId, companyId, cancellationToken))
            return null;

        var assignment = await (
            from a in context.ParkingAssignments.AsNoTracking()
            join p in context.ParkingSpots on a.ParkingSpotId equals p.Id
            join l in context.LeaseContracts on a.LeaseContractId equals l.Id
            join t in context.Tenants on l.TenantId equals t.Id
            join b in context.Buildings on p.BuildingId equals b.Id
            join unit in context.Apartments on l.ApartmentId equals unit.Id
            where a.ParkingSpotId == parkingSpotId
                && a.CompanyId == companyId && p.CompanyId == companyId && l.CompanyId == companyId
                && t.CompanyId == companyId && b.CompanyId == companyId && unit.CompanyId == companyId
                && p.BuildingId == l.BuildingId && unit.BuildingId == l.BuildingId
                && a.Status == ParkingAssignmentStatus.Active
                && l.Status == ContractStatus.Active
            select new
            {
                a.Id,
                ParkingSpotId = p.Id,
                p.SpotCode,
                p.ParkingType,
                Location = p.LocationDescription ?? string.Empty,
                LeaseContractId = l.Id,
                TenantId = t.Id,
                TenantName = t.Name,
                a.AssignedFrom,
                a.AssignedTo,
                a.Status
            }).SingleOrDefaultAsync(cancellationToken);

        return assignment is null
            ? null
            : new ParkingAssignmentDto(assignment.Id, assignment.ParkingSpotId, assignment.SpotCode,
                assignment.ParkingType.ToString(), assignment.Location, assignment.LeaseContractId,
                assignment.TenantId, assignment.TenantName, assignment.AssignedFrom, assignment.AssignedTo,
                assignment.Status.ToString());
    }

    public async Task<List<ParkingAssignmentDto>> GetCurrentByLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var assignments = await (
            from a in context.ParkingAssignments.AsNoTracking()
            join p in context.ParkingSpots on a.ParkingSpotId equals p.Id
            join l in context.LeaseContracts on a.LeaseContractId equals l.Id
            join t in context.Tenants on l.TenantId equals t.Id
            join b in context.Buildings on p.BuildingId equals b.Id
            join unit in context.Apartments on l.ApartmentId equals unit.Id
            where a.LeaseContractId == leaseContractId
                && a.CompanyId == companyId && p.CompanyId == companyId && l.CompanyId == companyId
                && t.CompanyId == companyId && b.CompanyId == companyId && unit.CompanyId == companyId
                && p.BuildingId == l.BuildingId && unit.BuildingId == l.BuildingId
                && a.Status == ParkingAssignmentStatus.Active
                && l.Status == ContractStatus.Active
            orderby p.SpotCode, a.Id
            select new
            {
                a.Id,
                ParkingSpotId = p.Id,
                p.SpotCode,
                p.ParkingType,
                Location = p.LocationDescription ?? string.Empty,
                LeaseContractId = l.Id,
                TenantId = t.Id,
                TenantName = t.Name,
                a.AssignedFrom,
                a.AssignedTo,
                a.Status
            }).ToListAsync(cancellationToken);

        return assignments.Select(assignment => new ParkingAssignmentDto(
            assignment.Id, assignment.ParkingSpotId, assignment.SpotCode, assignment.ParkingType.ToString(),
            assignment.Location, assignment.LeaseContractId, assignment.TenantId, assignment.TenantName,
            assignment.AssignedFrom, assignment.AssignedTo, assignment.Status.ToString())).ToList();
    }

    public async Task EndActiveByLeaseAsync(Guid leaseContractId, Guid companyId, DateOnly endedOn,
        DateTimeOffset updatedAt, Guid? updatedBy, CancellationToken cancellationToken = default)
    {
        var active = await context.ParkingAssignments
            .Where(a => a.CompanyId == companyId && a.LeaseContractId == leaseContractId
                && a.Status == ParkingAssignmentStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var assignment in active)
            assignment.EndAssignment(endedOn < assignment.AssignedFrom ? assignment.AssignedFrom : endedOn, updatedAt, updatedBy);
    }

    public async Task AddAsync(ParkingAssignment assignment, CancellationToken cancellationToken = default) =>
        await context.ParkingAssignments.AddAsync(assignment, cancellationToken);
}
