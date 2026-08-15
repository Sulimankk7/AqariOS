using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Allows command handlers to register asynchronous actions that execute strictly
/// AFTER the database transaction has successfully committed in TransactionBehavior.
/// </summary>
public interface IPostCommitRegistrar
{
    void RegisterPostCommitAction(Func<CancellationToken, Task> action);
    IReadOnlyList<Func<CancellationToken, Task>> Actions { get; }
    void Clear();
}
