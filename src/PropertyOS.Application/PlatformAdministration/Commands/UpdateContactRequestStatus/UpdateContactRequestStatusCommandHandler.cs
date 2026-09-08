using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.PlatformAdministration.Enums;
namespace PropertyOS.Application.PlatformAdministration.Commands.UpdateContactRequestStatus;
public sealed class UpdateContactRequestStatusCommandHandler : IRequestHandler<UpdateContactRequestStatusCommand, ContactRequestDetailDto>
{
    private readonly IApplicationDbContext _db; private readonly ITenantContext _tenant; private readonly IBusinessClock _clock;
    public UpdateContactRequestStatusCommandHandler(IApplicationDbContext db, ITenantContext tenant, IBusinessClock clock) { _db = db; _tenant = tenant; _clock = clock; }
    public async Task<ContactRequestDetailDto> Handle(UpdateContactRequestStatusCommand request, CancellationToken cancellationToken)
    {
        if (!_tenant.IsPlatformAdmin) throw new UnauthorizedAccessException("Platform administrator scope is required.");
        var entity = await _db.ContactRequests.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ?? throw new NotFoundException("Contact request was not found.");
        entity.UpdateStatus(Enum.Parse<ContactRequestStatus>(request.Status, true), _clock.UtcNow);
        return new(entity.Id, entity.Name, entity.CompanyName, entity.PhoneNumber, entity.NumberOfBuildings, entity.Notes, entity.Status.ToString(), entity.CreatedAt, entity.UpdatedAt);
    }
}
