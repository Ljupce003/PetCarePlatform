FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AppointmentService/PetCarePlatform.AppointmentService.Api/PetCarePlatform.AppointmentService.Api.csproj", "AppointmentService/PetCarePlatform.AppointmentService.Api/"]
COPY ["AppointmentService/PetCarePlatform.AppointmentService.Application/PetCarePlatform.AppointmentService.Application.csproj", "AppointmentService/PetCarePlatform.AppointmentService.Application/"]
COPY ["AppointmentService/PetCarePlatform.AppointmentService.Infrastructure/PetCarePlatform.AppointmentService.Infrastructure.csproj", "AppointmentService/PetCarePlatform.AppointmentService.Infrastructure/"]
COPY ["AppointmentService/PetCarePlatform.AppointmentService.Domain/PetCarePlatform.AppointmentService.Domain.csproj", "AppointmentService/PetCarePlatform.AppointmentService.Domain/"]
RUN dotnet restore "AppointmentService/PetCarePlatform.AppointmentService.Api/PetCarePlatform.AppointmentService.Api.csproj"
COPY . .
WORKDIR "/src/AppointmentService/PetCarePlatform.AppointmentService.Api"
RUN dotnet build "./PetCarePlatform.AppointmentService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PetCarePlatform.AppointmentService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PetCarePlatform.AppointmentService.Api.dll"]
