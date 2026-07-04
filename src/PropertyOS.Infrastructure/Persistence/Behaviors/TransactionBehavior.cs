using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Persistence.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly PropertyOsDbContext _dbContext;

    public TransactionBehavior(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
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
            // 3. BeginTransactionAsync
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 4. Execute next()
                var response = await next();

                // 5. SaveChangesAsync exactly once at the behavior boundary for tracked mutations
                await _dbContext.SaveChangesAsync(cancellationToken);

                // 6. CommitTransactionAsync
                await transaction.CommitAsync(cancellationToken);

                return response;
            }
            catch
            {
                // 7. If any failure occurs, RollbackTransactionAsync and rethrow
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
