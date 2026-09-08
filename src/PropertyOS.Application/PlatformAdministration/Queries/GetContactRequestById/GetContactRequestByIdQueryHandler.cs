using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
namespace PropertyOS.Application.PlatformAdministration.Queries.GetContactRequestById;
public sealed class GetContactRequestByIdQueryHandler : IRequestHandler<GetContactRequestByIdQuery, ContactRequestDetailDto>
{
    private readonly IApplicationDbContext _db; private readonly ITenantContext _tenant;
    public GetContactRequestByIdQueryHandler(IApplicationDbContext db, ITenantContext tenant) { _db = db; _tenant = tenant; }
    public async Task<ContactRequestDetailDto> Handle(GetContactRequestByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_tenant.IsPlatformAdmin) throw new UnauthorizedAccessException("Platform administrator scope is required.");
        return await _db.ContactRequests.AsNoTracking().Where(x => x.Id == request.Id)
            .Select(x => new ContactRequestDetailDto(x.Id, x.Name, x.CompanyName, x.PhoneNumber, x.NumberOfBuildings, x.Notes, x.Status.ToString(), x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Contact request was not found.");
    }
}
