# TransactionService

API hexagonal que actúa como sistema de registro de transacciones. Sus puertos de entrada son los endpoints REST bajo `/api/transactions`; los puertos de salida incluyen PostgreSQL (transacciones + outbox) y Kafka (`transactions.created`, `transactions.status`).

## Superficie de API
| Método y ruta | Descripción | Request / Query params | Respuesta |
| --- | --- | --- | --- |
| `POST /api/transactions` | Crea una transacción `Pending`. | `CreateTransactionRequest` con `SourceAccountId`, `TargetAccountId`, `TransferTypeId`, `Value`, `Description`. | `201 Created` con `TransactionDto` (ids, valor, `Status`, `CreatedAt`). |
| `GET /api/transactions/{transactionExternalId}?createdAt=YYYY-MM-DD` | Obtiene el estado actual. El `createdAt` asegura que el request se haga contra el snapshot correcto. | Query `createdAt` obligatorio. | `TransactionStatusDto` (`TransactionExternalId`, `Status`, `Reason`, `UpdatedAt`). |
| `PUT /api/transactions/{transactionExternalId}/status` | Actualiza el estado después de antifraude. | `UpdateTransactionStatusRequest` (`Status`, `Reason`). | `200 OK` con `TransactionStatusDto`. |
| `GET /api/transactions/accounts/{sourceAccountId}/daily-total?date=YYYY-MM-DD` | Calcula el total aprobado del día para un origen. | Query `date` obligatorio. | `AccountDailyTotalDto` (cuenta, fecha, monto total). |

Consulta [`src/Web/wwwroot/api/specification.json`](src/Web/wwwroot/api/specification.json) para los esquemas OpenAPI.

## Modelo de dominio y comandos
- `CreateTransactionCommand` valida los GUID de cuentas, que `Value` sea positivo y que `TransferTypeId` corresponda a un valor del enum `TransferType`. El comando registra timestamps en zona local (`TimeZone__TimeZoneId`) y almacena una representación `TransactionDto`.
- La entidad `Transaction` nace en estado `Pending` y mantiene `OccurredOn`, `CreatedAt` y `UpdatedAt` en UTC/local.

## Estados y eventos
- Estados permitidos: `Pending`, `Approved`, `Rejected`, `AntiFraudFailed`. Las transiciones válidas están definidas en `TransactionStatePolicy.CanTransition`.
- `UpdateTransactionStatusCommand` decide qué método invocar (`Approve`, `Reject`, `MarkAsAntiFraudFailed`) y valida restricciones adicionales (razones obligatorias para rechazos, opcionales para aprobaciones, etc.).
- Cada cambio exitoso publica un `TransactionStatusIntegrationEvent` en el topic `transactions.status` mediante el outbox.

## Consultas y reportes
- `GetTransactionStatusQuery` requiere el `createdAt` para desambiguar registros y aplica reglas de validación con FluentValidation.
- `GetAccountDailyTotalQuery` suma únicamente transacciones aprobadas en la fecha local indicada; esta consulta alimenta al `AntiFraudService` para el límite diario.

## Outbox y workers
- `CreateTransactionCommand` registra un `OutboxMessage` con payload `TransactionCreatedIntegrationEvent`. El mensaje describe la transacción (`TransactionExternalId`, cuentas, tipo, valor, `CreatedAt`).
- `TransactionOutboxWorker` resuelve el topic usando `OutboxTopicResolver` y publica en `transactions.created`.
- `TransactionFraudWorker` consume ese topic, llama al `AntiFraudService` y luego vuelve a este servicio vía `PUT /status`.

## Configuración
Archivos relevantes:
- `.env` (Docker) y [`src/Web/appsettings.json`](src/Web/appsettings.json).
- Variables clave: `ConnectionStrings__TransactionServiceDb`, `Kafka__BootstrapServers`, `Kafka__ClientId`, `TimeZone__TimeZoneId`, `ASPNETCORE_URLS`.

## Ejecución local
```bash
cd TransactionService/src/Web
dotnet run --project TransactionService.Web.csproj
```
Luego prueba los endpoints con `curl` o herramientas como `httpie`. Usa el worker de outbox (`docker compose up transaction-outbox-worker`) para que las transacciones lleguen a Kafka.
