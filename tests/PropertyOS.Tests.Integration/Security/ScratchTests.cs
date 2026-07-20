using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using PropertyOS.Domain.Companies;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Security
{
    [Collection("Postgres collection")]
    public class ScratchTest : IAsyncLifetime, IClassFixture<PostgresTestFixture>
    {
        private readonly PostgresTestFixture _fixture;

        public ScratchTest(PostgresTestFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task PrintRoles()
        {
            var db = _fixture.Context;
            
            // Check if propertyos is a superuser or bypasses RLS
            using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'propertyos'";
            await db.Database.OpenConnectionAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Console.WriteLine("SUPERUSER: " + reader.GetBoolean(0));
                Console.WriteLine("BYPASSRLS: " + reader.GetBoolean(1));
            }
        }
    }
}
