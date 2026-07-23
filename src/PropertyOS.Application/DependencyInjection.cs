using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;

namespace PropertyOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PropertyOS.Application.Common.Behaviors.ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<PropertyOS.Application.Files.Services.IFileValidationService, PropertyOS.Application.Files.Services.FileValidationService>();
        services.AddScoped<PropertyOS.Application.Documents.Services.IDocumentCategorySeeder, PropertyOS.Application.Documents.Services.DocumentCategorySeeder>();

        return services;
    }
}
