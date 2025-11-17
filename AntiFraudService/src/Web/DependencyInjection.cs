using AntiFraudService.Infrastructure.Data;
using NSwag;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.AddBuildingBlocksWebServices<ApplicationDbContext>((document, _) =>
        {
            document.Info ??= new OpenApiInfo();
            document.Info.Title = "AntiFraudService API";
        });
    }
}
