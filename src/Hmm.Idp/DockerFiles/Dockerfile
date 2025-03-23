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

# Copy Entity Framework Core tools
COPY --from=build /src/*.csproj ./
COPY --from=build /src/Data/Migrations ./Data/Migrations

# Install the EF Core tools and network utilities
RUN apt-get update && apt-get install -y curl netcat-openbsd

# Create entrypoint script
RUN echo '#!/bin/bash \n\
    # Wait for the database to be ready \n\
    echo "Waiting for SQL Server to be ready..." \n\
    for i in {1..120}; do \n\
    echo "Checking SQL Server connection attempt $i/120..." \n\
    if nc -z sqlserver 1433; then \n\
    echo "SQL Server is up and running!" \n\
    sleep 5 \n\
    break \n\
    fi \n\
    sleep 2 \n\
    if [ $i -eq 120 ]; then \n\
    echo "Timed out waiting for SQL Server, but continuing anyway..." \n\
    fi \n\
    done \n\
    \n\
    # Start the application \n\
    echo "Starting the application..." \n\
    exec dotnet Hmm.Idp.dll \n\
    ' > /app/entrypoint.sh

RUN chmod +x /app/entrypoint.sh

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Entry point
ENTRYPOINT ["/app/entrypoint.sh"]
