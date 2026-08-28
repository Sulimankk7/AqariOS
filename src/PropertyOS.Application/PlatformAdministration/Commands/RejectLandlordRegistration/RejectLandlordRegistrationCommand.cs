using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Commands.RejectLandlordRegistration;

public sealed record RejectLandlordRegistrationCommand(Guid RegistrationId, string Reason)
    : ICommand<LandlordRegistrationReviewResultDto>;
