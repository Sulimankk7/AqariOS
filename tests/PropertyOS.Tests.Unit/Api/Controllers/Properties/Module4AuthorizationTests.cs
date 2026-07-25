using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Controllers;
using PropertyOS.Application.Properties.Security;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Properties;

public class Module4AuthorizationTests
{
    private static readonly Type[] Module4Controllers = new[]
    {
        typeof(BuildingsController),
        typeof(FloorsController),
        typeof(ApartmentsController),
        typeof(ParkingSpotsController)
    };

    [Theory]
    [InlineData(typeof(BuildingsController))]
    [InlineData(typeof(FloorsController))]
    [InlineData(typeof(ApartmentsController))]
    [InlineData(typeof(ParkingSpotsController))]
    public void Controller_ShouldBeDecoratedWithAuthorizeAttribute(Type controllerType)
    {
        var attribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        attribute.Should().NotBeNull($"Controller {controllerType.Name} must be decorated with [Authorize]");
    }

    [Theory]
    [InlineData(typeof(BuildingsController), "Create", PropertyPermissions.Create)]
    [InlineData(typeof(BuildingsController), "GetById", PropertyPermissions.Read)]
    [InlineData(typeof(BuildingsController), "List", PropertyPermissions.Read)]
    [InlineData(typeof(BuildingsController), "Update", PropertyPermissions.Update)]
    [InlineData(typeof(BuildingsController), "Archive", PropertyPermissions.Delete)]

    [InlineData(typeof(FloorsController), "Create", PropertyPermissions.Create)]
    [InlineData(typeof(FloorsController), "ListByBuilding", PropertyPermissions.Read)]
    [InlineData(typeof(FloorsController), "GetById", PropertyPermissions.Read)]
    [InlineData(typeof(FloorsController), "Update", PropertyPermissions.Update)]
    [InlineData(typeof(FloorsController), "Archive", PropertyPermissions.Delete)]

    [InlineData(typeof(ApartmentsController), "Create", PropertyPermissions.Create)]
    [InlineData(typeof(ApartmentsController), "List", PropertyPermissions.Read)]
    [InlineData(typeof(ApartmentsController), "GetById", PropertyPermissions.Read)]
    [InlineData(typeof(ApartmentsController), "Update", PropertyPermissions.Update)]
    [InlineData(typeof(ApartmentsController), "Archive", PropertyPermissions.Delete)]

    [InlineData(typeof(ParkingSpotsController), "Create", PropertyPermissions.Create)]
    [InlineData(typeof(ParkingSpotsController), "ListByBuilding", PropertyPermissions.Read)]
    [InlineData(typeof(ParkingSpotsController), "GetById", PropertyPermissions.Read)]
    [InlineData(typeof(ParkingSpotsController), "Update", PropertyPermissions.Update)]
    [InlineData(typeof(ParkingSpotsController), "Archive", PropertyPermissions.Delete)]
    public void Action_ShouldHaveFineGrainedAuthorizePolicy(Type controllerType, string methodName, string expectedPolicy)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull($"Action {methodName} must exist on {controllerType.Name}");

        var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttr.Should().NotBeNull($"Action {controllerType.Name}.{methodName} must have an [Authorize] attribute");
        authorizeAttr!.Policy.Should().Be(expectedPolicy, $"Action {controllerType.Name}.{methodName} must require policy '{expectedPolicy}'");
    }

    [Fact]
    public void AllPublicActions_ShouldHaveFineGrainedAuthorizePolicy()
    {
        foreach (var controllerType in Module4Controllers)
        {
            var publicActions = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() == null);

            foreach (var action in publicActions)
            {
                var authorizeAttr = action.GetCustomAttribute<AuthorizeAttribute>();
                authorizeAttr.Should().NotBeNull(
                    $"Action {controllerType.Name}.{action.Name} must have fine-grained [Authorize(Policy = ...)]");
                authorizeAttr!.Policy.Should().NotBeNullOrWhiteSpace(
                    $"Action {controllerType.Name}.{action.Name} policy must not be empty");
            }
        }
    }

    [Theory]
    [InlineData(PropertyPermissions.Read, PropertyPermissions.Read, true)]
    [InlineData(PropertyPermissions.Read, PropertyPermissions.Manage, true)]
    [InlineData(PropertyPermissions.Read, "unrelated.permission", false)]
    [InlineData(PropertyPermissions.Create, PropertyPermissions.Create, true)]
    [InlineData(PropertyPermissions.Create, PropertyPermissions.Manage, true)]
    [InlineData(PropertyPermissions.Create, PropertyPermissions.Read, false)]
    [InlineData(PropertyPermissions.Update, PropertyPermissions.Update, true)]
    [InlineData(PropertyPermissions.Update, PropertyPermissions.Manage, true)]
    [InlineData(PropertyPermissions.Update, PropertyPermissions.Read, false)]
    [InlineData(PropertyPermissions.Delete, PropertyPermissions.Delete, true)]
    [InlineData(PropertyPermissions.Delete, PropertyPermissions.Manage, true)]
    [InlineData(PropertyPermissions.Delete, PropertyPermissions.Read, false)]
    public void PropertyPolicy_ShouldEvaluatePermissionClaimsAndManageOverrideCorrectly(
        string policyName, string userPermissionClaim, bool expectedSuccess)
    {
        // Arrange
        var options = new AuthorizationOptions();
        options.AddPolicy(PropertyPermissions.Read, p => p.RequireClaim("permissions", PropertyPermissions.Read, PropertyPermissions.Manage));
        options.AddPolicy(PropertyPermissions.Create, p => p.RequireClaim("permissions", PropertyPermissions.Create, PropertyPermissions.Manage));
        options.AddPolicy(PropertyPermissions.Update, p => p.RequireClaim("permissions", PropertyPermissions.Update, PropertyPermissions.Manage));
        options.AddPolicy(PropertyPermissions.Delete, p => p.RequireClaim("permissions", PropertyPermissions.Delete, PropertyPermissions.Manage));

        var policy = options.GetPolicy(policyName);
        policy.Should().NotBeNull();

        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userPermissionClaim))
        {
            claims.Add(new Claim("permissions", userPermissionClaim));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var hasRequiredClaim = policy!.Requirements
            .OfType<Microsoft.AspNetCore.Authorization.Infrastructure.ClaimsAuthorizationRequirement>()
            .Any(req => req.AllowedValues != null && req.AllowedValues.Contains(userPermissionClaim));

        // Assert
        hasRequiredClaim.Should().Be(expectedSuccess);
    }
}
