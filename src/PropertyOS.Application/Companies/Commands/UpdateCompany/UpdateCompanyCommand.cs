using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand(
    Guid Id,
    string LegalName,
    string DisplayName,
    string PrimaryPhone,
    string? PrimaryEmail
) : ICommand;
