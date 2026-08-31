using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Common.Numbering;

public enum IdentifierSuggestionKind { LeaseContract, Building, Floor, Apartment }

public sealed record GetNextIdentifierSuggestionQuery(
    IdentifierSuggestionKind Kind,
    Guid? BuildingId = null,
    Guid? FloorId = null) : IRequest<IdentifierSuggestionDto>;

public sealed record IdentifierSuggestionDto(string Value);

public sealed class GetNextIdentifierSuggestionQueryHandler
    : IRequestHandler<GetNextIdentifierSuggestionQuery, IdentifierSuggestionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBusinessClock _clock;

    public GetNextIdentifierSuggestionQueryHandler(IApplicationDbContext db, ITenantContext tenant, IBusinessClock clock)
    { _db = db; _tenant = tenant; _clock = clock; }

    public async Task<IdentifierSuggestionDto> Handle(GetNextIdentifierSuggestionQuery request, CancellationToken ct)
    {
        var companyId = _tenant.CompanyId ?? throw new UnauthorizedAccessException("Company context is required.");
        return request.Kind switch
        {
            IdentifierSuggestionKind.LeaseContract => new(await NextLease(companyId, ct)),
            IdentifierSuggestionKind.Building => new(await NextBuilding(companyId, ct)),
            IdentifierSuggestionKind.Floor => new((await NextFloor(companyId, Required(request.BuildingId), ct)).ToString()),
            IdentifierSuggestionKind.Apartment => new(await NextApartment(companyId, Required(request.FloorId), ct)),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Kind))
        };
    }

    private async Task<string> NextLease(Guid companyId, CancellationToken ct)
    {
        var year = _clock.GetJordanBusinessDate().Year;
        var prefix = $"LSE-{year}-";
        var values = await _db.LeaseContracts.AsNoTracking().Where(x => x.CompanyId == companyId && x.ContractNumber.StartsWith(prefix)).Select(x => x.ContractNumber).ToListAsync(ct);
        return $"{prefix}{IdentifierSequence.NextSuffix(values, prefix):D3}";
    }

    private async Task<string> NextBuilding(Guid companyId, CancellationToken ct)
    {
        const string prefix = "BLD-";
        var values = await _db.Buildings.AsNoTracking().Where(x => x.CompanyId == companyId && x.InternalCode != null && x.InternalCode.StartsWith(prefix)).Select(x => x.InternalCode!).ToListAsync(ct);
        return $"{prefix}{IdentifierSequence.NextSuffix(values, prefix):D3}";
    }

    private async Task<short> NextFloor(Guid companyId, Guid buildingId, CancellationToken ct)
    {
        var exists = await _db.Buildings.AnyAsync(x => x.Id == buildingId && x.CompanyId == companyId, ct);
        if (!exists) throw new KeyNotFoundException("Building was not found.");
        var numbers = await _db.Floors.AsNoTracking().Where(x => x.BuildingId == buildingId && x.CompanyId == companyId).Select(x => x.FloorNumber).ToListAsync(ct);
        return numbers.Count == 0 ? (short)0 : checked((short)(numbers.Max() + 1));
    }

    private async Task<string> NextApartment(Guid companyId, Guid floorId, CancellationToken ct)
    {
        var floor = await _db.Floors.AsNoTracking().Where(x => x.Id == floorId && x.CompanyId == companyId).Select(x => new { x.BuildingId, x.FloorNumber }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Floor was not found.");
        var prefix = $"APT-{floor.FloorNumber}";
        var values = await _db.Apartments.AsNoTracking().Where(x => x.BuildingId == floor.BuildingId && x.UnitNumber.StartsWith(prefix)).Select(x => x.UnitNumber).ToListAsync(ct);
        return $"{prefix}{IdentifierSequence.NextSuffix(values, prefix):D2}";
    }

    private static Guid Required(Guid? value) => value ?? throw new ArgumentException("The parent identifier is required.");
}

public static class IdentifierSequence
{
    public static int NextSuffix(IEnumerable<string> values, string prefix) =>
        values.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => x[prefix.Length..])
            .Select(x => int.TryParse(x, out var n) ? n : 0)
            .DefaultIfEmpty(0).Max() + 1;
}
