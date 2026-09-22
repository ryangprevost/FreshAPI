# Multi-stage build: SDK for build/publish, ASP.NET runtime for the final image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first for better layer caching.
COPY ["src/Api/FreshApi.Api.csproj", "src/Api/"]
COPY ["src/Application/FreshApi.Application.csproj", "src/Application/"]
COPY ["src/Domain/FreshApi.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/FreshApi.Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Api/FreshApi.Api.csproj"

COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "FreshApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FreshApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FreshApi.Api.dll"]
