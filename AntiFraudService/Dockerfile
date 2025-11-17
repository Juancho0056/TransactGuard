
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

COPY ["BuildingBlocks/Application/Application.csproj", "BuildingBlocks/Application/"]
COPY ["BuildingBlocks/Domain/Domain.csproj", "BuildingBlocks/Domain/"]
COPY ["AntiFraudService/src/Application/Application.csproj", "AntiFraudService/src/Application/"]
COPY ["AntiFraudService/src/Domain/Domain.csproj", "AntiFraudService/src/Domain/"]
COPY ["AntiFraudService/src/Infrastructure/Infrastructure.csproj", "AntiFraudService/src/Infrastructure/"]
COPY ["AntiFraudService/src/Web/Web.csproj", "AntiFraudService/src/Web/"]
RUN dotnet restore "AntiFraudService/src/Web/Web.csproj"

COPY BuildingBlocks/ BuildingBlocks/
COPY AntiFraudService/ AntiFraudService/
WORKDIR /src/AntiFraudService/src/Web
RUN dotnet publish "Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AntiFraudService.Web.dll"]
