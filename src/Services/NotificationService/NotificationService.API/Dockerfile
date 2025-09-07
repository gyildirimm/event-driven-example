FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/NotificationService/NotificationService.API/NotificationService.API.csproj", "src/Services/NotificationService/NotificationService.API/"]
COPY ["src/Services/NotificationService/NotificationService.Application/NotificationService.Application.csproj", "src/Services/NotificationService/NotificationService.Application/"]
COPY ["src/Services/NotificationService/NotificationService.Domain/NotificationService.Domain.csproj", "src/Services/NotificationService/NotificationService.Domain/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Services/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "src/Services/NotificationService/NotificationService.Infrastructure/"]
COPY ["src/Services/NotificationService/NotificationService.Persistence/NotificationService.Persistence.csproj", "src/Services/NotificationService/NotificationService.Persistence/"]
RUN dotnet restore "src/Services/NotificationService/NotificationService.API/NotificationService.API.csproj"
COPY . .
WORKDIR "/src/src/Services/NotificationService/NotificationService.API"
RUN dotnet build "./NotificationService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NotificationService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]
