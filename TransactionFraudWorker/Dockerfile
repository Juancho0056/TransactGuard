FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

COPY ["TransactionFraudWorker/src/TransactionFraudWorker/TransactionFraudWorker.csproj", "TransactionFraudWorker/src/TransactionFraudWorker/"]
COPY ["BuildingBlocks/Application/Application.csproj", "BuildingBlocks/Application/"]
COPY ["BuildingBlocks/Domain/Domain.csproj", "BuildingBlocks/Domain/"]
COPY ["BuildingBlocks/Messaging/Messaging.csproj", "BuildingBlocks/Messaging/"]
RUN dotnet restore "TransactionFraudWorker/src/TransactionFraudWorker/TransactionFraudWorker.csproj"

COPY TransactionFraudWorker/ TransactionFraudWorker/
COPY BuildingBlocks/ BuildingBlocks/
WORKDIR /src/TransactionFraudWorker/src/TransactionFraudWorker
RUN dotnet publish "TransactionFraudWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionFraudWorker.dll"]
