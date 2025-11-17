# TransactGuard antifraud platform

TransactGuard es un monorepo .NET que implementa una plataforma antifraude con arquitectura hexagonal. Cada microservicio y worker expone puertos de entrada (HTTP o Kafka) y puertos de salida (bases de datos PostgreSQL, Kafka y APIs internas) mediante adaptadores aislados en las carpetas `Application`, `Domain`, `Infrastructure` y `Web`. Este documento ofrece una visión transversal; cada servicio tiene un README dedicado con más detalles.

## Ecosistema orquestado con Docker

| Componente                  | Tipo                     | Puerto expuesto | Responsabilidad                                                              |
| --------------------------- | ------------------------ | --------------- | ---------------------------------------------------------------------------- |
| `antifraud-db`              | PostgreSQL 14            | 5432            | Persistencia de evaluaciones antifraude y ledger de aprobadas.               |
| `transaction-db`            | PostgreSQL 14            | 5433            | Persistencia de transacciones y outbox.                                      |
| `kafka` / `zookeeper`       | Confluent Platform 5.5.3 | 9092 / 2181     | Transporte de eventos `transactions.created`, `transactions.status` y DLQ.   |
| `antifraud-service`         | ASP.NET Minimal APIs     | 8081            | API `/api/evaluations` que evalúa transacciones y publica su dictamen.       |
| `transaction-service`       | ASP.NET Minimal APIs     | 8082            | API `/api/transactions` para crear, consultar y actualizar transacciones.    |
| `transaction-outbox-worker` | Worker .NET              | N/A             | Despacha el outbox del TransactionService hacia Kafka.                       |
| `transaction-fraud-worker`  | Worker .NET              | N/A             | Consume `transactions.created`, invoca antifraude y actualiza transacciones. |

## Requisitos y estructura de la solución

- **SDK requerido:** .NET SDK 8.0.204 (ver [`global.json`](global.json)).
- **Solución principal:** `TransactGuard.sln` agrupa los proyectos de los servicios, workers, BuildingBlocks y pruebas.
- **Convenciones:** cada servicio sigue el mismo layout hexagonal (`src/Application`, `src/Domain`, `src/Infrastructure`, `src/Web`). Los workers residen en `src/<WorkerName>` y reutilizan los paquetes compartidos de `BuildingBlocks`.

Ejecuta compilaciones o pruebas desde la raíz del repo:

```bash
dotnet restore TransactGuard.sln
dotnet build TransactGuard.sln
dotnet test TransactGuard.sln
```

## Flujo transaccional end-to-end

1. **Creación** – `TransactionService` recibe `POST /api/transactions` y persiste la transacción en estado `Pending`. Se registra un `TransactionCreatedIntegrationEvent` en la tabla de outbox.
2. **Publicación** – `TransactionOutboxWorker` reutiliza el `ApplicationDbContext` del servicio, lee el outbox en lotes y publica en Kafka `transactions.created` (ver `TransactionTopics.TransactionsCreated`).
3. **Evaluación** – `TransactionFraudWorker` consume el topic, llama a `AntiFraudService` (`/api/evaluations`) y, según la respuesta, invoca `PUT /api/transactions/{id}/status` para aprobar, rechazar o marcar como `AntiFraudFailed`.
4. **Dictamen** – `AntiFraudService` aplica las políticas de límite por transacción y límite diario. El resultado se persiste, se guarda en el ledger de aprobadas y se publica en `transactions.status` (`AntiFraudTopics.TransactionEvaluated`).
5. **Lectura** – otros servicios pueden consultar el estado vía `GET /api/transactions/{id}?createdAt=YYYY-MM-DD` o suscribirse a `transactions.status`.

## Mapa de APIs

| Servicio             | Endpoint base       | Operaciones clave                                                                                                                                                                              |
| -------------------- | ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `TransactionService` | `/api/transactions` | Crear transacción, consultar estado (requiere `createdAt`), actualizar estado post-antifraude, obtener total diario de cuentas. Detalles en [README específico](TransactionService/README.md). |
| `AntiFraudService`   | `/api/evaluations`  | Recibe la transacción, evalúa límites, reutiliza resultados cacheados y publica el dictamen. Detalles en [su README](AntiFraudService/README.md).                                              |

Los workers no exponen HTTP pero su configuración y flujo se documentan en [TransactionFraudWorker/README.md](TransactionFraudWorker/README.md) y [TransactionOutboxWorker/README.md](TransactionOutboxWorker/README.md).

## Configuración y despliegue

- **Archivos `.env`:**
  - `.env`: valores QA para `docker compose` (puertos, nombres de BD, rutas a envs).
  - `AntiFraudService/.env`, `TransactionService/.env` y `TransactionOutboxWorker/.env`: definen cadenas de conexión, `ASPNETCORE_URLS`, `Kafka__BootstrapServers` y `Kafka__ClientId` para cada proceso.
- **`appsettings*.json`:** agrega `TimeZone.TimeZoneId`, `Kafka` y parámetros de procesamiento. Los valores se pueden sobrescribir mediante variables de entorno (`Kafka__BootstrapServers`, `Processing__BatchSize`, etc.) cuando se ejecuta fuera de Docker.
- **Despliegues locales:** levantar `docker compose up -d --build` con las variables adecuadas. Los servicios ASP.NET escuchan en `http://localhost:8081` y `http://localhost:8082`. Los workers se ejecutan como contenedores independientes pero pueden correrse también con `dotnet run` desde sus proyectos.

## Pruebas automatizadas

`TransactGuard` contiene cuatro proyectos de pruebas (dominio de antifraude, dominio de transacciones, BuildingBlocks y utilidades compartidas). Ejecuta todo el set con `dotnet test TransactGuard.sln` antes de proponer cambios para asegurar que las reglas de negocio y las políticas antifraude se mantienen.
