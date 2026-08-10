using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantFamilyMember;

public record CreateTenantFamilyMemberCommand(
    Guid TenantId,
    string Name,
    string RelationshipType,
    string? AgeBracket = null
) : ICommand<Guid>;
