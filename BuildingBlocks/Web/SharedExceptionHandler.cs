using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BuildingBlocks.Web;

public sealed class SharedExceptionHandler : IExceptionHandler
{
    private readonly ILogger<SharedExceptionHandler> _logger;

    public SharedExceptionHandler(ILogger<SharedExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ctx.Response.HasStarted)
            return false;

        var pd = new ProblemDetails
        {
            Title = "Unhandled Exception",
            Detail = ex.Message,
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://errors.example.com/common/internal",
            Instance = ctx.Request?.Path
        };

        pd.Extensions["traceId"] = Activity.Current?.Id ?? ctx.TraceIdentifier;
        _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(pd, ct);
        return true;
    }
}