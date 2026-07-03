using System;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current authenticated user's context.
/// Returns null for unauthenticated requests or background jobs
/// running without a user context.
/// </summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }
}
