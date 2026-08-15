using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Infrastructure.Persistence.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly PropertyOsDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPostCommitRegistrar _postCommitRegistrar;

    public TransactionBehavior(
        PropertyOsDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        IServiceScopeFactory scopeFactory,
        IPostCommitRegistrar postCommitRegistrar)
    {
        _dbContext = dbContext;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _postCommitRegistrar = postCommitRegistrar;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. If not an ICommand<TResponse>, call next() and do not begin a transaction.
        if (request is not ICommand<TResponse> && request is not ICommand)
        {
            return await next();
        }

        // 2. If CurrentTransaction is already present, call next() directly.
        if (_dbContext.Database.CurrentTransaction != null)
        {
            return await next();
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            TResponse response;
            bool commitSucceeded = false;

            await using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
            {
                _logger.LogInformation(
                    "[DIAG:TransactionBehavior] Transaction opened. TransactionId={TransactionId} RequestType={RequestType}",
                    transaction.TransactionId, typeof(TRequest).Name);

                try
                {
                    // 4. Execute next()
                    response = await next();

                    var entries = _dbContext.ChangeTracker.Entries().ToList();
                    _logger.LogInformation(
                        "[DIAG:TransactionBehavior] ChangeTracker has {EntryCount} tracked entries before SaveChangesAsync.",
                        entries.Count);

                    var buildingIds = entries
                        .Where(e => e.Entity is Building)
                        .Select(e => ((Building)e.Entity).Id)
                        .ToList();

                    int rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "[DIAG:TransactionBehavior] SaveChangesAsync completed. RowsAffected={RowsAffected} TransactionId={TransactionId}",
                        rowsAffected, transaction.TransactionId);

                    // 6. CommitTransactionAsync — Database transaction boundary
                    await transaction.CommitAsync(cancellationToken);
                    commitSucceeded = true;
                    _logger.LogInformation(
                        "[DIAG:TransactionBehavior] Transaction committed. TransactionId={TransactionId}",
                        transaction.TransactionId);
                }
                catch (Exception ex) when (ex is not DbUpdateConcurrencyException)
                {
                    _logger.LogError(
                        ex,
                        "[DIAG:TransactionBehavior] Exception caught — rolling back transaction. " +
                        "TransactionId={TransactionId} ExceptionType={ExceptionType} Message={Message}",
                        transaction.TransactionId, ex.GetType().Name, ex.Message);

                    _postCommitRegistrar?.Clear();
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
                catch
                {
                    _postCommitRegistrar?.Clear();
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }

            // -----------------------------------------------------------------------
            // POST-COMMIT EXECUTION: Isolated from the DB transaction try/catch block.
            // Runs ONLY when commitSucceeded == true.
            // Any post-commit exception or cancellation is handled independently and
            // will NEVER trigger RollbackAsync or convert a committed account into HTTP 500.
            // -----------------------------------------------------------------------
            if (commitSucceeded && _postCommitRegistrar != null && _postCommitRegistrar.Actions.Any())
            {
                var actionsToExecute = _postCommitRegistrar.Actions.ToList();
                _postCommitRegistrar.Clear();

                _logger.LogInformation(
                    "[DIAG:TransactionBehavior] Database transaction committed successfully. Executing {ActionCount} registered post-commit actions.",
                    actionsToExecute.Count);

                foreach (var postCommitAction in actionsToExecute)
                {
                    try
                    {
                        await postCommitAction(cancellationToken);
                    }
                    catch (Exception postCommitEx)
                    {
                        _logger.LogError(
                            postCommitEx,
                            "[DIAG:TransactionBehavior] Non-fatal exception or cancellation executing post-commit action for request {RequestType}.",
                            typeof(TRequest).Name);
                    }
                }
            }

            return response;
        });
    }
}
