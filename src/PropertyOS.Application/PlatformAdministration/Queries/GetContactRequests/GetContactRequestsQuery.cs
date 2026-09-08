using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetContactRequests;
public sealed record GetContactRequestsQuery(int Page = 1, int PageSize = 20, string? Status = null) : ITransactionalRequest<ContactRequestPageDto>;
