using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace WebAPI.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        
        const string bearerSchemeId = "bearer"; // lowercase per RFC 7235

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CleanArchitecture API",
                Version = "v1"
            });

            // Include XML documentation comments so controller and action summaries
            // and remarks appear in the generated OpenAPI spec and Swagger UI.
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            // Enable JWT bearer token support in Swagger UI
            options.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = bearerSchemeId,
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Paste only the JWT, without the 'Bearer ' prefix."
            });

            // Swashbuckle 10 / Microsoft.OpenApi v2+ expects a delegate here
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(bearerSchemeId, document)] = []
            });
        });

        return services;
    }
}
