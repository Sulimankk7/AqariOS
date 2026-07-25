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

// Append appsettings.Local.json to allow local overrides while preserving framework defaults
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Configure Sentry
builder.WebHost.UseSentry();

// Add services to the container.
builder.Services.AddControllers();

// Add Global Exception Handler
builder.Services.AddExceptionHandler<PropertyOS.Api.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"] ?? "PropertyOS-Secret-Signing-Key-Minimum-32-Bytes-Length!";
var jwtIssuer = jwtSection["Issuer"] ?? "PropertyOS";
var jwtAudience = jwtSection["Audience"] ?? "PropertyOS-Clients";
var jwtSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret));

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
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = jwtSigningKey,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PropertyOS.Application.Properties.Security.PropertyPermissions.Read, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Properties.Security.PropertyPermissions.Read, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Properties.Security.PropertyPermissions.Create, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Properties.Security.PropertyPermissions.Create, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Properties.Security.PropertyPermissions.Update, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Properties.Security.PropertyPermissions.Update, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Properties.Security.PropertyPermissions.Delete, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Properties.Security.PropertyPermissions.Delete, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));
});

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
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddHangfireServer(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromMilliseconds(500);
        });
    }
}

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString ?? string.Empty);

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global fixed-window rate limiter — config-driven with safe defaults.
    var globalPermitLimit  = builder.Configuration.GetValue<int>("RateLimiting:Global:PermitLimit",  100);
    var globalWindowSecs   = builder.Configuration.GetValue<int>("RateLimiting:Global:WindowSeconds", 60);
    var globalQueueLimit   = builder.Configuration.GetValue<int>("RateLimiting:Global:QueueLimit",     0);

    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? (!string.IsNullOrWhiteSpace(context.Connection.Id) ? context.Connection.Id : "unknown-client"),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit       = globalPermitLimit,
                QueueLimit        = globalQueueLimit,
                Window            = TimeSpan.FromSeconds(globalWindowSecs)
            }));

    // Return ProblemDetails JSON on 429 so clients and tests can parse the body.
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status   = StatusCodes.Status429TooManyRequests,
            Title    = "Rate limit exceeded",
            Detail   = "Too many requests. Please slow down.",
            Extensions = { ["errorCode"] = "RATE_LIMIT_EXCEEDED" }
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
    };

    // Named per-route policy (Marketplace viewing limiter) — preserved unchanged.
    options.AddPolicy("ViewingRequestLimit", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Connection.Id,
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit  = 0,
                Window      = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Synchronize Platform RBAC Permission Catalog & Reconcile System Role Grants
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<PropertyOS.Application.Common.Interfaces.IPermissionCatalogSeeder>();
    await seeder.SeedAndReconcileAsync();
}

app.UseExceptionHandler();

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
