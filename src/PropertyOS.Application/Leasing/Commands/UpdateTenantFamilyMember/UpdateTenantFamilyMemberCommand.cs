using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantFamilyMember;

public record UpdateTenantFamilyMemberCommand(
    Guid TenantId,
    Guid FamilyMemberId,
    string Name,
    string RelationshipType,
    string? AgeBracket = null
) : ICommand;
