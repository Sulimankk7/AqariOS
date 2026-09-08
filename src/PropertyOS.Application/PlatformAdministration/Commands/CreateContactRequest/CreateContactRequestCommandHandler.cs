using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.PlatformAdministration;

namespace PropertyOS.Application.PlatformAdministration.Commands.CreateContactRequest;

public sealed class CreateContactRequestCommandHandler : IRequestHandler<CreateContactRequestCommand, ContactRequestCreatedDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBusinessClock _clock;
    public CreateContactRequestCommandHandler(IApplicationDbContext db, IBusinessClock clock) { _db = db; _clock = clock; }

    public async Task<ContactRequestCreatedDto> Handle(CreateContactRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = ContactRequest.Create(request.Name, request.CompanyName, request.PhoneNumber, request.NumberOfBuildings, request.Notes, _clock.UtcNow);
        await _db.ContactRequests.AddAsync(entity, cancellationToken);
        return new(entity.Id, entity.Status.ToString(), entity.CreatedAt);
    }
}
