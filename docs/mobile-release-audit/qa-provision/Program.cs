using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Infrastructure.Persistence;

var connection = Environment.GetEnvironmentVariable("AQARIOS_QA_CONNECTION") ?? throw new Exception("QA connection required");
var cs = new NpgsqlConnectionStringBuilder(connection);
if (cs.Host is not ("localhost" or "127.0.0.1") || cs.Database != "aqarios_mobile_qa_20260909")
    throw new Exception("Refusing non-disposable database");
var options = new DbContextOptionsBuilder<PropertyOsDbContext>().UseNpgsql(connection)
    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)).Options;
await using var context = new PropertyOsDbContext(options);
await context.Database.MigrateAsync();
Console.WriteLine("Disposable QA database migrated. " + (await context.Database.GetAppliedMigrationsAsync()).Count() + " migrations.");
if (args.Contains("seed-admin")) {
    await using var conn = new NpgsqlConnection(connection);
    await conn.OpenAsync();
    var hash = new PropertyOS.Infrastructure.Identity.PasswordHasher().HashPassword("QaAudit!20260909");
    await using var cmd = new NpgsqlCommand("""
      INSERT INTO roles(id,company_id,code,name_en,name_ar,is_system,created_at,updated_at)
      SELECT '00000000-0000-4000-8000-000000009901',null,'SYSTEM_ADMIN','QA system admin','QA system admin',true,now(),now()
      WHERE NOT EXISTS (SELECT 1 FROM roles WHERE code='SYSTEM_ADMIN' AND company_id IS NULL);
      INSERT INTO users(id,email,password_hash,password_algorithm,full_name,preferred_language,is_active,created_at,updated_at)
      VALUES ('00000000-0000-4000-8000-000000009902','qa-admin@example.invalid',@hash,'argon2id','QA Platform Admin','ar',true,now(),now()) ON CONFLICT DO NOTHING;
      INSERT INTO user_system_roles(id,user_id,role_id,granted_at)
      SELECT '00000000-0000-4000-8000-000000009903','00000000-0000-4000-8000-000000009902',id,now() FROM roles WHERE code='SYSTEM_ADMIN' AND company_id IS NULL
      ON CONFLICT DO NOTHING;
      """, conn);
    cmd.Parameters.AddWithValue("hash", hash);
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("Disposable platform admin seeded; no existing users touched.");
}
