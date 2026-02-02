FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app ./

# Copy Entity Framework Core migrations
COPY --from=build /src/Data/Migrations ./Data/Migrations

# Copy initialization scripts
COPY DockerFiles/scripts ./scripts

# Install network utilities for health checks
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    netcat-openbsd \
    && rm -rf /var/lib/apt/lists/*

# Make entrypoint script executable
RUN chmod +x /app/scripts/entrypoint.sh

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV SEED_DATA=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/.well-known/openid-configuration || exit 1

# Expose ports
EXPOSE 80
EXPOSE 443

# Entry point
ENTRYPOINT ["/app/scripts/entrypoint.sh"]
