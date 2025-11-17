# TransactionOutboxWorker

Worker responsable de drenar la tabla de outbox del TransactionService y publicar los eventos en Kafka respetando la arquitectura hexagonal (el dominio solo escribe outbox, este worker se encarga de la integración).

## Propósito y dependencias
- Reutiliza el mismo `ApplicationDbContext` del TransactionService mediante `AddAuditableDbContext<ApplicationDbContext, IApplicationDbContext>`.
- Usa `AddOutboxBackgroundService` del paquete `BuildingBlocks.Messaging.Outbox` para leer mensajes pendientes en lotes.
- Publica a Kafka a través del `IKafkaProducer` configurado con `Kafka__BootstrapServers` y `Kafka__ClientId`.

## Resolución de topics
`OutboxTopicResolver` mapea cada `OutboxMessage` según su tipo:
- `TransactionCreatedIntegrationEvent` → `TransactionTopics.TransactionsCreated` (`transactions.created`).

Para soportar nuevos eventos basta con añadir más casos al resolver y asegurarse de que los comandos correspondientes registran el mensaje en el outbox.

## Configuración operativa
Archivo [`src/TransactionOutboxWorker/appsettings.json`](src/TransactionOutboxWorker/appsettings.json):
- `ConnectionStrings.TransactionServiceDb` – cadena de conexión a la base que contiene `OutboxMessages`.
- `Kafka` – `BootstrapServers` y `ClientId` usados al publicar.
- `TimeZone.TimeZoneId` – se comparte con el servicio para calcular timestamps auditable.
- `Processing` – `BatchSize` y `PollingIntervalSeconds` controlan cuánto lee cada iteración del background service.

Ejecuta el worker con:
```bash
cd TransactionOutboxWorker/src/TransactionOutboxWorker
dotnet run
```
O levántalo junto al resto de servicios con `docker compose up transaction-outbox-worker`.
