FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Npgsql probes for GSSAPI on connect; install the runtime library to avoid a native-load error.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["src/Services/TreatmentService/TreatmentService.csproj", "src/Services/TreatmentService/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/Services/TreatmentService/TreatmentService.csproj"
COPY . .
WORKDIR "/workspace/src/Services/TreatmentService"
RUN dotnet build "./TreatmentService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TreatmentService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TreatmentService.dll"]
