using System;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// Internal test controller for integration test mutation verification.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/test")]
[Route("api/test")]
[AllowAnonymous]
public class TestController : ControllerBase
{
    private readonly PropertyOsDbContext _dbContext;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of TestController.
    /// </summary>
    public TestController(PropertyOsDbContext dbContext, IHostEnvironment environment)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _environment = environment;
    }

    /// <summary>
    /// Mutates entity state to trigger AuditLog creation for request tracking verification.
    /// </summary>
    [HttpPost("mutate")]
    public async Task<IActionResult> Mutate()
    {
        // Test-harness-only endpoint (anonymous DB write): invisible outside Development.
        if (!_environment.IsDevelopment())
            return NotFound();

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
