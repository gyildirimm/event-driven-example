FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/StockService/StockService.API/StockService.API.csproj", "src/Services/StockService/StockService.API/"]
COPY ["src/Services/StockService/StockService.Domain/StockService.Domain.csproj", "src/Services/StockService/StockService.Domain/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Services/StockService/StockService.Application/StockService.Application.csproj", "src/Services/StockService/StockService.Application/"]
COPY ["src/Services/StockService/StockService.Infrastructure/StockService.Infrastructure.csproj", "src/Services/StockService/StockService.Infrastructure/"]
COPY ["src/Services/StockService/StockService.Persistence/StockService.Persistence.csproj", "src/Services/StockService/StockService.Persistence/"]
RUN dotnet restore "src/Services/StockService/StockService.API/StockService.API.csproj"
COPY . .
WORKDIR "/src/src/Services/StockService/StockService.API"
RUN dotnet build "./StockService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StockService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StockService.API.dll"]
