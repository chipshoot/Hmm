# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#
# Single-container deployment: PostgreSQL runs inside the same image as the
# .NET app, mirroring src/Hmm.Idp/DockerFiles/Dockerfile. The data lives in
# the named volume mounted at /var/lib/postgresql/data (see compose.api.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/Hmm.ServiceApi/Hmm.ServiceApi.csproj", "src/Hmm.ServiceApi/"]
COPY ["src/Hmm.ServiceApi.DtoEntity/Hmm.ServiceApi.DtoEntity.csproj", "src/Hmm.ServiceApi.DtoEntity/"]
COPY ["src/Hmm.Automobile/Hmm.Automobile.csproj", "src/Hmm.Automobile/"]
COPY ["src/Hmm.Core/Hmm.Core.csproj", "src/Hmm.Core/"]
COPY ["src/Hmm.Core.Map/Hmm.Core.Map.csproj", "src/Hmm.Core.Map/"]
COPY ["src/Hmm.Utility/Hmm.Utility.csproj", "src/Hmm.Utility/"]
COPY ["src/Hmm.Core.Dal.EF/Hmm.Core.Dal.EF.csproj", "src/Hmm.Core.Dal.EF/"]

RUN dotnet restore "src/Hmm.ServiceApi/Hmm.ServiceApi.csproj"

# Copy source code
COPY . .

WORKDIR "/src/src/Hmm.ServiceApi"
RUN dotnet publish "Hmm.ServiceApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image: ASP.NET Core + embedded PostgreSQL.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish ./

# Bring in the entrypoint and any future helper scripts.
COPY src/Hmm.ServiceApi/DockerFiles/scripts ./scripts

# Install PostgreSQL + curl (used by the healthcheck).
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl \
        netcat-openbsd \
        postgresql \
        postgresql-client \
    && rm -rf /var/lib/apt/lists/*

# Strip Windows line endings (in case the script was committed via a Win editor)
# and make it executable.
RUN sed -i 's/\r$//' /app/scripts/*.sh && chmod +x /app/scripts/entrypoint.sh

# Default environment.
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV PGDATA=/var/lib/postgresql/data

# Pre-create the data dir with the right ownership (it'll be the mount target).
RUN mkdir -p /var/lib/postgresql/data && chown -R postgres:postgres /var/lib/postgresql

# Healthcheck: hit Swagger or root path to confirm the app is alive.
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/swagger/v1/swagger.json || exit 1

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["/app/scripts/entrypoint.sh"]
