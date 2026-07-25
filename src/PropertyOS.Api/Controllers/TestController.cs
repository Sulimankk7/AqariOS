using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// Internal test controller for integration test mutation verification.
/// </summary>
[ApiController]
[Route("api/test")]
[AllowAnonymous]
public class TestController : ControllerBase
{
    private readonly PropertyOsDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of TestController.
    /// </summary>
    public TestController(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Mutates entity state to trigger AuditLog creation for request tracking verification.
    /// </summary>
    [HttpPost("mutate")]
    public async Task<IActionResult> Mutate()
    {
        var user = new User
        {
            Email = $"audit_test_{Guid.NewGuid():N}@test.com",
            FullName = "Audit Test User",
            PasswordHash = "argon2id.test",
            PasswordAlgorithm = "argon2id",
            IsActive = true
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(user.Id.ToString());
        });
    }
}
