# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["src/Shortener.Host.Api/Shortener.Host.Api.csproj", "Shortener.Host.Api/"]
COPY ["src/Shortener.Application/Shortener.Application.csproj", "Shortener.Application/"]
COPY ["src/Shortener.Application.Abstractions/Shortener.Application.Abstractions.csproj", "Shortener.Application.Abstractions/"]
COPY ["src/Shortener.Domain/Shortener.Domain.csproj", "Shortener.Domain/"]
COPY ["src/Shortener.Infrastructure.Database/Shortener.Infrastructure.Database.csproj", "Shortener.Infrastructure.Database/"]
COPY ["src/Shortener.Infrastructure.Abstractions/Shortener.Infrastructure.Abstractions.csproj", "Shortener.Infrastructure.Abstractions/"]
COPY ["src/Shortener.Infrastructure.ServiceBus/Shortener.Infrastructure.ServiceBus.csproj", "Shortener.Infrastructure.ServiceBus/"]
COPY ["src/Shortener.Infrastructure.Shared/Shortener.Infrastructure.Shared.csproj", "Shortener.Infrastructure.Shared/"]
COPY ["src/Shortener.ServiceDefaults/Shortener.ServiceDefaults.csproj", "Shortener.ServiceDefaults/"]

RUN dotnet restore "Shortener.Host.Api/Shortener.Host.Api.csproj"

COPY src/Shortener.Host.Api/ Shortener.Host.Api/
COPY src/Shortener.Application/ Shortener.Application/
COPY src/Shortener.Application.Abstractions/ Shortener.Application.Abstractions/
COPY src/Shortener.Domain/ Shortener.Domain/
COPY src/Shortener.Infrastructure.Database/ Shortener.Infrastructure.Database/
COPY src/Shortener.Infrastructure.Abstractions/ Shortener.Infrastructure.Abstractions/
COPY src/Shortener.Infrastructure.ServiceBus/ Shortener.Infrastructure.ServiceBus/
COPY src/Shortener.Infrastructure.Shared/ Shortener.Infrastructure.Shared/
COPY src/Shortener.ServiceDefaults/ Shortener.ServiceDefaults/

WORKDIR /src/Shortener.Host.Api
RUN dotnet build "Shortener.Host.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Shortener.Host.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Shortener.Host.Api.dll"]
