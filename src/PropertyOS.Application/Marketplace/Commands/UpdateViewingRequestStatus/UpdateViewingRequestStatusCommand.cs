using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Application.Marketplace.Commands.UpdateViewingRequestStatus;

public record UpdateViewingRequestStatusCommand(
    Guid Id,
    ViewingRequestStatus Status,
    string? StaffNotes
) : ICommand;
