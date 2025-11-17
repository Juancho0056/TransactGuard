using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using BuildingBlocks.Application.Abstractions.Identity;
using BuildingBlocks.Web.Exceptions;
using BuildingBlocks.Web.ProblemDetailsExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSwag;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebApplicationBuilderExtensions
{

    public static IServiceCollection AddProblemDetailsWithCorrelation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.AddCorrelation(context.HttpContext);
            };
        });

        return services;
    }

    public static IHostApplicationBuilder AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var keyVaultSection = builder.Configuration.GetSection("KeyVault");
        var configuredUri = keyVaultSection["Uri"];
        var vaultName = keyVaultSection["Name"];

        if (!string.IsNullOrWhiteSpace(configuredUri)
            && Uri.TryCreate(configuredUri, UriKind.Absolute, out var parsedUri))
        {
            builder.Configuration.AddAzureKeyVault(parsedUri, new DefaultAzureCredential());
            return builder;
        }

        if (string.IsNullOrWhiteSpace(vaultName))
        {
            return builder;
        }

        var sanitizedName = vaultName.Trim();
        var uriBuilder = sanitizedName.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? new Uri(sanitizedName, UriKind.Absolute)
            : new Uri($"https://{sanitizedName}.vault.azure.net/");

        builder.Configuration.AddAzureKeyVault(uriBuilder, new DefaultAzureCredential());

        return builder;
    }

    public static IHostApplicationBuilder AddBuildingBlocksWebServices<TDbContext>(
        this IHostApplicationBuilder builder,
        Action<OpenApiDocument, IServiceProvider>? configureOpenApi = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();


        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<TDbContext>();

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetailsWithCorrelation();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument((settings, serviceProvider) =>
        {
            settings.PostProcess = document =>
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title ??= builder.Environment.ApplicationName;
                configureOpenApi?.Invoke(document, serviceProvider);
            };
        });

        return builder;
    }

    public static IApplicationBuilder UseProblemDetailsStatusCodePages(
        this IApplicationBuilder app,
        Action<HttpContext, ProblemDetails>? configureNotFound = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;

            if (response.StatusCode == StatusCodes.Status404NotFound)
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://errors.example.com/common/not-found",
                    Title = "Recurso no encontrado",
                    Detail = "No se encontró el recurso solicitado.",
                    Instance = context.HttpContext.Request.Path
                };

                problem.AddCorrelation(context.HttpContext);
                configureNotFound?.Invoke(context.HttpContext, problem);

                response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(problem)
                    .ConfigureAwait(false);
            }
        });
    }
}
