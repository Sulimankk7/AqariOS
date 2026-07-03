using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Pre-authentication implementation of ICurrentUserContext (Module 1 boundary).
/// Returns null, enforcing fail-closed behavior before Module 3's Identity is implemented.
/// </summary>
internal sealed class NullCurrentUserContext : ICurrentUserContext
{
    public Guid? UserId => null;
}
