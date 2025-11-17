FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

COPY ["BuildingBlocks/Application/Application.csproj", "BuildingBlocks/Application/"]
COPY ["BuildingBlocks/Domain/Domain.csproj", "BuildingBlocks/Domain/"]
COPY ["TransactionService/src/Application/Application.csproj", "TransactionService/src/Application/"]
COPY ["TransactionService/src/Domain/Domain.csproj", "TransactionService/src/Domain/"]
COPY ["TransactionService/src/Infrastructure/Infrastructure.csproj", "TransactionService/src/Infrastructure/"]
COPY ["TransactionService/src/Web/Web.csproj", "TransactionService/src/Web/"]
RUN dotnet restore "TransactionService/src/Web/Web.csproj"

COPY BuildingBlocks/ BuildingBlocks/
COPY TransactionService/ TransactionService/
WORKDIR /src/TransactionService/src/Web
RUN dotnet publish "Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionService.Web.dll"]
