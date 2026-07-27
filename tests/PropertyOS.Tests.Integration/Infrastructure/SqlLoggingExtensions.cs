using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PropertyOS.Tests.Integration.Infrastructure;

/// <summary>
/// Opt-in EF SQL logging for test debugging: set PROPERTYOS_TEST_SQL_LOG to a file path
/// before running tests to capture every executed command. No-op otherwise.
/// </summary>
public static class SqlLoggingExtensions
{
    public static DbContextOptionsBuilder<TContext> LogSqlWhenRequested<TContext>(this DbContextOptionsBuilder<TContext> options)
        where TContext : DbContext
    {
        var sqlLogPath = Environment.GetEnvironmentVariable("PROPERTYOS_TEST_SQL_LOG");
        if (!string.IsNullOrEmpty(sqlLogPath))
        {
            options.LogTo(
                message => System.IO.File.AppendAllText(sqlLogPath, message + Environment.NewLine),
                LogLevel.Information);
            options.EnableSensitiveDataLogging();
        }

        return options;
    }
}
