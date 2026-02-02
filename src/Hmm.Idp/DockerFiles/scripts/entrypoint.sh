#!/bin/bash
set -e

echo "========================================"
echo "Hmm.Idp Docker Entrypoint"
echo "========================================"

# Configuration
DB_HOST="${DB_HOST:-sqlserver}"
DB_PORT="${DB_PORT:-1433}"
MAX_RETRIES="${MAX_RETRIES:-60}"
RETRY_INTERVAL="${RETRY_INTERVAL:-2}"

# Wait for SQL Server to be ready
echo "Waiting for SQL Server at ${DB_HOST}:${DB_PORT}..."
for i in $(seq 1 $MAX_RETRIES); do
    echo "Connection attempt $i/${MAX_RETRIES}..."
    if nc -z $DB_HOST $DB_PORT 2>/dev/null; then
        echo "SQL Server is up and accepting connections!"
        # Give it a few more seconds to fully initialize
        sleep 5
        break
    fi

    if [ $i -eq $MAX_RETRIES ]; then
        echo "WARNING: Timed out waiting for SQL Server after $MAX_RETRIES attempts."
        echo "Continuing anyway - the application will retry database connection..."
    fi

    sleep $RETRY_INTERVAL
done

# Run database initialization script if sqlcmd is available
if command -v sqlcmd &> /dev/null; then
    echo "Running database initialization script..."
    if [ -f "/app/scripts/init-db.sql" ]; then
        sqlcmd -S $DB_HOST,$DB_PORT -U sa -P "${SA_PASSWORD:-Password1!}" -i /app/scripts/init-db.sql || true
    fi
fi

# Set environment variable to trigger seeding
export SEED_DATA=true

echo "========================================"
echo "Starting Hmm.Idp Application"
echo "Environment: ${ASPNETCORE_ENVIRONMENT:-Production}"
echo "========================================"

# Start the application
exec dotnet Hmm.Idp.dll
