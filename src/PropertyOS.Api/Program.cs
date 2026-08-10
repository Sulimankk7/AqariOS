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

// Configure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins == null || origins.Length == 0)
        {
            origins = new[] { "http://localhost:5173" };
        }

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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

// Configure JWT Authentication & Authorization.
// Fail-fast policy: no insecure fallback secret. Startup refuses to run with a missing,
// too-short, or (outside Development) placeholder Jwt:Secret.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || System.Text.Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing or shorter than 32 bytes. Configure a unique secret " +
        "(e.g. via appsettings.Local.json or environment variables) before starting the API.");
}
if (!builder.Environment.IsDevelopment() &&
    jwtSecret == "placeholder-long-secret-key-32-chars-minimum")
{
    throw new InvalidOperationException(
        "Jwt:Secret is still the committed development placeholder. Set a real secret for non-Development environments.");
}
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

    // SignalR websocket/SSE clients cannot send an Authorization header; the JS client
    // passes the JWT as ?access_token=… . Honor it for hub paths only.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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

    options.AddPolicy(PropertyOS.Application.Leasing.Security.LeasingPermissions.Create, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Leasing.Security.LeasingPermissions.Create, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Leasing.Security.LeasingPermissions.Approve, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Leasing.Security.LeasingPermissions.Approve, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Financials.Security.FinancialsPermissions.PaymentsApprove, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Financials.Security.FinancialsPermissions.PaymentsApprove, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Financials.Security.FinancialsPermissions.ExpensesCreate, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Financials.Security.FinancialsPermissions.ExpensesCreate, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Financials.Security.FinancialsPermissions.ExpensesApprove, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Financials.Security.FinancialsPermissions.ExpensesApprove, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Financials.Security.FinancialsPermissions.ReceiptsIssue, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Financials.Security.FinancialsPermissions.ReceiptsIssue, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.MaintenanceCreate, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.MaintenanceCreate, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.MaintenanceUpdateStatus, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.MaintenanceUpdateStatus, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.MaintenanceComment, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.MaintenanceComment, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.TenantPortalAccess, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.TenantPortalAccess));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.DocumentsUpload, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.DocumentsUpload, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.DocumentsManageCategories, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.DocumentsManageCategories, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.NotificationsSend, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.NotificationsSend, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.NotificationsManageTemplates, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.NotificationsManageTemplates, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Common.Security.PlatformPermissions.NotificationsViewAll, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Common.Security.PlatformPermissions.NotificationsViewAll, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));

    options.AddPolicy(PropertyOS.Application.Companies.Security.CompaniesPermissions.Manage, policy =>
        policy.RequireClaim("permissions", PropertyOS.Application.Companies.Security.CompaniesPermissions.Manage, PropertyOS.Application.Properties.Security.PropertyPermissions.Manage));
});

// In-app notification transport over SignalR (implements the Application-layer pusher).
builder.Services.AddSingleton<PropertyOS.Application.Notifications.Services.IInAppNotificationPusher, PropertyOS.Api.Services.SignalRInAppNotificationPusher>();

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

        // Retry-After: prefer the limiter's own hint, fall back to the global window.
        var retryAfterSeconds = context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter)
            ? (int)retryAfter.TotalSeconds
            : globalWindowSecs;
        context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, retryAfterSeconds).ToString();

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

    // Credential-guessing defenses: dedicated, stricter fixed windows for login and OTP
    // issuance, layered ON TOP of the global limiter (both apply). Config-driven with
    // conservative defaults.
    var loginPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
    var loginWindowSecs  = builder.Configuration.GetValue<int>("RateLimiting:Login:WindowSeconds", 60);
    var loginQueueLimit  = builder.Configuration.GetValue<int>("RateLimiting:Login:QueueLimit", 0);
    options.AddPolicy("AuthLoginLimit", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? (!string.IsNullOrWhiteSpace(context.Connection.Id) ? context.Connection.Id : "unknown-client"),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = loginPermitLimit,
                QueueLimit  = loginQueueLimit,
                Window      = TimeSpan.FromSeconds(loginWindowSecs)
            }));

    var otpPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:OtpRequest:PermitLimit", 3);
    var otpWindowSecs  = builder.Configuration.GetValue<int>("RateLimiting:OtpRequest:WindowSeconds", 60);
    var otpQueueLimit  = builder.Configuration.GetValue<int>("RateLimiting:OtpRequest:QueueLimit", 0);
    options.AddPolicy("AuthOtpRequestLimit", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? (!string.IsNullOrWhiteSpace(context.Connection.Id) ? context.Connection.Id : "unknown-client"),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = otpPermitLimit,
                QueueLimit  = otpQueueLimit,
                Window      = TimeSpan.FromSeconds(otpWindowSecs)
            }));
});

// Proxy trust: X-Forwarded-For is honored ONLY from proxies inside configured
// KnownNetworks (CIDR list). Rate-limit partition keys use RemoteIpAddress, so an
// untrusted client cannot rotate its partition by forging the header.
builder.Services.Configure<ForwardedHeadersOptions>(forwardedOptions =>
{
    forwardedOptions.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

    // The defaults trust loopback proxies; our config list REPLACES them so trust is
    // exactly what deployment declares (fail-closed: empty config = trust no proxy).
    forwardedOptions.KnownProxies.Clear();
    forwardedOptions.KnownNetworks.Clear();

    var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();
    foreach (var cidr in knownNetworks)
    {
        var parts = cidr.Split('/');
        if (parts.Length == 2 &&
            System.Net.IPAddress.TryParse(parts[0], out var prefix) &&
            int.TryParse(parts[1], out var prefixLength))
        {
            forwardedOptions.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
        }
    }
});

var app = builder.Build();

// Synchronize Platform RBAC Permission Catalog & Reconcile System Role Grants
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<PropertyOS.Application.Common.Interfaces.IPermissionCatalogSeeder>();
    await seeder.SeedAndReconcileAsync();
}

// Must run before anything that reads RemoteIpAddress (rate limiter partitions).
app.UseForwardedHeaders();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                diagnosticContext.Set("UserId", userId);
            }

            var companyId = user.FindFirst("company_id")?.Value;
            if (!string.IsNullOrEmpty(companyId))
            {
                diagnosticContext.Set("CompanyId", companyId);
            }
        }
    };
});

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Schedule recurring background jobs (only when Hangfire storage is configured)
if (!string.IsNullOrEmpty(connectionString))
{
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<PropertyOS.Infrastructure.Leasing.Jobs.ExpireLeaseContractsJob>(
        "leasing-expire-lease-contracts",
        job => job.ExecuteSweepAsync(null, PropertyOS.Infrastructure.Leasing.Jobs.ExpireLeaseContractsJob.DefaultBatchSize, CancellationToken.None),
        "15 0 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman") });

    recurringJobs.AddOrUpdate<PropertyOS.Infrastructure.Financials.Jobs.GenerateScheduledInstallmentsJob>(
        "financials-generate-scheduled-installments",
        job => job.ExecuteSweepAsync(null, PropertyOS.Infrastructure.Financials.Jobs.GenerateScheduledInstallmentsJob.DefaultBatchSize, CancellationToken.None),
        "0 1 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman") });

    recurringJobs.AddOrUpdate<PropertyOS.Infrastructure.Financials.Jobs.MarkOverdueRentPaymentsJob>(
        "financials-mark-overdue-rent-payments",
        job => job.ExecuteSweepAsync(null, PropertyOS.Infrastructure.Financials.Jobs.MarkOverdueRentPaymentsJob.DefaultBatchSize, CancellationToken.None),
        "45 0 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman") });

    recurringJobs.AddOrUpdate<PropertyOS.Infrastructure.Financials.Jobs.ExpireStaleEfawateercomTransactionsJob>(
        "financials-expire-stale-efawateercom-transactions",
        job => job.ExecuteSweepAsync(null, PropertyOS.Infrastructure.Financials.Jobs.ExpireStaleEfawateercomTransactionsJob.DefaultBatchSize, CancellationToken.None),
        "0 * * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman") });

    recurringJobs.AddOrUpdate<PropertyOS.Infrastructure.Notifications.Jobs.DispatchNotificationsJob>(
        "notifications-dispatch-notifications",
        job => job.ExecuteSweepAsync(PropertyOS.Infrastructure.Notifications.Jobs.DispatchNotificationsJob.DefaultBatchSize, CancellationToken.None),
        "*/5 * * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman") });
}

app.MapHub<PropertyOS.Api.Hubs.NotificationsHub>("/hubs/notifications");

// Hangfire Dashboard deliberately unmapped (would need its own authorization filter).
// app.UseHangfireDashboard("/hangfire");

app.Run();

// Required for integration testing WebApplicationFactory reference
public partial class Program { }
