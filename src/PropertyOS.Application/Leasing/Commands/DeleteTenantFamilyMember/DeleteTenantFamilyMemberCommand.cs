using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantFamilyMember;

public record DeleteTenantFamilyMemberCommand(
    Guid TenantId,
    Guid FamilyMemberId
) : ICommand;
