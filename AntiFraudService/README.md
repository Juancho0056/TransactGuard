# AntiFraudService

Servicio minimal API construido con arquitectura hexagonal. El puerto de entrada es `POST /api/evaluations` (ver [`src/Web/Endpoints/Evaluations.cs`](src/Web/Endpoints/Evaluations.cs)); los puertos de salida son Kafka y los adaptadores de datos que almacenan las evaluaciones y consultan el total diario de cada cuenta.

## Flujo interno del endpoint `/api/evaluations`
1. El request contiene `TransactionExternalId`, cuentas origen/destino, tipo de transferencia y monto.
2. El `EvaluateTransactionCommandHandler` busca una evaluación previa en `ITransactionEvaluationsRepository`. Si existe, devuelve el dictamen cacheado y vuelve a publicar el evento en Kafka (`transactions.status`).
3. Si no existe evaluación previa, el handler compone el contexto (`EvaluationContext`) con el monto (`Money`), el `AccountId` y la hora local calculada a partir de `TimeZone.TimeZoneId`.
4. Se evalúa una política compuesta (`CompositeAntiFraudPolicy`) con:
   - `SingleTransactionLimitPolicy` (límite por transacción).
   - `DailyAmountLimitPolicy`, que consulta el total aprobado del día a través del puerto `ITransactionsReadPort` (implementado contra `TransactionService`).
5. Si la decisión es aprobatoria, se registra en `IApprovedTransactionsLedger` para mantener el ledger diario.
6. Se persiste la entidad `TransactionEvaluation` mediante EF Core y se emite el evento `TransactionEvaluationIntegrationEvent` en el topic `transactions.status`.

## Políticas y reglas de negocio
| Política | Límite por defecto | Descripción |
| --- | --- | --- |
| `SingleTransactionLimitPolicy` | USD 2,000 (`DefaultLimitAmount`) | Rechaza montos individuales que superen el límite configurado. |
| `DailyAmountLimitPolicy` | USD 20,000 acumulados | Consulta el puerto `TransactionsReadPort` para sumar las transacciones aprobadas del día y rechaza si se excede el máximo. |

Las políticas se instancian en el handler y aceptan parámetros para ajustar los límites sin tocar el dominio.

## Persistencia e integración
- **Evaluaciones y ledger:** `Infrastructure/Data` define `ApplicationDbContext`, las migraciones y el repositorio `TransactionEvaluationsRepository`. El ledger de aprobadas se escribe a través de `IApprovedTransactionsLedger`.
- **Lectura del TransactionService:** el puerto `ITransactionsReadPort` abstrae las consultas de totales diarios. Su implementación realiza llamadas HTTP al `TransactionService` para obtener `GetAccountDailyTotal`.

## Configuración operacional
Variables clave (ver `.env` y [`src/Web/appsettings.json`](src/Web/appsettings.json)):
- `ConnectionStrings__AntiFraudServiceDb` – cadena de conexión PostgreSQL.
- `Kafka__BootstrapServers` / `Kafka__ClientId` – broker y client id para publicar evaluaciones.
- `TimeZone__TimeZoneId` – zona horaria utilizada para agrupar las transacciones diarias.
- `ASPNETCORE_URLS` y `ASPNETCORE_ENVIRONMENT` – puerto HTTP y entorno.

## Eventos emitidos
El servicio publica en `transactions.status` (constante `AntiFraudTopics.TransactionEvaluated`). El payload `TransactionEvaluationIntegrationEvent` contiene:
- `TransactionExternalId`
- `IsApproved`
- `Reason` (códigos de razón definidos en dominio)
- `EvaluatedAt`

Este evento es consumido por workers u otros servicios interesados en cambios de estado.

## Ejecución local
```bash
cd AntiFraudService/src/Web
dotnet run --project AntiFraudService.Web.csproj
```
Define las variables de entorno anteriores o usa `AntiFraudService/.env` cuando ejecutes vía Docker.
