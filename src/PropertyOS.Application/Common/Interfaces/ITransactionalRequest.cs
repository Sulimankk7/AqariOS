using MediatR;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Marks a request that requires the existing transaction pipeline even when it is read-only.
/// Platform queries use this so transaction-local RLS platform scope is applied.
/// </summary>
public interface ITransactionalRequest<out TResponse> : IRequest<TResponse>
{
}
