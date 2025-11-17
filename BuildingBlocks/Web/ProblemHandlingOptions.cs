namespace BuildingBlocks.Web;

public sealed class ProblemHandlingOptions
{
    public string? DefaultTypeBaseUri { get; set; } = "https://errors.example.com";
    public string CorrelationHeaderName { get; set; } = "X-Correlation-Id";
}