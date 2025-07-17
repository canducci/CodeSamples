# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
RUN apt-get update && apt-get install curl -y
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenTelemetry/MyWebApi/MyWebApi.csproj", "OpenTelemetry/MyWebApi/"]
RUN dotnet restore "./OpenTelemetry/MyWebApi/MyWebApi.csproj"
COPY . .
WORKDIR "/src/OpenTelemetry/MyWebApi"
RUN dotnet build "./MyWebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MyWebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK CMD curl --fail http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "MyWebApi.dll"]