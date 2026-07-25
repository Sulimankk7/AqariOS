using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using PropertyOS.Application.Identity.Provisioning;
using PropertyOS.Application.Identity.Provisioning.Steps;

namespace PropertyOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PropertyOS.Application.Common.Behaviors.ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        // Tenant Provisioning Pipeline & Steps
        services.AddScoped<ITenantProvisioningStep, CreateUserStep>();
        services.AddScoped<ITenantProvisioningStep, CreateCompanyStep>();
        services.AddScoped<ITenantProvisioningStep, SaveInitialTenantEntitiesStep>();
        services.AddScoped<ITenantProvisioningStep, CreateCompanySettingsStep>();
        services.AddScoped<ITenantProvisioningStep, CreateCompanyAdminRoleStep>();
        services.AddScoped<ITenantProvisioningStep, CreateUserCompanyMembershipStep>();
        services.AddScoped<ITenantProvisioningStep, SaveProvisionedTenantEntitiesStep>();
        services.AddScoped<ITenantProvisioningStep, GenerateAuthSessionStep>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        services.AddScoped<PropertyOS.Application.Files.Services.IFileValidationService, PropertyOS.Application.Files.Services.FileValidationService>();
        services.AddScoped<PropertyOS.Application.Documents.Services.IDocumentCategorySeeder, PropertyOS.Application.Documents.Services.DocumentCategorySeeder>();

        return services;
    }
}
