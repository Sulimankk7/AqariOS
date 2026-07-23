using Asp.Versioning;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using PropertyOS.Application;
using PropertyOS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Configure Sentry
builder.WebHost.UseSentry();

// Add services to the container.
builder.Services.AddControllers();

// Add Application & Infrastructure Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure JWT Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

// Configure OpenAPI & Scalar
builder.Services.AddOpenApi();

// Configure SignalR
builder.Services.AddSignalR();

// Configure Hangfire
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromMilliseconds(500);
    });
}

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString ?? string.Empty);

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ViewingRequestLimit", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Connection.Id,
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Map SignalR Hubs & Hangfire Dashboard in later phases
// app.MapHub<NotificationsHub>("/hubs/notifications");
// app.UseHangfireDashboard("/hangfire");

app.Run();

// Required for integration testing WebApplicationFactory reference
public partial class Program { }
