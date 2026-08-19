# Build context is the repository root, so the other layers can be restored too.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Npgsql probes for GSSAPI on connect; without this library it logs a load error on every start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace

COPY ["src/Services/AppointmentService/AppointmentService.csproj", "src/Services/AppointmentService/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/Services/AppointmentService/AppointmentService.csproj"

COPY . .
WORKDIR "/workspace/src/Services/AppointmentService"
RUN dotnet publish "./AppointmentService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AppointmentService.dll"]
