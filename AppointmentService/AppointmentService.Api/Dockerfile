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
WORKDIR /src

# Copy only the project files first so the restore layer is cached until they change.
COPY ["AppointmentService/AppointmentService.Domain/AppointmentService.Domain.csproj", "AppointmentService/AppointmentService.Domain/"]
COPY ["AppointmentService/AppointmentService.Application/AppointmentService.Application.csproj", "AppointmentService/AppointmentService.Application/"]
COPY ["AppointmentService/AppointmentService.Infrastructure/AppointmentService.Infrastructure.csproj", "AppointmentService/AppointmentService.Infrastructure/"]
COPY ["AppointmentService/AppointmentService.Api/AppointmentService.Api.csproj", "AppointmentService/AppointmentService.Api/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "AppointmentService/AppointmentService.Api/AppointmentService.Api.csproj"

COPY . .
WORKDIR "/src/AppointmentService/AppointmentService.Api"
RUN dotnet publish "./AppointmentService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AppointmentService.Api.dll"]
