using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace BuildingBlocks.Web.ProblemDetailsExt;

public static class ProblemDetailsExtensions
{
    private const string TraceIdKey = "traceId";
    private const string CorrelationIdKey = "correlationId";
    private const string CorrelationHeaderName = "X-Correlation-Id";

    public static void AddCorrelation(this ProblemDetails problem, HttpContext httpContext)
    {
        if (!problem.Extensions.ContainsKey(TraceIdKey))
        {
            problem.Extensions[TraceIdKey] = httpContext.TraceIdentifier;
        }

        if (httpContext.Request.Headers.TryGetValue(CorrelationHeaderName, out StringValues correlationId)
            && !StringValues.IsNullOrEmpty(correlationId)
            && !problem.Extensions.ContainsKey(CorrelationIdKey))
        {
            problem.Extensions[CorrelationIdKey] = correlationId.ToString();
        }
    }
}
