using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure;

/// <summary>
/// Defines the xUnit collection for integration tests that require a real PostgreSQL database.
/// All test classes that share this collection will run sequentially against the same
/// Testcontainers PostgreSQL instance, avoiding the overhead of spinning up multiple containers.
/// Each test class is responsible for calling PostgresTestFixture.ResetDatabaseAsync()
/// before each test to ensure data isolation.
/// </summary>
[CollectionDefinition("Postgres collection")]
public class PostgresCollection : ICollectionFixture<PostgresTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
