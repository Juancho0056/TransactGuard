using AntiFraudService.Application.Transactions.Commands.EvaluateTransaction;
using AntiFraudService.Application.Transactions.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AntiFraudService.Web.Endpoints;

public static class EvaluationsEndpoints
{
    public static void MapEvaluationsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/evaluations");

        group.MapPost("/", EvaluateTransaction)
            .WithSummary("Evaluates a transaction against anti-fraud policies")
            .Produces<EvaluationResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> EvaluateTransaction(
        [FromBody] EvaluateTransactionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new EvaluateTransactionCommand(
            request.TransactionExternalId,
            request.SourceAccountId,
            request.TargetAccountId,
            request.TransferType,
            request.Value,
            request.OccurredOn);

        var decision = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Ok(decision);
    }
}

public sealed record EvaluateTransactionRequest(
    Guid TransactionExternalId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    string TransferType,
    decimal Value,
    DateTimeOffset? OccurredOn);
