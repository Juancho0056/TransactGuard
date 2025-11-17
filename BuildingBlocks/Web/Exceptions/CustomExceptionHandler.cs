using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Web.ProblemDetailsExt;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;

namespace BuildingBlocks.Web.Exceptions;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validationException:
                await HandleValidationExceptionAsync(httpContext, validationException).ConfigureAwait(false);
                break;
            case BadHttpRequestException badHttpRequestException:
                await HandleBadHttpRequestAsync(httpContext, badHttpRequestException).ConfigureAwait(false);
                break;
            case ArgumentException:
            case FormatException:
            case JsonException:
                await HandleInvalidPayloadAsync(httpContext).ConfigureAwait(false);
                break;
            case NotFoundException:
            case KeyNotFoundException:
                await HandleNotFoundAsync(httpContext).ConfigureAwait(false);
                break;
            case UnauthorizedAccessException:
                await HandleUnauthorizedAccessAsync(httpContext).ConfigureAwait(false);
                break;
            case ForbiddenAccessException:
                await HandleForbiddenAccessAsync(httpContext).ConfigureAwait(false);
                break;
            case DbUpdateConcurrencyException:
                await HandleConcurrencyConflictAsync(httpContext).ConfigureAwait(false);
                break;
            case InvalidOperationException:
                await HandleConflictAsync(httpContext).ConfigureAwait(false);
                break;
            case TimeoutException:
            case TaskCanceledException:
                await HandleTimeoutAsync(httpContext).ConfigureAwait(false);
                break;
            case OperationCanceledException:
                await HandleClientCancelledAsync(httpContext).ConfigureAwait(false);
                break;
            case ProduceException<string, string>:
                await HandleMessageBusUnavailableAsync(httpContext).ConfigureAwait(false);
                break;
            default:
                _logger.LogError(exception, "Unhandled exception has occurred while executing the request.");
                await HandleUnknownExceptionAsync(httpContext).ConfigureAwait(false);
                break;
        }

        return true;
    }

    private Task HandleValidationExceptionAsync(HttpContext httpContext, ValidationException exception)
    {
        var errorMessages = exception.Errors
            .SelectMany(kvp => kvp.Value ?? Array.Empty<string>())
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToArray();

        var detail = errorMessages.Length == 0
            ? "La solicitud contiene datos inválidos."
            : string.Join(" ", errorMessages);

        var problem = new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://errors.example.com/validation/invalid-data",
            Title = "Solicitud inválida",
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.AddCorrelation(httpContext);

        return WriteProblemDetailsAsync(httpContext, problem);
    }

    private Task HandleInvalidPayloadAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status400BadRequest,
            "https://errors.example.com/validation/invalid-data",
            "Invalid request",
            "The request contains invalid data."));

    private Task HandleBadHttpRequestAsync(HttpContext httpContext, BadHttpRequestException exception)
    {
        var detail = string.IsNullOrWhiteSpace(exception.Message)
            ? "La solicitud contiene datos inválidos."
            : exception.Message;

        return WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status400BadRequest,
            "https://errors.example.com/validation/invalid-data",
            "Solicitud inválida",
            detail));
    }

    private Task HandleNotFoundAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status404NotFound,
            "https://errors.example.com/transactions/not-found",
            "Resource not found",
            "The requested transaction was not found."));

    private Task HandleUnauthorizedAccessAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status401Unauthorized,
            "https://errors.example.com/auth/unauthorized",
            "Unauthorized",
            "The request could not be authenticated."));

    private Task HandleForbiddenAccessAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status403Forbidden,
            "https://errors.example.com/auth/forbidden",
            "Forbidden",
            "You do not have permission to perform this action."));

    private Task HandleConcurrencyConflictAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status409Conflict,
            "https://errors.example.com/data/concurrency",
            "Concurrency conflict",
            "The entity was modified by another process."));

    private Task HandleConflictAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status409Conflict,
            "https://errors.example.com/common/conflict",
            "Conflict",
            "The current state prevents completing the operation."));

    private Task HandleTimeoutAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status504GatewayTimeout,
            "https://errors.example.com/gateway/timeout",
            "Timeout",
            "The service took too long to respond."));

    private Task HandleClientCancelledAsync(HttpContext httpContext)
    {
        if (!httpContext.RequestAborted.IsCancellationRequested)
        {
            return HandleTimeoutAsync(httpContext);
        }

        return WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            499,
            "https://errors.example.com/common/client-cancelled",
            "Client cancelled request",
            "The connection was closed before completing the operation."));
    }

    private Task HandleMessageBusUnavailableAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status503ServiceUnavailable,
            "https://errors.example.com/integration/bus-unavailable",
            "Message bus unavailable",
            "It was not possible to publish the event. Please try again."));

    private Task HandleUnknownExceptionAsync(HttpContext httpContext)
        => WriteProblemDetailsAsync(httpContext, CreateProblemDetails(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "https://errors.example.com/common/internal",
            "Internal server error",
            "An unexpected error occurred while processing the request."));

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string type,
        string title,
        string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.AddCorrelation(httpContext);

        return problem;
    }

    private static readonly JsonSerializerOptions ProblemDetailsSerializerOptions = new(JsonSerializerDefaults.Web);

    private static Task WriteProblemDetailsAsync(HttpContext httpContext, ProblemDetails problem)
    {
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return httpContext.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            ProblemDetailsSerializerOptions,
            "application/problem+json");
    }
}
