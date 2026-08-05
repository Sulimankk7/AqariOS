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

    public TransactionBehavior(
        PropertyOsDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _scopeFactory = scopeFactory;
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
            _logger.LogInformation(
                "[DIAG:TransactionBehavior] Transaction opened. TransactionId={TransactionId} RequestType={RequestType}",
                transaction.TransactionId, typeof(TRequest).Name);

            try
            {
                // 4. Execute next()
                var response = await next();

                // -----------------------------------------------------------------------
                // DIAGNOSTIC: Dump all change-tracker entries before SaveChangesAsync.
                // -----------------------------------------------------------------------
                var entries = _dbContext.ChangeTracker.Entries().ToList();
                _logger.LogInformation(
                    "[DIAG:TransactionBehavior] ChangeTracker has {EntryCount} tracked entries before SaveChangesAsync.",
                    entries.Count);

                // Collect Building IDs now, before SaveChanges clears Modified state.
                var buildingIds = entries
                    .Where(e => e.Entity is Building)
                    .Select(e => ((Building)e.Entity).Id)
                    .ToList();

                foreach (var entry in entries)
                {
                    _logger.LogInformation(
                        "[DIAG:TransactionBehavior] Entry: EntityType={EntityType} EntityState={EntityState}",
                        entry.Entity.GetType().Name, entry.State);

                    if (entry.State == EntityState.Modified)
                    {
                        foreach (var prop in entry.Properties.Where(p => p.IsModified))
                        {
                            _logger.LogInformation(
                                "[DIAG:TransactionBehavior]   Modified property: {PropertyName} | OriginalValue={OriginalValue} | CurrentValue={CurrentValue}",
                                prop.Metadata.Name,
                                prop.OriginalValue ?? "(null)",
                                prop.CurrentValue ?? "(null)");
                        }
                    }

                    // Log DeletedAt snapshot for every ISoftDeletable regardless of state.
                    if (entry.Entity is PropertyOS.Domain.Common.ISoftDeletable)
                    {
                        var deletedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
                        if (deletedAtProp != null)
                        {
                            _logger.LogInformation(
                                "[DIAG:TransactionBehavior]   ISoftDeletable: EntityType={EntityType} DeletedAt.OriginalValue={OriginalValue} DeletedAt.CurrentValue={CurrentValue} DeletedAt.IsModified={IsModified}",
                                entry.Entity.GetType().Name,
                                deletedAtProp.OriginalValue ?? "(null)",
                                deletedAtProp.CurrentValue ?? "(null)",
                                deletedAtProp.IsModified);
                        }
                    }
                }

                // -----------------------------------------------------------------------
                // DIAGNOSTIC: SaveChangesAsync — capture return value (rows affected).
                // -----------------------------------------------------------------------
                int rowsAffected;
                try
                {
                    rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException concurrencyEx)
                {
                    _logger.LogError(
                        concurrencyEx,
                        "[DIAG:TransactionBehavior] DbUpdateConcurrencyException caught during SaveChangesAsync. " +
                        "EF Core issued an UPDATE but 0 rows were affected. " +
                        "Likely cause: xmin mismatch (optimistic concurrency) or RLS blocked the UPDATE.");

                    foreach (var efEntry in concurrencyEx.Entries)
                    {
                        _logger.LogError(
                            "[DIAG:TransactionBehavior]   ConcurrencyException Entry: EntityType={EntityType}",
                            efEntry.Entity.GetType().Name);
                    }

                    throw;
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(
                        saveEx,
                        "[DIAG:TransactionBehavior] Exception during SaveChangesAsync: {ExceptionType} — {Message}",
                        saveEx.GetType().Name, saveEx.Message);
                    throw;
                }

                _logger.LogInformation(
                    "[DIAG:TransactionBehavior] SaveChangesAsync completed. RowsAffected={RowsAffected} TransactionId={TransactionId}",
                    rowsAffected, transaction.TransactionId);

                if (rowsAffected == 0 && entries.Any(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached))
                {
                    _logger.LogWarning(
                        "[DIAG:TransactionBehavior] WARNING: SaveChangesAsync returned 0 rows affected, " +
                        "but the ChangeTracker had non-Unchanged entries BEFORE the call. " +
                        "Possible causes: all entries were already Unchanged by the time SaveChanges ran, " +
                        "xmin concurrency suppressed the UPDATE silently, or RLS rejected the UPDATE without throwing.");
                }

                // -----------------------------------------------------------------------
                // DIAGNOSTIC STEP 1: Re-query using the SAME DbContext — inside transaction.
                // AsNoTracking() bypasses the identity cache.
                // IgnoreQueryFilters() disables the global soft-delete filter (b.DeletedAt == null).
                // Running inside the same transaction means PostgreSQL READ COMMITTED will
                // return the uncommitted write made by SaveChangesAsync in this transaction.
                // If the row shows DeletedAt=(null) here, SaveChangesAsync generated NO UPDATE.
                // The EF Core Database.Command logger emits the SQL for this SELECT.
                // -----------------------------------------------------------------------
                if (buildingIds.Any())
                {
                    _logger.LogInformation(
                        "[DIAG:TransactionBehavior] === POST-SAVE VERIFICATION (same DbContext, INSIDE transaction) ===");

                    foreach (var buildingId in buildingIds)
                    {
                        var sameCtxBuilding = await _dbContext.Buildings
                            .AsNoTracking()
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(b => b.Id == buildingId, cancellationToken);

                        if (sameCtxBuilding is null)
                        {
                            _logger.LogWarning(
                                "[DIAG:TransactionBehavior] SAME-CTX: Building Id={BuildingId} NOT FOUND with IgnoreQueryFilters. Row may not exist.",
                                buildingId);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "[DIAG:TransactionBehavior] SAME-CTX: Id={Id} | DeletedAt={DeletedAt} | DeletedBy={DeletedBy} | IsActive={IsActive} | UpdatedAt={UpdatedAt}",
                                sameCtxBuilding.Id,
                                sameCtxBuilding.DeletedAt?.ToString("o") ?? "(null)",
                                sameCtxBuilding.DeletedBy?.ToString() ?? "(null)",
                                sameCtxBuilding.IsActive,
                                sameCtxBuilding.UpdatedAt.ToString("o"));
                        }
                    }
                }

                // 6. CommitTransactionAsync
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "[DIAG:TransactionBehavior] Transaction committed. TransactionId={TransactionId}",
                    transaction.TransactionId);

                // -----------------------------------------------------------------------
                // DIAGNOSTIC STEP 2: Re-query using a BRAND-NEW DbContext (fresh DI scope).
                // Completely independent: fresh connection, fresh change tracker, no shared state.
                // Runs AFTER CommitAsync — if the commit reached PostgreSQL, this MUST see the
                // updated row. If DeletedAt is still (null) here, the commit never persisted.
                // The fresh scope also gets a fresh TenantSessionInterceptor — its query
                // executes WITHOUT app.current_company_id set (no active transaction), so RLS
                // will either reject it or read row without SET LOCAL in scope.
                // We use IgnoreQueryFilters() to bypass the soft-delete EF filter.
                // -----------------------------------------------------------------------
                if (buildingIds.Any())
                {
                    _logger.LogInformation(
                        "[DIAG:TransactionBehavior] === POST-COMMIT VERIFICATION (fresh DbContext, OUTSIDE transaction) ===");

                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var freshContext = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();

                    foreach (var buildingId in buildingIds)
                    {
                        var freshBuilding = await freshContext.Buildings
                            .AsNoTracking()
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(b => b.Id == buildingId, cancellationToken);

                        if (freshBuilding is null)
                        {
                            _logger.LogWarning(
                                "[DIAG:TransactionBehavior] FRESH-CTX: Building Id={BuildingId} NOT FOUND with IgnoreQueryFilters. Row absent in PostgreSQL.",
                                buildingId);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "[DIAG:TransactionBehavior] FRESH-CTX: Id={Id} | DeletedAt={DeletedAt} | DeletedBy={DeletedBy} | IsActive={IsActive} | UpdatedAt={UpdatedAt}",
                                freshBuilding.Id,
                                freshBuilding.DeletedAt?.ToString("o") ?? "(null)",
                                freshBuilding.DeletedBy?.ToString() ?? "(null)",
                                freshBuilding.IsActive,
                                freshBuilding.UpdatedAt.ToString("o"));
                        }
                    }
                }

                return response;
            }
            catch (Exception ex) when (ex is not DbUpdateConcurrencyException)
            {
                _logger.LogError(
                    ex,
                    "[DIAG:TransactionBehavior] Exception caught — rolling back transaction. " +
                    "TransactionId={TransactionId} ExceptionType={ExceptionType} Message={Message}",
                    transaction.TransactionId, ex.GetType().Name, ex.Message);

                // 7. If any failure occurs, RollbackTransactionAsync and rethrow
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
