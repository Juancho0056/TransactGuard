using BuildingBlocks.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TransactionService.Application.Transactions.Commands.CreateTransaction;
using TransactionService.Application.Transactions.Commands.UpdateTransactionStatus;
using TransactionService.Application.Transactions.Models;
using TransactionService.Application.Transactions.Queries.GetAccountDailyTotal;
using TransactionService.Application.Transactions.Queries.GetTransactionStatus;

namespace TransactionService.Web.Endpoints;

public static class TransactionsEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/transactions");

        group.MapPost("/", CreateTransaction)
            .WithSummary("Creates a new transaction")
            .Produces<TransactionDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{transactionExternalId:guid}", GetTransactionStatus)
            .WithSummary("Gets the current status for a transaction")
            .Produces<TransactionStatusDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{transactionExternalId:guid}/status", UpdateTransactionStatus)
            .WithSummary("Updates the transaction status after the anti-fraud evaluation")
            .Produces<TransactionStatusDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/accounts/{sourceAccountId:guid}/daily-total", GetAccountDailyTotal)
            .WithSummary("Gets the approved amount for an account on a given date")
            .Produces<AccountDailyTotalDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransactionCommand(
            request.SourceAccountId,
            request.TargetAccountId,
            request.TransferTypeId,
            request.Value,
            request.Description);

        var transaction = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        return Results.Created($"/api/Transactions/{transaction.TransactionExternalId}", transaction);
    }

    private static async Task<IResult> GetTransactionStatus(
        Guid transactionExternalId,
        [FromQuery] DateOnly createdAt,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetTransactionStatusQuery(transactionExternalId, createdAt);
        var status = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(status);
    }

    private static async Task<IResult> UpdateTransactionStatus(
        Guid transactionExternalId,
        [FromBody] UpdateTransactionStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTransactionStatusCommand(
            transactionExternalId,
            request.Status,
            request.Reason);

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAccountDailyTotal(
        Guid sourceAccountId,
        [FromQuery] DateOnly date,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetAccountDailyTotalQuery(sourceAccountId, date);
        var total = await sender.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(total);
    }
}

public sealed record CreateTransactionRequest(
    Guid SourceAccountId,
    Guid TargetAccountId,
    int TransferTypeId,
    decimal Value,
    string? Description);

public sealed record UpdateTransactionStatusRequest(TransactionStatus Status, string? Reason);
