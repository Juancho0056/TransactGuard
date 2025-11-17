# TransactionFraudWorker

Worker .NET 8 con arquitectura hexagonal orientada a eventos. Es el consumidor oficial del topic `transactions.created` y se responsabiliza por invocar al AntiFraudService y actualizar TransactionService.

## Dependencias y arranque
El `Program.cs` registra:
- `KafkaConsumerSettings` (bootstrap servers, topic, `GroupId`, creación automática de topics, particiones) para el consumidor.
- `AntiFraudApiSettings` y `TransactionServiceApiSettings` que abastecen los clientes HTTP.
- `KafkaDlqSettings` para publicar dead-letters mediante `KafkaDeadLetterQueuePublisher`.
- `KafkaConsumerProcessingSettings` (máximo de reintentos, backoff exponencial con jitter) usados por la clase base `KafkaConsumerWorker`.

## Ciclo de procesamiento
1. El `TransactionCreatedWorker` recibe un `TransactionCreatedIntegrationEvent` desde Kafka.
2. Invoca `AntiFraudClient.EvaluateTransactionAsync`, que hace `POST api/Evaluations` al AntiFraudService.
3. Según `IsApproved`, determina el estado `Approved` o `Rejected` y llama a `TransactionServiceClient.UpdateStatusAsync` (`PUT api/Transactions/{id}/status`).
4. Si ambos pasos son exitosos, confirma el offset. Si hay fallos permanentes, emite un mensaje a la DLQ y marca la transacción como `AntiFraudFailed` a través del TransactionService.
5. Los fallos transitorios disparan reintentos automáticos según `Processing.MaxRetries`, `InitialBackoffSeconds`, `MaxBackoffSeconds` y `EnableJitter`.

## Estrategias de error y DLQ
- **Excepciones permanentes** (`AntiFraudPermanentException`, `TransactionServicePermanentException`): se envía un dead-letter con metadatos `FailureType=Permanent` y el último estado conocido, y se intenta actualizar el TransactionService a `AntiFraudFailed`.
- **Excepciones transitorias** (`AntiFraudTransientException`, `TransactionServiceTransientException`, `HttpRequestException`, timeouts): se reintenta hasta agotar `MaxRetries`. Si se agota, se publica en la DLQ con `FailureType=TransientExhausted` y se marca la transacción como `AntiFraudFailed`.
- **Errores inesperados**: también terminan en la DLQ con `FailureType=Unexpected` tras agotar reintentos.
- La DLQ se publica en Kafka mediante `KafkaDeadLetterQueuePublisher` en el topic configurado (`KafkaDlq.Topic`, por defecto `transactions.created.dlq`). Cada mensaje incluye `transactionId`, payload original y trazas.

## Clientes HTTP y manejo de códigos
- `AntiFraudClient` y `TransactionServiceClient` loguean cada request, consideran `>=500` como transitorio y `>=400` como permanente, y acotan la longitud del cuerpo de error.
- Si el `HttpClient` cancela por timeout (OperationCanceled sin `cancellationToken` cancelado), se eleva como error transitorio.
- Ambos clientes se registran con `IHttpClientFactory` para reutilizar conexiones y permiten configurar `BaseAddress` mediante `appsettings` o variables (`AntiFraudApi__BaseAddress`, `TransactionServiceApi__BaseAddress`).

## Configuración
Ver [`src/TransactionFraudWorker/appsettings.json`](src/TransactionFraudWorker/appsettings.json):
- `Kafka`: `BootstrapServers`, `Topic`, `GroupId`, `EnsureTopicExists`, `TopicCreationMaxAttempts`, particiones y factor de replicación.
- `AntiFraudApi` / `TransactionServiceApi`: `BaseAddress` (URLs internas o externas).
- `Processing`: `MaxRetries`, `InitialBackoffSeconds`, `MaxBackoffSeconds`, `EnableJitter`, `JitterSeconds`.
- `KafkaDlq`: `BootstrapServers`, `Topic`, `Acks`.

Ejecuta el worker con:
```bash
cd TransactionFraudWorker/src/TransactionFraudWorker
dotnet run
```
o como contenedor vía `docker compose up transaction-fraud-worker`.
