using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionFraudWorker.Models;

namespace TransactionFraudWorker.Clients;

public sealed class AntiFraudClient : IAntiFraudClient
{
    private static readonly string EvaluationsEndpoint = "api/Evaluations";

    private readonly HttpClient _httpClient;
    private readonly ILogger<AntiFraudClient> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public AntiFraudClient(HttpClient httpClient, ILogger<AntiFraudClient> logger, JsonSerializerOptions serializerOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serializerOptions = serializerOptions;
    }

    public async Task<EvaluationResultDto> EvaluateTransactionAsync(TransactionCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("The AntiFraudService base address is not configured.");
        }

        var request = new
        {
            integrationEvent.TransactionExternalId,
            integrationEvent.SourceAccountId,
            integrationEvent.TargetAccountId,
            integrationEvent.TransferType,
            Value = integrationEvent.Value,
            OccurredOn = integrationEvent.CreatedAt
        };

        _logger.LogInformation(
            "Sending fraud evaluation for transaction {TransactionId} to {BaseAddress}/{Endpoint}",
            integrationEvent.TransactionExternalId,
            _httpClient.BaseAddress,
            EvaluationsEndpoint);

        try
        {
            using var response = await _httpClient
                .PostAsJsonAsync(EvaluationsEndpoint, request, _serializerOptions, cancellationToken)
                .ConfigureAwait(false);

            var statusCode = response.StatusCode;
            var responseBody = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "AntiFraudService responded with status code {StatusCode} for transaction {TransactionId}",
                (int)statusCode,
                integrationEvent.TransactionExternalId);

            if ((int)statusCode >= 500)
            {
                LogErrorBody(responseBody);
                throw new AntiFraudTransientException(
                    $"AntiFraudService responded with status code {(int)statusCode} ({statusCode}).");
            }

            if ((int)statusCode >= 400)
            {
                LogErrorBody(responseBody);

                var message = string.IsNullOrWhiteSpace(responseBody)
                    ? "respuesta inválida del servicio de antifraude"
                    : responseBody.Trim();

                throw new AntiFraudPermanentException(message);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new AntiFraudPermanentException("respuesta inválida del servicio de antifraude");
            }

            try
            {
                var result = JsonSerializer.Deserialize<EvaluationResultDto>(responseBody, _serializerOptions);
                if (result is null)
                {
                    throw new AntiFraudPermanentException("respuesta inválida del servicio de antifraude");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new AntiFraudPermanentException("respuesta inválida del servicio de antifraude", ex);
            }
        }
        catch (AntiFraudClientException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AntiFraudTransientException("La solicitud al servicio de antifraude se canceló por timeout.");
        }
        catch (HttpRequestException ex)
        {
            throw new AntiFraudTransientException("Error de red al llamar al servicio de antifraude.", ex);
        }
    }

    private void LogErrorBody(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("AntiFraudService returned an error response with an empty body.");
            return;
        }

        var summary = responseBody.Length <= 512
            ? responseBody
            : responseBody.Substring(0, 512);

        _logger.LogWarning("AntiFraudService error body summary: {Summary}", summary);
    }
}
