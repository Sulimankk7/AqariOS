using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Persistence.Behaviors;

/// <summary>
/// Scoped container holding post-commit callbacks to be executed after transaction commit.
/// </summary>
public class PostCommitRegistrar : IPostCommitRegistrar
{
    private readonly List<Func<CancellationToken, Task>> _actions = new();

    public void RegisterPostCommitAction(Func<CancellationToken, Task> action)
    {
        if (action != null)
        {
            _actions.Add(action);
        }
    }

    public IReadOnlyList<Func<CancellationToken, Task>> Actions => _actions.AsReadOnly();

    public void Clear()
    {
        _actions.Clear();
    }
}
