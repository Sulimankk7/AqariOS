using System.Reflection;
using FluentAssertions;
using Xunit;

namespace PropertyOS.Tests.Unit.Architecture;

public class DependencyRuleTests
{
    [Fact]
    public void DomainAssembly_ShouldNotReferenceForbiddenTechnologies()
    {
        // Arrange
        var domainAssembly = Assembly.Load("PropertyOS.Domain");
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        var forbiddenKeywords = new[]
        {
            "EntityFrameworkCore",
            "Npgsql",
            "Hangfire",
            "SignalR",
            "Serilog",
            "Sentry",
            "AspNetCore"
        };

        // Assert
        foreach (var reference in referencedAssemblies)
        {
            foreach (var keyword in forbiddenKeywords)
            {
                reference.Name.Should().NotContain(keyword, 
                    $"Domain assembly should not reference technology: {keyword} (found: {reference.Name})");
            }
        }
    }

    [Fact]
    public void DomainAssembly_ShouldNotReferenceOtherProjects()
    {
        // Arrange
        var domainAssembly = Assembly.Load("PropertyOS.Domain");
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        var forbiddenProjects = new[]
        {
            "PropertyOS.Application",
            "PropertyOS.Infrastructure",
            "PropertyOS.Api"
        };

        // Assert
        foreach (var reference in referencedAssemblies)
        {
            foreach (var project in forbiddenProjects)
            {
                reference.Name.Should().NotContain(project, 
                    $"Domain assembly should not reference other projects: {project} (found: {reference.Name})");
            }
        }
    }
}
