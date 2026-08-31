using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.Notifications.Options;
using PropertyOS.Infrastructure;
using PropertyOS.Infrastructure.Notifications.Services;

namespace PropertyOS.Tests.Unit.Infrastructure.Notifications;

public sealed class ResendEmailSenderTests
{
    [Fact]
    public async Task SendAsync_UsesResendEmailsEndpointAndBearerAuthentication()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
        {
            capturedRequest = request;
            _ = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"email-id\"}", Encoding.UTF8, "application/json")
            };
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.resend.com/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var sender = CreateSender(httpClient);

        var accepted = await sender.SendAsync(
            "recipient@example.com",
            "Password reset",
            "<p>Reset</p>",
            "Reset");

        accepted.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(new Uri("https://api.resend.com/emails"));
        capturedRequest.Headers.Authorization?.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization?.Parameter.Should().Be("test-api-key");
    }

    [Fact]
    public async Task SendAsync_ProviderRejectionReturnsFalseWithoutExposingProviderResponse()
    {
        const string providerMessage = "You can only send testing emails to private.user@gmail.com using re_secret-value.";
        var logger = new CapturingLogger<ResendEmailSender>();
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(
                $"{{\"name\":\"validation_error\",\"message\":\"{providerMessage}\"}}",
                Encoding.UTF8,
                "application/json")
        }));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = CreateSender(httpClient, logger);

        var accepted = await sender.SendAsync(
            "recipient@example.com",
            "Password reset",
            "<p>Reset</p>");

        accepted.Should().BeFalse();
        var diagnostic = string.Join(Environment.NewLine, logger.Messages);
        diagnostic.Should().Contain("StatusCode=403");
        diagnostic.Should().Contain("ErrorType=validation_error");
        diagnostic.Should().NotContain("private.user@gmail.com");
        diagnostic.Should().NotContain("re_secret-value");
    }

    [Fact]
    public void InfrastructureRegistration_ResolvesResendEmailSenderForIEmailSender()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=registration_test;Username=test;Password=test",
                ["Resend:ApiKey"] = "test-api-key",
                ["Resend:SenderEmail"] = "onboarding@resend.dev",
                ["Resend:SenderName"] = "AqariOS"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>()
            .Should().BeOfType<ResendEmailSender>();
    }

    private static ResendEmailSender CreateSender(
        HttpClient httpClient,
        ILogger<ResendEmailSender>? logger = null) => new(
        httpClient,
        Options.Create(new ResendOptions
        {
            ApiKey = "test-api-key",
            SenderEmail = "onboarding@resend.dev",
            SenderName = "AqariOS"
        }),
        Options.Create(new FrontendOptions { BaseUrl = "https://app.example.com" }),
        logger ?? NullLogger<ResendEmailSender>.Instance);

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => _send(request, cancellationToken);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
