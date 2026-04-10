#!/bin/bash
set -e

echo "========================================"
echo "Hmm.Idp Docker Entrypoint"
echo "========================================"

# Initialize and start PostgreSQL within the same container
PGDATA="${PGDATA:-/var/lib/postgresql/data}"
PG_USER="${PG_USER:-postgres}"
PG_DB="${PG_DB:-HmmIdp}"
PG_PASSWORD="${PG_PASSWORD:-IdpDockerPassword1!}"

# Detect PostgreSQL binary directory (Debian installs to /usr/lib/postgresql/<version>/bin)
PG_BIN_DIR=""
if [ -d /usr/lib/postgresql ]; then
    PG_VERSION_DIR=$(ls /usr/lib/postgresql 2>/dev/null | sort -V | tail -n 1)
    if [ -n "$PG_VERSION_DIR" ] && [ -d "/usr/lib/postgresql/$PG_VERSION_DIR/bin" ]; then
        PG_BIN_DIR="/usr/lib/postgresql/$PG_VERSION_DIR/bin"
    fi
fi

if [ -z "$PG_BIN_DIR" ]; then
    echo "ERROR: Could not find PostgreSQL binaries under /usr/lib/postgresql/*/bin"
    exit 1
fi

echo "Using PostgreSQL binaries at: $PG_BIN_DIR"
export PATH="$PG_BIN_DIR:$PATH"

# Initialize database cluster if not already done
if [ ! -f "$PGDATA/PG_VERSION" ]; then
    echo "Initializing PostgreSQL data directory..."
    chown -R postgres:postgres "$PGDATA"
    su postgres -c "$PG_BIN_DIR/initdb -D $PGDATA"

    # Configure PostgreSQL to accept local connections with password
    echo "host all all 127.0.0.1/32 md5" >> "$PGDATA/pg_hba.conf"
    echo "local all all trust" >> "$PGDATA/pg_hba.conf"

    # Listen only on localhost
    echo "listen_addresses = '127.0.0.1'" >> "$PGDATA/postgresql.conf"
fi

# Ensure data directory ownership (in case of volume mount)
chown -R postgres:postgres "$PGDATA"

# Start PostgreSQL
echo "Starting PostgreSQL..."
su postgres -c "$PG_BIN_DIR/pg_ctl -D $PGDATA -l /var/lib/postgresql/pg.log start"

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
for i in $(seq 1 30); do
    if su postgres -c "$PG_BIN_DIR/pg_isready -h 127.0.0.1" > /dev/null 2>&1; then
        echo "PostgreSQL is ready!"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "WARNING: PostgreSQL did not become ready in time."
    fi
    sleep 1
done

# Create database and set password if needed
DB_EXISTS=$(su postgres -c "$PG_BIN_DIR/psql -h 127.0.0.1 -tAc \"SELECT 1 FROM pg_database WHERE datname = '$PG_DB'\"" 2>/dev/null || true)
if [ "$DB_EXISTS" != "1" ]; then
    echo "Creating database '$PG_DB'..."
    su postgres -c "$PG_BIN_DIR/psql -h 127.0.0.1 -c \"CREATE DATABASE \\\"$PG_DB\\\";\""
fi
su postgres -c "$PG_BIN_DIR/psql -h 127.0.0.1 -c \"ALTER USER postgres PASSWORD '$PG_PASSWORD';\""

echo "PostgreSQL is running with database '$PG_DB'"

# Set environment variable to trigger seeding
export SEED_DATA=true

echo "========================================"
echo "Starting Hmm.Idp Application"
echo "Environment: ${ASPNETCORE_ENVIRONMENT:-Production}"
echo "========================================"

# Start the application
exec dotnet Hmm.Idp.dll
