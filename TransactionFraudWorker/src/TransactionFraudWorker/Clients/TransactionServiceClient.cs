using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionFraudWorker.Models;

namespace TransactionFraudWorker.Clients;

public sealed class TransactionServiceClient : ITransactionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TransactionServiceClient> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionServiceClient(HttpClient httpClient, ILogger<TransactionServiceClient> logger, JsonSerializerOptions serializerOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serializerOptions = serializerOptions;
    }

    public async Task UpdateStatusAsync(Guid transactionExternalId, TransactionStatus status, string? reason, CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("The TransactionService base address is not configured.");
        }

        var endpoint = $"api/Transactions/{transactionExternalId:D}/status";
        var request = new { Status = status, Reason = reason };

        _logger.LogInformation(
            "Updating transaction {TransactionId} status to {Status} via {BaseAddress}/{Endpoint}",
            transactionExternalId,
            status,
            _httpClient.BaseAddress,
            endpoint);

        try
        {
            using var response = await _httpClient
                .PutAsJsonAsync(endpoint, request, _serializerOptions, cancellationToken)
                .ConfigureAwait(false);

            var statusCode = response.StatusCode;
            var responseBody = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "TransactionService responded with status code {StatusCode} when updating {TransactionId}",
                (int)statusCode,
                transactionExternalId);

            if ((int)statusCode >= 500)
            {
                LogErrorBody(responseBody);
                throw new TransactionServiceTransientException(
                    $"TransactionService responded with status code {(int)statusCode} ({statusCode}).");
            }

            if ((int)statusCode >= 400)
            {
                LogErrorBody(responseBody);

                var message = string.IsNullOrWhiteSpace(responseBody)
                    ? "El servicio de transacciones devolvió una respuesta inválida."
                    : responseBody.Trim();

                throw new TransactionServicePermanentException(message);
            }
        }
        catch (TransactionServiceClientException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransactionServiceTransientException("La solicitud al servicio de transacciones se canceló por timeout.");
        }
        catch (HttpRequestException ex)
        {
            throw new TransactionServiceTransientException("Error de red al llamar al servicio de transacciones.", ex);
        }
    }

    private void LogErrorBody(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("TransactionService returned an error response with an empty body.");
            return;
        }

        var summary = responseBody.Length <= 512
            ? responseBody
            : responseBody.Substring(0, 512);

        _logger.LogWarning("TransactionService error body summary: {Summary}", summary);
    }
}
