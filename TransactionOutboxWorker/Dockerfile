FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

COPY ["TransactionOutboxWorker/src/TransactionOutboxWorker/TransactionOutboxWorker.csproj", "TransactionOutboxWorker/src/TransactionOutboxWorker/"]
COPY ["TransactionService/src/Application/Application.csproj", "TransactionService/src/Application/"]
COPY ["TransactionService/src/Domain/Domain.csproj", "TransactionService/src/Domain/"]
COPY ["TransactionService/src/Infrastructure/Infrastructure.csproj", "TransactionService/src/Infrastructure/"]
COPY ["BuildingBlocks/Application/Application.csproj", "BuildingBlocks/Application/"]
COPY ["BuildingBlocks/Domain/Domain.csproj", "BuildingBlocks/Domain/"]
COPY ["BuildingBlocks/Messaging/Messaging.csproj", "BuildingBlocks/Messaging/"]
COPY ["BuildingBlocks/Web/Web.csproj", "BuildingBlocks/Web/"]

RUN dotnet restore "TransactionOutboxWorker/src/TransactionOutboxWorker/TransactionOutboxWorker.csproj"

COPY . .
WORKDIR /src/TransactionOutboxWorker/src/TransactionOutboxWorker
RUN dotnet publish "TransactionOutboxWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionOutboxWorker.dll"]
