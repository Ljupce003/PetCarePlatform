# Build context is the repository root, so the other layers can be restored too.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Npgsql probes for GSSAPI on connect; without this library it logs a load error on every start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project files first so the restore layer is cached until they change.
COPY ["PetService/PetService.Domain/PetService.Domain.csproj", "PetService/PetService.Domain/"]
COPY ["PetService/PetService.Application/PetService.Application.csproj", "PetService/PetService.Application/"]
COPY ["PetService/PetService.Infrastructure/PetService.Infrastructure.csproj", "PetService/PetService.Infrastructure/"]
COPY ["PetService/PetService.Api/PetService.Api.csproj", "PetService/PetService.Api/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "PetService/PetService.Api/PetService.Api.csproj"

COPY . .

WORKDIR "/src/PetService/PetService.Api"

RUN dotnet publish "./PetService.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PetService.Api.dll"]