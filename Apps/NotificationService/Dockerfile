FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Libs/src/SUI.Shared/SUI.Shared.csproj", "Libs/src/SUI.Shared/"]
COPY ["Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj", "Apps/NotificationService/src/SUI.NotificationService/"]
COPY ["Apps/NotificationService/src/SUI.NotificationService.Application/SUI.NotificationService.Application.csproj", "Apps/NotificationService/src/SUI.NotificationService.Application/"]
COPY ["Apps/NotificationService/src/SUI.NotificationService.Infrastructure/SUI.NotificationService.Infrastructure.csproj", "Apps/NotificationService/src/SUI.NotificationService.Infrastructure/"]
COPY ["Apps/NotificationService/src/SUI.NotificationService.Mesh/SUI.NotificationService.Mesh.csproj", "Apps/NotificationService/src/SUI.NotificationService.Mesh/"]
COPY ["Apps/NotificationService/src/SUI.NotificationService.Webhooks/SUI.NotificationService.Webhooks.csproj", "Apps/NotificationService/src/SUI.NotificationService.Webhooks/"]
RUN dotnet restore "Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj"

COPY Libs/src/ Libs/src/
COPY Apps/NotificationService/src/ Apps/NotificationService/src/
RUN dotnet publish "Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj" --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SUI.NotificationService.dll"]
