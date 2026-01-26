#!/bin/bash

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &
pid=$!

# Wait for SQL Server to start
echo "Waiting for SQL Server to be ready..."
for i in {1..60};
do
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1
    if [ $? -eq 0 ]
    then
        echo "SQL Server is ready."
        break
    else
        echo "Not ready yet..."
        sleep 1
    fi
done

# Run the setup script
echo "Running setup script..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /setup.sql

# Wait for the SQL Server process to finish (which should be never, unless stopped)
wait $pid
