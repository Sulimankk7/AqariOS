using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Commands.ApproveLandlordRegistration;

public sealed record ApproveLandlordRegistrationCommand(Guid RegistrationId)
    : ICommand<LandlordRegistrationReviewResultDto>;
