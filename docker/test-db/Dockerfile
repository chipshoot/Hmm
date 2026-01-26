FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# Install dos2unix to fix potential line ending issues with scripts from Windows
RUN apt-get update && apt-get install -y dos2unix && rm -rf /var/lib/apt/lists/*

# Copy artifacts
# Note: These paths assume you run the build command from the repository root
COPY docker/sqlserver/init-db.sql /init-db.sql
COPY docker/test-db/seed-data.sql /seed-data.sql
COPY docker/test-db/setup.sql /setup.sql
COPY docker/test-db/entrypoint.sh /entrypoint.sh

# Fix permissions and line endings
RUN dos2unix /entrypoint.sh && \
    chmod +x /entrypoint.sh

USER mssql

ENTRYPOINT ["/entrypoint.sh"]
