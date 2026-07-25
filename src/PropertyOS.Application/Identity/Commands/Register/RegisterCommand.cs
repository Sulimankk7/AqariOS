using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Application.Identity.Commands.Register;

/// <summary>
/// MediatR command for registering a new user and provisioning a tenant company organization.
/// </summary>
public record RegisterCommand(
    string FullName,
    string? Email,
    string? Phone,
    string Password,
    string CompanyName,
    string? DisplayName = null,
    CompanyType CompanyType = CompanyType.IndividualOwner,
    string CountryCode = "JO",
    string PreferredLanguage = "ar",
    string? IpAddress = null,
    string? UserAgent = null
) : ICommand<RegisterResponseDto>;
