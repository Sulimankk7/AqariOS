using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.PlatformAdministration.Enums;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetContactRequests;
public sealed class GetContactRequestsQueryHandler : IRequestHandler<GetContactRequestsQuery, ContactRequestPageDto>
{
    private readonly IApplicationDbContext _db; private readonly ITenantContext _tenant;
    public GetContactRequestsQueryHandler(IApplicationDbContext db, ITenantContext tenant) { _db = db; _tenant = tenant; }
    public async Task<ContactRequestPageDto> Handle(GetContactRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!_tenant.IsPlatformAdmin) throw new UnauthorizedAccessException("Platform administrator scope is required.");
        var query = _db.ContactRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ContactRequestStatus>(request.Status, true, out var status)) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new ContactRequestListItemDto(x.Id, x.Name, x.CompanyName, x.PhoneNumber, x.NumberOfBuildings, x.Status.ToString(), x.CreatedAt)).ToListAsync(cancellationToken);
        return new(items, request.Page, request.PageSize, total);
    }
}
